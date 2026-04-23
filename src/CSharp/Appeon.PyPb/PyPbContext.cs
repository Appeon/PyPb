using Python.Deployment;
using Python.Runtime;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Appeon.PyPb;

public class PyPbContext
{
    private static readonly object _lock = new();
    public static PyPbContext? Instance { get; internal set; }
    private readonly static Dictionary<string, Tuple<string, string>> _registeredOutputCallbacks = [];
    private static bool _runtimeInitialized = false;
    private bool _usingPythonIncluded = false;
    private PyModule? InitialScope;

    public string RuntimePath { get; private set; } = "";

    internal PyPbContext(string pythonPath)
    {
        if (PythonEngine.IsInitialized)
        {
            RuntimeData.FormatterType = typeof(NoopFormatter);
            InitializeScope();
            return;
        }
        if (!_runtimeInitialized)
        {
            Runtime.PythonDLL ??= pythonPath;
            RuntimePath = Path.GetDirectoryName(pythonPath) ?? string.Empty;
            Environment.SetEnvironmentVariable("PYTHONPATH", Path.GetDirectoryName(pythonPath), EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONHOME", Path.GetDirectoryName(pythonPath), EnvironmentVariableTarget.Process);
            _runtimeInitialized = true;
        }

        if (!PythonEngine.IsInitialized)
        {
            RuntimeData.FormatterType = typeof(NoopFormatter);
            PythonEngine.Initialize();
            //PythonEngine.BeginAllowThreads();
            InitializeScope();
        }


    }

    public static bool IsInit()
    {
        return PythonEngine.IsInitialized || Instance is not null;
    }

    [MemberNotNull(nameof(InitialScope))]
    private void InitializeScope()
    {
        InitialScope = Py.CreateScope();
    }

    private int DoRegisterCallback(string key
        , string callbackObject
        , string callbackEvent
        , Dictionary<string, Tuple<string, string>> dict
        , out string? error)
    {
        error = null;
        if (callbackObject.Contains(','))
        {
            error = "Invalid character in callback object name".ToPyPbErrorDefinitionString();
            return -1;
        }

        if (callbackEvent.Contains(','))
        {
            error = "Invalid character in callback event name".ToPyPbErrorDefinitionString();
            return -1;
        }

        if (dict.ContainsKey(key))
            return 0;

        dict[key] = new(callbackObject, callbackEvent);

        return 0;
    }

    private static int DoUnresgisterCallback(string key
        , Dictionary<string, Tuple<string, string>> dict)
    {
        if (!dict.ContainsKey(key))
            return 0;

        dict.Remove(key);
        return 0;
    }

    internal PyModule CreateScope()
    {

        if (InitialScope is null)
            throw new InvalidOperationException("ContextScope could not be initialized");
        if (_registeredOutputCallbacks.Count > 0)
        {
            var newScope = new RedirectedOutputPyModule(InvokeCallbacks);
            newScope.ImportAll(InitialScope);
        }

        return InitialScope.NewScope();
    }

    internal PyModule CreateScopeFrom(PyModule other)
    {
        var scope = CreateScope();
        scope.ImportAll(other);
        return scope;
    }

    private void InvokeCallbacks(string msg)
    {
        foreach (var (key, tuple) in _registeredOutputCallbacks)
        {
            PowerBuilder.RegisteredObject.TriggerEvent(key, tuple.Item2, msg);
        }
    }

    /// <summary>
    /// Execute a command on the associated runtime's python.exe
    /// </summary>
    /// <param name="args"></param>
    /// <param name="error"></param>
    /// <returns>the status code of the executed command</returns>
    public int ExecuteInPythonExe(string args, out string? error)
    {
        error = null;
        try
        {
            var exe = Path.Combine(RuntimePath, "python.exe");
            if (!File.Exists(exe))
            {
                error = "Cannot locate python.exe".ToPyPbErrorDefinitionString(pyargs: args);
                return -1;
            }

            Process p = new();
            p.StartInfo.FileName = exe;
            p.StartInfo.Arguments = args;
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            p.WaitForExit();


            if (p.ExitCode != 0)
            {
                error = $"Process exited with non-zero status {p.ExitCode}".ToPyPbErrorDefinitionString(pyargs: args);
            }
            return p.ExitCode;
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace);
            return -1;
        }
    }

    /// <summary>
    /// Register a PowerBuilder callback event that is called when the Python Runtime writes to stdout
    /// 
    /// This will only work if <see cref="EnableStdoutRedirection"/> is called.
    /// </summary>
    /// <param name="callbackObject"></param>
    /// <param name="callbackEvent"></param>
    /// <param name="error"></param>
    /// <returns>0 if success</returns>
    public int RegisterForConsoleOutput(string key, string callbackObject, string callbackEvent, out string? error)
    {
        return DoRegisterCallback(key, callbackObject, callbackEvent, _registeredOutputCallbacks, out error);
    }


    /// <summary>
    /// Unregisters a PowerBuilder callback event 
    /// <see cref="RegisterForConsoleOutput(string, string, out string?)"/>
    /// </summary>
    /// <param name="callbackObject"></param>
    /// <param name="callbackEvent"></param>
    /// <param name="error"></param>
    /// <returns>0 if success</returns>
    public static int UnregisterFromConsoleOutput(string key)
    {
        return DoUnresgisterCallback(key, _registeredOutputCallbacks);
    }

    /// <summary>
    /// Terminates the current Python Context and releases the associated resources
    /// </summary>
    public static void Terminate()
    {
        PythonEngine.Shutdown();

        Instance = null;
    }

    /// <summary>
    /// Intialize a Python Context (only one per process).
    /// This method is thread-safe
    /// </summary>
    /// <param name="pythonPath">The path to the Python DLL </param>
    /// <param name="error">the error if one occurred</param>
    /// <returns>the <see cref="PyPbContext"/> instance, null if an error occurred</returns>
    public static PyPbContext? Init(string pythonPath, out string? error)
    {
        lock (_lock)
        {
            error = null;

            if (Instance is not null)
            {
                return Instance;
            }

            if (!Path.Exists(pythonPath))
            {
                error = "Specified path is not valid or does not exist";
                return null;
            }

            try
            {
                Instance = new PyPbContext(pythonPath);
                return Instance;
            }
            catch (Exception e)
            {
                error = ("Unable to initialize Python Context: " + e.Message);
                return null;
            }
        }
    }


    /// <summary>
    /// Intialize a Python Context (only one per process). This variation installs the Python runtime 
    /// using Python.Included
    /// This method is thread-safe
    /// </summary>
    /// <param name="dependencies">an array of dependencies to install before returning </param>
    /// <param name="error">the error if one occurred</param>
    /// <returns>the <see cref="PyPbContext"/> instance, null if an error occurred</returns>
    public static PyPbContext? Init(string[]? dependencies, out string? error)
    {
        error = null;
        lock (_lock)
        {
            if (Instance is not null)
            {
                error = "Already initialized".ToPyPbErrorDefinitionString();
                return null;
            }

            error = null;

            try
            {
                var source = new Installer.DownloadInstallationSource
                {
                    DownloadUrl = "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip"
                };
                source.DownloadUrl = (!Environment.Is64BitProcess) ? source.DownloadUrl.Replace("amd64", "win32") : source.DownloadUrl;
                Installer.Source = source;
                Installer.SetupPython().Wait();
                Installer.TryInstallPip().Wait();
                if (dependencies is not null)
                    foreach (var dep in dependencies)
                    {
                        Installer.PipInstallModule(dep);
                    }

                /// This must keep parity with Python.Included's downloaded runtime
                /// 
                var pythonPath = Path.Combine(Installer.EmbeddedPythonHome, "python311.dll");

                Runtime.PythonDLL ??= pythonPath;

                Instance = new PyPbContext(pythonPath) { _usingPythonIncluded = true };
                return Instance;
            }
            catch (Exception e)
            {
                error = ("Unable to initialize Python Context: " + e.Message);
                return null;
            }
        }
    }

    /// <summary>
    /// Install python packages using PIP. This method can only be called if this context was 
    /// initialized with Python.Included
    /// is running 
    /// </summary>
    /// <param name="dependencies">the list of dependencies to install</param>
    /// <param name="error">[OUT] error string if one occurs</param>
    /// <returns>0 on success</returns>
    public int InstallDependencies(string[] dependencies, out string? error)
    {
        error = null;
        if (!_usingPythonIncluded)
        {
            error = "cannot install dependencies. Not using Python.Included";
            return -1;
        }

        try
        {
            foreach (var dep in dependencies)
            {
                Installer.PipInstallModule(dep).Wait();
            }
        }
        catch (Exception e)
        {
            error = ("failed to install dependencies: " + e.Message);
            return -1;
        }

        return 0;
    }

    /// <summary>
    /// Load a Python module
    /// </summary>
    /// <param name="path"></param>
    /// <param name="error"></param>
    /// <returns>the resulting python module, null if an error occured</returns>
    public PyPbModule? LoadModule(string path, out string? error)
    {
        error = null;

        using var gil = Py.GIL();
        try
        {
            string directory;

            var moduleName = Path.GetFileNameWithoutExtension(path);

            if (File.Exists(path))
            {
                directory = Path.GetDirectoryName(path) ?? throw new Exception("Specified path is not supported (Directory name).");
            }
            else if (Directory.Exists(path))
            {
                string fullPath = Path.GetFullPath(path);
                var parentDirectory = Directory.GetParent(path) ?? throw new Exception("Specified path is not supported (Parent).");
                directory = parentDirectory.FullName;
            }
            else
            {
                throw new Exception("Path is invalid");
            }

            using var scope = CreateScope();

            scope.Exec("import sys");
            scope.Exec($"sys.path.append('{directory?.Replace("\\", "\\\\")}')");

            if (PyModule.Import(moduleName) is not PyModule module)
            {
                error = "Could not load module from specified path".ToPyPbErrorDefinitionString(pytarget: path);
                return null;
            }

            int res = PyObjectProxyBuilder.CreateProxyFrom<PyPbModule, PyModule>(this, module, out var dynModule, out error);
            if (res != 0)
            {
                return null;
            }

            return (PyPbModule?)dynModule;
        }
        catch (Exception e)
        {
            error = $"Error occurred trying to load module: {e.Message}".ToPyPbErrorDefinitionString(stack: e.StackTrace, pytarget: path);
            return null;
        }
    }

    /// <summary>
    /// Load a Python module from a provided string
    /// </summary>
    /// <param name="moduleName">the name to give the module</param>
    /// <param name="code">the code that defines the module</param>
    /// <param name="error">[OUT]</param>
    /// <returns>the resulting python module, null if an error occured</returns>
    public PyPbModule? LoadModuleFromString(string moduleName, string code, out string? error)
    {
        error = null;

        using var gil = Py.GIL();
        try
        {
            if (PyModule.FromString(moduleName, code) is not PyModule module)
            {
                error = "Could not load module from specified code".ToPyPbErrorDefinitionString();
                return null;
            }

            int res = PyObjectProxyBuilder.CreateProxyFrom<PyPbModule, PyModule>(this, module, out var dynModule, out error);
            if (res != 0)
            {
                return null;
            }

            return (PyPbModule?)dynModule;
        }
        catch (Exception e)
        {
            error = $"Error occurred trying to load module: {e.Message}".ToPyPbErrorDefinitionString(stack: e.StackTrace);
            return null;
        }
    }

    /// <summary>
    /// Returns a Python package
    /// </summary>
    /// <param name="packageName">the name of the package</param>
    /// <param name="error">the error if one occurs</param>
    /// <returns>the imported package</returns>
    public PyPbModule? Import(string packageName, out string? error)
    {
        error = null;

        using var _ = Py.GIL();
        try
        {
            if (PyModule.Import(packageName) is not PyModule module)
            {

                error = $"Could not load module [{packageName}]".ToPyPbErrorDefinitionString(pyargs: packageName);
                return null;
            }

            var res = PyObjectProxyBuilder.CreateProxyFrom(this, module, out PyPbModule? dynModule, out error);
            if (res != 0)
            {
                error = ("could not create proxy from package: " + error).ToPyPbErrorDefinitionString(pyargs: packageName);
                return null;
            }

            return dynModule;
        }
        catch (Exception e)
        {
            error = ($"Could not load module [{packageName}]: " + e.Message).ToPyPbErrorDefinitionString(e.StackTrace, pyargs: packageName);
            return null;
        }
    }


    /// <summary>
    /// Imports an object from a module
    /// </summary>
    /// <param name="from">name of the module</param>
    /// <param name="import">name of the object to import</param>
    /// <param name="error">[OUT] error</param>
    /// <returns>the object if found</returns>
    public PyPbObject? FromImportObject(string from, string import, out string? error)
    {
        error = null;

        using var _ = Py.GIL();

        try
        {
            if (CreateScope().Import(from) is not PyModule module)
            {
                error = $"Could not import module {from}".ToPyPbErrorDefinitionString(pyargs: $"from {from} import {import}");
                return null;
            }

            if (module.GetAttr(import) is not PyObject obj)
            {
                error = $"Could not find object [{import}] in module [{from}]".ToPyPbErrorDefinitionString(pyargs: $"from {from} import {import}");
                return null;
            }

            var res = PyObjectProxyBuilder.CreateProxyFrom(this, obj, out PyPbObject? dynModule, out error);
            if (res != 0)
            {
                error = ("Could not create proxy from object: " + error).ToPyPbErrorDefinitionString(pyargs: $"from {from} import {import}");
                return null;
            }

            return dynModule;
        }
        catch (Exception e)
        {
            error = ("Error occurred when importing: " + e.Message).ToPyPbErrorDefinitionString(e.StackTrace, pyargs: $"from {from} import {import}");
            return null;
        }

    }

    /// <summary>
    /// Imports a submodule
    /// </summary>
    /// <param name="from">name of the root module</param>
    /// <param name="import">name of the submodule to import</param>
    /// <param name="error">[OUT] error</param>
    /// <returns>the module if found</returns>
    public PyPbModule? FromImportModule(string from, string import, out string? error)
    {
        error = null;

        using var _ = Py.GIL();

        try
        {
            PyModule? submod = null;

            try
            {
                var rootModule = CreateScope().Import(from);
                PyDict locals = new();
                locals["module"] = rootModule;
                submod = CreateScope().Eval($"getattr(module, \"{import}\")", locals).ReinterpretAsModule();
            }
            catch
            {
            }

            if (submod is null)
                try
                {
                    submod = CreateScope().Import($"{from}.{import}") as PyModule;
                }
                catch
                {
                }

            if (submod is null)
            {
                error = "Could not resolve submodule".ToPyPbErrorDefinitionString(pyargs: $"from {from} import {import}");
                return null;
            }

            var res = PyObjectProxyBuilder.CreateProxyFrom(this, submod, out PyPbModule? dynModule, out error);
            if (res != 0)
            {
                error = ("Could not create proxy from object: " + error).ToPyPbErrorDefinitionString(pyargs: $"from {from} import {import}");
                return null;
            }

            return dynModule;
        }
        catch (Exception e)
        {
            error = ("Error occurred when importing: " + e.Message).ToPyPbErrorDefinitionString(e.StackTrace, pyargs: $"from {from} import {import}");
            return null;
        }

    }

    /// <summary>
    /// Execute a raw Python statement with locals
    /// </summary>
    /// <param name="statement"></param>
    /// <param name="localIds">a string array specifying the locals' identifiers</param>
    /// <param name="localVals">an object array specifying the locals' values</param>
    /// <param name="result"></param>
    /// <param name="error"></param>
    /// <returns>0 on success</returns>
    public int ExecuteStatement(string statement, InvocationRequest localsDict, out PyPbObject? result, out string? error)
    {
        result = null;
        try
        {
            var locals = new PyDict();
            foreach (var pair in localsDict.NamedArguments)
            {
                locals[pair.Key] = pair.Value.ToPyObject();
            }

            var pyobj = CreateScope().Eval(statement, locals);

            var res = PyObjectProxyBuilder.CreateProxyFrom(this, pyobj, out PyPbObject? dynModule, out error);
            if (res != 0)
            {
                error = ("Could not create proxy from object: " + error).ToPyPbErrorDefinitionString(pyargs: statement);
                return -1;
            }

            result = dynModule;
            return 0;
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace, pyargs: statement);
            return -1;
        }

    }


    /// <summary>
    /// Execute a raw Python statemnt
    /// </summary>
    /// <param name="statement"></param>
    /// <param name="result">[OUT]</param>
    /// <param name="error">[OUT]</param>
    /// <returns>0 on success</returns>
    public int ExecuteStatement(string statement, out PyPbObject? result, out string? error)
    {
        return ExecuteStatement(statement, new("stub"), out result, out error);
    }
}

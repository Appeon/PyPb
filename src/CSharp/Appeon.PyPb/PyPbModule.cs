using Python.Runtime;
using System.Net.Security;
using static Appeon.PyPb.Constants;

namespace Appeon.PyPb;


/// <summary>
/// An abstraction of a Python module
/// </summary>
public class PyPbModule : AbstractInvocationProxy, IDisposable
{
    private bool disposedValue;
    private readonly PyModule _module;

    public PyModule Module => _module;

    public PyPbModule(PyModule module, PyPbContext context) : base(module, context)
    {
        _module = module;
        _object = module;
    }

    /// <summary>
    /// Instantiate a class contained in the current module with no constructor parameters
    /// </summary>
    /// <param name="name">the name of the class</param>
    /// <param name="result">[out] the resulting instance</param>
    /// <param name="error">[out] the error that occurred</param>
    /// <returns>0 on success</returns>
    public int Instantiate(string name, out PyPbObject? result, out string? error)
    {
        return Instantiate(new InvocationRequest(name), out result, out error);
    }

    /// <summary>
    /// Instantiate a class contained in the current module
    /// </summary>
    /// <param name="invocationReq">the invocatin request</param>
    /// <param name="result">[out] the resulting instance</param>
    /// <param name="error">[out] the error that occurred</param>
    /// <returns>0 on success</returns>
    public int Instantiate(InvocationRequest invocationReq, out PyPbObject? result, out string? error)
    {
        result = null;
        error = null;

        try
        {
            InvokePythonCallable(invocationReq.TargetName,
                new LabeledPredicate<PyObject>("Specified target is not a Type"
                    , obj => obj.GetPythonType().Name == Constants.PythonTypeClass)
                , out result
                , out error
                , ErrorParadigm.PyPb
                , [.. invocationReq.Arguments]);

            return 0;
        }
        catch (Exception e)
        {
            error = ($"Error when trying to get instantiate class [{invocationReq.TargetName}]" +
                $"with {invocationReq.Arguments.Count} arguments.\n" +
                e.Message).ToPyPbErrorDefinitionString(e.StackTrace, pytarget: invocationReq.TargetName, pyargs: invocationReq.SerializeArguments());
            return -1;
        }
    }


#pragma warning disable CA1822 // Mark members as static
    public InvocationRequest CreateInvocationRequest(string target) => new(target);
#pragma warning restore CA1822 // Mark members as static

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                base._object.Dispose();
            }

            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected override PyObject?[] TransformArguments(PyObject source, PyObject?[] arguments, Py.KeywordArguments? kwargs = null) => arguments;

    protected override PyObject ExecutePythonDelegate(PyObject source, PyObject?[] arguments, Py.KeywordArguments? kwargs = null)
    {
        if (arguments is null)
            throw new NullReferenceException(nameof(arguments));
        kwargs ??= new();
        return source.Invoke(arguments!, kwargs);
    }

    protected override PyObject GetExecutionTarget(PyObject source, string targetName)
    {
        if (source is PyModule module)
        {
            return module.GetAttr(targetName);
        }
        else throw new InvalidOperationException("Execution context is not a module");

    }
}
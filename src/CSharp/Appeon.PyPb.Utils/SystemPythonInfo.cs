using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Appeon.PyPb.Utils
{
    public partial class SystemPythonInfo
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[]? ppszOtherDirs);
        const int MAX_PATH = 260;

        public static bool PythonInPath(out string? path)
        {
            path = null;

            var sb = new StringBuilder("python.exe", MAX_PATH);
            var ret = PathFindOnPath(sb, null);
            if (ret)
                path = Path.GetDirectoryName(sb.ToString());
            return ret;
        }

        public static int GetDefaultPythonArchitecture(out string? arch, out string? error)
        {
            arch = null;
            error = null;

            PythonInPath(out var path);
            if (path is not null)
            {
                return GetPythonArchitecture(path, out arch, out error);
            }

            return -1;
        }

        public static int GetPythonArchitecture(string pythonPath, out string? arch, out string? error)
        {
            arch = null;
            error = null;

            var path = FindPythonExeInPath(pythonPath);

            using var p = new Process();

            if (path is null)
            {
                error = "Invalid path specified";
                return -1;
            }

            p.StartInfo.FileName = path;

            p.StartInfo.Arguments = "-c \"import platform; print(platform.architecture()[0])\"";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardError = true;
            try
            {
                p.Start();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    error = p.StandardError.ReadToEnd();
                    return -1;
                }
                arch = p.StandardOutput.ReadToEnd().Trim(['\n', '\r', '\t']);
                return 0;
            }
            catch (Exception e)
            {
                error = e.Message;

                return -1;
            }
        }

        public static int CanImportModules(string path, string[] modules, out string? error)
        {
            var pythonExe = FindPythonExeInPath(path);
            var pythonStatement = new StringBuilder();

            modules = [.. modules.Select(m => $"'{m}'")];

            pythonStatement.AppendLine("import importlib.util as u");


            pythonStatement.AppendLine($"for m in [{string.Join(',', modules)}]:");
            pythonStatement.AppendLine($"    if u.find_spec(m) is None:");
            pythonStatement.AppendLine($"        print('No loader for module ' + m)");

            error = null;

            if (pythonExe is null)
            {
                error = "Specified Python path is invalid";
                return -1;
            }

            var p = new Process();
            p.StartInfo.FileName = pythonExe;
            p.StartInfo.Arguments = $"-c \"{pythonStatement}\"";
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.WaitForExit();

            error = p.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error))
            {
                return -1;
            }

            error = $"{p.StandardOutput.ReadToEnd()}";

            if (error.Length == 0)
                return 0;

            return -1;

        }

        public static int CanImportModule(string path, string module, out string? error)
        {
            return CanImportModules(path, [module], out error);
        }

        public static PythonRuntimeInformation GetRuntimeInformation(string path)
        {
            string exePath;
            string dllPath = "";
            if (File.Exists(path))
                path = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Invalid path");

            if (Directory.Exists(path))
            {
                exePath = Path.Combine(path, "python.exe");
                if (!File.Exists(exePath))
                {
                    throw new FileNotFoundException("Could not find python.exe in the specified directory");
                }

                foreach (var file in Directory.GetFiles(path, "python*.dll"))
                {
                    dllPath = (file.Length > dllPath.Length) ? file : dllPath;
                }

                if (!File.Exists(dllPath))
                    throw new FileNotFoundException("Could not find a Python DLL in the specified directory");

                Process p = new();
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = "-c \"import sys; print(sys.version)\"";
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
                if (p.ExitCode != 0)
                    throw new Exception("Python process returned non-success status code " + p.ExitCode);

                var versionResult = p.StandardOutput.ReadToEnd();

                var version = PythonVersionRegex().Match(versionResult).Groups[1].Value;
                byte bitness = byte.Parse(PythonBitnessRegex().Match(versionResult).Groups[1].Value);

                return new PythonRuntimeInformation(dllPath, exePath, path, version, bitness);
            }

            throw new InvalidOperationException("Could not obtain runtime information from the specified path");
        }

        private static string? FindPythonExeInPath(string path)
        {
            if (File.Exists(path)
                && PythonBinaryMatchRegex().IsMatch(Path.GetFileName(path)))
            {
                path = Path.GetDirectoryName(path) ?? Directory.GetDirectoryRoot(path);
            }

            if (Directory.Exists(path)
                && Directory.GetFiles(path)
                    .Select(s => s.ToLowerInvariant())
                    .FirstOrDefault(s => s.EndsWith("python.exe")) is var childPath
                && childPath is not null)
            {
                return childPath;
            }

            return null;
        }

        [GeneratedRegex(@"python(?:[0-9]+\.dll|\.exe)", RegexOptions.IgnoreCase)]
        private static partial Regex PythonBinaryMatchRegex();

        [GeneratedRegex(@"^([0-9\.]+) ", RegexOptions.IgnoreCase)]
        private static partial Regex PythonVersionRegex();

        [GeneratedRegex(@" ([0-9]{2}) bit", RegexOptions.IgnoreCase)]
        private static partial Regex PythonBitnessRegex();

    }
}

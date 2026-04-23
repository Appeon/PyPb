using Python.Deployment;
using Python.Runtime;

namespace Appeon.OpenpyxlWrapper
{
    public class XlsxWriterBuilder
    {
        public static int Build(out XlsxWriter? writer, out string? error)
        {
            writer = null;
            error = null;

            try
            {
                if (!PythonEngine.IsInitialized)
                {
                    var source = new Installer.DownloadInstallationSource
                    {
                        DownloadUrl = "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip"
                    };
                    source.DownloadUrl = (!Environment.Is64BitProcess) ? source.DownloadUrl.Replace("amd64", "win32") : source.DownloadUrl;
                    Installer.Source = source;
                    Installer.SetupPython().Wait();
                    Installer.TryInstallPip().Wait();
                    Installer.PipInstallModule("openpyxl").Wait();

                    /// This must keep parity with Python.Included package's provided runtime
                    Runtime.PythonDLL ??= "python311.dll";

                    PythonEngine.Initialize();
                }

                using var _ = Py.GIL();
                if (PyModule.Import("openpyxl") is not PyModule module || module.IsNone())
                {
                    error = "Could not import openpyxl";
                    return -1;
                }

                PyDict locals = new();
                var workbook = module.Eval("Workbook()");
                locals["wb"] = workbook;
                var worksheet = module.Eval("wb.active", locals);

                writer = new XlsxWriter(module, workbook, worksheet);

                return 0;
            }
            catch (Exception e)
            {
                error = "Failed to init XlsxWriter: " + e.Message;
                return -1;
            }

        }
    }
}

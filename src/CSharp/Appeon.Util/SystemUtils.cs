using System.Diagnostics;

namespace Appeon.Util
{
    public class SystemUtils
    {
        public static int Run(string program, string arguments, bool showWindow, out string? stdout, out string? stderr)
        {
            stdout = null;

            try
            {
                Process p = new();
                p.StartInfo.FileName = program;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = !showWindow;
                p.Start();
                p.WaitForExit();

                stdout = p.StandardOutput.ReadToEnd();
                stderr = p.StandardError.ReadToEnd();

                return p.ExitCode;
            }
            catch (Exception e)
            {
                stderr = "Could not create process: " + e.Message;
                return -1;
            }


        }
    }
}

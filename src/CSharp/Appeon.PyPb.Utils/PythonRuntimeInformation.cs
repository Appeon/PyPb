namespace Appeon.PyPb.Utils;

public record PythonRuntimeInformation(
    string DllPath,
    string ExePath,
    string PythonHome,
    string Version,
    byte Bitness)
{
}

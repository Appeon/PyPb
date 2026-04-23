using Appeon.PyPb.Tests.Unit.Facilities;
using Appeon.PyPb.Utils;
using System.ComponentModel;
using System.Diagnostics;

namespace Appeon.PyPb.Tests.Unit;

[TestCaseOrderer(
    ordererTypeName: "Appeon.PyPb.Tests.PriorityOrderer",
    ordererAssemblyName: "Appeon.PyPb.Tests")]
[Collection("PyPbContext-dependent tests collection")]
public class SystemPythonInfoTests
{
    private readonly ContextProvider container;

    public SystemPythonInfoTests(ContextProvider container)
    {
        this.container = container;
    }

    [Fact]
    [TestPriority]
    public void CanFindPythonInPath()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "python.exe",
                Arguments = "--version"
            }
        };

        process.Start();
        process.WaitForExit();
        Assert.True(SystemPythonInfo.PythonInPath(out var _) == (process.ExitCode == 0));
    }

    [Fact]
    [TestPriority]
    public void CanGetPythonRuntimeArchitecture()
    {
        var res = SystemPythonInfo.GetPythonArchitecture(
            container.Context.RuntimePath,
            out var arch,
            out var error);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(arch);
        Assert.Equal(0, res);

        if (Environment.Is64BitProcess)
        {
            Assert.Equal("64bit", arch);
        }
        else
        {
            Assert.Equal("32bit", arch);
        }

    }
}

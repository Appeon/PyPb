using Appeon.PyPb.Inspector;

namespace Appeon.PyPb.Tests.Unit.Facilities;

public class PyPbContextContainer
{
    public PyPbContext? Context { get; set; }
    public PyPbModule? Module { get; set; }
    public PyPbObject? Object { get; set; }

    public PythonInspector? Inspector { get; set; }
}

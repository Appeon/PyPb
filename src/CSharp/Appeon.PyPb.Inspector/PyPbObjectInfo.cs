namespace Appeon.PyPb.Inspector;

public class PyPbObjectInfo(PyPbObject @object, string name, string type)
{
    public PyPbObject PyPbObject { get; } = @object;
    public string Name { get; } = name;
    public string Type { get; } = type;
}

using BenchmarkDotNet.Attributes;
using Python.Runtime;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Appeon.PyPb.Tests.Benchmarks;

public class PyPbTests
{
    private const string ClassWithMethod = "from functools import reduce\r\n\r\nclass ClassWithMethod:\r\n    def __init__(self):\r\n        pass\r\n    \r\n    def method(self, top):       \r\n        list = [str(x) for x in range(0, top)]\r\n        concat = reduce(lambda x, y: x + \", \" + y, list)\r\n        \r\n        return concat";
    private readonly PyPbContext _context;

    public int PropertyCount { get; set; } = 100;
    public int InvocationCount { get; set; } = 100;
    private PyObject PyObject;
    private PyPbObject PyPbObject;
    private MethodInfo TargetMethod;
    private readonly Random random;

    public PyPbTests()
    {
        _context = PyPbContext.Init([], out var error) ?? throw new Exception($"Could not initialize context: {error}");
        random = new Random();
        CreateObjects();
    }

    [MemberNotNull(nameof(PyObject))]
    [MemberNotNull(nameof(PyPbObject))]
    [MemberNotNull(nameof(TargetMethod))]
    private void CreateObjects()
    {
        using var _ = Py.GIL();
        var pypbmodule = _context.LoadModuleFromString("ClassWithMethod", ClassWithMethod, out var error)
            ?? throw new Exception($"Could not initialize module: {error}"); ;

        pypbmodule.Instantiate("ClassWithMethod", out PyPbObject, out error);

        if (error is not null)
            throw new Exception($"Could not instantiate class: {error}");

        if (PyPbObject is null)
            throw new Exception("Could not instantiate class.");

        if (PyModule.FromString("ClassWithMethod", ClassWithMethod) is not PyModule module)
            throw new Exception("Could not load module");

        var @class = module.GetAttr("ClassWithMethod") ?? throw new Exception("Could not get class object");

        PyObject = @class.Invoke() ?? throw new Exception("Could not instantiate class");

        TargetMethod = PyPbObject.GetType().GetMethod("method") ?? throw new Exception("Could not load dynamic method");
    }

    [Benchmark]
    public int AccessPropertiesPyPb()
    {
        string propName;
        for (int i = 0; i < PropertyCount; ++i)
        {
            propName = $"property{i}";
            PyPbObject.Set(propName, random.NextInt64().ToString(), out var error);
            PyPbObject.Get(propName, out var value, out error);

        }

        return 0;
    }

    [Benchmark]
    public int AccessPropertiesPythonNet()
    {
        using var _ = Py.GIL();

        string propName;
        for (int i = 0; i < PropertyCount; ++i)
        {
            propName = $"property{i}";
            PyObject.SetAttr(propName, random.NextInt64().ToString().ToPython());
            PyObject.GetAttr(propName);
        }

        return 0;
    }

    [Benchmark]
    public string InvokeFunctionPyPb()
    {

        var req = new InvocationRequest("method");
        req.AddArgument(100);

        PyPbObject? result = null;

        for (int i = 0; i < InvocationCount; ++i)
        {
            PyPbObject.Invoke(req, out result, out var error);
        }


        return result?.ToString() ?? string.Empty;
    }

    [Benchmark]
    public string InvokeFunctionPythonNet()
    {
        using var _ = Py.GIL();

        var arg = new PyTuple([100.ToPython()]);

        PyObject? result = null;
        for (int i = 0; i < InvocationCount; ++i)
        {
            result = PyObject.InvokeMethod("method", arg);
        }

        return result?.ToString() ?? string.Empty;
    }

    [Benchmark]
    public string InvokeFunctionDynamic()
    {
        object? result = null;
        object?[] args = [100, null, null];
        for (int i = 0; i < InvocationCount; ++i)
        {
            result = TargetMethod.Invoke(PyPbObject, args);
        }
        return result?.ToString() ?? string.Empty;
    }

}

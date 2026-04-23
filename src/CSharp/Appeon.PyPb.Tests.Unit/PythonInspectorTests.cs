using Appeon.PyPb.Inspector;
using Appeon.PyPb.Tests.Common;
using Appeon.PyPb.Tests.Unit.Facilities;
using Appeon.Util;

namespace Appeon.PyPb.Tests.Unit;

[TestCaseOrderer(
    ordererTypeName: "Appeon.PyPb.Tests.PriorityOrderer",
    ordererAssemblyName: "Appeon.PyPb.Tests")]
[Collection("PyPbContext-dependent tests collection")]
public class PythonInspectorTests
{
    private readonly ContextProvider context;

    public PythonInspectorTests(ContextProvider context)
    {
        this.context = context;
    }

    [Fact]
    [TestPriority]
    public void CanCreatePythonInspector()
    {
        Assert.NotNull(context.Context);

        var result = PythonInspectorBuilder.Build(context.Context, out var inspector, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, result);
        Assert.NotNull(inspector);

        context.Inspector = inspector;
    }

    [Fact]
    [TestPriority(1)]
    public void CanInspectModule()
    {
        using var script = new TempTextFile($"PyBTester.py", ContextProvider.SamplePythonScript);

        var res = context.Inspector.InspectModule(script.Path, out CustomList<PyPbObjectInfo?> info, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);

        Assert.NotNull(info);
    }

    [Fact]
    [TestPriority(1)]
    public void CanInspectObject()
    {
        var res = context.Inspector.InspectObject(context.Object!, out PyPbObjectInfo[]? info, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(info);

        Assert.True(info.Length >= 2);
    }


    [Fact]
    [TestPriority(1)]
    public void CanGetFunctionSignature()
    {
        var res = context.Inspector.InspectObject(context.Object!, out PyPbObjectInfo[]? info, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(info);

        var functions = info.Where(p => p.Type == "function");

        Assert.True(functions.Any());

        foreach (var objinfo in functions)
        {
            res = PythonInspector.GetFunctionSignature(objinfo.PyPbObject, out var signature, out error);

            if (error is not null)
                throw new Exception(error);

            Assert.Equal(0, res);
            Assert.NotNull(signature);
        }
    }

    [Fact]
    [TestPriority(1)]
    public void CanGetFunctionParameters()
    {
        var res = context.Inspector.InspectObject(context.Object!, out PyPbObjectInfo[]? info, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(info);

        var functions = info.Where(p => p.Type == "function");

        Assert.True(functions.Any());

        foreach (var objinfo in functions)
        {
            res = PythonInspector.GetFunctionParameters(objinfo.PyPbObject.GetPyObject(), out var @params, out error);

            if (error is not null)
                throw new Exception(error);

            Assert.Equal(0, res);
            Assert.NotNull(@params);
        }
    }


}

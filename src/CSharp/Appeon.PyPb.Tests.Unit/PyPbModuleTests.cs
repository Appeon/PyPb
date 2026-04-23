using Appeon.PyPb.Tests.Unit.Facilities;

namespace Appeon.PyPb.Tests.Unit;

[TestCaseOrderer(
    ordererTypeName: "Appeon.PyPb.Tests.PriorityOrderer",
    ordererAssemblyName: "Appeon.PyPb.Tests")]
[Collection("PyPbContext-dependent tests collection")]
public class PyPbModuleTests
{
    private readonly ContextProvider container;

    public PyPbModuleTests(ContextProvider container)
    {
        this.container = container;
    }

    [Fact]
    [TestPriority(1)]
    public void CanReadAndWriteProperties()
    {
        var res = container.Module.Set("testProperty", true, out var error);

        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);

        res = container.Module.Get("testProperty", out var obj, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(obj);
        Assert.Equal(0, res);

        res = obj.ToBool(out var @bool, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.True(@bool);
    }

    [Fact]
    [TestPriority]
    public void CanInvokeModuleFunctionWithName()
    {
        var res = container.Module.Invoke("createPyBTesterInstance", out var result, out var error);

        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.NotNull(result);
    }

    [Fact]
    [TestPriority]
    public void CanInvokeModuleFunctionWithInvocationRequests()
    {
        InvocationRequest req = new("createPyBTesterInstance");
        var res = container.Module.Invoke(req, out var result, out var error);

        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.NotNull(result);
    }

    [Fact]
    [TestPriority]
    public void CanInvokeMethodWithKwargs()
    {
        InvocationRequest req = new("namedArguments");
        req.AddNamedArgument("first", "");

        var res = container.Module.Invoke(req, out var obj, out var error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.NotNull(obj);
        obj.ToInt(out int value, out _);
        Assert.Equal(1, value);

        req.ClearArguments();
        req.AddNamedArgument("second", "");

        res = container.Object.Invoke(req, out obj, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.NotNull(obj);
        obj.ToInt(out value, out _);
        Assert.Equal(2, value);
    }

    [Fact]
    [TestPriority]
    public void CanInvokeModuleFunctionWithDynamicFunction()
    {
        var type = container.Module.GetType();
        var method = type.GetMethod("createPyBTesterInstance", [typeof(PyPbObject).MakeByRefType(), typeof(string).MakeByRefType()]);

        object[] args = new object[2];
        Assert.NotNull(method);
        var statusCode = method.Invoke(container.Module, args);
        Assert.IsAssignableFrom<int>(statusCode);
        Assert.NotNull(args[0]);
        Assert.Null(args[1]);

    }

    [Fact]
    [TestPriority(-1)]
    public void CanInstantiateClassWithExplicitName()
    {
        int res = container.Module.Instantiate("PyBTester", out var @object, out var error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.NotNull(@object);

        container.Object = @object;
    }

    [Fact]
    [TestPriority(-1)]
    public void CanInstantiateClassWithInvocationRequest()
    {
        InvocationRequest req = new("PyBTester");
        int res = container.Module.Instantiate(req, out var @object, out var error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.NotNull(@object);

        container.Object = @object;
    }

    [Fact]
    [TestPriority(-1)]
    public void CanInstantiateClassWithDynamicMethodWithOutParams()
    {
        var method = container.Module.GetType().GetMethod("PyBTester",
        [
            typeof(PyPbObject).MakeByRefType(),
            typeof(string).MakeByRefType()
        ]);

        Assert.NotNull(method);

        object[] args = new object[2];
        var res = method.Invoke(container.Module, args);

        if (args[1] is not null)
            throw new Exception(args[1].ToString());
        Assert.Equal(0, res);
        Assert.IsAssignableFrom<PyPbObject>(args[0]);
        Assert.NotNull(args[0]);

        container.Object = (PyPbObject)args[0];
    }

    [Fact]
    [TestPriority(-1)]
    public void CanInstantiateClassWithDynamicMethodWithNoOutParams()
    {
        var method = container.Module.GetType().GetMethod("PyBTester", []);

        Assert.NotNull(method);

        var res = method.Invoke(container.Module, []);

        Assert.NotNull(res);
        Assert.IsAssignableFrom<PyPbObject>(res);

        container.Object = (PyPbObject)res;
    }

    [Fact]
    [TestPriority]
    public void CanRedirectOutput()
    {

        bool redirected = false;
        var redirModule = new RedirectedOutputPyModule((stdout) =>
        {
            if (stdout.Equals("42"))
                redirected = true;
        });

        redirModule.ImportAll(container.Module.Module);

        var pmodule = new PyPbModule(redirModule, container.Context);
        int res = pmodule.Instantiate("PyBTester", out var pybtester, out var error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.NotNull(pybtester);

        var req = new InvocationRequest("Print", "40", "41", "42");
        res = pybtester.Invoke(req, out var pyresult, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.NotNull(pybtester);

        Assert.True(redirected);
    }

    [Fact]
    [TestPriority]
    public void CanGetMember()
    {
        var res = container.Module.GetMember("PyBTester", out var obj, out var error);
        if (error is not null)
        {
            throw new Exception(error);
        }
        Assert.Equal(0, res);
        Assert.NotNull(obj);

    }

    [Fact]
    [TestPriority]
    public void FailsToGetNonExistingMember()
    {
        var res = container.Module.GetMember("Adds", out var obj, out var error);

        Assert.NotNull(error);
        Assert.NotEqual(0, res);
        Assert.Null(obj);
    }



}
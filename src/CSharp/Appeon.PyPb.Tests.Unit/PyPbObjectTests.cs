using Appeon.PyPb.Tests.Unit.Facilities;

namespace Appeon.PyPb.Tests.Unit;

[TestCaseOrderer(
    ordererTypeName: "Appeon.PyPb.Tests.PriorityOrderer",
    ordererAssemblyName: "Appeon.PyPb.Tests")]
[Collection("PyPbContext-dependent tests collection")]
public class PyPbObjectTests
{
    private readonly ContextProvider container;

    public PyPbObjectTests(ContextProvider container)
    {
        this.container = container;
    }


    [Fact]
    [TestPriority(1)]
    public void CanReadAndWriteProperties()
    {
        var res = container.Object.Set("testProperty", true, out var error);

        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);

        res = container.Object.Get("testProperty", out var obj, out error);
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
    public void GracefullyFailsToInvokeMethodDirectlyIfArgumentsNotProvided()
    {
        var res = container.Object.Invoke("Add", out var obj, out var error);
        Assert.NotEqual(0, res);
        Assert.Null(obj);
        Assert.NotNull(error);
    }

    [Fact]
    [TestPriority]
    public void CanInvokeMethodWithInvocationRequest()
    {
        InvocationRequest req = new("Add", 6, 9);

        var res = container.Object.Invoke(req, out var obj, out var error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.NotNull(obj);

        res = obj.ToInt(out var value, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.Equal(15, value);
    }

    [Fact]
    [TestPriority]
    public void CanInvokeMethodWithKwargs()
    {
        InvocationRequest req = new("namedArguments");
        req.AddNamedArgument("first", "");

        var res = container.Object.Invoke(req, out var obj, out var error);
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
    public void CanInvokeMethodWithDynamicMethodWithOutParams()
    {
        var method = container.Object.GetType().GetMethod("Add",
        [
            typeof(object),
            typeof(object),
            typeof(PyPbObject).MakeByRefType(),
            typeof(string).MakeByRefType()
        ]);
        Assert.NotNull(method);

        var arguments = new object?[] { 6, 9, null, null };
        var statusCode = method.Invoke(container.Object, arguments);

        var res = Assert.IsAssignableFrom<int>(statusCode);
        var obj = Assert.IsAssignableFrom<PyPbObject>(arguments[2]);

        if (arguments[3] is not null)
            throw new Exception(arguments[3].ToString());

        Assert.Equal(0, res);
        Assert.NotNull(obj);

        res = obj.ToInt(out var value, out var error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.Equal(15, value);
    }

    [Fact]
    [TestPriority]
    public void CanInvokeMethodWithDynamicMethodWithNoOutParams()
    {
        var method = container.Object.GetType().GetMethod("Add", [typeof(object), typeof(object)]);
        Assert.NotNull(method);

        var arguments = new object?[] { 6, 9 };
        var retVal = method.Invoke(container.Object, arguments);

        var retPyPb = Assert.IsAssignableFrom<PyPbObject>(retVal);

        Assert.NotNull(retPyPb);

        var res = retPyPb.ToInt(out var value, out var error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(0, res);
        Assert.Equal(15, value);
    }

    [Fact]
    [TestPriority]
    public void CanConvertToInt()
    {
        var res = container.Object.Invoke("getInt", out var result, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(result);

        result.ToInt(out var intValue, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(42, intValue);
    }

    [Fact]
    [TestPriority]
    public void CanConvertStringToIntWhenCastable()
    {
        var res = container.Object.Invoke("getIntString", out var result, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(result);

        result.ToInt(out var intValue, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(42, intValue);
    }

    [Fact]
    [TestPriority]
    public void CanConvertToBool()
    {
        var res = container.Object.Invoke("getBool", out var result, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(result);

        result.ToBool(out var boolValue, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.True(boolValue);
    }

    [Fact]
    [TestPriority]
    public void CanConvertToFloat()
    {
        var res = container.Object.Invoke("getFloat", out var result, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(result);

        result.ToFloat(out var floatValue, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(4.2f, floatValue);
    }

    [Fact]
    [TestPriority]
    public void CanConvertToDouble()
    {
        var res = container.Object.Invoke("getDouble", out var result, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(result);

        result.ToDouble(out var doubleValue, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(4.2, doubleValue);
    }

    [Fact]
    [TestPriority]
    public void CanConvertToString()
    {
        var res = container.Object.Invoke("getString", out var result, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(result);

        result.ToString(out var stringValue, out error);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal("abc", stringValue);
    }

    [Fact]
    [TestPriority]
    public void GracefullyFailsToConvertToWrongType()
    {
        var res = container.Object.Invoke("getString", out var result, out var error);

        if (error is not null)
            throw new Exception(error);

        Assert.Equal(0, res);
        Assert.NotNull(result);

        result.ToInt(out var intValue, out error);

        Assert.NotNull(error);
        Assert.NotEqual(42, intValue);

        result.ToFloat(out var floatValue, out error);

        Assert.NotNull(error);
        Assert.NotEqual(4.2f, floatValue);
    }

    [Fact]
    [TestPriority]
    public void CanGetMember()
    {
        var res = container.Object.GetMember("Add", out var obj, out var error);
        if (error is not null)
        {
            throw new Exception(error);
        }
        Assert.Equal(0, res);
        Assert.NotNull(obj);

    }

    [Fact]
    [TestPriority]
    public void FailsToGetNonExistentMember()
    {
        var res = container.Object.GetMember("Adds", out var obj, out var error);

        Assert.NotNull(error);
        Assert.NotEqual(0, res);
        Assert.Null(obj);
    }

    [Fact]
    [TestPriority]
    public void FailsToCallNonCallable()
    {
        var @object = container.Context.FromImportObject("os", "sys", out var error);

        if (error is not null)
        {
            throw new Exception(error);
        }

        Assert.NotNull(@object);

        var exitCode = @object.Call(out var res, out error);

        Assert.NotEqual(0, exitCode);
        Assert.NotNull(error);
        Assert.Null(res);
    }

    [Fact]
    [TestPriority]
    public void CanBeCalledIfCallable()
    {
        var @object = container.Context.FromImportObject("platform", "version", out var error);

        if (error is not null)
        {
            throw new Exception(error);
        }

        Assert.NotNull(@object);

        var exitCode = @object.Call(out var res, out error);

        Assert.Equal(0, exitCode);
        Assert.Null(error);
        Assert.NotNull(res);
    }

    [Fact]
    [TestPriority]
    public void CanAccessArrayByIndex()
    {
        var module = container.Module;
        int res = module.Get("array", out var @object, out var error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(@object);

        res = @object.AtIndex(3, out var value, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(value);

        res = value.ToInt(out int val, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(3, val);
    }

    [Fact]
    [TestPriority]
    public void CanSetArrayItemByIndex()
    {
        var module = container.Module;
        int res = module.Get("array", out var @object, out var error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(@object);

        res = @object.SetAtIndex(3, 67, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);

        res = @object.AtIndex(3, out @object, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(@object);

        res = @object.ToInt(out var val, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(67, val);
    }

    [Fact]
    [TestPriority]
    public void FailsToSetArrayItemByIndexOutOfRange()
    {
        var module = container.Module;
        int res = module.Get("array", out var @object, out var error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(@object);

        res = @object.SetAtIndex(10, 67, out error);
        Assert.NotEqual(0, res);
        Assert.NotNull(error);

    }

    [Fact]
    [TestPriority]
    public void FailsToAccessArrayOutOfBounds()
    {
        var module = container.Module;
        int res = module.Get("array", out var @object, out var error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(@object);

        res = @object.AtIndex(10, out var value, out error);
        Assert.NotEqual(0, res);
        Assert.NotNull(error);
        Assert.Null(value);
    }

    [Fact]
    [TestPriority]
    public void CanAccessDictionaryByKey()
    {
        var module = container.Module;
        int res = module.Get("dictionary", out var @object, out var error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(@object);

        res = @object.AtKey("first", out var value, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(value);

        res = value.ToInt(out int val, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(1, val);
    }

    [Fact]
    [TestPriority]
    public void CanSetDictionaryItemByIndex()
    {
        var module = container.Module;
        int res = module.Get("dictionary", out var @object, out var error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(@object);

        res = @object.SetAtKey("fourth", 4, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);

        res = @object.AtKey("fourth", out @object, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(@object);

        res = @object.ToInt(out var val, out error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.Equal(4, val);
    }

    [Fact]
    [TestPriority]
    public void FailsToAccessDictionaryNonExistentKey()
    {
        var module = container.Module;
        int res = module.Get("dictionary", out var @object, out var error);
        Assert.Equal(0, res);
        if (error is not null)
            throw new Exception(error);
        Assert.NotNull(@object);

        res = @object.AtKey("zero", out var value, out error);
        Assert.NotEqual(0, res);
        Assert.NotNull(error);
        Assert.Null(value);
    }
}

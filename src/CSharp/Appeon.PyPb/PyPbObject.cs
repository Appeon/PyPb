using Python.Runtime;
using System.ComponentModel.DataAnnotations;
namespace Appeon.PyPb;

/// <summary>
/// Wrapper for Python objects. Offers functions for setting/getting properties and
/// invoking functions. Can only be created through a <see cref="PyPbModule"/>.
/// </summary>
public class PyPbObject(PyObject @object, PyPbContext context) : AbstractInvocationProxy(@object, context), IDisposable
{
    private bool disposedValue;

    public PyObject GetPyObject() => _object;

    /// <summary>
    /// Invoke the object as if it were a callable. Parameterless variant
    /// </summary>
    /// <param name="result">[OUT] result of the call</param>
    /// <param name="error">[OUT] error if one occurred</param>
    /// <returns>0 on success</returns>
    public int Call(out PyPbObject? result, out string? error)
    {
        return Call(new InvocationRequest("stub"), out result, out error);
    }

    /// <summary>
    /// Invoke the object as if it were a callable. InvocationRequest
    /// </summary>
    /// <param name="request">InvocationRequest instance describing the parameters. Target name is ignored</param>
    /// <param name="result">[OUT] result of the call</param>
    /// <param name="error">[OUT] error if one occurred</param>
    /// <returns>0 on success</returns>
    public int Call(InvocationRequest request, out PyPbObject? result, out string? error)
    {
        result = null;

        if (!_object.HasAttr("__call__"))
        {
            error = "Object is not callable".ToPyPbErrorDefinitionString(pytarget: request.TargetName.ToString(), pyargs: request.SerializeArguments());
            return -1;
        }

        try
        {
            return InvokePythonCallable("__call__", out result, out error, Constants.ErrorParadigm.PyPb, [.. request.Arguments]);
        }
        catch (Exception e)
        {
            error = ("Error occurred when invoking __call__: " + e.Message)
                .ToPyPbErrorDefinitionString(
                    e.StackTrace,
                    pytarget: request.TargetName.ToString(),
                    pyargs: request.SerializeArguments());
            return -1;
        }
    }

    public int ToInt(out int value, out string? error)
    {
        value = 0;
        error = null;

        using var _ = Py.GIL();
        PyDict dict = new();

        try
        {
            var scope = _context.CreateScope();
            dict["input"] = _object;
            scope.Exec("output = int(input)", dict);
            value = dict["output"].As<int>();
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace);
            return -1;
        }

        return 0;
    }

    public int ToString(out string? value, out string? error)
    {
        value = null;
        error = null;

        using var _ = Py.GIL();
        PyDict dict = new();

        try
        {
            var scope = _context.CreateScope();
            dict["input"] = _object;
            scope.Exec("output = str(input)", dict);
            value = dict["output"].As<string>();
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace); ;
            return -1;
        }

        return 0;
    }

    public int ToBool(out bool? value, out string? error)
    {
        value = null;
        error = null;

        using var _ = Py.GIL();
        PyDict dict = new();

        try
        {
            var scope = _context.CreateScope();
            dict["input"] = _object;
            scope.Exec("output = bool(input)", dict);
            value = dict["output"].As<bool>();
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace); ;
            return -1;
        }

        return 0;
    }

    public int ToFloat(out float? value, out string? error)
    {
        value = null;
        error = null;

        using var _ = Py.GIL();
        PyDict dict = new();

        try
        {
            var scope = _context.CreateScope();
            dict["input"] = _object;
            scope.Exec("output = float(input)", dict);
            value = dict["output"].As<float>();
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace); ;
            return -1;
        }

        return 0;
    }

    public int ToDouble(out double? value, out string? error)
    {
        value = null;
        error = null;

        using var _ = Py.GIL();
        PyDict dict = new();

        try
        {
            var scope = _context.CreateScope();
            dict["input"] = _object;
            scope.Exec("output = float(input)", dict);
            value = dict["output"].As<double>();
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace); ;
            return -1;
        }

        return 0;
    }

    /// <summary>
    /// Accesses this object's items by their index. object[i]
    /// </summary>
    /// <param name="i">the index</param>
    /// <param name="value">[OUT] the item</param>
    /// <param name="error">[OUT] an error if one occurs</param>
    /// <returns>0 on success</returns>
    public int AtIndex(int i, out PyPbObject? value, out string? error)
    {
        value = null;

        int val;

        try
        {
            var item = _object.GetItem(i);

            val = WrapPyObject(item, out value, out error);
            if (!string.IsNullOrEmpty(error))
                error = error.ToPyPbErrorDefinitionString(pytarget: i.ToString(), pyargs: value?.ToString() ?? "");
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace, pytarget: i.ToString(), pyargs: value?.ToString() ?? "");
            return -1;
        }

        return val;
    }

    /// <summary>
    /// Sets the item's value to <paramref name="value"/>
    /// </summary>
    /// <param name="i">the index</param>
    /// <param name="value">the value</param>
    /// <param name="error">[OUT] and error if one occurs</param>
    /// <returns>0 on success</returns>
    public int SetAtIndex(int i, object? value, out string? error)
    {
        error = null;
        try
        {
            _object.SetItem(i, value?.ToPyObject() ?? PyObject.None);
            return 0;
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace, pytarget: i.ToString(), pyargs: value?.ToString() ?? "");
            return -1;
        }
    }


    /// <summary>
    /// Accesses this object's items by their index. object[key]
    /// </summary>
    /// <param name="key">the key</param>
    /// <param name="value">[OUT] the item</param>
    /// <param name="error">[OUT] an error if one occurs</param>
    /// <returns>0 on success</returns>
    public int AtKey(string key, out PyPbObject? value, out string? error)
    {
        value = null;

        int val;

        try
        {
            var item = _object.GetItem(key);

            val = WrapPyObject(item, out value, out error);
            if (!string.IsNullOrEmpty(error))
                error = error.ToPyPbErrorDefinitionString(pytarget: key, pyargs: value?.ToString() ?? "");
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace, pytarget: key, pyargs: value?.ToString() ?? "");
            return -1;
        }

        return val;
    }

    /// <summary>
    /// Sets the item's value to <paramref name="value"/>
    /// </summary>
    /// <param name="key">the key</param>
    /// <param name="value">the value</param>
    /// <param name="error">[OUT] and error if one occurs</param>
    /// <returns>0 on success</returns>
    public int SetAtKey(string key, object? value, out string? error)
    {
        error = null;
        try
        {
            _object.SetItem(key, value?.ToPyObject() ?? PyObject.None);
            return 0;
        }
        catch (Exception e)
        {
            error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace, pytarget: key, pyargs: value?.ToString() ?? "");
            return -1;
        }
    }

    public override string? ToString()
    {
        return _object.ToString();
    }


#pragma warning disable CA1822 // Mark members as static
    public InvocationRequest CreateInvocationRequest(string target) => new(target);
#pragma warning restore CA1822 // Mark members as static

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _object.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~PyPbObject()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected override PyObject?[] TransformArguments(PyObject source, PyObject?[] arguments, Py.KeywordArguments? kwargs = null)
    {
        return [.. arguments];
    }

    protected override PyObject ExecutePythonDelegate(PyObject source, PyObject?[] arguments, Py.KeywordArguments? kwargs = null)
    {
        kwargs ??= new();
        return source.Invoke(arguments!, kwargs);
    }

    protected override PyObject GetExecutionTarget(PyObject source, string targetName)
    {
        using var scope = _context.CreateScope();
        var locals = new PyDict();

        return source.GetAttr(targetName);
    }
}

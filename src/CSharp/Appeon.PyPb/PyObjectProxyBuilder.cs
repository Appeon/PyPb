using Python.Runtime;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace Appeon.PyPb;

/// <summary>
/// Builds dynamic classes based on a <see cref="PyObject"/>
/// </summary>
public static partial class PyObjectProxyBuilder
{
    private static readonly ModuleBuilder DynamicModuleBuilder;

    [GeneratedRegex(@"[^_a-zA-Z]")]
    private static partial Regex NonLettersAndUnderscoreRegex();

    static PyObjectProxyBuilder()
    {
        DynamicModuleBuilder = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName("DynamicAssembly" + DateTime.Now.GetHashCode())
                                    , AssemblyBuilderAccess.Run)
            .DefineDynamicModule("DynamicModule");

    }


    /// <summary>
    /// Creates an <see cref="AbstractInvocationProxy"/> child class dynamicall with methods 
    /// imitating the signature of a <see cref="PyObject"/>. The generated methods provide two additional 
    /// parameters: a <code>out object ref</code> and <code>out string ref</code> for passing the 
    /// execution result and error respectively. These dynamic methods are set to return an int, indicating
    /// the execution result.
    /// </summary>
    /// <typeparam name="T">The AbstractInvocationProxy type that will wrap the resulting <typeparamref name="PyTarget"/></typeparam>
    /// <typeparam name="PyTarget">The type of object that will be being wrapped</typeparam>
    /// <param name="context"></param>
    /// <param name="pyObject"></param>
    /// <param name="result"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    public static int CreateProxyFrom<T, PyTarget>(PyPbContext context, PyTarget pyObject, out T? result, out string? error) // must return simple error
        where T : AbstractInvocationProxy
         where PyTarget : PyObject
    {
        result = null;
        error = null;
        using var _ = Py.GIL();

        if (pyObject.IsNone())
        {
            return 0;
        }

        try
        {
            using var scope = context.CreateScope();
            dynamic inspect = scope.Import("inspect");
            var className = GetFullClassName<T>(pyObject);
            if (className is null)
            {
                error = "could not obtain entity name";
                return -1;
            }

            if (IsTypeDefinedInAssembly(className) is var type && type is null)
            {
                var res = inspect.getmembers_static(pyObject);

                var callables = new List<PyObject>();
                var objectCount = (int)(res.__len__());
                for (int i = 0; i < objectCount; i++)
                {
                    var pyObj = (PyObject)res[i][1];
                    if (pyObj.IsCallable() && pyObj.HasAttr("__name__"))
                    {
                        callables.Add(PrebindFunction(unboundFunction: pyObj, self: pyObject, scope));
                    }
                }

                var tb = BuildType<T>(className);

                foreach (var func in callables)
                {
                    var callSpec = (CallSpec)BuildCallSpec(func, inspect);
                    if (callSpec is null)
                        continue;
                    BuildMethodWithOutParams(callSpec.TargetName
                        , tb
                        , [.. callSpec.Params.Select(p => p.ParamType)]
                        , [.. callSpec.Params.Select(p => p.Name)]
                        );
                    BuildMethodWithNoOutParams(callSpec.TargetName
                        , tb
                        , [.. callSpec.Params.Select(p => p.ParamType)]
                        , [.. callSpec.Params.Select(p => p.Name)]
                        );
                }

                BuildConstructor<T>(tb, [typeof(PyTarget), typeof(PyPbContext)]);

                type = tb.CreateType();

            }
            var instance = Activator.CreateInstance(type, [pyObject, context]);

            result = instance as T;

            return 0;
        }
        catch (Exception e)
        {
            error = e.Message;

            return -1;
        }
    }

    private static PyObject PrebindFunction(PyObject unboundFunction, PyObject self, PyModule scope)
    {
        scope.Import("types");
        string? qualifiedName;
        try
        {
            qualifiedName = unboundFunction.GetAttr("__qualname__").ToString();
        }
        catch
        { /// if callable doesn't have __qualname__ must be static
            return unboundFunction;
        }


        if (qualifiedName is null)
        {
            return unboundFunction;
        }
        var functionClassName = self.GetAttr("__class__").GetAttr("__name__").ToString();
        if (functionClassName is null)
            return unboundFunction;
        if (qualifiedName.Contains($"."))
        //if (qualifiedName.Contains($"{functionClassName}."))
        {
            PyDict locals = new();
            locals["f"] = unboundFunction;
            locals["s"] = self;
            return scope.Eval("types.MethodType(f, s)", locals);
        }

        return unboundFunction;
    }

    private static string GetFullClassName<T>(PyObject obj)
        where T : AbstractInvocationProxy
    {
        var sb = new StringBuilder();
        var classAttr = obj.GetAttr("__class__") ?? throw new InvalidOperationException("unable to get class name");

        if (typeof(T) != typeof(PyPbModule))
        {
            sb.Append($"{classAttr.GetAttr("__module__") ?? throw new InvalidOperationException("unable to get module name")}");
            sb.Append($"_{classAttr.GetAttr("__name__") ?? throw new InvalidOperationException("unable to get module name")}");
        }
        else
        {
            sb.Append(classAttr.GetAttr("__name__").ToString()!);
            sb.Append($"_{obj.GetAttr("__name__") ?? throw new InvalidOperationException("unable to get module name")}");
        }


        return NonLettersAndUnderscoreRegex().Replace(sb.ToString(), "_");
    }

    private static Type? IsTypeDefinedInAssembly(string typeName)
    {
        return DynamicModuleBuilder.GetType(typeName);
    }

    private static CallSpec? BuildCallSpec(PyObject callable, dynamic inspect)
    {

        List<ParamSpec> paramSpecs = [];

        dynamic paramIter;
        try
        {
            paramIter = inspect.signature(callable).parameters.__iter__();
        }
        catch
        {
            return null;
        }

        try
        {
            while (true)
                paramSpecs.Add(new ParamSpec(((PyObject)paramIter.__next__()).ToString()
                                             ?? throw new InvalidOperationException("unable to get parameters")
                                                , typeof(object)));
        }
        catch
        {
            /// Python signals iteration end with exceptions
        }
        return new(
                callable
                .GetAttr("__name__")
                .ToString()
                ?? throw new InvalidOperationException("Could not obtain class name")
            , paramSpecs);
    }

    private static TypeBuilder BuildType<T>(string typeName)
        where T : AbstractInvocationProxy
    {
        return DynamicModuleBuilder.DefineType(
        typeName,
        TypeAttributes.Public | TypeAttributes.Class,
        typeof(T));
    }

    private static ConstructorBuilder BuildConstructor<T>(TypeBuilder tb, Type[] ctorArgs)
        where T : AbstractInvocationProxy
    {
        var cb = tb.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName
            , CallingConventions.Standard | CallingConventions.HasThis
            , ctorArgs);

        var baseCtor = typeof(T).GetConstructor(ctorArgs)
            ?? throw new InvalidOperationException("unable to find base class constructor");

        var il = cb.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, baseCtor);
        il.Emit(OpCodes.Ret);

        return cb;

    }

    private static MethodBuilder BuildMethodWithOutParams(string methodName, TypeBuilder typeBuilder, Type[] parameterTypes, string[] parameterNames)
    {
        parameterTypes = [.. parameterTypes, Type.GetType("Appeon.PyPb.PyPbObject&")!, Type.GetType("System.String&")!];
        parameterNames = [.. parameterNames, "_out", "_error"];
        var method = typeof(PyPbObject).GetMethod(nameof(PyPbObject.InvokePythonCallable),
            [
                typeof(string),
                typeof(PyPbObject).MakeByRefType(),
                typeof(string).MakeByRefType(),
                typeof(Constants.ErrorParadigm),
                typeof(object[])
            ]) ?? throw new InvalidOperationException("Specified method does not exist");

        var mb = typeBuilder.DefineMethod(methodName,
            MethodAttributes.Public | MethodAttributes.HideBySig,
            CallingConventions.Standard | CallingConventions.HasThis,
            typeof(int),
            parameterTypes);

        for (int i = parameterNames.Length - 2; i < parameterNames.Length; ++i)
        {
            var pm = mb.DefineParameter(i + 1, ParameterAttributes.Out, DateTime.Now.GetHashCode() + parameterNames[i]);
        }

        var parameters = parameterTypes.Length;

        var il = mb.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldstr, methodName);
        il.Emit(OpCodes.Ldarg_S, parameters - 1); // result out param
        il.Emit(OpCodes.Ldarg_S, parameters); // error out param
        il.Emit(OpCodes.Ldc_I4, 0); // ErrorParadigm 0 = plain
        il.Emit(OpCodes.Ldc_I4, parameters - 2);
        il.Emit(OpCodes.Newarr, typeof(object));

        for (int i = 0; i < parameters - 2; ++i)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 1);
            if (parameterTypes[i].IsPrimitive)
            {
                il.Emit(OpCodes.Box, parameterTypes[i]);
            }
            il.Emit(OpCodes.Stelem_Ref);
        }


        il.Emit(OpCodes.Tailcall);
        il.Emit(OpCodes.Call, method);
        il.Emit(OpCodes.Ret);

        return mb ?? throw new InvalidOperationException("Could not create method.");
    }

    private static MethodBuilder BuildMethodWithNoOutParams(string methodName, TypeBuilder typeBuilder, Type[] parameterTypes, string[] parameterNames)
    {
        var method = typeof(PyPbObject).GetMethod(nameof(PyPbObject.InvokePythonCallable),
            [
                typeof(string),
                typeof(Constants.ErrorParadigm),
                typeof(object[])
            ]) ?? throw new InvalidOperationException("Could not find specified method");

        var mb = typeBuilder.DefineMethod(methodName,
            MethodAttributes.Public | MethodAttributes.HideBySig,
            CallingConventions.Standard | CallingConventions.HasThis,
            typeof(PyPbObject),
            parameterTypes);

        var parameters = parameterTypes.Length;

        var il = mb.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldstr, methodName);
        il.Emit(OpCodes.Ldc_I4, 0); // ErrorParadigm 0 = plain
        il.Emit(OpCodes.Ldc_I4, parameters);
        il.Emit(OpCodes.Newarr, typeof(object));

        for (int i = 0; i < parameters; ++i)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 1);
            if (parameterTypes[i].IsPrimitive)
            {
                il.Emit(OpCodes.Box, parameterTypes[i]);
            }
            il.Emit(OpCodes.Stelem_Ref);
        }


        il.Emit(OpCodes.Tailcall);
        il.Emit(OpCodes.Call, method);
        il.Emit(OpCodes.Ret);

        return mb ?? throw new InvalidOperationException("Could not create method.");
    }

}

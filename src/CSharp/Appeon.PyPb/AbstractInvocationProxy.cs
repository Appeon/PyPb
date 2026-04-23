using Python.Runtime;
using System.ComponentModel.DataAnnotations;
using static Appeon.PyPb.Constants;

namespace Appeon.PyPb
{
    /// <summary>
    /// Class that works as a proxy for Python invocation calls.
    /// 
    /// For dynamic objects created by <see cref="PyObjectProxyBuilder"/>, their dynamic methods will forward
    /// their call to this method, which will take care of executing the target python code, wrapping the execution,
    /// handling errors and if necessary, creating a new proxy from the result
    /// 
    /// </summary>
    /// <param name="object">Target <see cref="PyObject"/></param>
    /// <param name="context"></param>
    public abstract class AbstractInvocationProxy(PyObject @object, PyPbContext context)
    {


        protected PyObject _object = @object;
        protected PyPbContext _context = context;
        public ErrorParadigm ActiveErrorParadigm { get; set; }

        /// <summary>
        /// Transform the incoming arguments to accomodate the inheritor's behavior
        /// </summary>
        /// <param name="source"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        /// <param name="kwargs"></param>
        protected abstract PyObject?[] TransformArguments(PyObject source, PyObject?[] arguments, Py.KeywordArguments? kwargs = null);

        /// <summary>
        /// Execute the python code passing the arguments in a way appropriate to the inheritor's behavior
        /// </summary>
        /// <param name="source">target python object</param>
        /// <param name="arguments">arguments to the call</param>
        /// <returns></returns>
        protected abstract PyObject ExecutePythonDelegate(PyObject source, PyObject?[] arguments, Py.KeywordArguments? kwargs = null);

        /// <summary>
        /// Obtain the appropriate Python object to execute according to the inheritor's behavior
        /// </summary>
        /// <param name="source">source object</param>
        /// <param name="targetName">invocation target name</param>
        /// <returns></returns>
        protected abstract PyObject GetExecutionTarget(PyObject source, string targetName);

        /// <summary>
        /// Attempts to invoke <paramref name="methodName"/> on the underlying PyObject.
        /// </summary>
        /// <param name="methodName">the name of the python method to call</param>
        /// <param name="shouldExecute">test that will determine if target can be executed</param>
        /// <param name="result">[out] result of the python call</param>
        /// <param name="error">[out] error if occurred</param>
        /// <param name="args">[in] arguments to the python call</param>
        /// <returns>0 if success, -1 on error</returns>
        internal int InvokePythonCallable(
            string methodName,
            LabeledPredicate<PyObject> canExecute,
            out PyPbObject? result,
            out string? error,
            ErrorParadigm errorParadigm = ErrorParadigm.PyPb,
            params object[] args)
        {
            result = null;

            if (canExecute.Predicate(GetExecutionTarget(_object, methodName)))
                return InvokePythonCallable(methodName, out result, out error, errorParadigm, args);

            error = ("Invocation target failed precondition: " + canExecute.Label).ToPyPbErrorDefinitionString(pytarget: methodName, pyargs: args.SerializeArray());
            return -1;
        }

        /// <summary>
        /// Method that will be called by the dynamic methods created by <see cref="PyObjectProxyBuilder.CreateProxyFrom"/>
        /// This is the central method that all future implementations should base themselves around to facilitate debugging
        /// and replicability
        /// </summary>
        /// <param name="methodName">the name of the python method to call</param>
        /// <param name="result">[out] result of the python call</param>
        /// <param name="error">[out] error if occurred</param>
        /// <param name="args">[in] arguments to the python call</param>
        /// <returns>0 if success, -1 on error</returns>
        public int InvokePythonCallable(
            string methodName,
            out PyPbObject? result,
            out string? error,
            ErrorParadigm errorParadigm = ErrorParadigm.PyPb,
            params object[] args) // must return simple error
        {
            result = null;
            error = null;

            object[] posArgs = args.Where(a => a is not PyKwArg).ToArray() ?? [];
            PyKwArg[] kwArgs = args.Where(a => a is PyKwArg).Select(a => (PyKwArg)a).ToArray() ?? [];

            if (_object is null)
            {
                error = ("Object is corrupted: no PyObject").ToPyPbErrorDefinitionString(
                    pytarget: methodName,
                    pyargs: args.SerializeArray(),
                    errorParadigm: errorParadigm);
                return -1;
            }
            if (_context is null)
            {
                error = "Object is corrupted: no PyContext".ToPyPbErrorDefinitionString(
                    pytarget: methodName,
                    pyargs: args.SerializeArray(),
                    errorParadigm: errorParadigm);
                return -1;
            }

            using var _ = Py.GIL();
            try
            {
                var kwargs = new Py.KeywordArguments();
                foreach (var (kw, val) in kwArgs)
                {
                    kwargs[kw] = val.ToPyObject();
                }

                PyObject?[] pyArgs = [.. posArgs
                        .Select(arg => arg.ToPyObject())
                    ];

                pyArgs = TransformArguments(_object, pyArgs, kwargs);
                using var scope = _context.CreateScope();

                var target = GetExecutionTarget(_object, methodName);
                var pyRes = ExecutePythonDelegate(target, pyArgs, kwargs);


                if (pyRes.IsNone())
                {
                    result = new PyPbObject(pyRes, _context);
                    return 0;
                }

                int res = WrapPyObject(pyRes, out var obj, out var _error);

                if (_error is not null)
                {
                    error = ("Could not create object proxy: " + _error).ToPyPbErrorDefinitionString(
                        pytarget: methodName,
                        pyargs: args.SerializeArray(),
                        errorParadigm: errorParadigm);
                    return -1;
                }
                result = obj;
                return res;
            }
            catch (Exception e)
            {
                error = ($"Error when trying to invoke function {methodName}.\n" +
                    e.Message).ToPyPbErrorDefinitionString(
                    e.StackTrace,
                    pytarget: methodName,
                    pyargs: args.SerializeArray(),
                    errorParadigm: errorParadigm);
                return -1;
            }
        }

        /// <summary>
        /// Method that will be called by the dynamic methods created by <see cref="PyObjectProxyBuilder.CreateProxyFrom"/>
        /// This is the central method that all future implementations should base themselves around to facilitate debugging
        /// and replicability
        /// </summary>
        /// <param name="methodName">the name of the python method to call</param>
        /// <param name="args">[in] arguments to the python call</param>
        /// <returns><see cref="PyPbObject"/></returns>
        public PyPbObject? InvokePythonCallable(string methodName, ErrorParadigm errorParadigm = ErrorParadigm.PyPb, params object[] args)
        {
            InvokePythonCallable(methodName, out var obj, out var error, errorParadigm, args);

            if (error is not null)
            {
                throw new Exception(error);
            }

            return obj;
        }

        /// <summary>
        /// Wraps a PyObject into a PyPbObject object.
        /// 
        /// This method will do a simple wrap if the object is one of Python's native types, or 
        /// generate a proxy object with <see cref="PyObjectProxyBuilder.CreateProxyFrom{T, PyTarget}(PyPbContext, PyTarget, out T?, out string?)"/>
        /// if it's not
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="wrapped"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        protected int WrapPyObject(PyObject? obj, out PyPbObject? wrapped, out string? error) // must return simple error
        {
            error = null;
            if (obj is null)
            {
                wrapped = null;
                return 0;
            }
            var pythonClassName = obj.GetAttr("__class__").GetAttr("__name__").ToString();
            switch (pythonClassName)
            {
                /// Don't create proxy for native types, it's error prone
                case "str":
                case "int":
                case "float":
                case "bool":
                    wrapped = new PyPbObject(obj, _context);
                    return 0;
                default:
                    return PyObjectProxyBuilder.CreateProxyFrom(_context, obj, out wrapped, out error);
            }
        }


        /// <summary>
        /// Gets a member of a PyPbObject
        /// </summary>
        /// <param name="name">[in] the name of the member</param>
        /// <param name="object">[out] the member</param>
        /// <param name="error">[out] an error if one occurred</param>
        /// <returns></returns>
        public int GetMember(string name, out PyPbObject? @object, out string? error)
        {
            @object = null;
            try
            {
                using var _ = Py.GIL();
                var res = _object.GetAttr(name);

                var sc = WrapPyObject(res, out @object, out var _error);
                error = _error?.ToPyPbErrorDefinitionString(pytarget: name);
                return sc;
            }
            catch (Exception e)
            {
                error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace, pytarget: name);
                return -1;
            }
        }

        /// <summary>
        /// Get the value from a property in the current object
        /// </summary>
        /// <param name="property">the name of the property</param>
        /// <param name="value">[out] <see cref="PyObject"/> value</param>
        /// <param name="error">[out] an error if it occurred</param>
        /// <returns>0 on success</returns>
        protected int Get(string property, out PyObject? value, out string? error) // must return simple error
        {
            value = null;
            error = null;

            using var _ = Py.GIL();

            using var scope = _context.CreateScope();

            try
            {
                value = _object.GetAttr(property);
                return 0;
            }
            catch (Exception e)
            {
                error = ($"Error when trying to get object's property [{property}].\n" +
                    e.Message).ToPyPbErrorDefinitionString(e.StackTrace, pytarget: property);
                return -1;

            }
        }


        /// <summary>
        /// Get the value from a property in the current object
        /// </summary>
        /// <param name="property">the name of the property</param>
        /// <param name="value">[out] <see cref="PyObject"/> value</param>
        /// <param name="error">[out] an error if it occurred</param>
        /// <returns>0 on success</returns>
        public int Get(string property, out PyPbObject? value, out string? error)
        {
            value = null;
            var res = Get(property, out PyObject? returnVal, out var _error);

            if (res != 0)
            {
                error = _error;
                return -1;
            }

            if (returnVal is null || returnVal.IsNone())
            {
                error = "";
                return 0;
            }

            var sc = WrapPyObject(returnVal, out value, out _error);
            error = _error?.ToPyPbErrorDefinitionString(pytarget: property);
            return sc;
        }

        /// <summary>
        /// Sets the value for a property in the current object
        /// </summary>
        /// <param name="property">the property to set</param>
        /// <param name="value">the value to set. C# objects will be converted to <see cref="PyObject"/>
        /// through <see cref="PyObject.FromManagedObject(object)"/>, <see cref="PyObject"/>s will be 
        /// set as-is</param>
        /// <param name="error">an error if one occurs</param>
        /// <returns>0 on success</returns>
        public int Set(string property, object? value, out string? error)
        {
            error = null;
            PyObject? pyObj = null;

            using var _ = Py.GIL();

            try
            {
                if (value is PyPbObject pybobject) value = pybobject._object;
                if (value is not null && value is not PyObject) pyObj = PyObject.FromManagedObject(value);
                else pyObj = (PyObject?)value;

                _object.SetAttr(property, pyObj ?? PyObject.None);

                return 0;
            }
            catch (Exception e)
            {
                error = ($"Error when trying to set object's property [{property}].\n" +
                    e.Message).ToPyPbErrorDefinitionString(e.StackTrace, pytarget: property, pyargs: value?.ToString() ?? "");
                return -1;
            }
        }

        /// <summary>
        /// Invokes a function in the current object
        /// </summary>
        /// <param name="request">the <see cref="InvocationRequest"/></param>
        /// <param name="result">[out] the result of the function</param>
        /// <param name="error">[out] the error if one occurred</param>
        /// <returns>0 on success</returns>
        public int Invoke(InvocationRequest request, out PyPbObject? result, out string? error)
        {
            result = null;

            try
            {
                object[] kwargs = request.NamedArguments.Select(k => new PyKwArg(k.Key, k.Value)).ToArray() ?? [];
                var sc = InvokePythonCallable(request.TargetName, out result, out error, ErrorParadigm.PyPb, [.. request.Arguments, .. kwargs]);
                return sc;
            }
            catch (Exception e)
            {
                error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace, pytarget: request.TargetName, pyargs: request.SerializeArguments());
                return -1;
            }
        }

        /// <summary>
        /// Invokes a function defined in the current module without parameters
        /// </summary>
        /// <param name="name">the function name</param>
        /// <param name="result">[out] the function's return value</param>
        /// <param name="error">[out] the error that occurred</param>
        /// <returns>0 on success</returns>
        public int Invoke(string name, out PyPbObject? result, out string? error)
        {
            return Invoke(new InvocationRequest(name), out result, out error);
        }
    }
}
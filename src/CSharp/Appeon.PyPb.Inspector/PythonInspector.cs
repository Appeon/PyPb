using Appeon.Util;
using Python.Runtime;

namespace Appeon.PyPb.Inspector
{
    public class PythonInspector
    {
        private readonly PyPbContext context;
        internal PythonInspector(PyPbContext _context)
        {
            this.context = _context;
        }

        /// <summary>
        /// Inspects the members of a module, including functions and classes
        /// </summary>
        /// <param name="modulePath">[in] the path to the module</param>
        /// <param name="objectArr">[out] a <see cref="CustomList{T}"/> instance containing the object references to the members' info containers</param>
        /// <param name="error">[out] an error if one occurred</param>
        /// <returns>0 on success</returns>
        /// 
        public int InspectModule(string modulePath, out CustomList<PyPbObjectInfo?> objectArr, out string? error)
        {
            var res = InspectModule(modulePath, out PyPbObjectInfo[]? subObjectArr, out error);

            if (subObjectArr is null)
                objectArr = [];
            else
                objectArr = [.. subObjectArr];

            return res;
        }


        /// <summary>
        /// Inspects the members of a module, including functions and classes
        /// </summary>
        /// <param name="modulePath">[in] the path to the module</param>
        /// <param name="nameArr">[out] an array of member names</param>
        /// <param name="objectArr">[out] an array containing the <see cref="PyPbObject"/>s references to the members</param>
        /// <param name="types">[out] a string array containing the member types {function|class|other}</param>
        /// <param name="error">[out] an error if one occurred</param>
        /// <returns>0 on success</returns>
        private int InspectModule(string modulePath, out PyPbObjectInfo[]? objectArr, out string? error)
        {
            objectArr = null;
            error = null;
            using var _ = Py.GIL();
            var module = context.LoadModule(modulePath, out error);

            if (error is not null || module is null)
            {
                error = ("Could not load module: " + error).ToPyPbErrorDefinitionString(pytarget: modulePath);
                return -1;
            }

            return InspectModule(module, out objectArr, out error);
        }


        /// <summary>
        /// Inspects the members of a module, including functions and classes
        /// </summary>
        /// <param name="module">[in] the module to inspect</param>
        /// <param name="nameArr">[out] an array of member names</param>
        /// <param name="objects">[out] a <see cref="CustomList{T}"/> instance containing the object references to the members</param>
        /// <param name="types">[out] a string array containing the member types {function|class|other}</param>
        /// <param name="error">[out] an error if one occurred</param>
        /// <returns>0 on success</returns>
        public int InspectModule(PyPbModule module, out CustomList<PyPbObjectInfo>? objects, out string? error)
        {
            var res = InspectModule(module, out PyPbObjectInfo[]? subObjectArr, out error);

            if (subObjectArr is null)
                objects = [];
            else
                objects = [.. subObjectArr];

            return res;
        }


        /// <summary>
        /// Inspects the members of a module, including functions and classes
        /// </summary>
        /// <param name="modulePath">[in] the path to the module</param>
        /// <param name="nameArr">[out] an array of member names</param>
        /// <param name="objects">[out] an array containing the <see cref="PyPbObject"/>s references to the members</param>
        /// <param name="types">[out] a string array containing the member types {function|class|other}</param>
        /// <param name="error">[out] an error if one occurred</param>
        /// <returns>0 on success</returns>
        private int InspectModule(PyPbModule module, out PyPbObjectInfo[]? objects, out string? error)
        {
            return InspectModule(new PyPbObject(module.Module, context), out objects, out error);
        }

        /// <summary>
        /// Inspects the members of an object, including functions and classes
        /// </summary>
        /// <param name="module">[in] the module to inspect</param>
        /// <param name="objects">[out] a <see cref="CustomList{T}"/> instance containing the object references to the members</param>
        /// <param name="error">[out] an error if one occurred</param>
        /// <returns>0 on success</returns>
        public int InspectModule(PyPbObject module, out CustomList<PyPbObjectInfo>? objects, out string? error)
        {
            var res = InspectModule(module, out PyPbObjectInfo[]? subObjectArr, out error);

            if (subObjectArr is null)
                objects = [];
            else
                objects = [.. subObjectArr];
            return res;
        }

        /// <summary>
        /// Inspects the members of a module, including functions and classes
        /// </summary>
        /// <param name="module">[in] the module to inspect</param>
        /// <param name="objects">[out] an array containing the <see cref="PyPbObject"/>s references to the members</param>
        /// <param name="error">[out] an error if one occurred</param>
        /// <returns>0 on success</returns>
        public int InspectModule(PyPbObject module, out PyPbObjectInfo[]? objects, out string? error)
        {
            return InspectObject(module.GetPyObject(), out objects, out error);
        }

        /// <summary>
        /// Inspects the members of a module, including functions and classes
        /// </summary>
        /// <param name="obj">[in] the object to inspect</param>
        /// <param name="objects">[out] the array where the PyPbObjectInfo instances will be passed back</param>
        /// <param name="error">[out] an error if one occurred</param>
        /// <returns>0 on success</returns>
        public int InspectObject(PyPbObject obj, out PyPbObjectInfo[]? objects, out string? error)
        {
            return InspectObject(obj.GetPyObject(), out objects, out error);
        }

        /// <summary>
        /// Inspects the members of a module, including functions and classes
        /// </summary>
        /// <param name="obj">[in] the object to inspect</param>
        /// <param name="objects">[out] the array where the PyPbObjectInfo instances will be passed back</param>
        /// <param name="error">[out] an error if one occurred</param>
        /// <returns>0 on success</returns>
        public int InspectObject(PyObject obj, out PyPbObjectInfo[]? objects, out string? error)
        {
            error = null;
            objects = null;
            using var _ = Py.GIL();
            var objectsList = new List<PyPbObjectInfo>();

            try
            {
                dynamic inspect = Py.Import("inspect");
                string type;
                var res = inspect.getmembers(obj);

                var objectCount = (int)(res.__len__());
                for (int i = 0; i < objectCount; i++)
                {
                    if ((bool)inspect.isfunction(res[i][1]) || (bool)inspect.ismethod(res[i][1]) || (bool)inspect.ismethodwrapper(res[i][1]))
                        type = "function";
                    else if ((bool)inspect.isclass(res[i][1]))
                        type = "class";
                    else
                        type = "member";

                    objectsList.Add(new(new PyPbObject(res[i][1], context), (string)res[i][0], type));
                }
            }
            catch (Exception e)
            {
                error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace, pytarget: obj?.ToString() ?? string.Empty);

                return -1;
            }

            objects = [.. objectsList];
            return 0;
        }

        /// <summary>
        /// Obtains a callable object's signature
        /// </summary>
        /// <param name="function">[in] the callable <see cref="PyPbObject"/></param>
        /// <param name="signature">[out] the signature</param>
        /// <param name="error">[out] and error if one occurs</param>
        /// <returns>0 on success</returns>
        public static int GetFunctionSignature(PyPbObject function, out string? signature, out string? error)
        {
            return GetFunctionSignature(function.GetPyObject(), out signature, out error);
        }


        /// <summary>
        /// Obtains a callable object's signature
        /// </summary>
        /// <param name="function">[in] the callable <see cref="PyObject"/></param>
        /// <param name="signature">[out] the signature</param>
        /// <param name="error">[out] and error if one occurs</param>
        /// <returns>0 on success</returns>
        public static int GetFunctionSignature(PyObject function, out string? signature, out string? error)
        {
            signature = null;
            error = null;

            try
            {
                using var _ = Py.GIL();
                dynamic inspect = Py.Import("inspect");
                signature = (string)(inspect.signature(function).__str__());

            }
            catch (Exception e)
            {
                error = $"Failed to get method signature: {e.Message}".ToPyPbErrorDefinitionString(e.StackTrace, pytarget: function.ToString() ?? string.Empty);
                return -1;
            }

            return 0;
        }

        public static int GetFunctionParameters(PyPbObject function, out CustomList<ParamDef>? outParams, out string? error)
        {
            var res = GetFunctionParameters(function.GetPyObject(), out var @params, out error);
            outParams = [.. @params ?? []];
            return res;
        }

        public static int GetFunctionParameters(PyPbObject function, out ParamDef[]? parameters, out string? error)
        {
            return GetFunctionParameters(function.GetPyObject(), out parameters, out error);
        }

        public static int GetFunctionParameters(PyObject function, out ParamDef[]? parameters, out string? error)
        {
            parameters = null;
            error = null;

            using var _ = Py.GIL();

            try
            {
                var inspect = Py.Import("inspect") as PyModule;

                var dict = new PyDict();
                dict["func"] = function;
                var @params = inspect.Exec("""items = [ y for x, y in signature(func).parameters.items() ]""", dict);

                List<ParamDef> paramDefs = [];
                dynamic items = dict["items"];
                foreach (dynamic param in items)
                {
                    string? defaultValue = param.@default.__name__ == "_empty" ? null : param.ToString();
                    paramDefs.Add(new ParamDef(param.name.ToString(), param.kind.ToString(), defaultValue));
                }

                parameters = paramDefs.ToArray();
                return 0;
            }
            catch (Exception e)
            {
                error = e.Message.ToPyPbErrorDefinitionString(e.StackTrace, pytarget: function.ToString() ?? string.Empty);
                return -1;
            }
        }
    }
}

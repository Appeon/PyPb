using Python.Runtime;
using System.Security.Authentication;

namespace Appeon.PyPb;

public static class PyObjectExtensions
{
    /// <summary>
    /// Reinterprets the PyObject instance to a PyModule if the underlying Python object is of module type
    /// </summary>
    /// <param name="pyObject">the source object. It becomes invalid after the call</param>
    /// <returns>an instance of a <see cref="PyModule"/></returns>
    /// <exception cref="Exception">if the <paramref name="pyObject"/> is not a module </exception>
    public static PyModule ReinterpretAsModule(this PyObject pyObject)
    {
        if (!pyObject.GetAttr("__class__").GetAttr("__name__").ToString()!.Equals("module"))
            throw new Exception("Target object is not a module");
        var ptrField = typeof(PyObject).GetField("rawPtr", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var ptr = (IntPtr)(ptrField?.GetValue(pyObject) ?? throw new Exception("Could not get target object ptr"));
        ptrField.SetValue(pyObject, IntPtr.Zero);

        var module = new PyModule();
        var modulePtrField = typeof(PyModule).GetField("rawPtr", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        modulePtrField!.SetValue(module, ptr);

        return module.Reload();
    }
}

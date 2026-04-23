namespace Appeon.PyPb.Inspector;

public class PythonInspectorBuilder
{
    /// <summary>
    /// Initializes a python context and creates a <see cref="PythonInspector"/> instance
    /// </summary>
    /// <param name="pythonPath">[in] path to the Python Runtime DLL</param>
    /// <param name="pythonInspector">[out] a new <see cref="PythonInspector"/> instance</param>
    /// <param name="error">[out] an error if one occurred</param>
    /// <returns>0 on success</returns>
    public static int Build(string pythonPath, out PythonInspector? pythonInspector, out string? error)
    {
        pythonInspector = null;

        var context = PyPbContext.Init(pythonPath, out error);
        if (context is null)
        {
            error = "Could not initialize Python context: " + error;
            return -1;
        }

        return Build(context, out pythonInspector, out error);
    }

    /// <summary>
    /// Creates an instance of <see cref="PythonInspector"/>
    /// </summary>
    /// <param name="context">[in] the <see cref="PyPbContext"/></param>
    /// <param name="pythonInspector">[out] a new <see cref="PythonInspector"/> instance</param>
    /// <param name="error">[out] an error if one occurred</param>
    /// <returns>0 on success</returns>
    public static int Build(PyPbContext context, out PythonInspector? pythonInspector, out string? error)
    {
        error = null;
        pythonInspector = new PythonInspector(context);

        return 0;
    }
}

using Python.Runtime;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static Appeon.PyPb.Constants;

namespace Appeon.PyPb;

public static class PyPbObjectExtensions
{
    public static PyObject ToPyObject(this object obj)
    {
        return obj switch
        {
            PyPbObject pypb => pypb.GetPyObject(),
            PyObject py => py,
            null => PyObject.None,
            _ => obj.ToPython(),
        };
    }

    public static string Json(this PyPbErrorDefinition errorDefinition)
    {
        return JsonSerializer.Serialize(errorDefinition);
    }

    public static PyPbErrorDefinition ToPyPbErrorDefinition(
        this string @string,
        string stack = "",
        string pypbfunction = "",
        string arguments = "")
    {
        if (string.IsNullOrEmpty(stack))
        {
            var trace = new StackTrace(1, true);
            stack = trace.ToString();
        }

        return new(
        @string,
        stack,
        pypbfunction,
        arguments);
    }

    public static string ToPyPbErrorDefinitionString(
        this string @string,
        string? stack = null,
        string pytarget = "",
        string pyargs = "",
        ErrorParadigm errorParadigm = ErrorParadigm.PyPb)
    {

        if (errorParadigm == ErrorParadigm.Plain)
            return @string;

        if (string.IsNullOrEmpty(stack))
        {
            var trace = new StackTrace(1, true);
            stack = trace.ToString();
        }

        return new PyPbErrorDefinition(
        @string,
        stack,
        pytarget,
        pyargs).Json();
    }

    public static string SerializeArguments(this InvocationRequest req)
    {
        var sb = new StringBuilder();

        int i = 0;
        foreach (var arg in req.Arguments)
        {
            sb.Append('#');
            sb.Append(i);
            sb.Append(':');
            sb.AppendLine(arg.ToString());

            ++i;
        }

        foreach (var (name, value) in req.NamedArguments)
        {
            sb.Append(name);
            sb.Append(':');
            sb.AppendLine(value.ToString());
        }

        return sb.ToString();
    }

    public static string SerializeArray<T>(this T[] array)
    {
        return string.Join(", ", array);
    }
}

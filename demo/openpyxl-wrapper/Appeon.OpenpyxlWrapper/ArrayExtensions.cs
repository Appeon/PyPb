using Python.Runtime;

namespace Appeon.OpenpyxlWrapper;

public static class ArrayExtensions
{
    public static PyList ToPyList<T>(this T[] array)
    {
        var list = new PyList();

        foreach (var item in array)
        {
            list.Append(item.ToPython());
        }

        return list;
    }

}

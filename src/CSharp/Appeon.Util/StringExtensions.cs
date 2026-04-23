namespace Appeon.Util;

public class StringExtensions
{
    public static int Split(string str, string separator, out string[] tokens)
    {
        tokens = str.Split(separator);
        return tokens.Length;
    }
}

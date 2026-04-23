using System.Runtime.CompilerServices;

namespace Appeon.Util
{
    public class CustomList<T> : List<T>
    {
        public T At(int index) => this[index];
    }
}

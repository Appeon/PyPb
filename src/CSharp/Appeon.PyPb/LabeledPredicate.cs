namespace Appeon.PyPb;

internal class LabeledPredicate<T1>(string label, Func<T1, bool> predicate)
{
    public Func<T1, bool> Predicate { get; set; } = predicate;
    public string Label { get; set; } = label;
}

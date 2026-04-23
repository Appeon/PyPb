namespace Appeon.PyPb;

/// <summary>
/// Class defining a request for invocation of entities (e.g. functions, class instantiation)
/// </summary>
public class InvocationRequest
{
    public string TargetName { get; set; }

    /// <summary>
    /// List of arguments that will be passed to the target entity 
    /// upon invocation
    /// </summary>
    public List<object> Arguments { get; } = [];

    public Dictionary<string, object> NamedArguments { get; } = new();

    /// <param name="targetName">The name of the target</param>
    public InvocationRequest(string targetName)
    {
        TargetName = targetName;
    }


    /// <summary>
    /// Constructor overload for passing parameters at initialization
    /// </summary>
    /// <param name="targetName">the target's name</param>
    /// <param name="arguments">the list of arguments to pass to the target</param>
    public InvocationRequest(string targetName, params object[] arguments)
        : this(targetName)
    {
        Arguments = new List<object>(arguments);
    }

    /// <summary>
    /// Adds an argument to the request
    /// </summary>
    /// <param name="argument"></param>
    public void AddArgument(object argument)
    {
        Arguments.Add(argument);
    }

    /// <summary>
    /// Adds a named argument to the request. It overwrites values with the same name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public void AddNamedArgument(string name, object value)
    {
        NamedArguments[name] = value;
    }

    /// <summary>
    /// Removes a value from the Named Arguments list
    /// </summary>
    /// <param name="name"></param>
    public void RemoveNamedArgument(string name)
    {
        NamedArguments.Remove(name);
    }

    /// <summary>
    /// Clears the argument list
    /// </summary>
    public void ClearArguments()
    {
        Arguments.Clear();
        NamedArguments.Clear();
    }

}

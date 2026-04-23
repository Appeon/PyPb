namespace Appeon.PyPb;

public record PyPbErrorDefinition(string Message, string Stack, string PyPbFunction, string Arguments)
{
}

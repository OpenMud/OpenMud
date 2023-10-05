namespace OpenMud.Mudpiler.RuntimeEnvironment;

public class VerbConstraintFailureException : Exception
{
    public readonly string Reason;
    public readonly string Subject;
    public readonly string User;
    public readonly string Verb;

    public VerbConstraintFailureException(string verb, string user, string subject, string reason) :
        base($"{user} Could not interact with {subject} using verb '{verb}' because: {reason}")
    {
        Verb = verb;
        User = user;
        Subject = subject;
        Reason = reason;
    }
}
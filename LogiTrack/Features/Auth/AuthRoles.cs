namespace LogiTrack.Features.Auth;

public static class AuthRoles
{
    public const string Operator = "Operator";
    public const string Administrator = "Administrator";
}

public static class AuthPolicies
{
    public const string OperatorOrAdministrator = nameof(OperatorOrAdministrator);
    public const string Administrator = nameof(Administrator);
}

namespace JPL.NetCoreUtility.Shared.Multitenancy;

public class SprocConstants
{
    public static class DatabaseName
    {
        public const string JplAuth = "jpl_authentication";
        public const string JplUser = "jpl_user";
        public const string JplSecurity = "jpl_security";
    }

    public const string GetUsers = $"{DatabaseName.JplUser}.[User].[GetUsers]";
    public const string GetRoles = $"{DatabaseName.JplAuth}.[Identity].[GetRoles]";
    public const string GetScopes = $"{DatabaseName.JplAuth}.[Identity].[GetScopes]";
    public const string AddUserScopes = $"{DatabaseName.JplAuth}.[Identity].[AddUserScopes]";
    public const string GetUserScopes = $"{DatabaseName.JplAuth}.[Identity].[GetUserScopes]";
    public const string GetUserRoles = $"{DatabaseName.JplAuth}.[Identity].[GetUserRoles]";
    public const string DeleteUserScopes = $"{DatabaseName.JplAuth}.[Identity].[DeleteUserScopes]";
    public const string GeneratePermission = $"{DatabaseName.JplSecurity}.[Grant].[GeneratePermission]";
}
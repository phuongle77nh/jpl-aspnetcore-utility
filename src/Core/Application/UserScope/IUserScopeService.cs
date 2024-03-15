namespace JPL.NetCoreUtility.Application.UserScope;

public interface IUserScopeService : ITransientService
{
    Task<string> GenerateScopeHS(string tenantId);
    Task<List<UserScopeDto>> GetScope(string tenantId, string? roleQuery = null, string? scopeQuery = null, string? userQuery = null);
    Task<List<string>> ImportUserScope(string tenantId, List<JupEmployeeItem> jupEmpList);
    Task<string> SetScope(SetUserScopeInput input);
}
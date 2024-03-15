namespace JPL.NetCoreUtility.Application.Grant;

public interface IGrantService : ITransientService
{
    Task<List<ServiceDto>> ExportService(string serviceName);
    Task<string> ImportService(List<ServiceDto> services);
    Task<string> DeleteService(string serviceName);
    Task<List<string>> GeneratePermission(string serviceName);
    Task<List<PermissionDto>> GetListPermission();
    Task<List<string>> GeneratePermissionForEntity(string serviceName, string tableSchema, string tableName, string userEmail = "", string tenantId = "restaff");
}
using JPL.NetCoreUtility.Domain.Grant;
using System.Text.Json.Serialization;

namespace JPL.NetCoreUtility.Application.Grant;

public class BaseDto
{
    [JsonIgnore]
    public Guid Id { get; set; }
}

public class PermissionDto
{
    public int Value { get; set; }
    public string PermissionName { get; set; } = string.Empty;
}

public class ServiceDto : BaseDto
{
    public ServiceDto()
    {
        ServiceEntities = new List<ServiceEntityDto>();
    }

    public string ServiceName { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;

    public virtual List<ServiceEntityDto> ServiceEntities { get; set; }
}

public class ServiceEntityDto : BaseDto
{
    public ServiceEntityDto()
    {
        SecurityAttributes = new List<SecurityAttributeDto>();
        Policies = new List<PolicyDto>();
    }

    public string TableName { get; set; } = string.Empty;
    public string TableSchema { get; set; } = string.Empty;
    public string PrimaryColumnName { get; set; } = string.Empty;

    public string MyselfQuery { get; set; } = string.Empty;

    [JsonIgnore]
    public Guid ServiceId { get; set; }

    public virtual List<SecurityAttributeDto> SecurityAttributes { get; set; }
    public virtual List<PolicyDto> Policies { get; set; }
}

public class SecurityAttributeDto : BaseDto
{
    public string AttributeName { get; set; } = string.Empty;
    public string AttributeQuery { get; set; } = string.Empty;
}

public class PolicyLinkDto : BaseDto
{
    [JsonIgnore]
    public Guid PolicyId { get; set; }
    [JsonIgnore]
    public Guid EntityId { get; set; }
    public EntityTypeEnum EntityType { get; set; }
    public string EntityFormat { get; set; } = string.Empty;
    public string EntityValue { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;

}

public class PolicyDto : BaseDto
{
    public string PolicyName { get; set; } = string.Empty;
    public int Permission { get; set; }
    [JsonIgnore]
    public Guid ServiceEntityId { get; set; }
    public virtual List<PolicyLinkDto> PolicyLinks { get; set; }

    public PolicyDto()
    {
        PolicyLinks = new List<PolicyLinkDto>();
    }
}
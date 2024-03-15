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
    public string PermissionName { get; set; }
}

public class ServiceDto : BaseDto
{
    public string ServiceName { get; set; }
    public string Database { get; set; }

    public virtual List<ServiceEntityDto> ServiceEntities { get; set; }
}

public class ServiceEntityDto : BaseDto
{
    public ServiceEntityDto()
    {
        SecurityAttributes = new List<SecurityAttributeDto>();
        Policies = new List<PolicyDto>();
    }
    public string TableName { get; set; }
    public string TableSchema { get; set; }
    public string PrimaryColumnName { get; set; }

    public string MyselfQuery { get; set; }

    [JsonIgnore]
    public Guid ServiceId { get; set; }

    public virtual List<SecurityAttributeDto> SecurityAttributes { get; set; }
    public virtual List<PolicyDto> Policies { get; set; }
}

public class SecurityAttributeDto : BaseDto
{
    public string AttributeName { get; set; }
    public string AttributeQuery { get; set; }
}

public class PolicyLinkDto : BaseDto
{
    [JsonIgnore]
    public Guid PolicyId { get; set; }
    [JsonIgnore]
    public Guid EntityId { get; set; }
    public EntityTypeEnum EntityType { get; set; }
    public string EntityFormat { get; set; }
    public string EntityValue { get; set; }
    public string TenantId { get; set; }
    public string AttributeName { get; set; }

}

public class PolicyDto : BaseDto
{
    public string PolicyName { get; set; }
    public int Permission { get; set; }
    [JsonIgnore]
    public Guid ServiceEntityId { get; set; }
    public virtual List<PolicyLinkDto> PolicyLinks { get; set; }

}
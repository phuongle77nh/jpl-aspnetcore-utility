// See https://aka.ms/new-console-template for more information

using System.ComponentModel.DataAnnotations.Schema;

public class HolidayType
{
    public string TenantId { get; set; }

    /// <summary>
    /// Name of leave type.
    /// </summary>
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public HolidayType()
    {
    }
}

public enum GroupCriteriaType
{
    MatchAllCriteria,
    SpecificEmployee
}
public enum CriteriaOption
{
    Gender_Is_Male,
    Gender_Is_Female,
    UserType_Is_Employee,
    User_IsNot_Out
}
/// <summary>
/// Group - Setting to filter employees.
/// </summary>
public class Group
{
    public string? GroupName { get; set; }
    public bool IsAutoTrigger { get; set; }
    public GroupCriteriaType GroupCriteriaType { get; set; }

    public virtual List<GroupCriteria> GroupCriterias { get; set; } = default!;
    public virtual List<GroupMember> GroupMembers { get; set; } = default!;
    public virtual List<GroupMember> ExceptMembers { get; set; } = default!;
}

public class GroupCriteria
{
    public Guid GroupId { get; set; }

    public CriteriaOption CriteriaOption { get; set; }
}

public class GroupMember
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
}
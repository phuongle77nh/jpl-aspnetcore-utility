// See https://aka.ms/new-console-template for more information
using System.Text.Json;

//GenerateHolidayTypes();
GenerateGroupData();

static void GenerateHolidayTypes()
{
    List<string> types = new List<string>()
    {
        "New Year's Day",
        "Tet Holiday",
        "Hung Kings Festival",
        "Reunification Day",
        "International Labor Day",
        "Vietnam National Day",
        "National Day Holiday"
    };

    var items = new List<HolidayType>();
    foreach (string type in types)
    {
        var item = new HolidayType();
        item.Name = type;
        item.TenantId = "tenant";
        item.IsActive = true;
        items.Add(item);

    }

    string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
}

static void GenerateGroupData()
{
    var items = new List<Group>
    {
        new Group
        {
            GroupCriterias = new List<GroupCriteria>
            {
                new GroupCriteria()
            },
            GroupCriteriaType = GroupCriteriaType.MatchAllCriteria,
            GroupName = "All employees",
            IsAutoTrigger = true,
            GroupMembers = new List<GroupMember> { new GroupMember()},
            ExceptMembers = new List<GroupMember> { new GroupMember() }
        }
    };
    string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);

}
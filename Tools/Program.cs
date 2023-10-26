// See https://aka.ms/new-console-template for more information
using System.Text.Json;

GenerateHolidayTypes();

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
        item.TenantId = "restaff";
        item.IsActive = true;
        items.Add(item);

    }

    string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
}


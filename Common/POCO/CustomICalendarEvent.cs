using Ical.Net.DataTypes;
using System.Text.Json.Serialization;

namespace DBotDDRefuge.Common.POCO;

/// <summary>
/// 自定義 ICal 事件類別
/// </summary>
public class CustomICalendarEvent
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("startTime")]
    public IDateTime? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public IDateTime? EndTime { get; set; }
}
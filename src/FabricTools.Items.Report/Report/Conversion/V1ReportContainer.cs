using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using navidataIO.Utils.Json;

namespace FabricTools.Items.Report.Conversion;

/// <summary>
/// A container for the individual components of the V1 legacy <c>v1MajorReport.json</c> document.
/// </summary>
public class V1ReportContainer(V1MajorReportObject report)
{
    public V1MajorReportObject Report => report;

    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Ignore,
    };

    internal static class Properties
    {
        public const string Config = "config";
        public const string Filters = "filters";
        public const string Sections = "sections";
        public const string VisualContainers = "visualContainers";
    }

    /// <summary>
    /// Creates a new instance of the <see cref="V1ReportContainer"/> class from a JSON object.
    /// </summary>
    public static V1ReportContainer FromJson(JObject json) => 
        new(DeserializeObject(json, V1MajorReportObjectType.Report, Properties.Sections));

    private static V1MajorReportObject DeserializeObject(JObject json, V1MajorReportObjectType type,
        string? childCollection)
    {
        var config = JToken.Parse(json[Properties.Config]!.Value<string>()!) as JObject;
        var filters = json[Properties.Filters] is JValue { Type: JTokenType.String } token
            ? JArray.Parse(token.Value<string>()!)
            : null;
        var children = childCollection is not null && json[childCollection] is JArray array
            ? array.Select(t => DeserializeObject((t as JObject)!,
                    type == V1MajorReportObjectType.Report ? V1MajorReportObjectType.Page : V1MajorReportObjectType.Visual,
                    type == V1MajorReportObjectType.Report ? Properties.VisualContainers : null))
                .ToArray()
            : null;

        return new V1MajorReportObject(type, 
            json.RemoveProperties(Properties.Config, Properties.Filters, Properties.Sections, Properties.VisualContainers), 
            config!,
            filters,
            children);
    }

    /// <summary>
    /// Exports the report object to a (legacy) <c>report.json</c> JSON object.
    /// </summary>
    public JObject Export() => SerializeObject(report, V1MajorReportObjectType.Report, Properties.Sections);

    private static JObject SerializeObject(V1MajorReportObject obj, V1MajorReportObjectType expectedType,
        string? childCollection)
    {
        var result = obj.Base;
        obj.Type.AssertEquals(expectedType);

        result[Properties.Config] = JsonConvert.SerializeObject(obj.Config, JsonSerializerSettings);
        result[Properties.Filters] = JsonConvert.SerializeObject(obj.Filters ?? new JArray(), JsonSerializerSettings);

        if (childCollection is not null)
        {
            result[childCollection] = new JArray((obj.Children ?? []).Select(
                child => SerializeObject(child,
                    expectedType == V1MajorReportObjectType.Report 
                        ? V1MajorReportObjectType.Page 
                        : V1MajorReportObjectType.Visual, 
                    expectedType == V1MajorReportObjectType.Report
                        ? Properties.VisualContainers
                        : null))
            );
        }

        return result;
    }
}
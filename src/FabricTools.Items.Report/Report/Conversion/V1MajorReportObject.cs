using Newtonsoft.Json.Linq;

namespace FabricTools.Items.Report.Conversion;

/// <summary>
/// An enumeration of the types of major objects in the legacy report format.
/// </summary>
public enum V1MajorReportObjectType
{
    /// <summary>
    /// The report object type.
    /// </summary>
    Report,
    /// <summary>
    /// The page object type.
    /// </summary>
    Page,
    /// <summary>
    /// The visual object type.
    /// </summary>
    Visual
}

/// <summary>
/// A container for the individual JSON components of a legacy report object definition.
/// </summary>
/// <param name="Type">The object type.</param>
/// <param name="Base">The base JSON object.</param>
/// <param name="Config">The config JSON object.</param>
/// <param name="Filters">The optional filters array.</param>
/// <param name="Children">The collection of child objects. Only applies to report and pages.</param>
public record V1MajorReportObject(
    V1MajorReportObjectType Type, 
    JObject Base, 
    JObject Config, 
    JArray? Filters, 
    V1MajorReportObject[]? Children = null);
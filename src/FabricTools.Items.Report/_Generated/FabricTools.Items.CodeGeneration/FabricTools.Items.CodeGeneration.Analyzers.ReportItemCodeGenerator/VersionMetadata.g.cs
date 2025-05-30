﻿//----------------------
// <auto-generated>
//     Generated using FabricTools.Items.CodeGeneration 1.1.0+Branch.main.Sha.20b425801c5e5ce25afea6ff55befe43818af959.20b425801c5e5ce25afea6ff55befe43818af959, NJsonSchema: 11.2.0.0 (Newtonsoft.Json v13.0.0.0) (http://github.com/FabricTools)
// </auto-generated>
//----------------------


#nullable enable


namespace FabricTools.Items.Report.Definitions;

#pragma warning disable // Disable all warnings

/// <summary>
/// Defines version information about the report definition.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("FabricTools.Items.CodeGeneration", "1.1.0+Branch.main.Sha.20b425801c5e5ce25afea6ff55befe43818af959.20b425801c5e5ce25afea6ff55befe43818af959, NJsonSchema: 11.2.0.0 (Newtonsoft.Json v13.0.0.0)")]
[FabricTools.Items.ComponentModel.SourceSchema("https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json")]
public partial class VersionMetadata
{
    /// <summary>
    /// Defines the schema to use for an item.
    /// </summary>
    [Newtonsoft.Json.JsonProperty("$schema", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
    public string Schema { get; set; } = "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json";

    /// <summary>
    /// Defines the report definition version, format of version is major.minor.patch
    /// <br/>- major: &gt;=1
    /// <br/>- minor: &gt;=0
    /// <br/>- patch: always 0
    /// </summary>
    [Newtonsoft.Json.JsonProperty("version", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^[1-9][0-9]*\.(0|[1-9][0-9]*)\.0$")]
    public string Version { get; set; } = default!;


}
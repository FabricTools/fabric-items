// Copyright (c) 2024 navidata.io Corp

using Newtonsoft.Json.Linq;

namespace FabricTools.Items.Report;

internal interface IPbirDocumentNode
{
    /// <summary>
    /// The relative path of the document. Must be set relative to the parent document.
    /// </summary>
    RelativeFilePath? Path { get; set; }

    /// <summary>
    /// The document's parent document. Used to build the document hierarchy.
    /// </summary>
    IPbirDocumentNode? Parent { get; set; }
}

internal interface IPbirDocument : IPbirDocumentNode
{

    /// <summary>
    /// Gets or sets the original JSON representation of the document.
    /// </summary>
    JObject? OriginalJson { get; set; }

    /// <summary>
    /// Gets or sets the document's schema id.
    /// </summary>
    string Schema { get; set; }

    /// <summary>
    /// Gets the default schema id for the document type.
    /// This corresponds to the schema version the C# object model was generated from.
    /// </summary>
    string GetDefaultSchema();

}
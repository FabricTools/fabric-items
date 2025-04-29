// Copyright (c) 2024 navidata.io Corp

using Newtonsoft.Json.Linq;

namespace FabricTools.Items.Report;

using Definitions;

/// <summary>
/// Extensions for <see cref="IPbirDocumentNode"/> and <see cref="IPbirDocument"/> types.
/// </summary>
internal static class PbirDocumentExtensions
{
    /// <summary>
    /// Sets the <see cref="IPbirDocumentNode.Path"/> of a PBIR document.
    /// For internal use only.
    /// </summary>
    public static T SetPath<T>(this T document, RelativeFilePath? path)
        where T : IPbirDocumentNode
    {
        document.Path = path;
        return document;
    }


    /// <summary>
    /// Sets the <see cref="IPbirDocumentNode.Parent"/> of a PBIR document.
    /// For internal use only.
    /// </summary>
    public static T? SetParent<T>(this T? document, IPbirDocumentNode? parent)
        where T : IPbirDocumentNode
    {
        if (document is not null)
            document.Parent = parent;
        return document;
    }


    /// <summary>
    /// Sets the <see cref="IPbirDocument.OriginalJson"/> of a PBIR document.
    /// For internal use only.
    /// </summary>
    public static T SetJson<T>(this T document, JObject json)
        where T : IPbirDocument
    {
        document.OriginalJson = json;
        return document;
    }

    /// <summary>
    /// Returns the <see cref="IPbirDocument.OriginalJson"/> of a PBIR document.
    /// For internal use only.
    /// </summary>
    public static JObject? GetJson<T>(this T document) where T : IPbirDocument => document.OriginalJson;


    /// <summary>
    /// Returns the combined path of the document, relative to the root of the project.
    /// </summary>
    /// <exception cref="InvalidOperationException">The document, or any of its parents, has a missing <see cref="IPbirDocumentNode.Path"/>.</exception>
    public static RelativeFilePath GetEffectivePath(this IPbirDocumentNode doc)
    {
        if (doc.Path is null && doc is Bookmark bookmark)
            doc.Path = $"{bookmark.Name}.bookmark.json"; // TODO Check for null Name?

        if (doc.Path is null)
            throw new InvalidOperationException("The document has no path.");

        return doc.Parent switch
        {
            null => doc.Path.Value,
            Page => doc.Parent.GetEffectivePath().RemoveLast() /* remove "/page.json" */ + doc.Path.Value,
            VisualContainer => doc.Parent.Parent!.GetEffectivePath() + doc.Path.Value,
            _ => doc.Parent.GetEffectivePath() + doc.Path.Value
        };
    }

}

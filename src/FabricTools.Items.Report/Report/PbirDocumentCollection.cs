// Copyright (c) 2024 navidata.io Corp

using System.Collections;

// ReSharper disable NotDisposedResourceIsReturned

namespace FabricTools.Items.Report;

/// <summary>
/// An <see cref="IPbirDocuments{TDocument}"/> collection with a metadata document of type <typeparamref name="TMetadata"/>.
/// </summary>
public interface IPbirDocumentCollection<TDocument, out TMetadata> : IPbirDocuments<TDocument>
{
    /// <summary>
    /// Gets the metadata for the document collection.
    /// </summary>
    TMetadata? Metadata { get; }
}

/// <summary>
/// A generic collection of documents.
/// </summary>
public interface IPbirDocuments<T> : ICollection<T>
{
    /// <summary>
    /// Finds a document in the collection that matches the specified predicate.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when no document matches the predicate.</exception>
    T Find(Func<T, bool> predicate);
}


internal class PbirDocumentCollection<TDocument, TMetadata>(string relativePath)
    : PbirDocuments<TDocument>(relativePath), IPbirDocumentCollection<TDocument, TMetadata>
    where TDocument : class, IPbirDocument
    where TMetadata : class, IPbirDocument
{
    private TMetadata? _metadata;

    /// <inheritdocs/>
    public TMetadata? Metadata {
        get => _metadata;
        set => _metadata = value.SetParent(this);
    }
}

internal class PbirDocuments<TDocument> : IPbirDocuments<TDocument>, IPbirDocumentNode
    where TDocument : class, IPbirDocument
{
    private readonly ICollection<TDocument> _documents = [];

    #region IPbirDocument ~ only Path and Parent are used
    RelativeFilePath? IPbirDocumentNode.Path { get; set; }
    IPbirDocumentNode? IPbirDocumentNode.Parent { get; set; }
    #endregion

    public PbirDocuments(RelativeFilePath relativePath, IPbirDocument? parent = null)
    {
        this.SetPath(relativePath);
        if (parent is not null) this.SetParent(parent);
    }

    /// <inheritdocs/>
    public TDocument Find(Func<TDocument, bool> predicate) => _documents.FirstOrDefault(predicate) switch
    {
        null => throw new KeyNotFoundException(),
        var document => document
    };

    #region ICollection<TDocument> implementation

    /// <inheritdocs/>
    public int Count => _documents.Count;

    /// <inheritdocs/>
    public bool IsReadOnly => _documents.IsReadOnly;

    /// <inheritdocs/>
    public void Add(TDocument item)
    {
        (item as IPbirDocument).SetParent(this);
        _documents.Add(item);
    }

    /// <inheritdocs/>
    public void Clear()
    {
        _documents.Clear();
        // TODO Un-set parent on each item?
    }

    /// <inheritdocs/>
    public bool Contains(TDocument item) => _documents.Contains(item);

    /// <inheritdocs/>
    public void CopyTo(TDocument[] array, int arrayIndex)
    {
        _documents.CopyTo(array, arrayIndex);
    }

    /// <inheritdocs/>
    public IEnumerator<TDocument> GetEnumerator() => _documents.GetEnumerator();

    /// <inheritdocs/>
    public bool Remove(TDocument item) => _documents.Remove(item);

    /// <inheritdocs/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}

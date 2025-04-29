// Copyright (c) 2024 navidata.io Corp

namespace FabricTools.Items.ComponentModel;

/// <summary>
/// A component that has annotations.
/// </summary>
public interface IHasAnnotations
{
    /// <summary>
    /// Enumerates the component's annotations.
    /// </summary>
    IEnumerable<Annotation> Annotations { get; }

    /// <summary>
    /// Gets the keys of all annotations.
    /// </summary>
    IEnumerable<string> AnnotationKeys { get; }

    /// <summary>
    /// Tries to get the value of an annotation with the specified key.
    /// </summary>
    bool TryGetAnnotation(string key, out string? value);

    /// <summary>
    /// Gets or sets the value of an annotation with the specified key.
    /// </summary>
    string this[string key] { get; set; }
}

/// <summary>
/// A Json document annotation.
/// </summary>
public record Annotation(string Key, string Value);

/// <summary>
/// An implementation of <see cref="IHasAnnotations"/> that provides a base implementation for exposing annotations.
/// </summary>
/// <typeparam name="TAnnotation">The annotation type.</typeparam>
public abstract class HasAnnotations<TAnnotation> : IHasAnnotations
{
#pragma warning disable CS1591

    protected abstract ICollection<TAnnotation>? GetAnnotations();
    protected abstract ICollection<TAnnotation> EnsureAnnotations();
    protected abstract string GetKey(TAnnotation annotation);
    protected abstract Annotation Map(TAnnotation annotation);
    protected abstract TAnnotation Convert(Annotation annotation);
    protected abstract void UpdateAnnotation(TAnnotation annotation, string value);

#pragma warning restore CS1591

    /// <inheritdocs/>
    IEnumerable<Annotation> IHasAnnotations.Annotations => (GetAnnotations() ?? Enumerable.Empty<TAnnotation>()).Select(Map);

    /// <inheritdocs/>
    [Newtonsoft.Json.JsonIgnore]
    public IEnumerable<string> AnnotationKeys => GetAnnotations() switch
    {
        null => [],
        var annotations => annotations.Select(GetKey)
    };

    /// <inheritdocs/>
    public bool TryGetAnnotation(string key, out string? value)
    {
        if (GetAnnotations() is { } annotations && annotations.FirstOrDefault(a => GetKey(a) == key) is Annotation annotation)
        {
            value = annotation.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <inheritdocs/>
    [Newtonsoft.Json.JsonIgnore]
    string IHasAnnotations.this[string key] {
        get => Map(GetAnnotations() switch 
        {
            { } annotations when annotations.FirstOrDefault(a => GetKey(a) == key) is { } annotation => annotation,
            _ => throw new KeyNotFoundException($"No annotation with key '{key}' found.")
        }).Value;
        set
        {
            if (GetAnnotations() is { } annotations && annotations.FirstOrDefault(a => GetKey(a) == key) is { } annotation)
            {
                UpdateAnnotation(annotation, value);
            }
            else
            {
                EnsureAnnotations().Add(Convert(new Annotation(key, value)));
            }
        }
    }

}

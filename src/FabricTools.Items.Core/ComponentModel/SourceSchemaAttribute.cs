// Copyright (c) 2024 navidata.io Corp

namespace FabricTools.Items.ComponentModel;

/// <summary>
/// Contains metadata about the source schema of the generated type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SourceSchemaAttribute(string id, string? path = null) : Attribute
{
    /// <summary>
    /// The schema <c>$id</c>.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// The path within the source schema to the current code element.
    /// Does not apply if a type is annotated and the type corresponds to a root schema.
    /// </summary>
    public string? Path { get; } = path;
}
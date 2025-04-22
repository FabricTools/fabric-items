// Copyright (c) 2024 navidata.io Corp

using Newtonsoft.Json;
using AnyOfTypes.Newtonsoft.Json;

namespace FabricTools.Items.Json;

/// <summary>
/// A <see cref="AnyOfJsonConverter"/> that unwraps <see cref="Nullable{T}"/> instances.
/// </summary>
public class NullableAnyOfJsonConverter : JsonConverter
{
    private readonly AnyOfJsonConverter _converter = new();

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var rawValue = value switch
        {
            // Unwrap Nullable<AnyOf<,,>>:
            _ when value?.GetType().GetGenericTypeDefinition() == typeof(Nullable<>)
                => value.GetType().GetProperty("Value")?.GetValue(value),
            _
                => value
        };
        _converter.WriteJson(writer, rawValue!, serializer);
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var effectiveObjectType = objectType switch
        {
            // Unwrap Nullable<AnyOf<,,>>:
            _ when objectType.GetGenericTypeDefinition() == typeof(Nullable<>) => objectType.GenericTypeArguments[0],
            _ => objectType
        };
        return _converter.ReadJson(reader, effectiveObjectType, existingValue!, serializer);
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => _converter.CanConvert(objectType)
                                                        || (Nullable.GetUnderlyingType(objectType) is {} t && _converter.CanConvert(t));

}
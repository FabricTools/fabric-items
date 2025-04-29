// Copyright (c) 2024 navidata.io Corp

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Nelibur.ObjectMapper;
using AnyOfTypes.System.Text.Json.Extensions;
using AnyOfTypes.System.Text.Json.Matcher;
using AnyOfTypes.System.Text.Json.Matcher.Models;

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
        try
        {
            return _converter.ReadJson(reader, effectiveObjectType, existingValue!, serializer);
        }
        catch (Exception e)
        {
            var currentPath = reader.Path;
            var targetType = effectiveObjectType.ToString();

            // TODO Create descriptive exception message
            throw;
        }
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => _converter.CanConvert(objectType)
                                                        || (Nullable.GetUnderlyingType(objectType) is {} t && _converter.CanConvert(t));

}

public class AnyOfJsonConverter(bool ignoreCase = true) : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        var currentValue = value.GetNullablePropertyValue("CurrentValue");
        if (currentValue is null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        serializer.Serialize(writer, currentValue);
    }

    /// <summary>
    /// See
    /// - https://stackoverflow.com/questions/8030538/how-to-implement-custom-jsonconverter-in-json-net
    /// - https://stackoverflow.com/a/59286262/255966
    /// </summary>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        object? value;
        switch (reader.TokenType)
        {
            case JsonToken.Null:
                value = null;
                break;

            case JsonToken.StartObject:
                value = FindBestObjectMatch(reader, objectType.GetGenericArguments() ?? [], serializer);
                break;

            case JsonToken.StartArray:
                value = FindBestArrayMatch(reader, objectType, existingValue, serializer);
                break;

            default:
                value = GetSimpleValue(reader, existingValue);
                break;
        }

        if (value is null)
        {
            return Activator.CreateInstance(objectType);
        }

        return Activator.CreateInstance(objectType, value);
    }

    private static object? GetSimpleValue(JsonReader reader, object? existingValue)
    {
        var jValue = new JValue(reader.Value);

        object? value;
        switch (reader.TokenType)
        {
            case JsonToken.String:
                value = (string)jValue!;
                break;

            case JsonToken.Date:
                value = (DateTime)jValue;
                break;

            case JsonToken.Boolean:
                value = (bool)jValue;
                break;

            case JsonToken.Integer:
                value = (int)jValue;
                break;

            case JsonToken.Float:
                value = (double)jValue;
                break;

            default:
                value = jValue.Value;
                break;
        }

        if (value is null)
        {
            return existingValue;
        }

        return value;
    }

    private object? FindBestArrayMatch(JsonReader reader, Type? typeToConvert, object? existingValue, JsonSerializer serializer)
    {
        var enumerableTypes = typeToConvert?.GetGenericArguments().Where(t => t.IsAssignableFromIEnumerable()).ToArray() ?? [];
        var elementTypes = enumerableTypes.Select(t => t.GetElementTypeX()).ToArray();

        var list = new List<object?>();
        Type? elementType = null;

        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            object? value;
            if (reader.TokenType == JsonToken.StartObject)
            {
                value = FindBestObjectMatch(reader, elementTypes, serializer);
            }
            else
            {
                value = GetSimpleValue(reader, existingValue);
            }

            if (elementType is null)
            {
                // Type of the first element
                elementType = value?.GetType();
            }

            list.Add(value);
        }

        if (elementType is null)
        {
            // Type could not be determined
            return null;
        }

        var typedListDetails = list.CastToTypedList(elementType);

        foreach (var knownIEnumerableType in enumerableTypes)
        {
            if (knownIEnumerableType.GetElementTypeX() == elementType)
            {
                try
                {
                    TinyMapper.Bind(typedListDetails.ListType, knownIEnumerableType);
                    return TinyMapper.Map(typedListDetails.ListType, knownIEnumerableType, typedListDetails.List);
                }
                catch
                {
                    // The value "System.Object" is not of type
                    // "System.Collections.Generic.KeyValuePair`2[System.String,System.Collections.ObjectModel.Collection`1[FabricTools.Items.Report.Definitions.DataRepetitionSelector]]"
                    // and cannot be used in this generic collection. (Parameter 'value')
                    return typedListDetails.List;
                }
            }
        }

        return null;
    }

    private object? FindBestObjectMatch(JsonReader reader, Type[] types, JsonSerializer serializer)
    {
        var properties = new List<PropertyDetails>();
        var jObject = JObject.Load(reader);
        foreach (var element in jObject)
        {
            var propertyDetails = new PropertyDetails
            {
                CanRead = true,
                CanWrite = true,
                IsPublic = true,
                Name = element.Key
            };

            var val = element.Value!.ToObject<object?>();
            propertyDetails.PropertyType = val?.GetType();
            propertyDetails.IsValueType = val?.GetType().GetTypeInfo().IsValueType == true;

            properties.Add(propertyDetails);
        }

        var bestType = MatchFinder.FindBestType(ignoreCase, properties, types);
        if (bestType is not null)
        {
            var target = Activator.CreateInstance(bestType);

            using (JsonReader jObjectReader = CopyReaderForObject(reader, jObject))
            {
                serializer.Populate(jObjectReader, target);
            }

            return target;
        }

        return null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.FullName?.StartsWith("AnyOfTypes.AnyOf`") == true;
    }

    private static JsonReader CopyReaderForObject(JsonReader reader, JObject jObject)
    {
        var jObjectReader = jObject.CreateReader();
        jObjectReader.CloseInput = reader.CloseInput;
        jObjectReader.Culture = reader.Culture;
        jObjectReader.DateFormatString = reader.DateFormatString;
        jObjectReader.DateParseHandling = reader.DateParseHandling;
        jObjectReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
        jObjectReader.FloatParseHandling = reader.FloatParseHandling;
        jObjectReader.MaxDepth = reader.MaxDepth;
        jObjectReader.SupportMultipleContent = reader.SupportMultipleContent;
        return jObjectReader;
    }
}

// Copyright (c) 2024 navidata.io Corp

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FabricTools.Items.Json;

/// <summary>
/// A <see cref="StringEnumConverter"/> that serializes enum values as integers if their name is prefixed with an underscore.
/// </summary>
public class EnumJsonConverter : StringEnumConverter
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is Enum enumValue)
        {
            var enumType = enumValue.GetType();
            var intValue = Convert.ToInt32(value);

            if (Enum.GetName(enumType, value) is { } name && name.StartsWith("_") && name[1..] == intValue.ToString())
            {
                serializer.Serialize(writer, intValue);
                return;
            }
        }

        base.WriteJson(writer, value, serializer);
    }
}
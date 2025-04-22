// Copyright (c) 2024 navidata.io Corp

using Newtonsoft.Json;

namespace FabricTools.Items.Json;

/// <summary>
/// A <see cref="JsonConverter{T}"/> that serializes <see cref="double"/> values as integers if they are whole numbers.
/// </summary>
public class DoubleValueJsonConverter : JsonConverter<double>
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, double value, JsonSerializer serializer)
    {
        if (Math.Abs(value % 1) <= (double.Epsilon * 100)) // ref: https://stackoverflow.com/a/2751597/736263
        {
            serializer.Serialize(writer, (int)value);
        }
        else
        {
            //serializer.Serialize(writer, value); --> _MUST NOT_ use serializer as that would cause infinite recursion
            writer.WriteValue(value);
        }
    }

    /// <inheritdoc />
    public override double ReadJson(JsonReader reader, Type objectType, double existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool CanRead => false;
}
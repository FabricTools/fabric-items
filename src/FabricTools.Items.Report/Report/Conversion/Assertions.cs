namespace FabricTools.Items.Report.Conversion;

internal static class Assertions
{
    public static T? AssertEquals<T>(this T? value, T expectedValue)
    {
        if (expectedValue is IEquatable<T> v)
        {
            // IEquatable<T>.Equals()
            if (!v.Equals(value!)) throw new InvalidOperationException($"Did not receive expected value: {expectedValue}");
        }
        else
        {
            // object.Equals()
            if (!Equals(expectedValue, value)) throw new InvalidOperationException($"Did not receive expected value: {expectedValue}");
        }
        return value;
    }

}
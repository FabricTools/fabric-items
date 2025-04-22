// Copyright (c) 2024 navidata.io Corp

using Newtonsoft.Json;
using FabricTools.Items.Json;

namespace FabricTools.Items.Report.Tests;

public class EnumSerializationTests
{
    private readonly EnumJsonConverter _converter = new EnumJsonConverter();

    public enum ArithmeticOperatorKind
    {

        /// <summary>Add</summary>
        _0 = 0,

        /// <summary>Subtract</summary>
        _1 = 1,

        /// <summary>Multiple</summary>
        _2 = 2,

        /// <summary>Divide</summary>
        _3 = 3,

    }

    [Fact]
    public void CanSerializeIntEnum()
    {
        var json = JsonConvert.SerializeObject(
            new
            {
                Operator = ArithmeticOperatorKind._1
            }
            , Formatting.Indented
            , _converter);

        Assert.Contains("\"Operator\": 1", json);
    }
}
// Copyright (c) 2024 navidata.io Corp

using System.Collections.ObjectModel;
using AnyOfTypes;
using Newtonsoft.Json;
using FabricTools.Items.Json;
using Newtonsoft.Json.Linq;

namespace FabricTools.Items.Report.Tests;

public class AnyOfTests
{
    private readonly JsonSerializer _serializer = new();

    public AnyOfTests()
    {
        _serializer.Converters.Add(new NullableAnyOfJsonConverter());
    }

    public class Type1
    {
        public int Foo { get; set; }
    }

    public class Type2
    {
        public string Bar { get; set; }
    }

    public class Type3
    {
        public string Name { get; set; }
    }

    public class Type4
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    public class Type5
    {
        public AnyOfTypes.AnyOf<Type3, Type4>? Union { get; set; }
    }

    [Fact]
    public void AnyOf()
    {
        AnyOf<Type1, Type2> x = new Type1 { Foo = 12 };
        Assert.True(x.IsFirst);
    }

    [Fact]
    public void CanDeserializeType1()
    {
        var json = """
            {
                "Foo": 12
            }
            """;

        using var reader = new JsonTextReader(new StringReader(json));
        var result = _serializer.Deserialize<AnyOf<Type1, Type2>>(reader);

        Assert.True(result.IsFirst);
    }

    [Fact]
    public void CanDeserializeType2()
    {
        var json = """
            {
                "Bar": "value"
            }
            """;

        using var reader = new JsonTextReader(new StringReader(json));
        var result = _serializer.Deserialize<AnyOf<Type1, Type2>>(reader);

        Assert.True(result.IsSecond);
    }

    [Fact]
    public void CanDeserializeType4()
    {
        const string json = """
                            {
                                "name": "Name",
                                "number": 12
                            }
                            """;

        using var reader = new JsonTextReader(new StringReader(json));
        var result = _serializer.Deserialize<AnyOf<Type3, Type4>>(reader);

        Assert.True(result.IsSecond);
        Assert.Equal(12, result.Second.Number);
    }

    [Fact]
    public void CanDeserializeType5_null()
    {
        const string json = """
                            {
                            }
                            """;

        using var reader = new JsonTextReader(new StringReader(json));
        var result = _serializer.Deserialize<Type5>(reader);

        Assert.NotNull(result);
        Assert.Null(result.Union);
    }

    [Fact]
    public void CanDeserializeType5_First()
    {
        const string json = """
                            {
                                "union": {
                                    "name": "Name"
                                }
                            }
                            """;

        using var reader = new JsonTextReader(new StringReader(json));
        var result = _serializer.Deserialize<Type5>(reader);

        Assert.NotNull(result);
        Assert.True(result.Union!.Value.IsFirst);
        Assert.Equal("Name", result.Union.Value.First.Name);
    }

    [Fact]
    public void CanDeserializeType5_Second()
    {
        const string json = """
                            {
                                "union": {
                                    "name": "Name",
                                    "number": 12
                                }
                            }
                            """;

        using var reader = new JsonTextReader(new StringReader(json));
        var result = _serializer.Deserialize<Type5>(reader);

        Assert.NotNull(result);
        Assert.True(result.Union!.Value.IsSecond);
        Assert.Equal(12, result.Union.Value.Second.Number);
    }

}

public class AnyOfParserTests
{
    [Fact]
    public void CanParseBookmarkHighlightSelection()
    {
        var json = """
{
    "Selection": [{
        "DataMap": {
            "Key": [{
                "ScopeId": {
                    "Type": "ScopeId",
                    "Value": "1234"
                }
            }]
        }
    }]
}
""";
        var serializer = new JsonSerializer();
        serializer.Converters.Add(new NullableAnyOfJsonConverter());

        using var reader = new JsonTextReader(new StringReader(json));
        var result = serializer.Deserialize<TestBookmark>(reader);

        Assert.NotNull(result);
        Assert.True(result.Selection.HasValue);
        Assert.True(result.Selection.Value.IsSecond);
        Assert.Collection(result.Selection.Value.Second,
            item => {
                Assert.NotNull(item.DataMap);
                Assert.True(item.DataMap.ContainsKey("Key"));
                Assert.Collection(item.DataMap["Key"],
                    x => Assert.Equal("1234", x.ScopeId["Value"]));
            });
    }
}

public class TestBookmark
{
    public AnyOf<T1, ICollection<T2>>? Selection { get; set; }

    public class T1
    {
        public string Name { get; set; }
    }

    public class T2
    {
        public class Map : Dictionary<string, Collection<T3>>
        {
        }

        public Map DataMap { get; set; }
    }

    public class T3
    {
        public JToken ScopeId { get; set; }
    }
}
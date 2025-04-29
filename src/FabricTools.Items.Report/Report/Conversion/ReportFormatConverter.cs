// Copyright (c) 2024 navidata.io Corp

using System.Reflection;
using navidataIO.Utils.Json;
using FabricTools.Items.Report.Definitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FabricTools.Items.Report.Conversion;

/// <summary>
/// Performs format conversions of PBIR report definitions.
/// </summary>
public static class ReportFormatConverter
{
    private static readonly JsonSerializer DefaultJsonSerializer = PbirDefinitionWriter.CreateDefaultSerializer()
        .With(serializer => serializer.Formatting = Formatting.Indented);
    private static readonly JsonSerializer CompactJsonSerializer = PbirDefinitionWriter.CreateDefaultSerializer()
        .With(serializer => serializer.Formatting = Formatting.None);

    /// <summary>
    /// Converts a PBIR report definition to the legacy V1 report format.
    /// </summary>
    public static V1ReportContainer ConvertToV1Report(PbirDefinition reportDefinition)
    {
        // Assume the PbirDefinition has had no modifications and the OriginalJson can be used as the source
        // TODO Implement INPC pattern to set dirty marker

        var pagesSorted = reportDefinition.Pages.OrderBy(p => p.DisplayName).Select(p => p.Name).ToList();
        var pageOrdinals = (reportDefinition.Pages.Metadata?.PageOrder ?? [])
            .Select((name, i) => (Index: i, Name: name))
            .ToDictionary(
                x => x.Name,
                x => x.Index)
            ?? new();
        foreach (var pageName in pagesSorted)
        {
            if (!pageOrdinals.ContainsKey(pageName))
                pageOrdinals.Add(pageName, pageOrdinals.Count);
        }

        var pages = new List<V1MajorReportObject>();
        foreach (var page in reportDefinition.Pages)
        {
            var visuals = new List<V1MajorReportObject>();
            foreach (var visual in page.Visuals)
            {
                visuals.Add(ConvertVisual(visual));
            }
            pages.Add(ConvertPage(page, pageOrdinals[page.Name], visuals.ToArray()));
        }
        
        return new V1ReportContainer(
            ConvertReport(reportDefinition, pages.ToArray(), reportDefinition.Pages.Metadata?.ActivePageName switch
            {
                { } name => pageOrdinals[name],
                _ => 0 // safe fall-back
            })
        );
    }


    private static JArray? ConvertFilters<T>(T? filterConfiguration, JObject? filterConfigJson)
    {
        if (filterConfiguration is null || filterConfigJson?["filters"] is not JArray jFilters) return null;

        /* FILTER
         * ======
         * [ ] =name
         * [x] expression->field (QEC)
         * [ ] filterExpressionMetadata { expressions: QEC[] }
         * [ ] =filter?
         * [ ] =type
         * [x] =howCreated->int
         * [ ] expanded : bool?
         * [ ] isHiddenInViewMode : bool
         * [ ] isLockedInViewMode : bool
         * [ ] displayName : string
         * [ ] [restatement] : string
         * [ ] ordinal : int?
         * [ ] objects
         */
        return new JArray(jFilters.OfType<JObject>().Select(v2Filter => new JObject(v2Filter)
            .RenameProperty("field", "expression")
            .WithOptional("howCreated", v2Filter["howCreated"] switch
            {
                { Type: JTokenType.String} j 
                    when Enum.TryParse<FilterConfigurationFilterContainerHowCreated>(j.Value<string>(), true, out var howCreated)
                    => howCreated.AsInt(),
                _ => null
            })
        ));
    }

    private static JObject? CreateV1PrototypeQuery(VisualConfigurationQuery? v2Query, JObject? queryJson)
    {
        if (v2Query is null || queryJson is null) return null;

        // PrototypeQuery
        // ==============
        // { Version:2, From[], Select[], OrderBy[] }
        // ------------------------------------------
        // From: { Name, Entity, [Schema: "extension"], Type: int 0=Table }
        // Select: { QEC, Name, NativeReferenceName }
        // OrderBy: { Expression: QEC, Direction: int }

        static (string Entity, string? Schema)? ParseSourceRef(JToken sourceRefToken) => sourceRefToken switch
        {
            JObject obj when obj["Entity"] is { Type: JTokenType.String } entity
                => (Entity: entity.Value<string>()!, Schema: obj["Schema"]?.Value<string>()),
            _ => null
        };

        // Sources (FROM)
        var sources = queryJson.SelectTokens("..SourceRef").Select(ParseSourceRef)
        .Where(x => x is not null)
        .Aggregate(
            new Dictionary<(string Entity, string? Schema), string>(),
            (dict, x) =>
            {
                var item = x!.Value;
                if (!dict.ContainsKey(item))
                {
                    var i = 0;
                    var nameCandidate = item.Entity[..1].ToLower();
                    while (dict.ContainsValue(nameCandidate))
                    {
                        nameCandidate = $"{item.Entity[..1].ToLower()}{++i}";
                    }
                    dict[item] = nameCandidate;
                }
                return dict;
            });

        JObject MapExpression(QueryExpressionContainer field)
        {
            var qec = field.ToJson(DefaultJsonSerializer)!;
            foreach (var v2SrcRef in qec.SelectTokens("$..SourceRef"))
            {
                var sourceRef = ParseSourceRef(v2SrcRef)!;
                v2SrcRef.Replace(new JObject
                {
                    {"Source", sources[sourceRef.Value]}
                });
            }

            return (qec as JObject)!;
        }

        return new JObject
        {
            {"Version", 2},
            {"From", new JArray(sources.Select(s => new JObject {
                {"Name", s.Value},
                {"Entity", s.Key.Entity},
                {"Type", (int)EntitySourceType._0 /* TODO When will this not be 0? */}
                }.WithOptional("Schema", s.Key.Schema)))
            },
            {"Select", new JArray(v2Query.QueryState.Values.SelectMany(p => p.Projections)
                .Select(projection => MapExpression(projection.Field)
                    .WithProperty(nameof(QueryExpressionContainer.Name), projection.QueryRef)
                    .WithOptional(nameof(QueryExpressionContainer.NativeReferenceName), projection.NativeQueryRef)))
            }
        }
        .WithOptional("OrderBy", v2Query.SortDefinition?.Sort is {Count:>0} ? new JArray(v2Query.SortDefinition!.Sort.Select(
            sort => new JObject
            {
                {"Direction", sort.Direction.AsInt()},
                {"Expression", MapExpression(sort.Field)}
            })
        ) : null);
    }

    private static JToken? TryGetSingleVisual(VisualContainer visualContainer, JObject visualJson)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (visualContainer.Visual is null) return null;

        /*
         * - [x] visualType*
         * - [x] projections*
         * - [x] prototypeQuery* >> Output: { Version, From : EntitySource[] , Select : QEC[] }
         * - [x] filterSortOrder* (int)
         * - [x] drillFilterOtherVisuals* (bool)
         * - [x] [columnProperties]
         * - [x] [expansionStates]
         * - [x] [objects]
         * - [x] [vcObjects]
         * - [x] [hasDefaultSort]
         */

        var columnProps = visualContainer.Visual?.Query?.QueryState?.Values
            .SelectMany(x => x.Projections)
            .Where(p => !string.IsNullOrEmpty(p.DisplayName))
            .ToDictionary(
                x => x.QueryRef,
                x => x.DisplayName);

        var projections = visualContainer.Visual?.Query?.QueryState
            .ToDictionary(
                x => x.Key,
                x => x.Value.Projections);

        return new JObject
        {
            { "visualType", visualContainer.Visual!.VisualType },
        }
        .WithOptional("prototypeQuery",
            CreateV1PrototypeQuery(visualContainer.Visual.Query, visualJson.SelectObject("$.visual.query")))
        .WithOptional("objects",
            visualJson.SelectToken("$.visual.objects"))
        .WithOptional("vcObjects",
            visualJson.SelectToken("$.visual.visualContainerObjects"))
        .WithOptional("expansionStates",
            visualJson.SelectToken("$.visual.expansionStates"))
        .WithOptional("drillFilterOtherVisuals",
            visualJson.SelectToken("$.visual.drillFilterOtherVisuals"))
        .WithOptional("hasDefaultSort",
            visualJson.SelectToken("$.visual.query.sortDefinition.isDefaultSort"))
        .WithOptional("filterSortOrder", visualContainer.FilterConfig?.FilterSortOrder.AsInt())
        .WithOptional("columnProperties",
            columnProps switch
            {
                { Count:>0 } => new JObject(columnProps.Select(pair =>
                    new JProperty(pair.Key, new JObject
                    {
                        { "displayName", pair.Value }
                    }))),
                _ => null
            })
        .WithOptional("projections",
            projections switch
            {
                {Count:>0} => new JObject(projections.Select(p => 
                    new JProperty(p.Key, new JArray(p.Value.Select(v => new JObject {
                        {"queryRef", v.QueryRef}
                    }
                        .WithOptional("active", v.Active)
                    ))))),
                _ => null
            })
            ;
    }

    private static JToken? TryGetSingleVisualGroup(VisualContainer visualContainer, JObject visualJson)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (visualContainer.VisualGroup is null) return null;
        return new JObject
        {
            { "displayName", visualContainer.VisualGroup.DisplayName },
            { "groupMode", visualContainer.VisualGroup.GroupMode.AsInt() },
        }
        .WithOptional("objects", visualJson.SelectToken("$.visualGroup.objects"));
    }

    private static V1MajorReportObject ConvertVisual(VisualContainer visualContainer)
    {
        /*
         * [x] x
         * [x] y
         * [x] z
         * [x] width
         * [x] height
         * [x] [tabOrder]
         * --------------------
         * [x] config
         * [x] filters
         * [-] [query]
         * [-] [dataTransforms]
         *
         * CONFIG
         * [x] name
         * [x] layouts [{ id, position { x,y,z,width,height,tabOrder=0 } }] -- id:1 -> mobile.json
         * [x] singleVisual
         * [x] singleVisualGroup
         * [x] parentGroupName
         */

        var visualJson = visualContainer.GetJson() ?? throw new InvalidOperationException();

        var config = new JObject
            {
                { "name", visualContainer.Name },
                { "layouts", new JArray(new JObject
                {
                    { "id", 0 },
                    { "position", visualJson["position"] }
                }) }
            }
            .WithOptional("singleVisual",
                TryGetSingleVisual(visualContainer, visualJson))
            .WithOptional("singleVisualGroup",
                TryGetSingleVisualGroup(visualContainer, visualJson))
            .WithOptional("parentGroupName",
                visualContainer.ParentGroupName);

        if (visualContainer.MobileState?.GetJson() is { } mobileJson)
        {
            (config["layouts"] as JArray)!.Add(new JObject
            {
                { "id", 1 },
                { "position", mobileJson["position"] }
            });

            // TODO MobileState.Objects|.vcObjects
        }

        return new V1MajorReportObject(V1MajorReportObjectType.Visual,
            visualJson.RequireObject("$.position"),
            config,
            ConvertFilters(visualContainer.FilterConfig, visualJson.SelectObject("$.filterConfig")));
    }

    private static V1MajorReportObject ConvertPage(Page page, int ordinal, V1MajorReportObject[] visuals)
    {
        /*
         * [x] name
         * [x] displayName
         * [x] ordinal (from PagesMetadata)
         * [x] displayOption
         * [x] height
         * [x] width
         * ===================
         * [x] config{}
         * - [x] objects
         * - [x] relationships []
         * - [x] filterSortOrder >> see singleVisual
         * - [x] visibility
         * - [x] type
         * [x] filters[]
         * [x] visualContainers[]
         */
        var pageJson = page.GetJson() ?? throw new InvalidOperationException();

        return new V1MajorReportObject(V1MajorReportObjectType.Page,
            new JObject
            {
                {"name", page.Name},
                {"displayName", page.DisplayName},
                {"width", Convert.ToInt32(page.Width)},
                {"height", Convert.ToInt32(page.Height)},
                {"ordinal", ordinal},
                {"displayOption", page.DisplayOption.AsInt()},
            },
            new JObject()
                .WithOptional("objects", pageJson.SelectToken("$.objects"))
                .WithOptional("type", page.Type.AsInt())
                .WithOptional("visibility", page.Visibility.AsInt()) // AlwaysVisible, HiddenInViewMode
                .WithOptional("filterSortOrder", page.FilterConfig?.FilterSortOrder.AsInt())
                .WithOptional("relationships", page.VisualInteractions switch
                {
                    {Count:>0}=>new JArray(page.VisualInteractions.Select(r => new JObject
                    {
                        {"source", r.Source},
                        {"target", r.Target},
                        {"type", r.Type.AsInt()}
                    })),
                    _ => null
                })
            ,
            ConvertFilters(page.FilterConfig, pageJson.SelectObject("$.filterConfig")),
            visuals);
    }

    private static JObject ConvertBookmark(string bookmarkName,
        IDictionary<string, Bookmark> bookmarks)
    {
        var bookmark = bookmarks[bookmarkName];
        return new JObject(bookmark.GetJson() ?? throw new InvalidOperationException())
            .RemoveProperties(ItemSchemas.SchemaProperty);
    }

    private static JObject ConvertBookmark(BookmarksMetadataBookmarkGroupMetadata group,
        IDictionary<string, Bookmark> bookmarks) => new()
    {
        {"displayName", group.DisplayName},
        {"name", group.Name},
        {"children", new JArray(group.Children.Select(name => ConvertBookmark(name, bookmarks)))}
    };

    private static JObject ConvertSettings(JObject? v2Settings)
    {
        var result = new JObject();
        if (v2Settings is null)
            return result;

        foreach (var prop in typeof(ReportExplorationSettings).GetProperties())
        {
            if (prop.GetCustomAttribute<JsonPropertyAttribute>() is not {} jsonProp)
                continue;

            if (jsonProp.PropertyName is not {} name || v2Settings[jsonProp.PropertyName] is not { } token)
                continue;
#if NETSTANDARD
            if (prop.PropertyType.IsEnum
                && token.Type == JTokenType.String
                && Enum.GetNames(prop.PropertyType).Contains(token.Value<string>(), StringComparer.InvariantCultureIgnoreCase)
                && Enum.Parse(prop.PropertyType, token.Value<string>()!, ignoreCase: true) is {} enumValue)
#else
            if (prop.PropertyType.IsEnum
                && token.Type == JTokenType.String
                && Enum.TryParse(prop.PropertyType, token.Value<string>(), ignoreCase: true, out var enumValue))
#endif
            {
                result[name] = new JValue(Convert.ToInt32(enumValue));
                continue;
            }

            result[name] = token;
        }
        return result;
    }

    private static JToken ConvertTheme(ReportThemeMetadata theme) => new JObject
    {
        {"name", theme.Name},
        {"version", theme.ReportVersionAtImport},
        {"type", theme.Type.AsInt()},
    };

    private static IEnumerable<JToken> ConvertExtensions(ReportExtension? extensions)
    {
        if (extensions is null) yield break;

        yield return new JObject
        {
            {"name", extensions.Name},
        }
        .WithOptionalArray("entities", extensions.Entities, ConvertEntity);
        
        yield break;

        static JToken MapDataType(ReportExtensionPrimitiveTypeName v2Type) => v2Type switch
        {
            ReportExtensionPrimitiveTypeName.Text => V1ExtensionMeasureDataType.Text.AsInt(),
            ReportExtensionPrimitiveTypeName.Decimal => V1ExtensionMeasureDataType.Decimal.AsInt(),
            ReportExtensionPrimitiveTypeName.Double => V1ExtensionMeasureDataType.Double.AsInt(),
            ReportExtensionPrimitiveTypeName.Integer => V1ExtensionMeasureDataType.Integer.AsInt(),
            ReportExtensionPrimitiveTypeName.Boolean => V1ExtensionMeasureDataType.Boolean.AsInt(),
            ReportExtensionPrimitiveTypeName.Date => V1ExtensionMeasureDataType.Date.AsInt(),
            ReportExtensionPrimitiveTypeName.DateTime => V1ExtensionMeasureDataType.DateTime.AsInt(),
            ReportExtensionPrimitiveTypeName.DateTimeZone => V1ExtensionMeasureDataType.DateTimeZone.AsInt(),
            ReportExtensionPrimitiveTypeName.Time => V1ExtensionMeasureDataType.Time.AsInt(),
            ReportExtensionPrimitiveTypeName.Duration => V1ExtensionMeasureDataType.Duration.AsInt(),
            ReportExtensionPrimitiveTypeName.Binary => V1ExtensionMeasureDataType.Binary.AsInt(),
            _ => null
        };

        static JToken ConvertEntity(ReportExtensionEntity entity) => new JObject
        {
            {"name", entity.Name},
            {"extends", entity.Name},
        }
            .WithOptionalArray("measures", entity.Measures, measure => new JObject
            {
                {"name", measure.Name},
                {"dataType", MapDataType(measure.DataType)},
                {"expression", measure.Expression},
                {"hidden", measure.Hidden == true},
            }
                .WithOptional("description", measure.Description)
                .WithOptional("dataCategory", measure.DataCategory)
                .WithOptional("displayFolder", measure.DisplayFolder)
                .WithOptional("references", measure.References, refs => new JObject
                    {
                        {"unrecognizedReferences", refs.UnrecognizedReferences == true}
                    }
                    .WithOptionalArray("measures", refs.Measures, m => new JObject
                        {
                            {"schema", m.Schema},
                            {"entity", m.Entity},
                            {"name", m.Name}
                        }))
                .WithOptional("formatInformation", measure.FormatString, fmtString => new JObject
                {
                    {"formatString", fmtString}
                })
                .WithOptional("measureTemplate", measure.MeasureTemplate switch
                {
                    { } template => new JObject
                    {
                        {"daxTemplateName", template.DaxTemplateName},
                        {"version", template.Version}
                    },
                    _ => null
                })
            );
    }

    private static V1MajorReportObject ConvertReport(PbirDefinition pbir, V1MajorReportObject[] pages, int activeSectionIndex)
    {
        /*
         * [x] main
         * - [x] layoutOptimization
         * - [x] pods
         * - [x] publicCustomVisuals
         * - [x] resourcePackages
         * - [x] theme
         * [ ] config
         * - [x] version
         * - [x] themeCollection
         * - [x] activeSectionIndex
         * - [x] modelExtensions
         * - [x] defaultDrillFilterOtherVisuals
         * - [x] slowDataSourceSettings
         * - [x] settings
         *   - queryLimitOption:enum
         *   - exportDataMode:enum
         *   - isPersistentUserStateDisabled
         *   - hideVisualContainerHeader
         *   - useStylableVisualContainerHeader
         *   - useNewFilterPaneExperience
         *   - optOutNewFilterPaneExperience
         *   - defaultFilterActionIsDataFilter
         *   - useCrossReportDrillthrough
         *   - allowChangeFilterTypes
         *   - allowInlineExploration
         *   - disableFilterPaneSearch
         *   - enableDeveloperMode
         *   - useEnhancedTooltips
         *   - useScaledTooltips
         *   - useDefaultAggregateDisplayName
         *   - customMemoryLimit:string
         *   - customTimeoutLimit:string
         * - [x] objects{}
         * - [x] bookmarks[]
         * [x] filters
         */
        var report = pbir.Report;
        var reportJson = report.GetJson() ?? throw new InvalidOperationException();
        var bookmarks = pbir.Bookmarks.ToDictionary(b => b.Name);

        return new V1MajorReportObject(V1MajorReportObjectType.Report,
            new JObject
            {
                {"layoutOptimization", report.LayoutOptimization.AsInt()},
                {"pods", new JArray(pbir.Pages.Select((p,i) => p.PageBinding switch
                    {
                        {} binding => new JObject
                            {
                                {"name", binding.Name},
                                {"boundSection", p.Name},
                            }
                            .WithOptional("parameters", binding.Parameters.AsJson(CompactJsonSerializer))
                            .WithOptional("type", binding.Type.AsInt(returnDefault:false)) // Default, Drillthrough, Tooltip
                            .WithOptional("referenceScope", binding.ReferenceScope.AsInt()), // Default, CrossReport
                        null => null
                    }).WhereNotNull())},
                {"publicCustomVisuals", new JArray(report.PublicCustomVisuals ?? [])},
                {"resourcePackages", new JArray(report.ResourcePackages?.Select(p => new JObject
                    {
                        { "resourcePackage", new JObject {
                            {"disabled", p.Disabled == true},
                            {"items", new JArray(p.Items.Select(i => new JObject {
                                {"name", i.Name},
                                {"path", i.Path},
                                {"type", i.Type switch
                                    {
                                        ReportResourcePackageItemType.BaseTheme => V1ResourcePackageItemType.StaticResourceBaseTheme.AsInt(),
                                        ReportResourcePackageItemType.CustomTheme => V1ResourcePackageItemType.StaticResourceTheme.AsInt(),
                                        ReportResourcePackageItemType.Image => V1ResourcePackageItemType.StaticResourceImage.AsInt(),
                                        ReportResourcePackageItemType.ShapeMap => V1ResourcePackageItemType.StaticResourceShapeMap.AsInt(),
#if NETSTANDARD
                                        var t when Enum.GetNames(typeof(V1ResourcePackageItemType)).Contains(t.ToString())
                                            => Convert.ToInt32(Enum.Parse(typeof(V1ResourcePackageItemType), t.ToString(), ignoreCase: true)),
#else
                                        var t when Enum.TryParse<V1ResourcePackageItemType>(t.ToString(), ignoreCase: true, out var v1Value)
                                            => Convert.ToInt32(v1Value),
#endif
                                        var t => t.AsInt() // that's probably not right, but will do for now
                                    }}
                                }))},
                            {"name", p.Name},
                            {"type", p.Type.AsInt()},
                        }}
                    })
                    ?? [])},
            }
                .WithOptional("theme", report.ThemeCollection.CustomTheme?.Name),
            new JObject
            {
                {"version", "5.63" /* Apr-2025 version */ }, // TODO Extract from reportThemeSchema-x.xxx.json//description
                {"themeCollection", new JObject()
                    .WithOptional("baseTheme", report.ThemeCollection.BaseTheme, ConvertTheme)
                    .WithOptional("customTheme", report.ThemeCollection.CustomTheme, ConvertTheme)
                },
                {"activeSectionIndex", activeSectionIndex},
                {"modelExtensions", new JArray(ConvertExtensions(pbir.ReportExtensions))},
                //{"linguisticSchemaSyncVersion",null},
                {"settings", ConvertSettings(reportJson.SelectObject("$.settings"))},
                {"bookmarks",new JArray(pbir.Bookmarks.Metadata?.Items.Select(i => i switch
                {
                    {CurrentValue: BookmarksMetadataSingleBookmarkMetadata singleBookmark }
                        => ConvertBookmark(singleBookmark.Name, bookmarks),
                    {CurrentValue: BookmarksMetadataBookmarkGroupMetadata group }
                        => ConvertBookmark(group, bookmarks),
                    _
                        => throw new InvalidOperationException($"Unexpected bookmark metadata type: {i}")
                }) ?? [])}
            }
                .WithOptional("defaultDrillFilterOtherVisuals", report.Settings?.DefaultDrillFilterOtherVisuals)
                .WithOptional("objects", reportJson.SelectToken("$.objects"))
                .WithOptional("slowDataSourceSettings", reportJson.SelectToken("$.slowDataSourceSettings")),
            ConvertFilters(report.FilterConfig, reportJson.SelectObject("$.filterConfig")),
            pages);
    }
}

internal enum V1ExtensionMeasureDataType
{
    Null,
    Text,
    Decimal,
    Double,
    Integer,
    Boolean,
    Date,
    DateTime,
    DateTimeZone,
    Time,
    Duration,
    Binary,
    None,
    Variant
}

internal enum V1ResourcePackageItemType
{
    CustomVisualJavascript = 0,
    CustomVisualsCss = 1,
    CustomVisualScreenshot = 2,
    CustomVisualIcon = 3,
    CustomVisualWatermark = 4,
    CustomVisualMetadata = 5,
    StaticResourceImage = 100,
    StaticResourceShapeMap = 200,
    StaticResourceTheme = 201,
    StaticResourceBaseTheme = 202
}

internal static class EnumExtensions
{

    public static int? AsInt<T>(this T? enumValue, bool returnDefault = true) where T : struct, Enum =>
        enumValue switch
        {
#if NETSTANDARD
            { } value when Enum.IsDefined(typeof(T), value) => Convert.ToInt32(value) switch
#else
            { } value when Enum.IsDefined<T>(value) => Convert.ToInt32(value) switch
#endif
                {
                    0 when returnDefault => 0,
                    0 => null,
                    var n => n
                },
            _ => null,
        };

    public static int? AsInt<T>(this T enumValue, bool returnDefault = true) where T : struct, Enum =>
        enumValue switch
        {
#if NETSTANDARD
            _ when Enum.IsDefined(typeof(T), enumValue) => Convert.ToInt32(enumValue) switch
#else
            { } value when Enum.IsDefined<T>(value) => Convert.ToInt32(value) switch
#endif
                {
                    0 when returnDefault => 0,
                    0 => null,
                    var n => n
                },
            _ => null,
        };
}

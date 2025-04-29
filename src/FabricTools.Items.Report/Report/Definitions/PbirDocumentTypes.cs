// Copyright (c) 2024 navidata.io Corp

using FabricTools.Items.ComponentModel;

namespace FabricTools.Items.Report.Definitions;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member
#pragma warning disable IDE0090 // Use 'new(...)' to simplify object creation

[PbirDocument(PbirNames.ReportFile), Newtonsoft.Json.JsonObject]
public partial class Report : HasAnnotations<ReportAnnotation>
{
    protected override ReportAnnotation Convert(Annotation annotation) => new ReportAnnotation { Name = annotation.Key, Value = annotation.Value };
    protected override ICollection<ReportAnnotation>? GetAnnotations() => Annotations;
    protected override ICollection<ReportAnnotation> EnsureAnnotations() => Annotations ??= [];
    protected override string GetKey(ReportAnnotation annotation) => annotation.Name;
    protected override Annotation Map(ReportAnnotation annotation) => new Annotation(annotation.Name, annotation.Value);
    protected override void UpdateAnnotation(ReportAnnotation annotation, string value) => annotation.Value = value;
}

[PbirDocument(PbirNames.PageFile), Newtonsoft.Json.JsonObject]
public partial class Page : HasAnnotations<PageAnnotation>
{
    protected override PageAnnotation Convert(Annotation annotation) => new PageAnnotation { Name = annotation.Key, Value = annotation.Value };
    protected override ICollection<PageAnnotation>? GetAnnotations() => Annotations;
    protected override ICollection<PageAnnotation> EnsureAnnotations() => Annotations ??= [];
    protected override string GetKey(PageAnnotation annotation) => annotation.Name;
    protected override Annotation Map(PageAnnotation annotation) => new Annotation(annotation.Name, annotation.Value);
    protected override void UpdateAnnotation(PageAnnotation annotation, string value) => annotation.Value = value;

    [Newtonsoft.Json.JsonIgnore]
    public IPbirDocuments<VisualContainer> Visuals { get; } = new PbirDocuments<VisualContainer>(PbirNames.VisualsFolder);

    public Page()
    {
        ((IPbirDocumentNode)Visuals).SetParent(this);
    }
}

[PbirDocument(PbirNames.VisualFile), Newtonsoft.Json.JsonObject]
public partial class VisualContainer : HasAnnotations<VisualContainerAnnotation>
{
    protected override VisualContainerAnnotation Convert(Annotation annotation) => new VisualContainerAnnotation { Name = annotation.Key, Value = annotation.Value };
    protected override ICollection<VisualContainerAnnotation>? GetAnnotations() => Annotations;
    protected override ICollection<VisualContainerAnnotation> EnsureAnnotations() => Annotations ??= [];
    protected override string GetKey(VisualContainerAnnotation annotation) => annotation.Name;
    protected override Annotation Map(VisualContainerAnnotation annotation) => new Annotation(annotation.Name, annotation.Value);
    protected override void UpdateAnnotation(VisualContainerAnnotation annotation, string value) => annotation.Value = value;


    private VisualContainerMobileState? _mobileState;

    /// <summary>
    /// Gets or sets the (optional) mobile state of the visual.
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public VisualContainerMobileState? MobileState
    {
        get => _mobileState;
        set => _mobileState = value?.SetParent(this);
    }

}

[PbirDocument]
public partial class Bookmark { }

[PbirDocument(PbirNames.BookmarksMetadataFile)]
public partial class BookmarksMetadata { }

[PbirDocument(PbirNames.PagesMetadataFile)]
public partial class PagesMetadata { }

[PbirDocument(PbirNames.ReportExtensionFile)]
public partial class ReportExtension { }

[PbirDocument(PbirNames.VersionMetadataFile)]
public partial class VersionMetadata { }

[PbirDocument(PbirNames.VisualMobileFile)]
public partial class VisualContainerMobileState { }

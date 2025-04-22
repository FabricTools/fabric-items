# PBIR.NET

.NET SDK for the Fabric PBIR Format

The [Fabric PBIR Format](https://fabric.onl/pbir), currently in public preview, is a new schema-based and source control friendly format for Power BI reports. It brings the same code-first benefits to Power BI authors that the [TMDL](https://fabric.onl/tmdl) format brought to Power BI semantic models. PBIR is also known as the "Power BI Enhanced Report Format" or simply the "V4 format".

Whilst the original ideas and prototypes for what now is the PBIR format were implemented by [`pbi-tools`](https://github.com/pbi-tools) as a community-driven open-source initiative, the PBIR format is fully developed and supported by Microsoft. It effectively replaces black-box binary PBIX files which were ill-suited for source control and collaborative development.

This repository builds on top of Microsoft's publicly maintained [json schemas](https://fabric.onl/pbir-schemas) for PBIR documents and metadata files. It provides a .NET SDK for working with PBIR files, including reading, writing, and validating the contents of these files.

A number of publicly available PBIR samples can be found [here](https://github.com/FabricTools/pbir-samples). A Power BI report in PBIR format can be identified via the `version: "4.0"` property in the `definition.pbir` file. Furthermore, it will have a `definition/` sub-folder in the same directory, containing various json files, each of which with a `$schema` property pointing to the corresponding schema.

```json
{
  "version": "4.0",
  "datasetReference": {
    "byPath": {
      "path": "../AdventureWorks Sales.SemanticModel"
    }
  }
}
```

## Using the SDK

The PBIR.NET SDK is available as a NuGet package. You can install it using the following command:

```
dotnet add package FabricTools.Items.Report --prerelease
```

For convenience, import the base namespace:

```csharp
using FabricTools.Items.Report;
```

The API entry point is the `PbirDefinition` class. It corresponds to the `definition/` folder of a Fabric/Power BI Report item.

Use `PbirDefinition.FromPath(string path)` to load a PBIR definition from a file system folder. The path must point to the `definition/` folder containing a `report.json` file.

The resulting `PbirDefinition` object contains familiar properties like *Pages*, *Bookmarks*, etc. All properties can be read and modified. The SDK contains a strongly-typed API for all PBIR objects that is auto-generated from the JSON schemas.

A modified `PbirDefinition` object can be saved back to the file system using `PbirDefinition.Write(string path)`.

### Schema Validation

The `PbirValidator` class can be used to validate a single PBIR file against its schema. Either a file system path can be provided, or an in-memory `JObject`.

```csharp
var validator = new PbirValidator();
var errors = validator.Validate("./Report/definition/report.json");
if (errors.Count > 0)
{
	// handle reported validation errors
}
```

## Status and Roadmap

The SDK is currently in preview and under active development. Further features will be prioritized based on community feedback. A comprehensive API documentation is planned for the near future.

Please report any issues or feature requests on the [GitHub issue tracker](https://github.com/FabricTools/fabric-items/issues/new).

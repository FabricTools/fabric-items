// Copyright (c) 2024 navidata.io Corp

using System.IO.Abstractions;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;
using FabricTools.Items.Report.Schemas;

namespace FabricTools.Items.Report;

/// <summary>
/// Validates Power BI Report (PBIR) JSON files against the official schema.
/// </summary>
public class PbirValidator(IFileSystem? fileSystem = null)
{
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

    static PbirValidator()
    {
        Schemas = ReportDefinitionSchemas.LoadAsync(
            ReportDefinitionSchemaResources.FromAssembly(typeof(PbirValidator).Assembly) /* load embedded schemas from __this__ assembly */,
            ReportDefinitionSchemas.DefaultReferenceResolverFactory,
            CancellationToken.None
        )
        .Result;
    }

    internal static readonly ReportDefinitionSchemas Schemas;

    /// <summary>
    /// If <c>true</c>, allows loading of schemas from the url at a file's <c>$schema</c> property
    /// </summary>
    public bool AllowOnlineSchemaLoad { get; set; } = false;

    /// <summary>
    /// Gets or sets the settings for the <see cref="JsonSchemaValidator"/>.
    /// </summary>
    public JsonSchemaValidatorSettings ValidatorSettings { get; set; } = new();

    /// <summary>
    /// Validates a PBIR JSON file at the specified path.
    /// The schema is located at the <c>$schema</c> property of the file.
    /// Non-default schemas can be resolved if <see cref="AllowOnlineSchemaLoad"/> is <c>true</c>.
    /// </summary>
    public ICollection<ValidationError> Validate(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Value cannot be null or empty.", nameof(path));

        var json = JObject.Parse(_fileSystem.File.ReadAllText(path));

        return Validate(json);
    }

    /// <summary>
    /// Validates a PBIR JSON object.
    /// The schema is located at the <c>$schema</c> property of the document.
    /// Non-default schemas can be resolved if <see cref="AllowOnlineSchemaLoad"/> is <c>true</c>.
    /// </summary>
    public ICollection<ValidationError> Validate(JObject json)
    {
        if (json == null)
            throw new ArgumentNullException(nameof(json));

        var schemaId = (json["$schema"] ?? throw new InvalidOperationException("Missing schema ID.")).Value<string>()!; // TODO Better exception type
        if (!Uri.TryCreate(schemaId, UriKind.Absolute, out var schemaUri))
            throw new InvalidOperationException($"Invalid schema ID. Uri expected. Found: `{schemaId}`. Use an overload of Validate() that takes an explicit JsonSchema argument.");

        JsonSchema schema;
        try
        {
            schema = Schemas[schemaUri].Schema;
        }
        catch (KeyNotFoundException) when (AllowOnlineSchemaLoad)
        {
            schema = JsonSchema.FromUrlAsync(schemaId).Result;
        }
        catch (KeyNotFoundException ex)
        {
            throw new FileNotFoundException($"The schema for `{schemaId}` is not available offline, and 'AllowOnlineSchemaLoad' is disabled.", ex);
        }

        return Validate(json, schema);
    }

    /// <summary>
    /// Validates a JSON object against the specified schema.
    /// </summary>
    public ICollection<ValidationError> Validate(JObject json, JsonSchema schema)
    {
        if (json == null)
            throw new ArgumentNullException(nameof(json));
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        var validator = new JsonSchemaValidator(ValidatorSettings);
        return validator.Validate(json, schema);
    }
}
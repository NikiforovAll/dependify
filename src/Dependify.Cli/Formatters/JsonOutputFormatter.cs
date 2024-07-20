namespace Dependify.Cli.Formatters;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Dependify.Core.Graph;
using Depends.Core.Graph;

internal class JsonOutputFormatter(TextWriter textWriter) : IOutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new PolymorphicTypeResolver()
        };

    public void Dispose() => textWriter.Dispose();

    public void Write<T>(T data)
    {
        textWriter.WriteLine(JsonSerializer.Serialize(data, JsonOptions));

        textWriter.Flush();
    }

    internal class PolymorphicTypeResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var jsonTypeInfo = base.GetTypeInfo(type, options);

            var baseType = typeof(Node);
            if (jsonTypeInfo.Type == baseType)
            {
                jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$type",
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
                    DerivedTypes =
                    {
                        new JsonDerivedType(typeof(SolutionReferenceNode), nameof(SolutionReferenceNode)),
                        new JsonDerivedType(typeof(ProjectReferenceNode), nameof(ProjectReferenceNode)),
                        new JsonDerivedType(typeof(PackageReferenceNode), nameof(PackageReferenceNode)),
                    }
                };
            }

            return jsonTypeInfo;
        }
    }
}

namespace Dependify.Core.Serializers;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Dependify.Core.Graph;

public static class JsonGraphSerializer
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new PolymorphicTypeResolver()
        };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    public static string ToString(DependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        return JsonSerializer.Serialize(graph, JsonOptions);
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

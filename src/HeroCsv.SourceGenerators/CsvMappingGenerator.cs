using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace HeroCsv.SourceGenerators;

[Generator]
public class CsvMappingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes with [GenerateCsvMapping] attribute
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // Generate source for each marked class
        context.RegisterSourceOutput(classDeclarations,
            static (spc, source) => Execute(source!, spc));
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0;

    static ClassToGenerate? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Get the symbol for the class
        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
            return null;

        // Check if it has our attribute
        var attribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "GenerateCsvMappingAttribute" ||
                                a.AttributeClass?.Name == "GenerateCsvMapping");

        if (attribute == null)
            return null;

        // Extract attribute parameters
        var hasHeaders = GetAttributeValue(attribute, "HasHeaders", true);
        var delimiter = GetAttributeValue(attribute, "Delimiter", ',');
        var quote = GetAttributeValue(attribute, "Quote", '"');

        // Get all properties that should be mapped
        var properties = new List<PropertyInfo>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is IPropertySymbol property && property.DeclaredAccessibility == Accessibility.Public &&
                !property.IsStatic && !property.IsReadOnly && property.SetMethod != null)
            {
                // Check for CsvIgnore attribute
                var hasIgnore = property.GetAttributes()
                    .Any(a => a.AttributeClass?.Name == "CsvIgnoreAttribute" || 
                             a.AttributeClass?.Name == "CsvIgnore");
                
                if (!hasIgnore)
                {
                    var columnName = GetColumnName(property);
                    var columnIndex = GetColumnIndex(property);
                    
                    properties.Add(new PropertyInfo
                    {
                        Name = property.Name,
                        Type = property.Type.ToDisplayString(),
                        ColumnName = columnName,
                        ColumnIndex = columnIndex,
                        IsNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated
                    });
                }
            }
        }

        return new ClassToGenerate
        {
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classSymbol.Name,
            Properties = properties,
            HasHeaders = hasHeaders,
            Delimiter = delimiter,
            Quote = quote
        };
    }

    static T GetAttributeValue<T>(AttributeData attribute, string propertyName, T defaultValue)
    {
        var namedArgument = attribute.NamedArguments
            .FirstOrDefault(a => a.Key == propertyName);
        
        if (namedArgument.Key != null && namedArgument.Value.Value != null)
        {
            return (T)namedArgument.Value.Value;
        }

        return defaultValue;
    }

    static string? GetColumnName(IPropertySymbol property)
    {
        var attr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "CsvColumnNameAttribute" || 
                                a.AttributeClass?.Name == "CsvColumnName");
        
        if (attr?.ConstructorArguments.Length > 0)
        {
            return attr.ConstructorArguments[0].Value?.ToString();
        }

        return null;
    }

    static int? GetColumnIndex(IPropertySymbol property)
    {
        var attr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "CsvColumnIndexAttribute" || 
                                a.AttributeClass?.Name == "CsvColumnIndex");
        
        if (attr?.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int index)
        {
            return index;
        }

        return null;
    }

    static void Execute(ClassToGenerate classToGenerate, SourceProductionContext context)
    {
        var source = GenerateSource(classToGenerate);
        context.AddSource($"{classToGenerate.ClassName}.CsvMapping.g.cs", 
            SourceText.From(source, Encoding.UTF8));
    }

    static string GenerateSource(ClassToGenerate classInfo)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using HeroCsv;");
        sb.AppendLine("using HeroCsv.Core;");
        sb.AppendLine("using HeroCsv.Models;");
        sb.AppendLine();
        sb.AppendLine($"namespace {classInfo.Namespace};");
        sb.AppendLine();
        
        // Generate static extension class with mapping methods
        sb.AppendLine($"public static class {classInfo.ClassName}CsvExtensions");
        sb.AppendLine("{");
        
        // Generate ReadCsv method
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Reads CSV content and maps to {classInfo.ClassName} objects (AOT-safe, source-generated)");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public static IEnumerable<{classInfo.ClassName}> ReadCsv{classInfo.ClassName}(this string csvContent)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var options = new CsvOptions('{classInfo.Delimiter}', '{classInfo.Quote}', {classInfo.HasHeaders.ToString().ToLower()});");
        sb.AppendLine($"        return Csv.Read(csvContent, options, CreateFrom{classInfo.ClassName}Record);");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        
        // Generate ReadCsvFile method
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Reads CSV file and maps to {classInfo.ClassName} objects (AOT-safe, source-generated)");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public static IEnumerable<{classInfo.ClassName}> ReadCsv{classInfo.ClassName}FromFile(string filePath)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var options = new CsvOptions('{classInfo.Delimiter}', '{classInfo.Quote}', {classInfo.HasHeaders.ToString().ToLower()});");
        sb.AppendLine($"        return Csv.ReadFile(filePath, options, CreateFrom{classInfo.ClassName}Record);");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        
        // Generate factory method
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Factory method to create {classInfo.ClassName} from CSV record (AOT-safe)");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public static {classInfo.ClassName} CreateFrom{classInfo.ClassName}Record(ICsvRecord record)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return new {classInfo.ClassName}");
        sb.AppendLine($"        {{");
        
        // Generate property mappings
        for (int i = 0; i < classInfo.Properties.Count; i++)
        {
            var prop = classInfo.Properties[i];
            var index = prop.ColumnIndex ?? i;
            var comma = i < classInfo.Properties.Count - 1 ? "," : "";
            
            sb.AppendLine($"            {prop.Name} = {GeneratePropertyMapping(prop, index)}{comma}");
        }
        
        sb.AppendLine($"        }};");
        sb.AppendLine($"    }}");
        
        // Generate with headers method if needed
        if (classInfo.HasHeaders)
        {
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Reads CSV with headers and maps to {classInfo.ClassName} objects (AOT-safe, source-generated)");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public static IEnumerable<{classInfo.ClassName}> ReadCsv{classInfo.ClassName}WithHeaders(this string csvContent)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var options = new CsvOptions('{classInfo.Delimiter}', '{classInfo.Quote}', true);");
            sb.AppendLine($"        return Csv.ReadWithHeaders(csvContent, options, (headers, record) =>");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return new {classInfo.ClassName}");
            sb.AppendLine($"            {{");
            
            // Generate property mappings using header names
            for (int i = 0; i < classInfo.Properties.Count; i++)
            {
                var prop = classInfo.Properties[i];
                var columnName = prop.ColumnName ?? prop.Name;
                var comma = i < classInfo.Properties.Count - 1 ? "," : "";
                
                sb.AppendLine($"                {prop.Name} = {GenerateHeaderBasedMapping(prop, columnName)}{comma}");
            }
            
            sb.AppendLine($"            }};");
            sb.AppendLine($"        }});");
            sb.AppendLine($"    }}");
        }
        
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    static string GeneratePropertyMapping(PropertyInfo property, int index)
    {
        // Map common types to extension methods
        return property.Type switch
        {
            "string" => $"record.GetString({index})",
            "int" => $"record.GetInt32({index})",
            "int?" => $"record.TryGetInt32({index}, out var _{property.Name}) ? _{property.Name} : null",
            "long" => $"record.GetInt64({index})",
            "long?" => $"record.TryGetInt64({index}, out var _{property.Name}) ? _{property.Name} : null",
            "double" => $"record.GetDouble({index})",
            "double?" => $"record.TryGetDouble({index}, out var _{property.Name}) ? _{property.Name} : null",
            "decimal" => $"record.GetDecimal({index})",
            "decimal?" => $"record.TryGetDecimal({index}, out var _{property.Name}) ? _{property.Name} : null",
            "bool" => $"record.GetBoolean({index})",
            "bool?" => $"record.TryGetBoolean({index}, out var _{property.Name}) ? _{property.Name} : null",
            "System.DateTime" => $"record.GetDateTime({index})",
            "System.DateTime?" => $"record.TryGetDateTime({index}, out var _{property.Name}) ? _{property.Name} : null",
            "System.DateTimeOffset" => $"record.GetDateTimeOffset({index})",
            "System.DateTimeOffset?" => $"record.TryGetDateTimeOffset({index}, out var _{property.Name}) ? _{property.Name} : null",
            "System.Guid" => $"record.GetGuid({index})",
            "System.Guid?" => $"record.TryGetGuid({index}, out var _{property.Name}) ? _{property.Name} : null",
            _ => $"record.GetString({index})" // Default to string for unknown types
        };
    }

    static string GenerateHeaderBasedMapping(PropertyInfo property, string columnName)
    {
        var indexExpr = $"headers.GetFieldIndex(\"{columnName}\")";
        
        return property.Type switch
        {
            "string" => $"record.GetString({indexExpr})",
            "int" => $"record.GetInt32({indexExpr})",
            "int?" => $"record.TryGetInt32({indexExpr}, out var _{property.Name}) ? _{property.Name} : null",
            "long" => $"record.GetInt64({indexExpr})",
            "long?" => $"record.TryGetInt64({indexExpr}, out var _{property.Name}) ? _{property.Name} : null",
            "double" => $"record.GetDouble({indexExpr})",
            "double?" => $"record.TryGetDouble({indexExpr}, out var _{property.Name}) ? _{property.Name} : null",
            "decimal" => $"record.GetDecimal({indexExpr})",
            "decimal?" => $"record.TryGetDecimal({indexExpr}, out var _{property.Name}) ? _{property.Name} : null",
            "bool" => $"record.GetBoolean({indexExpr})",
            "bool?" => $"record.TryGetBoolean({indexExpr}, out var _{property.Name}) ? _{property.Name} : null",
            "System.DateTime" => $"record.GetDateTime({indexExpr})",
            "System.DateTime?" => $"record.TryGetDateTime({indexExpr}, out var _{property.Name}) ? _{property.Name} : null",
            "System.DateTimeOffset" => $"record.GetDateTimeOffset({indexExpr})",
            "System.DateTimeOffset?" => $"record.TryGetDateTimeOffset({indexExpr}, out var _{property.Name}) ? _{property.Name} : null",
            "System.Guid" => $"record.GetGuid({indexExpr})",
            "System.Guid?" => $"record.TryGetGuid({indexExpr}, out var _{property.Name}) ? _{property.Name} : null",
            _ => $"record.GetString({indexExpr})"
        };
    }

    class ClassToGenerate
    {
        public string Namespace { get; set; } = "";
        public string ClassName { get; set; } = "";
        public List<PropertyInfo> Properties { get; set; } = new();
        public bool HasHeaders { get; set; }
        public char Delimiter { get; set; }
        public char Quote { get; set; }
    }

    class PropertyInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string? ColumnName { get; set; }
        public int? ColumnIndex { get; set; }
        public bool IsNullable { get; set; }
    }
}
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ReOsuStoryboardPlayer.Avalonia.SourceGenerator;

[Generator]
public class TypeCollectedActivatorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 查找所有带有CollectTypeForActivator特性的类
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Attributes.CollectTypeForActivatorAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, _) => GetActivatorInfo(ctx)
        ).Where(static m => m is not null);

        // 同时收集所有类型信息用于继承关系分析
        var allTypes = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, _) => GetTypeInfo(ctx)
        ).Where(static m => m is not null).Collect();

        // 合并activator信息和类型信息
        var combined = provider.Combine(allTypes).Select(static (pair, _) =>
        {
            var (activator, allTypeInfos) = pair;
            if (activator == null) return null;

            // 查找所有继承自目标基类的类型
            var derivedTypes = allTypeInfos
                .Where(type => IsDerivedFrom(type!.TypeSymbol, activator.TargetBaseTypeSymbol))
                .Where(x => !x?.TypeSymbol.IsAbstract ?? false)
                .ToList();

            return new ActivatorGenerationInfo
            {
                ActivatorInfo = activator,
                DerivedTypes = derivedTypes
            };
        }).Where(static m => m is not null);

        context.RegisterSourceOutput(combined, GenerateActivator);
    }

    private static bool IsDerivedFrom(ITypeSymbol? typeSymbol, ITypeSymbol baseTypeSymbol)
    {
        if (typeSymbol == null || baseTypeSymbol == null)
            return false;

        var current = typeSymbol.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseTypeSymbol))
                return true;
            current = current.BaseType;
        }

        // 检查接口实现
        foreach (var iface in typeSymbol.AllInterfaces)
            if (SymbolEqualityComparer.Default.Equals(iface, baseTypeSymbol))
                return true;

        return false;
    }

    private static ActivatorInfo? GetActivatorInfo(GeneratorAttributeSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax) context.TargetNode;

        // 获取类的信息
        var className = classDeclaration.Identifier.ValueText;
        var namespaceDeclaration = classDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
        var ns = namespaceDeclaration?.Name?.ToString() ?? string.Empty;
        

        // 获取特性参数
        var attributeData = context.Attributes[0];
        if (attributeData.ConstructorArguments.Length != 1)
            return null;

        var targetType = attributeData.ConstructorArguments[0].Value as ITypeSymbol;
        if (targetType == null)
            return null;

        return new ActivatorInfo
        {
            ClassName = className,
            Namespace = ns,
            FullClassName = $"{ns}.{className}",
            TargetBaseType = targetType.ToDisplayString(),
            TargetBaseTypeName = targetType.Name,
            TargetBaseTypeSymbol = targetType,
            AssemblyName = context.SemanticModel.Compilation.Assembly.Name
        };
    }

    private static TypeInfo? GetTypeInfo(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax) context.Node;
        var semanticModel = context.SemanticModel;

        var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
        if (typeSymbol == null)
            return null;

        var namespaceDeclaration = classDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
        var ns = namespaceDeclaration?.Name?.ToString() ?? string.Empty;

        return new TypeInfo
        {
            ClassName = classDeclaration.Identifier.ValueText,
            Namespace = ns,
            FullClassName = $"{ns}.{classDeclaration.Identifier.ValueText}",
            TypeSymbol = typeSymbol
        };
    }

    private static void GenerateActivator(SourceProductionContext context, ActivatorGenerationInfo? info)
    {
        if (info == null) return;

        var source = GenerateActivatorClass(info);
        var fileName = $"{info.ActivatorInfo.ClassName}_Generated.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateActivatorClass(ActivatorGenerationInfo info)
    {
        var activatorInfo = info.ActivatorInfo;
        var derivedTypes = info.DerivedTypes;
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Collections.Frozen;");
        sb.AppendLine();
        sb.AppendLine($"namespace {activatorInfo.Namespace};");
        sb.AppendLine();
        sb.AppendLine(
            $"partial class {activatorInfo.ClassName}");
        sb.AppendLine("{");
        sb.AppendLine(
            $"    private {activatorInfo.ClassName}() {{}}");

        // 添加Default属性
        sb.AppendLine(
            $"    public static {GetInterfaceName(activatorInfo.TargetBaseType)} Default {{ get; }} = new {activatorInfo.ClassName}();");
        sb.AppendLine();

        // 生成类型收集字典
        sb.AppendLine(
            $"    private static readonly IDictionary<string, Func<IServiceProvider,{activatorInfo.TargetBaseType}>> _typeFactories = (new Dictionary<string, Func<IServiceProvider,{activatorInfo.TargetBaseType}>>()");
        sb.AppendLine("    {");

        // 为每个派生类型生成工厂方法
        foreach (var derivedType in derivedTypes)
            sb.AppendLine($"        {{ \"{derivedType!.FullClassName}\", p => Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<{derivedType.FullClassName}>(p) }},");

        sb.AppendLine("    }).ToFrozenDictionary();");
        sb.AppendLine();

        // 生成TryCreateInstance方法
        sb.AppendLine($"    public bool TryCreateInstance(IServiceProvider serviceProvider, string fullName, out {activatorInfo.TargetBaseType} obj)");
        sb.AppendLine("    {");
        sb.AppendLine("        obj = null;");
        sb.AppendLine("        if (_typeFactories.TryGetValue(fullName, out var factory))");
        sb.AppendLine("        {");
        sb.AppendLine("            var instance = factory(serviceProvider);");
        sb.AppendLine($"            if (instance is {activatorInfo.TargetBaseType} typedInstance)");
        sb.AppendLine("            {");
        sb.AppendLine("                obj = typedInstance;");
        sb.AppendLine("                return true;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GetInterfaceName(string targetType)
    {
        return $"ReOsuStoryboardPlayer.Avalonia.SourceGenerator.Interfaces.ITypeCollectedActivator<{targetType}>";
    }
}

internal class ActivatorInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FullClassName { get; set; } = string.Empty;
    public string TargetBaseType { get; set; } = string.Empty;
    public string TargetBaseTypeName { get; set; } = string.Empty;
    public ITypeSymbol TargetBaseTypeSymbol { get; set; } = null!;
    public string AssemblyName { get; set; } = string.Empty;
}

internal class TypeInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FullClassName { get; set; } = string.Empty;
    public ITypeSymbol TypeSymbol { get; set; } = null!;
}

internal class ActivatorGenerationInfo
{
    public ActivatorInfo ActivatorInfo { get; set; } = null!;
    public List<TypeInfo?> DerivedTypes { get; set; } = new();
}
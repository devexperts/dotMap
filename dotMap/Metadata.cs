/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using dotMap.Extensions;

namespace dotMap;

internal class Metadata
{
	private readonly Compilation _compilation;
	private readonly ImmutableArray<BaseTypeDeclarationSyntax> _types;
	private readonly SourceProductionContext _context;
	private static readonly Dictionary<ITypeSymbol, HashSet<ITypeSymbol>> _mappingsCache = new(SymbolEqualityComparer.Default);

	public Metadata(Compilation compilation, ImmutableArray<BaseTypeDeclarationSyntax> types, SourceProductionContext spc)
	{
		_compilation = compilation;
		_types = types;
		_context = spc;
	}

	public IReadOnlyList<MappingModels> GetModels()
	{
		var configurations = GetConfigurations();
		var models = new List<MappingModels>();
		foreach (var cfg in configurations)
		{
			_context.CancellationToken.ThrowIfCancellationRequested();
			models.Add(GetModelsFromConfigureMap(cfg.Type, cfg.Configurations));
		}

		return models;
	}

	private IReadOnlyList<(BaseTypeDeclarationSyntax Type, IReadOnlyList<IMapConfiguration> Configurations)> GetConfigurations()
	{
		List<(BaseTypeDeclarationSyntax, IReadOnlyList<IMapConfiguration>)> configurations = new();
		foreach (var type in _types)
		{
			_context.CancellationToken.ThrowIfCancellationRequested();
			var semanticModel = _compilation.GetSemanticModel(type.SyntaxTree);
			var cfgBuilder = new MapConfigurationBuilder(semanticModel, _context, type);
			var cfgs = cfgBuilder.Parse();
			if (cfgs.Any())
			{
				PopulateCache(cfgs);
				configurations.Add((type, cfgs));
			}
		}

		return configurations;
	}

	private void PopulateCache(IEnumerable<IMapConfiguration> cfgs)
	{
		foreach (var cfg in cfgs)
		{
			if (!_mappingsCache.TryGetValue(cfg.SourceType, out var mappings))
			{
				mappings = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
				_mappingsCache.Add(cfg.SourceType, mappings);
			}

			mappings.Add(cfg.DestType);
		}
	}

	private MappingModels GetModelsFromConfigureMap(BaseTypeDeclarationSyntax typeSyntax, IEnumerable<IMapConfiguration> configurations)
	{
		var models = configurations.Select(cfg => ProcessMapSyntax(cfg, typeSyntax)).ToList();
		var ns = GetNamespaces(typeSyntax).LastOrDefault() ?? "dotMap";
		return new MappingModels(
			GetFileName(ns, typeSyntax),
			ns,
			GetExtensionTypeName(typeSyntax),
			models);
	}

	private string GenerateGuidString() => Guid.NewGuid().ToString().Replace("-", "");
	
	private string GetFileName(string ns, SyntaxNode node)
	{
		return ns.Replace(".", "") + GetExtensionTypeName(node, "");
	}
	
	private string GetExtensionTypeName(SyntaxNode node)
	{
		return GetExtensionTypeName(node, "_");
	}
	
	private string GetExtensionTypeName(SyntaxNode node, string separator)
	{
		var typeNames = node.AncestorsAndSelf().OfType<BaseTypeDeclarationSyntax>().Select(t => t.Identifier.Text).Reverse().ToList();
		return typeNames.Any() ? string.Join(separator, typeNames) + "Extensions" : "DotMapExtensions" + GenerateGuidString();
	}

	private MappingModel ProcessMapSyntax(IMapConfiguration configuration, BaseTypeDeclarationSyntax typeSyntax)
	{
		var model = new MappingModel(GetUsings(typeSyntax), configuration.SourceType, configuration.DestType, configuration.NamingConvention, configuration.MappingMode);
		model.Parameter = configuration.GetParameter();
		model.FinallyBlock = configuration.GetFinallyBlock();
		model.ManualMappingLambda = configuration.GetManualMappingLambda();
		if (configuration.GetConstructor() is LambdaExpressionSyntax ctor)
		{
			model.Constructor = ctor;
		}

		foreach (var (member, lambda) in configuration.GetForMemberLambdas())
		{
			model.AddLambdaMember(member, lambda);
		}

		foreach (var item in GetMembers(configuration, model.MappingMode.Source))
		{
			//If the member has already been mapped we report warning and ignore the mapping
			if (model.ContainsLambdaMember(item.Context.DestinationMember))
			{
				_context.ReportDiagnostic(Diagnostic.Create(dotMapGen.RedundantForMemberUsageDiagnosticDescriptor, location: configuration.Location, item.Source.Symbol, item.Context.DestinationMember.Symbol));
			}
			else
			{
				if (item.IsConstructorDest)
				{
					model.AddCtorParameter(item.Source, item.Context);
				}
				else
				{
					model.AddMember(item.Source, item.Context);
				}

				configuration.RemoveDestMember(item.Context.DestinationMember);
			}
		}

		configuration.ReportDiagnostics(_context);
		return model;
	}

	private IEnumerable<MappingMember> GetMembers(IMapConfiguration configuration, SourceMappingMode mappingMode)
	{
		if (mappingMode is SourceMappingMode.MapPropsAndFields or SourceMappingMode.MapAllMembers)
		{
			foreach (var item in configuration.PropsAndFields)
			{
				var member = GetMember(item, mappedFromMethod: false);
				if (member != null) yield return member;
			}
		}

		if (mappingMode is SourceMappingMode.MapMethods or SourceMappingMode.MapAllMembers)
		{
			foreach (var item in configuration.Methods)
			{
				var member = GetMember(item, mappedFromMethod: true);
				if (member != null) yield return member;
			}
		}

		if (configuration.DestType.EnumUnderlyingType != null)
		{
			foreach (var item in configuration.EnumFields)
			{
				var member = GetMember(item, mappedFromMethod: false, mappedFromEnum: true);
				if (member != null) yield return member;
			}
		}
	}

	private MappingMember? GetMember(MapConfiguration.MappingMember member, bool mappedFromMethod, bool mappedFromEnum = false)
	{
		if (mappedFromEnum)
		{
			return new MappingMember(member.Source, new MemberMappingContext(member.Dest, CascadeMapping.None, mappedFromMethod), member.IsConstructorDest);
		}
		else if (member.Dest.Type.IsConvertible(member.Source.Type, _compilation))
		{
			return new MappingMember(member.Source, new MemberMappingContext(member.Dest, CascadeMapping.None, mappedFromMethod), member.IsConstructorDest);
		}
		else
		{
			var sourceType = RemoveNullability(member.Source.Type);
			var destType = RemoveNullability(member.Dest.Type);
			if (HasMappingSyntax(sourceType, destType))
			{
				var cascadeMapping = sourceType.IsValueType ? CascadeMapping.ValueType : CascadeMapping.ReferenceType;
				return new MappingMember(member.Source, new MemberMappingContext(new TypedMemberSymbol(member.Dest.Symbol, destType), cascadeMapping, mappedFromMethod), member.IsConstructorDest);
			}
		}

		return null;
	}

	private ITypeSymbol RemoveNullability(ITypeSymbol type)
	{
		return type.IsValueType && type.NullableAnnotation == NullableAnnotation.Annotated && type is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1
			? namedType.TypeArguments.Single()
			: type;
	}

	private bool HasMappingSyntax(ITypeSymbol srcTypeSymbol, ITypeSymbol dstTypeSymbol)
	{
		return _mappingsCache.TryGetValue(srcTypeSymbol, out var mappings) && mappings.Contains(dstTypeSymbol);
	}

	private IReadOnlyList<string> GetUsings(SyntaxNode mapSyntax, string template = "{0} {1};", SyntaxKind? prohibited = SyntaxKind.PrivateKeyword)
	{
		var namespaces = GetNamespaces(mapSyntax);
		var usings = mapSyntax.SyntaxTree.GetCompilationUnitRoot().Usings.Select(u => string.Format(template, "using", u.Name?.ToFullString())).Concat(namespaces.Select(ns => string.Format(template, "using", ns)));
		var types = mapSyntax.AncestorsAndSelf().OfType<BaseTypeDeclarationSyntax>().Reverse().ToList();
		return types.Any() ? usings.Concat(GetStaticUsings(namespaces.Last(), types, template, prohibited)).ToList() : usings.ToList();
	}

	private IEnumerable<string> GetNamespaces(SyntaxNode mapSyntax)
	{
		var namespaces = mapSyntax.AncestorsAndSelf().OfType<BaseNamespaceDeclarationSyntax>().Select(ns => ns.Name.ToFullString().Trim()).Reverse().SelectMany(ns => ns.Split('.')).ToArray().ToArray();
		for (var i = 0; i < namespaces.Length; i++)
		{
			yield return $"{string.Join(".", namespaces.Take(i + 1))}";
		}
	}

	private IEnumerable<string> GetStaticUsings(string ns, IReadOnlyList<BaseTypeDeclarationSyntax> types, string template, SyntaxKind? prohibited)
	{
		for (var i = 0; i < types.Count; i++)
		{
			if (prohibited != null && types[i].Modifiers.Any(m => m.IsKind(prohibited.Value))) yield break;
			yield return string.Format(template, "using static", $"{ns}.{string.Join(".", types.Take(i + 1).Select(t => t.Identifier.Text))}");
		}
	}

	private record MappingMember(TypedMemberSymbol Source, MemberMappingContext Context, bool IsConstructorDest);
}
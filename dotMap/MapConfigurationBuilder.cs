/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotMap;

internal class MapConfigurationBuilder
{
	private readonly SemanticModel _semanticModel;
	private readonly SourceProductionContext _context;
	private readonly BaseTypeDeclarationSyntax _typeSyntax;
	private readonly Lazy<INamedTypeSymbol?> _typeSymbol;

	public MapConfigurationBuilder(SemanticModel semanticModel, SourceProductionContext context, BaseTypeDeclarationSyntax typeSyntax)
	{
		_semanticModel = semanticModel;
		_context = context;
		_typeSyntax = typeSyntax;
		_typeSymbol = new Lazy<INamedTypeSymbol?>(() => _semanticModel.GetDeclaredSymbol(_typeSyntax) as INamedTypeSymbol);
	}

	public IReadOnlyList<IMapConfiguration> Parse()
	{
		return _typeSyntax.IsInheritedFrom(nameof(IMapConfig), nameof(IMappable<object>)) && _typeSyntax.TryGetMethod(nameof(IMapConfig.ConfigureMap), out var configureMapMethod) && configureMapMethod != null
			? Parse(configureMapMethod).ToList()
			: GetDeclarativeConfiguration().ToList();
	}

	private IEnumerable<ImperativeMapConfiguration> Parse(MethodDeclarationSyntax configureMapMethod)
	{
		var calls = configureMapMethod.DescendantNodes()
			.OfType<GenericNameSyntax>()
			.Where(syntax => syntax.Identifier.ValueText is nameof(MapConfig<object>.To) or nameof(MapConfig<object>.From))
			.SelectNonNullable(syntax => syntax.FirstAncestorOrSelf<InvocationExpressionSyntax>() is InvocationExpressionSyntax expr ? new
			{
				Direction = syntax.Identifier.ValueText,
				Syntax = expr
			} : null)
			.SelectNonNullable(model => model.Syntax.FirstAncestorOrSelf<ExpressionStatementSyntax>() is ExpressionStatementSyntax expr ? new
			{
				model.Direction,
				Syntax = expr,
				Type = _semanticModel.GetTypeInfo(model.Syntax).Type as INamedTypeSymbol
			} : null);

		foreach (var call in calls)
		{
			if (call.Type != null && call.Type.Name == nameof(Map<object, object>) &&
				call.Type.ContainingNamespace.Name == nameof(dotMap) && call.Type.TypeArguments.Length == 2 &&
				call.Type.TypeArguments[0] is INamedTypeSymbol firstArgument &&
				call.Type.TypeArguments[1] is INamedTypeSymbol secondArgument)
			{
				yield return new ImperativeMapConfiguration(firstArgument, secondArgument, call.Syntax, _semanticModel, _context);
			}
		}
	}

	private IEnumerable<DeclarativeMapConfiguration> GetDeclarativeConfiguration()
	{
		var cfgModels = ParseAttributes().ToList();
		var multiSources = cfgModels.GroupBy(m => m.DestType, SymbolEqualityComparer.Default).ToDictionary(m => m.Key, m => m.Count(), SymbolEqualityComparer.Default);
		foreach (var cfg in cfgModels)
		{
			yield return new DeclarativeMapConfiguration(cfg.SourceType, cfg.DestType, cfg.Attribute, _semanticModel, _context, multiSources.TryGetValue(cfg.DestType, out var dstSources) && dstSources > 1);
		}
	}

	private IEnumerable<DeclarativeMapModel> ParseAttributes()
	{
		if (_typeSymbol.Value != null)
		{
			foreach (var attrSyntax in _typeSyntax.GetAttributes<MapToAttribute>(_semanticModel))
			{
				var type = GetType(attrSyntax);
				if (type != null)
				{
					yield return new DeclarativeMapModel(_typeSymbol.Value, type, attrSyntax);
				}
			}

			foreach (var attrSyntax in _typeSyntax.GetAttributes<MapFromAttribute>(_semanticModel))
			{
				var type = GetType(attrSyntax);
				if (type != null)
				{
					yield return new DeclarativeMapModel(type, _typeSymbol.Value, attrSyntax);
				}
			}
		}
	}

	private INamedTypeSymbol? GetType(AttributeSyntax attribute)
	{
		if (attribute.ArgumentList != null && attribute.ArgumentList.Arguments.Count > 0 &&
					attribute.ArgumentList.Arguments[0].ChildNodes().OfType<TypeOfExpressionSyntax>().FirstOrDefault() is TypeOfExpressionSyntax typeOfSyntax)
		{
			if (typeOfSyntax.GetNamedTypeSymbol(_semanticModel) is INamedTypeSymbol namedDestType)
			{
				return namedDestType;
			}
		}

		return null;
	}

	private record DeclarativeMapModel(INamedTypeSymbol SourceType, INamedTypeSymbol DestType, AttributeSyntax Attribute);
}

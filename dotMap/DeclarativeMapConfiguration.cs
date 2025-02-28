/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.Extensions;
using dotMap.NamingConventions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotMap;

internal class DeclarativeMapConfiguration : MapConfiguration, IMapConfiguration
{
	private readonly Lazy<HashSet<TypedMemberSymbol>> _destCtorParameters;
	private readonly Lazy<HashSet<TypedMemberSymbol>> _destMembers;
	private readonly AttributeSyntax _attribute;
	private readonly SourceProductionContext _context;
	private readonly bool _multipleSources;
	private readonly Lazy<MappingNamingConvention> _namingConvention;
	private readonly Lazy<MappingMode> _mappingMode;
	private readonly List<TypedMemberSymbol> _invalidIgnorings;
	private readonly dotMapDefaults.Defaults _defaults;
	private readonly List<string> _invalidParameters;

	public DeclarativeMapConfiguration(INamedTypeSymbol sourceType, INamedTypeSymbol destType, AttributeSyntax attribute, SemanticModel semanticModel, SourceProductionContext context, bool multipleSources)
		: base(sourceType, destType, semanticModel)
	{
		_defaults = dotMapDefaults.GetOrAdd(semanticModel.Compilation.Assembly);
		_destCtorParameters = new(GetDestCtorParameters);
		_destMembers = new(GetDestMembers);
		_attribute = attribute;
		_context = context;
		_multipleSources = multipleSources;
		_namingConvention = new Lazy<MappingNamingConvention>(GetNamingConvention);
		_mappingMode = new Lazy<MappingMode>(GetMappingMode);
		_invalidIgnorings = new();
		_invalidParameters = new();
	}

	private HashSet<TypedMemberSymbol> GetDestMembers()
	{
		if (DestType.EnumUnderlyingType != null)
		{
			return new HashSet<TypedMemberSymbol>(DestType.GetMappableEnumFields().Where(m => !IsIgnored(m)));
		}
		else if (MappingMode.Dest is DestMappingMode.MapToPropsAndFields or DestMappingMode.MapToAllAvailableMembers)
		{
			return new HashSet<TypedMemberSymbol>(DestType.GetMappablePropsAndFields(excludeReadOnly: true).Where(m => !IsIgnored(m)));
		}

		return new HashSet<TypedMemberSymbol>();
	}

	private bool IsIgnored(TypedMemberSymbol member)
	{
		var attributes = member.Symbol.GetAttributes<IgnoreAttribute>().ToList();
		foreach (var ignoreFor in attributes.SelectMany(attr => attr.NamedArguments).Where(attr => attr.Key == nameof(IgnoreAttribute.For)))
		{
			if (ignoreFor.Value.Value is INamedTypeSymbol type && SymbolEqualityComparer.Default.Equals(type, SourceType))
			{
				return true;
			}
		}

		var hasIgnoreWithoutFor = attributes.Any(attr => attr.NamedArguments.Length == 0);
		if (_multipleSources && hasIgnoreWithoutFor)
		{
			_invalidIgnorings.Add(member);
		}
		return hasIgnoreWithoutFor;
	}

	private HashSet<TypedMemberSymbol> GetDestCtorParameters()
	{
		return MappingMode.Dest is DestMappingMode.MapToConstructor or DestMappingMode.MapToAllAvailableMembers
			? new HashSet<TypedMemberSymbol>(DestType.GetMappableConstructorParameters())
			: new HashSet<TypedMemberSymbol>();
	}

	public MappingMode MappingMode => _mappingMode.Value;

	public MappingNamingConvention NamingConvention => _namingConvention.Value;

	public Location Location => _attribute.GetLocation();

	public IReadOnlyList<MappingMember> Methods => GetMethods(_destCtorParameters.Value, _destMembers.Value, NamingConvention).ToList();

	public IReadOnlyList<MappingMember> PropsAndFields => GetPropsAndFields(_destCtorParameters.Value, _destMembers.Value, NamingConvention).ToList();
	
	public IReadOnlyList<MappingMember> EnumFields => GetEnumFields(_destCtorParameters.Value, _destMembers.Value, NamingConvention).ToList();

	public LambdaExpressionSyntax? GetConstructor() => null;

	public LambdaExpressionSyntax? GetFinallyBlock() => null;

	public LambdaExpressionSyntax? GetManualMappingLambda() => null;

	private MappingNamingConvention GetNamingConvention()
	{
		return new MappingNamingConvention(
			GetNamingConvention(nameof(MapAttribute.DestinationMembersNamingConvention)) ?? _defaults.GetDestMembersNamingConvention(DestType.GetFullName()),
			GetNamingConvention(nameof(MapAttribute.DestinationConstructorNamingConvention)) ?? _defaults.GetDestConstructorNamingConvention(DestType.GetFullName()));
	}

	private INamingConvention? GetNamingConvention(string memberName)
	{
		var typeOfExpression = LastOrDefault<TypeOfExpressionSyntax>(memberName);
		return typeOfExpression != null && typeOfExpression.GetNamedTypeSymbol(SemanticModel) is INamedTypeSymbol type
			? NamingConventionCache.GetNamingConvention(type, SemanticModel, _context) : null;
	}

	private T? LastOrDefault<T>(string memberName) where T : class
	{
		return _attribute.ArgumentList?.Arguments.LastOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == memberName)?.Expression as T;
	}

	private MappingMode GetMappingMode()
	{
		var srcExpr = LastOrDefault<MemberAccessExpressionSyntax>(nameof(MapAttribute.SourceMappingMode));
		var dstExpr = LastOrDefault<MemberAccessExpressionSyntax>(nameof(MapAttribute.DestMappingMode));

		if (IsEnumMapConfig)
		{
			if (srcExpr != null) _invalidParameters.Add(nameof(MapAttribute.SourceMappingMode));
			if (dstExpr != null) _invalidParameters.Add(nameof(MapAttribute.DestMappingMode));
		}

		var src = srcExpr != null && Enum.TryParse<SourceMappingMode>(srcExpr.Name.Identifier.ValueText, out var srcMappingMode)
			? srcMappingMode : _defaults.GetSourceMappingMode(SourceType.GetFullName());
		var dst = dstExpr != null && Enum.TryParse<DestMappingMode>(dstExpr.Name.Identifier.ValueText, out var dstMappingMode)
			? dstMappingMode : _defaults.GetDestMappingMode(DestType.GetFullName());
		return new MappingMode(src, dst);
	}

	public IEnumerable<(TypedMemberSymbol Member, ArgumentSyntax Lambda)> GetForMemberLambdas() => Enumerable.Empty<(TypedMemberSymbol, ArgumentSyntax)>();

	public void ReportDiagnostics(SourceProductionContext context)
	{
		foreach (var member in _destCtorParameters.Value.Concat(_destMembers.Value))
		{
			context.ReportDiagnostic(Diagnostic.Create(dotMapGen.UnmappedMemberDiagnosticDescriptor, Location, $"'{member.GetDisplayString()}'"));
		}

		foreach (var member in _invalidIgnorings)
		{
			context.ReportDiagnostic(Diagnostic.Create(dotMapGen.InvalidIgnoreDiagnosticDescriptor, Location, $"'{member.GetDisplayString()}'", SourceType.Name));
		}

		foreach (var parameter in _invalidParameters)
		{
			context.ReportDiagnostic(Diagnostic.Create(dotMapGen.InvalidEnumFeatureUsage, Location, parameter, "mapping mode cannot be set for enum configuration"));
		}
	}

	public INamedTypeSymbol? GetParameter() => null;

	public void RemoveDestMember(TypedMemberSymbol member)
		=> _destMembers.Value.Remove(member);
}

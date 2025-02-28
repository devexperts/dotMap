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

internal class ImperativeMapConfiguration : MapConfiguration, IMapConfiguration
{
	private readonly Lazy<MappingNamingConvention> _namingConvention;
	private readonly Lazy<MappingMode> _mappingMode;
	private readonly Lazy<HashSet<TypedMemberSymbol>> _destCtorParameters;
	private readonly Lazy<HashSet<TypedMemberSymbol>> _destMembers;
	private readonly ExpressionStatementSyntax _configuration;
	private readonly dotMapDefaults.Defaults _defaults;
	private readonly Lazy<IReadOnlyList<MappingMember>> _propsAndFields;
	private readonly Lazy<IReadOnlyList<MappingMember>> _methods;
	private readonly Lazy<IReadOnlyList<MappingMember>> _enumFields;
	private readonly List<string> _invalidExpressions;

	public ImperativeMapConfiguration(INamedTypeSymbol sourceType, INamedTypeSymbol destType, ExpressionStatementSyntax configuration, SemanticModel semanticModel, SourceProductionContext context)
		: base(sourceType, destType, semanticModel)
	{
		_defaults = dotMapDefaults.GetOrAdd(semanticModel.Compilation.Assembly);
		_configuration = configuration;
		var parser = new DefaultsParser(configuration, semanticModel, context);
		_namingConvention = new(() => GetNamingConvention(parser));
		_mappingMode = new(() => GetMappingMode(parser));
		_destCtorParameters = new(GetDestCtorParameters);
		_destMembers = new(GetDestMembers);
		_propsAndFields = new(() => GetPropsAndFields(_destCtorParameters.Value, _destMembers.Value, _namingConvention.Value).ToList());
		_methods = new(() => GetMethods(_destCtorParameters.Value, _destMembers.Value, NamingConvention).ToList());
		_enumFields = new(() => GetEnumFields(_destCtorParameters.Value, _destMembers.Value, _namingConvention.Value).ToList());
		_invalidExpressions = new();
	}

	private MappingNamingConvention GetNamingConvention(DefaultsParser parser)
	{
		var conventions = parser.GetNamingConvention();
		return new MappingNamingConvention(
			conventions.Members ?? _defaults.GetDestMembersNamingConvention(DestType.GetFullName()),
			conventions.Ctor ?? _defaults.GetDestConstructorNamingConvention(DestType.GetFullName()));
	}

	private MappingMode GetMappingMode(DefaultsParser parser)
	{
		var modes = parser.GetMappingMode();

		if(IsEnumMapConfig && (modes.Source != null || modes.Dest != null))
		{
			_invalidExpressions.Add(nameof(Map<object, object>.WithMappingMode));
		}

		return new MappingMode(
			modes.Source ?? _defaults.GetSourceMappingMode(SourceType.GetFullName()),
			modes.Dest ?? _defaults.GetDestMappingMode(DestType.GetFullName()));
	}

	public MappingNamingConvention NamingConvention => _namingConvention.Value;

	public MappingMode MappingMode => _mappingMode.Value;

	public Location Location => _configuration.GetLocation();

	public IReadOnlyList<MappingMember> PropsAndFields => _propsAndFields.Value;

	public IReadOnlyList<MappingMember> Methods => _methods.Value;

	public IReadOnlyList<MappingMember> EnumFields => _enumFields.Value;

	private HashSet<TypedMemberSymbol> GetDestMembers()
	{
		var ignored = new HashSet<ISymbol>(GetIgnoredMembers(_configuration, DestType), SymbolEqualityComparer.Default);
		if (DestType.EnumUnderlyingType != null)
		{
			return new HashSet<TypedMemberSymbol>(DestType.GetMappableEnumFields().Where(m => !ignored.Contains(m.Symbol, SymbolEqualityComparer.Default)));
		}
		else if (MappingMode.Dest is DestMappingMode.MapToPropsAndFields or DestMappingMode.MapToAllAvailableMembers)
		{
			return new HashSet<TypedMemberSymbol>(DestType.GetMappablePropsAndFields(excludeReadOnly: true).Where(m => !ignored.Contains(m.Symbol, SymbolEqualityComparer.Default)));
		}
		
		return new HashSet<TypedMemberSymbol>();
	}

	private HashSet<TypedMemberSymbol> GetDestCtorParameters()
	{
		if (MappingMode.Dest is DestMappingMode.MapToConstructor or DestMappingMode.MapToAllAvailableMembers)
		{
			return new HashSet<TypedMemberSymbol>(DestType.GetMappableConstructorParameters());
		}

		return new HashSet<TypedMemberSymbol>();
	}

	private static IEnumerable<ISymbol> GetIgnoredMembers(ExpressionStatementSyntax cfg, INamedTypeSymbol destType)
	{
		var ignoreCalls = cfg.GetDescendantMemberCalls(nameof(Map<object, object>.Ignore));
		foreach (var lambdaIdentifier in ignoreCalls)
		{
			var args = lambdaIdentifier.GetParentInvocationArguments();
			if (args != null && args.Count > 0 && GetMemberNameFromLambda(args[0]) is string propName && destType.GetMembers(propName).SingleOrDefault() is ISymbol ignored)
			{
				yield return ignored;
			}
		}
	}

	public INamedTypeSymbol? GetParameter()
	{
		var parameterCall = _configuration.GetDescendantMemberCalls(nameof(Map<object, object>.WithParameter)).LastOrDefault();
		if (parameterCall != null && SemanticModel.GetSymbolInfo(parameterCall).Symbol is IMethodSymbol method && method.ReturnType is INamedTypeSymbol returnType
			&& returnType.TypeArguments.Length == 3 && returnType.TypeArguments[2] is INamedTypeSymbol parameterType)
		{
			if (IsEnumMapConfig) 
				_invalidExpressions.Add(method.ToDisplayString(new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters)));
			return parameterType;
		}

		return null;
	}

	public LambdaExpressionSyntax? GetConstructor()
		=> GetLambda(nameof(Map<object, object>.ConstructFrom));

	public LambdaExpressionSyntax? GetFinallyBlock()
		=> GetLambda(nameof(Map<object, object>.Finally));

	public LambdaExpressionSyntax? GetManualMappingLambda()
		=> GetLambda(nameof(Map<object, object>.With)); 

	private LambdaExpressionSyntax? GetLambda(string memberName)
	{
		var invocation = _configuration.GetDescendantMemberCalls(memberName).LastOrDefault();
		if (invocation != null)
		{
			if (memberName != nameof(Map<object, object>.With) && IsEnumMapConfig)
				_invalidExpressions.Add(invocation.Identifier.ValueText);

			var args = invocation.GetParentInvocationArguments();
			if (args is { Count: > 0 } && args[0].ChildNodes().FirstOrDefault() is LambdaExpressionSyntax lambda)
			{
				return lambda;
			}
		}

		return null;
	}

	public IEnumerable<(TypedMemberSymbol Member, ArgumentSyntax Lambda)> GetForMemberLambdas()
	{
		var forMemberCalls = _configuration.GetDescendantMemberCalls(nameof(Map<object, object>.ForMember));
		foreach (var forMemberCall in forMemberCalls)
		{
			if(IsEnumMapConfig) _invalidExpressions.Add(forMemberCall.Identifier.ValueText);

			var args = forMemberCall.GetParentInvocationArguments();
			if (args != null && args.Count > 1 && GetMemberNameFromLambda(args[0]) is string propName)
			{
				var lambda = args[1];
				if (_destMembers.Value.SingleOrDefault(s => s.Symbol.Name == propName) is TypedMemberSymbol member)
				{
					_destMembers.Value.Remove(member);
					yield return (member, lambda);
				}
			}
		}
	}

	private static string? GetMemberNameFromLambda(ArgumentSyntax arg)
	{
		return arg.DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault()?.Identifier.ValueText;
	}

	public void ReportDiagnostics(SourceProductionContext context)
	{
		foreach (var member in _destCtorParameters.Value.Concat(_destMembers.Value))
		{
			context.ReportDiagnostic(Diagnostic.Create(dotMapGen.UnmappedMemberDiagnosticDescriptor, Location, $"'{member.GetDisplayString()}'"));
		}

		foreach (var expression in _invalidExpressions)
		{
			context.ReportDiagnostic(Diagnostic.Create(dotMapGen.InvalidEnumFeatureUsage, Location, expression, "feature cannot be used for enum configuration"));
		}
	}

	public void RemoveDestMember(TypedMemberSymbol member)
		=> _destMembers.Value.Remove(member);
}

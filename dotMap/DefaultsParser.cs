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

internal class DefaultsParser
{
	private readonly SyntaxNode _configuration;
	private readonly SemanticModel _semanticModel;
	private readonly SourceProductionContext _context;

	public DefaultsParser(SyntaxNode configuration, SemanticModel semanticModel, SourceProductionContext context)
	{
		_configuration = configuration;
		_semanticModel = semanticModel;
		_context = context;
	}

	public (SourceMappingMode? Source, DestMappingMode? Dest) GetMappingMode()
	{
		SourceMappingMode? source = null;
		DestMappingMode? dest = null;
		var call = _configuration.GetDescendantMemberCalls(nameof(Map<object, object>.WithMappingMode)).LastOrDefault();
		if (call != null)
		{
			var args = call.GetParentInvocationArguments();
			if (args != null && args.Count > 0 && args[0].Expression is MemberAccessExpressionSyntax srcExpr &&
				Enum.TryParse<SourceMappingMode>(srcExpr.Name.Identifier.ValueText, out var srcMappingMode))
			{
				source = srcMappingMode;
				if (args.Count > 1 && args[1].Expression is MemberAccessExpressionSyntax dtsExpr &&
					Enum.TryParse<DestMappingMode>(dtsExpr.Name.Identifier.ValueText, out var dstMappingMode))
				{
					dest = dstMappingMode;
				}
			}
		}

		return (source, dest);
	}

	public (INamingConvention? Members, INamingConvention? Ctor) GetNamingConvention()
	{
		var call = _configuration.GetDescendantMemberCalls(nameof(Map<object, object>.WithDestinationNamingConvention)).LastOrDefault();
		INamingConvention? members = null, ctor = null;
		if (call is GenericNameSyntax genericCall && genericCall.TypeArgumentList.Arguments.Count > 0)
		{
			members = GetNamingConvention(genericCall.TypeArgumentList.Arguments[0]);
			if (genericCall.TypeArgumentList.Arguments.Count > 1)
			{
				ctor = GetNamingConvention(genericCall.TypeArgumentList.Arguments[1]);
			}
		}

		return (members, ctor);
	}

	private INamingConvention? GetNamingConvention(TypeSyntax argument)
	{
		return _semanticModel.GetSymbolInfo(argument).Symbol is INamedTypeSymbol typeSymbol ? NamingConventionCache.GetNamingConvention(typeSymbol, _semanticModel, _context) : null;
	}
}

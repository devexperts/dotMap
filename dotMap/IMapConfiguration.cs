/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static dotMap.MapConfiguration;

namespace dotMap;

internal interface IMapConfiguration
{
	INamedTypeSymbol SourceType { get; }
	INamedTypeSymbol DestType { get; }
	MappingMode MappingMode { get; }
	MappingNamingConvention NamingConvention { get; }
	Location Location { get; }
	IReadOnlyList<MappingMember> Methods { get; }
	IReadOnlyList<MappingMember> PropsAndFields { get; }
	IReadOnlyList<MappingMember> EnumFields { get; }

	INamedTypeSymbol? GetParameter();
	LambdaExpressionSyntax? GetConstructor();
	LambdaExpressionSyntax? GetFinallyBlock();
	LambdaExpressionSyntax? GetManualMappingLambda();
	IEnumerable<(TypedMemberSymbol Member, ArgumentSyntax Lambda)> GetForMemberLambdas();
	void ReportDiagnostics(SourceProductionContext context);
	void RemoveDestMember(TypedMemberSymbol member);
}
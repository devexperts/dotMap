/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.NamingConventions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotMap;

internal enum CascadeMapping { None, ReferenceType, ValueType };
internal record MappingMode(SourceMappingMode Source, DestMappingMode Dest);
internal record MappingNamingConvention(INamingConvention MembersConvention, INamingConvention ConstructorConvention);
internal record MemberMappingContext(TypedMemberSymbol DestinationMember, CascadeMapping CascadeMapping, bool MappedFromMethod);
internal record TypedMemberSymbol(ISymbol Symbol, ITypeSymbol Type)
{
	bool IEquatable<TypedMemberSymbol>.Equals(TypedMemberSymbol other)
		=> SymbolEqualityComparer.Default.Equals(Symbol, other.Symbol);

	public override int GetHashCode()
		=> SymbolEqualityComparer.Default.GetHashCode(Symbol);
	
}
internal record MappingModels(string FileName, string? Namespace, string ExtensionTypeName, IReadOnlyCollection<MappingModel> Models);
internal record MappingModel(IReadOnlyList<string> TypeUsings, INamedTypeSymbol MapFromType, INamedTypeSymbol MapToType, MappingNamingConvention NamingConvention, MappingMode MappingMode)
{
	public INamedTypeSymbol? Parameter { get; set; }

	public LambdaExpressionSyntax? Constructor { get; set; }

	public LambdaExpressionSyntax? FinallyBlock { get; set; }
	
	public LambdaExpressionSyntax? ManualMappingLambda { get; set; }

	public Dictionary<TypedMemberSymbol, MemberMappingContext> ConstructorParameters { get; } = new();

	public Dictionary<TypedMemberSymbol, MemberMappingContext> Members { get; } = new();

	public Dictionary<TypedMemberSymbol, ArgumentSyntax> MethodGroupMembers { get; } = new();

	public Dictionary<TypedMemberSymbol, LambdaExpressionSyntax> LambdaMembers { get; } = new();

	public void AddCtorParameter(TypedMemberSymbol member, MemberMappingContext propertyMappingContext)
	{
		ConstructorParameters.Add(member, propertyMappingContext);
	}

	public void AddMember(TypedMemberSymbol member, MemberMappingContext propertyMappingContext)
	{
		Members.Add(member, propertyMappingContext);
	}

	public void AddLambdaMember(TypedMemberSymbol member, ArgumentSyntax lambdaSyntax)
	{
		if (lambdaSyntax.ChildNodes().FirstOrDefault() is LambdaExpressionSyntax lambda)
		{
			LambdaMembers.Add(member, lambda);
		}
		else
		{
			MethodGroupMembers.Add(member, lambdaSyntax);
		}
	}

	internal bool ContainsLambdaMember(TypedMemberSymbol typedMember)
		=> LambdaMembers.ContainsKey(typedMember) || MethodGroupMembers.ContainsKey(typedMember);
}
/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.Extensions;
using Microsoft.CodeAnalysis;

namespace dotMap;

internal abstract class MapConfiguration
{
	protected MapConfiguration(INamedTypeSymbol sourceType, INamedTypeSymbol destType, SemanticModel semanticModel)
	{
		SourceType = sourceType;
		DestType = destType;
		SemanticModel = semanticModel;
	}

	public INamedTypeSymbol SourceType { get; }

	public INamedTypeSymbol DestType { get; }

	public bool IsEnumMapConfig => DestType.EnumUnderlyingType != null || SourceType.EnumUnderlyingType != null;

	protected SemanticModel SemanticModel { get; }

	protected IEnumerable<MappingMember> GetPropsAndFields(ISet<TypedMemberSymbol> destCtorParameters, ISet<TypedMemberSymbol> destMembers, MappingNamingConvention namingConvention)
		=> GetMembers(SourceType.GetMappablePropsAndFields(excludeReadOnly: false), destCtorParameters, destMembers, namingConvention);

	protected IEnumerable<MappingMember> GetMethods(ISet<TypedMemberSymbol> destCtorParameters, ISet<TypedMemberSymbol> destMembers, MappingNamingConvention namingConvention)
		=> GetMembers(SourceType.GetMappableMethods(), destCtorParameters, destMembers, namingConvention);

	protected IEnumerable<MappingMember> GetEnumFields(ISet<TypedMemberSymbol> destCtorParameters, ISet<TypedMemberSymbol> destMembers, MappingNamingConvention namingConvention)
		=> GetMembers(SourceType.GetMappableEnumFields(), destCtorParameters, destMembers, namingConvention);

	private IEnumerable<MappingMember> GetMembers(IEnumerable<TypedMemberSymbol> sourceSymbols, ISet<TypedMemberSymbol> destCtorParameters, ISet<TypedMemberSymbol> destMembers, MappingNamingConvention namingConvention)
	{
		foreach (var source in sourceSymbols)
		{
			if (source.GetMatchingSymbol(destCtorParameters, namingConvention.ConstructorConvention, out var matchingSymbol) && matchingSymbol is TypedMemberSymbol matchingCtorPrm)
			{
				destCtorParameters.Remove(matchingCtorPrm);
				yield return new MappingMember(source, matchingCtorPrm, IsConstructorDest: true);
			}
			else if (source.GetMatchingSymbol(destMembers, namingConvention.MembersConvention, out matchingSymbol) && matchingSymbol is TypedMemberSymbol matchingMember)
			{
				yield return new MappingMember(source, matchingMember, IsConstructorDest: false);
			}
		}
	}

	public record MappingMember(TypedMemberSymbol Source, TypedMemberSymbol Dest, bool IsConstructorDest);
}

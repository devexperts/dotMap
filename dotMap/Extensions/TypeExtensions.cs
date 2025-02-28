/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotMap.Extensions;

internal static class TypeExtensions
{
	internal static IEnumerable<TypedMemberSymbol> GetMappablePropsAndFields(this INamedTypeSymbol typeSymbol, bool excludeReadOnly)
	{
		var fields = typeSymbol.GetMembers().OfType<IFieldSymbol>();
		var props = typeSymbol.GetMembers().OfType<IPropertySymbol>();
		if (excludeReadOnly) props = props.Where(p => !p.IsReadOnly);
		return fields.Select(f => new TypedMemberSymbol(f, f.Type)).Concat(props.Select(p => new TypedMemberSymbol(p, p.Type)))
			.Where(m => !m.Symbol.IsStatic && m.Symbol.DeclaredAccessibility != Accessibility.Private);
	}

	internal static IEnumerable<TypedMemberSymbol> GetMappableMethods(this INamedTypeSymbol typeSymbol)
	{
		return typeSymbol.GetMembers().OfType<IMethodSymbol>()
			.Where(m => m.MethodKind == MethodKind.Ordinary
						&& !m.IsGenericMethod
						&& !m.IsStatic
						&& !m.ReturnsVoid
						&& m.Parameters.IsEmpty
						&& m.DeclaredAccessibility != Accessibility.Private)
			.Select(s => new TypedMemberSymbol(s, s.ReturnType));
	}

	internal static IEnumerable<TypedMemberSymbol> GetMappableConstructorParameters(this INamedTypeSymbol typeSymbol)
	{
		var ctors = typeSymbol.Constructors.Where(c => (int)c.DeclaredAccessibility > 3);
		if (typeSymbol.IsRecord)
		{
			ctors = ctors.Where(c => c.Parameters.Length != 1 || !SymbolEqualityComparer.Default.Equals(c.Parameters.Single().Type, typeSymbol));
		}

		var filteredCtors = ctors.ToArray();
		return filteredCtors.Length == 1
			? filteredCtors.Single().Parameters.Select(p => new TypedMemberSymbol(p, p.Type))
			: Enumerable.Empty<TypedMemberSymbol>();
	}

	internal static IEnumerable<TypedMemberSymbol> GetMappableEnumFields(this INamedTypeSymbol typeSymbol)
	{
		var fields = typeSymbol.GetMembers().OfType<IFieldSymbol>();
		return fields
			.Where(f => f.IsStatic && f.DeclaredAccessibility != Accessibility.Private)
			.Select(f => new TypedMemberSymbol(f, f.Type));
	}

	public static bool IsConvertible(this ITypeSymbol destType, ITypeSymbol sourceType, Compilation compilation)
	{
		return compilation.ClassifyConversion(sourceType, destType).IsImplicit;
	}

	public static bool IsInheritedFrom(this BaseTypeDeclarationSyntax thisType, params string[] baseTypes)
	{
		var currentBaseTypes = thisType.BaseList?.Types;
		if (currentBaseTypes is null) return false;

		var baseTypesIdentifiers = currentBaseTypes
			.Value
			.Where(k => k.Type is SimpleNameSyntax)
			.Select(k => (SimpleNameSyntax)k.Type)
			.Select(k => k.Identifier.ValueText)
			.ToList();

		return baseTypes.Any(k => baseTypesIdentifiers.Any(e => e.Contains(k)));
	}

	public static string GetFullName(this ITypeSymbol type) => type.ToDisplayString().TrimEnd('?');

	public static string GetFullNamespace(this ITypeSymbol type) => type.ContainingNamespace.ToDisplayString();

	public static bool HasAttributeName<T>(this BaseTypeDeclarationSyntax type) where T : Attribute
	{
		return type.GetAttributes<T>().Any();
	}

	public static IEnumerable<AttributeSyntax> GetAttributes<T>(this BaseTypeDeclarationSyntax type, SemanticModel semanticModel) where T : Attribute
	{
		var fullAttributeName = typeof(T).FullName;
		foreach (var attribute in GetAttributes<T>(type).Where(attr => semanticModel.GetTypeInfo(attr).Type is ITypeSymbol attrType && attrType.GetFullName() == fullAttributeName))
		{
			yield return attribute;
		}
	}

	private static IEnumerable<AttributeSyntax> GetAttributes<T>(this BaseTypeDeclarationSyntax type) where T : Attribute
	{
		var suffixedAttributeName = typeof(T).Name;
		var attributeName = suffixedAttributeName.EndsWith(nameof(Attribute)) ? suffixedAttributeName.Remove(suffixedAttributeName.Length - nameof(Attribute).Length) : suffixedAttributeName;

		foreach (var attr in type.AttributeLists.SelectMany(attrGroup => attrGroup.Attributes))
		{
			var attrName = attr.Name.ToString();
			if (attrName == attributeName || attrName == suffixedAttributeName)
			{
				yield return attr;
			}
		}
	}

	public static IEnumerable<AttributeData> GetAttributes<T>(this ISymbol type) where T : Attribute
	{
		var attributeFullName = typeof(T).FullName;
		return type.GetAttributes().Where(attr => attr.AttributeClass?.GetFullName() == attributeFullName);
	}

	public static INamedTypeSymbol? GetNamedTypeSymbol(this TypeOfExpressionSyntax typeOfSyntax, SemanticModel semanticModel)
	{
		return semanticModel.GetTypeInfo(typeOfSyntax.Type).Type as INamedTypeSymbol;
	}
	
	public static string GetMapToTypeName(this ITypeSymbol type)
	{
		var sb = new StringBuilder(type.Name);
		while (type.ContainingType != null)
		{
			type = type.ContainingType;
			sb.Insert(0, ".").Insert(0, type.Name);
		}

		return sb.Insert(0, "MapTo").Replace(".", "").ToString();
	}
}

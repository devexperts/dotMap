/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.NamingConventions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotMap.Extensions;

internal static class MemberExtensions
{
	public static IReadOnlyCollection<SimpleNameSyntax> GetMethodCallsFromLambda(this SyntaxNode node, string methodName)
	{
		if (node.Parent?.Parent is InvocationExpressionSyntax invocation && invocation.ArgumentList.Arguments.Count == 1)
		{
			var lambdaSyntax = invocation.ArgumentList.Arguments[0];
			return lambdaSyntax.GetDescendantMemberCalls(methodName);
		}

		return Array.Empty<SimpleNameSyntax>();
	}

	public static IReadOnlyList<ArgumentSyntax>? GetParentInvocationArguments(this SimpleNameSyntax identifier)
	{
		var invocation = identifier.Parent?.Parent as InvocationExpressionSyntax;
		return invocation?.ArgumentList.Arguments;
	}

	public static bool GetMatchingSymbol(this TypedMemberSymbol sourceMember, IEnumerable<TypedMemberSymbol> destMembers, INamingConvention namingConvention, out TypedMemberSymbol? matchedSymbol)
	{
		matchedSymbol = destMembers.FirstOrDefault(dest => namingConvention.Convert(sourceMember.Symbol.Name) == dest.Symbol.Name);
		return matchedSymbol != null;
	}

	public static bool TryGetMethod(this BaseTypeDeclarationSyntax type, string methodName, out MethodDeclarationSyntax? method)
	{
		method = type.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(x => x.Identifier.ValueText == methodName);
		return method != null;
	}

	public static IReadOnlyCollection<SimpleNameSyntax> GetDescendantMemberCalls(this SyntaxNode method, string methodName)
	{
		return method.DescendantNodes().OfType<SimpleNameSyntax>().Where(x => x.Identifier.ValueText == methodName).ToList();
	}

	public static string GetDisplayString(this TypedMemberSymbol member) => $"{member.Type.Name} {member.Symbol.ContainingType.GetFullName()}.{member.Symbol.Name}";
}

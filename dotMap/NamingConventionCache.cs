/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.Extensions;
using dotMap.NamingConventions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace dotMap;

internal static class NamingConventionCache
{
	private static readonly Dictionary<string, INamingConvention> _namingConventionsCache = new();

	public static INamingConvention? GetNamingConvention(INamedTypeSymbol typeSymbol, SemanticModel semanticModel, SourceProductionContext context)
	{
		var typeName = typeSymbol.GetFullName();
		if (!_namingConventionsCache.TryGetValue(typeName, out var namingConvention))
		{
			var type = Assembly.GetExecutingAssembly().GetType(typeName);
			if (type != null)
			{
				namingConvention = Activator.CreateInstance(type) as INamingConvention;
				if (namingConvention != null)
				{
					_namingConventionsCache.Add(typeName, namingConvention);
				}
			}
			else
			{
				var syntaxTrees = GetSyntaxTrees(semanticModel.Compilation, new HashSet<string>(GetAllTypesInUse(typeSymbol).Select(t => t.Name))).Distinct().ToList();
				if (syntaxTrees.Any())
				{
					var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOverflowChecks(true).WithOptimizationLevel(OptimizationLevel.Release);
					var compilation = CSharpCompilation.Create("dotMapExtensibility.dll", syntaxTrees, GetReferences(), options: compilationOptions);
					using var stream = new MemoryStream();
					var result = compilation.Emit(stream);
					if (result.Success)
					{
						//TODO: Solve it properly
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
						var asm = Assembly.Load(stream.ToArray());
#pragma warning restore RS1035 //
						type = asm.GetType(typeName);
						namingConvention = Activator.CreateInstance(type) as INamingConvention;
						if (namingConvention != null)
						{
							_namingConventionsCache.Add(typeName, namingConvention);
						}
					}
					else
					{
						context.ReportDiagnostic(Diagnostic.Create(dotMapGen.NamingConventionCompilationDiagnosticDescriptor, location: null, typeName));
						foreach (var diagnostic in result.Diagnostics)
						{
							context.ReportDiagnostic(diagnostic);
						}
					}
				}
			}
		}

		return namingConvention;
	}

	private static IEnumerable<ITypeSymbol> GetAllTypesInUse(INamedTypeSymbol typeSymbol)
	{
		yield return typeSymbol;
		foreach (var attr in typeSymbol.GetAttributes<IncludeTypeAttribute>())
		{
			if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments.Single() is TypedConstant typedConstant && typedConstant.Value is ITypeSymbol includeTypeSymbol)
			{
				yield return includeTypeSymbol;
			}
		}
	}

	private static IEnumerable<SyntaxTree> GetSyntaxTrees(Compilation compilation, HashSet<string> types)
	{
		foreach (var typeSyntax in compilation.SyntaxTrees.SelectMany(t => t.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>())
		{
			if (types.Contains(typeSyntax.Identifier.ValueText))
			{
				yield return typeSyntax.SyntaxTree;
			}
		}
	}

	private static IEnumerable<MetadataReference> GetReferences()
	{
		if (AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == "netstandard") is Assembly assembly)
		{
			yield return MetadataReference.CreateFromFile(assembly.Location);
		}
		yield return MetadataReference.CreateFromFile(typeof(INamingConvention).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
	}
}

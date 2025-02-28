/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using dotMap.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotMap;

[Generator]
[SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "<Pending>")]
public class dotMapGen : IIncrementalGenerator
{
	internal static DiagnosticDescriptor UnmappedMemberDiagnosticDescriptor =
		new("DM0001", "Unmapped member",
			"You should either map or ignore {0}",
			"Mapping",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

	internal static DiagnosticDescriptor InvalidIgnoreDiagnosticDescriptor =
		new("DM0002", "Invalid [Ignore] attribute usage",
			"Specify mapping source for member {0} via [Ignore(For = typeof({1}))]",
			"Mapping",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

	internal static DiagnosticDescriptor InvalidConstructFromDiagnosticDescriptor =
		new("DM0003", "Invalid ConstructFrom lambda",
			"ConstructFrom lambda is {0}",
			"Mapping",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

	internal static DiagnosticDescriptor NamingConventionCompilationDiagnosticDescriptor =
		new("DM0004", "Naming convention compilation error",
			"There are some errors compiling {0}, please apply [IncludeType(typeof(T))] if there are types in use from the same assembly or [IncludeAssembly(\"assemblyFilePath\")] otherwise",
			"Compilation",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

	internal static DiagnosticDescriptor RedundantForMemberUsageDiagnosticDescriptor =
		new("DM0005", "Redundant ForMember usage",
			"There is no need for an additional configuration as an implicit mapping exists '{0}' -> '{1}'",
			"Mapping",
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

	internal static DiagnosticDescriptor InvalidEnumFeatureUsage =
		new("DM0006", "Invalid feature usage",
			"'{0}' cannot be used in this context: {1}",
			"Mapping",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true); 

	private static readonly string[] _triggerTypes = { nameof(IMappable<object>), nameof(IMapConfig) };

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		dotMapDefaults.Reset();
		var types = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (s, _) => IsMappableType(s),
			transform: static (ctx, _) => (BaseTypeDeclarationSyntax)ctx.Node
		);
		var invocations = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (s, _) => IsDefaultsConfiguration(s),
			transform: static (ctx, _) => (InvocationExpressionSyntax)ctx.Node
		);

		 var compilationAndInvocations = context.CompilationProvider.Combine(invocations.Collect());
		context.RegisterSourceOutput(compilationAndInvocations, static (spc, source) => SetMappingsDefaults(source.Left, spc, source.Right));
		var compilationAndTypes = context.CompilationProvider.Combine(types.Collect());
		context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => GenerateMappings(source.Left, source.Right, spc));
	}

	private static bool IsDefaultsConfiguration(SyntaxNode syntaxNode)
	{
		return syntaxNode is InvocationExpressionSyntax expr && expr.DescendantNodes().OfType<SimpleNameSyntax>()
			.Select(s => s.Identifier.ValueText).Take(2).SequenceEqual([nameof(dotMapDefaults), nameof(dotMapDefaults.Configure)])
			&& expr.ArgumentList.Arguments.Count is 1 or 2;
	}

	private static bool IsMappableType(SyntaxNode syntaxNode) => syntaxNode is BaseTypeDeclarationSyntax type &&
		(type.IsInheritedFrom(_triggerTypes) || type.HasAttributeName<MapToAttribute>() || type.HasAttributeName<MapFromAttribute>());

	private static void SetMappingsDefaults(Compilation compilation, SourceProductionContext spc, ImmutableArray<InvocationExpressionSyntax> invocations)
	{
		foreach (var invocation in invocations)
		{
			string? scope;
			ArgumentSyntax expressionArgument;
			if (invocation.ArgumentList.Arguments.Count == 2 && invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
			{  
				scope = literal.Token.ValueText;
				expressionArgument = invocation.ArgumentList.Arguments[1];
			}
			else
			{
				scope = null;
				expressionArgument = invocation.ArgumentList.Arguments.Single();
			}

			var expression = expressionArgument.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
			if (expression != null)
			{
				var parser = new DefaultsParser(expression, compilation.GetSemanticModel(invocation.SyntaxTree), spc);
				var defaults = dotMapDefaults.GetOrAdd(compilation.Assembly);
				var conventions = parser.GetNamingConvention();
				var modes = parser.GetMappingMode();
				if (conventions.Members != null) defaults.SetDestMembersNamingConvention(scope, conventions.Members);
				if (conventions.Ctor != null) defaults.SetDestConstructorNamingConvention(scope, conventions.Ctor);
				if (modes.Source != null) defaults.SetSourceMappingMode(scope, modes.Source.Value);
				if (modes.Dest != null) defaults.SetDestMappingMode(scope, modes.Dest.Value);
			}
		}
	}

	private static void GenerateMappings(Compilation compilation, ImmutableArray<BaseTypeDeclarationSyntax> types, SourceProductionContext spc)
	{
		if (types.IsDefaultOrEmpty) return;

		var metadata = new Metadata(compilation, types, spc);
		var models = metadata.GetModels();
		foreach (var mappingModels in models)
		{
			var sourceCodeGen = new SourceCodeGenerator(mappingModels, spc);
			var parsed = SyntaxFactory.ParseCompilationUnit(sourceCodeGen.GenerateExtensionClass());
			var normalized = parsed.NormalizeWhitespace("	").ToFullString();
			spc.AddSource($"{mappingModels.FileName}.g.cs", normalized);
		}
	}
}
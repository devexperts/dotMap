/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using Xunit;

namespace dotMap.Tests
{
	public class EnumDiagnosticsTheoryData : TheoryData<string, IEnumerable<string>>
	{
		#region TEST CASES

		private const string ENUM_TO_ENUM = @"
			config.Map<Enum1>().To<DestEnum>()
				.WithDestinationNamingConvention<CamelCaseConvention>()
				.ForMember(d => d, s => (DestEnum)s)
				.WithMappingMode(SourceMappingMode.MapAllMembers, DestMappingMode.MapToAllAvailableMembers)
				.WithParameter<int>()
				.ConstructFrom((old_enum, arg) => DestEnum.second)
				.Finally((source, dest, additional_arg) => dest = DestEnum.second);";

		private const string ENUM_TO_MODEL = @"
			config.Map<DestEnum>().To<TestModel1>()
				.ForMember(d => d.Property, s => s)
				.WithMappingMode(SourceMappingMode.MapAllMembers, DestMappingMode.MapToPropsAndFields)
				.WithParameter<int>()
				.ConstructFrom((source, int_arg) => new TestModel1() { Property = DestEnum.first })
				.Finally((source, dest, int_arg) => dest.Property = DestEnum.first);";

		private const string MODEL_TO_ENUM = @"
			config.Map<TestModel1>().To<DestEnum>()
				.ForMember(d => d, s => s.Property)
				.WithMappingMode(SourceMappingMode.MapAllMembers, DestMappingMode.MapToAllAvailableMembers)
				.WithParameter<int>()
				.ConstructFrom((old_obj, arg) => old_obj.Property)
				.Finally((source, dest, additional_arg) => dest = DestEnum.second);";

		private const string DECLARATIVE_ENUM_TO_ENUM = @"
	[MapTo(typeof(DestEnum), SourceMappingMode = SourceMappingMode.MapAllMembers, DestMappingMode = DestMappingMode.MapToPropsAndFields, DestinationMembersNamingConvention = typeof(CamelCaseConvention))]";

		private const string DECLARATIVE_ENUM_TO_MODEL = @"
	[MapTo(typeof(TestModel1), SourceMappingMode = SourceMappingMode.MapAllMembers, DestMappingMode = DestMappingMode.MapToPropsAndFields, DestinationMembersNamingConvention = typeof(CamelCaseConvention))]";

		private const string DECLARATIVE_MODEL_TO_ENUM = @"
	[MapFrom(typeof(TestModel1), SourceMappingMode = SourceMappingMode.MapAllMembers, DestMappingMode = DestMappingMode.MapToPropsAndFields, DestinationMembersNamingConvention = typeof(CamelCaseConvention))]";

		#endregion

		private string WrapToConfiguration(string source)
			=> @"
	public class Configuration : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			" + source + @"
		}
	}";

		public EnumDiagnosticsTheoryData()
		{
			var imperativeErrorMsg = "'FEATURE' cannot be used in this context: feature cannot be used for enum configuration";
			var declarativeErrorMsg = "'FEATURE' cannot be used in this context: mapping mode cannot be set for enum configuration";

			var expectedErrors = new[]
			{
				imperativeErrorMsg.Replace("FEATURE","ForMember"),
				imperativeErrorMsg.Replace("FEATURE","WithMappingMode"),
				imperativeErrorMsg.Replace("FEATURE","WithParameter<Int32>"),
				imperativeErrorMsg.Replace("FEATURE","ConstructFrom"),
				imperativeErrorMsg.Replace("FEATURE","Finally"),
			};

			Add(WrapToConfiguration(ENUM_TO_ENUM), expectedErrors);
			Add(WrapToConfiguration(ENUM_TO_MODEL), expectedErrors);
			Add(WrapToConfiguration(MODEL_TO_ENUM), expectedErrors);

			expectedErrors =
			[
				declarativeErrorMsg.Replace("FEATURE","SourceMappingMode"),
				declarativeErrorMsg.Replace("FEATURE","DestMappingMode"),
			];
			Add(DECLARATIVE_ENUM_TO_ENUM, expectedErrors);
			Add(DECLARATIVE_ENUM_TO_MODEL, expectedErrors);
			Add(DECLARATIVE_MODEL_TO_ENUM, expectedErrors);
		}
	}

	public class EnumDiagnosticTests
	{
		[Theory]
		[ClassData(typeof(EnumDiagnosticsTheoryData))]
		public void EnumMappingConfig_Throws_CompilationError_When_UnsupportedMethodInUse(string source, IEnumerable<string> expectedDiagnostics)
		{
			var template = @"
using dotMap;
using dotMap.NamingConventions;

namespace TestSnippet
{
	public class TestModel1
	{
		public DestEnum Property;
	}

	public enum Enum1 { First, Second };

	public enum DestEnum { first, second };

	$PLACEHOLDER$
	public enum Enum2 { First, Second };
}
";

			var syntaxTree = CSharpSyntaxTree.ParseText(template.Replace("$PLACEHOLDER$", source));
			var compilation = CSharpCompilation.Create("testAssembly", [syntaxTree], [MetadataReference.CreateFromFile(typeof(dotMapGen).Assembly.Location)]);
			var generator = new dotMapGen();
			var driver = CSharpGeneratorDriver.Create(generator);

			driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
			AssertDiagnostics(expectedDiagnostics, diagnostics);

		}

		private void AssertDiagnostics(IEnumerable<string> expected, IEnumerable<Diagnostic> actual)
		{
			foreach (var diagnostic in expected)
			{
				Assert.Contains(actual, d => d.Severity == DiagnosticSeverity.Error &&
					d.Id == "DM0006" &&
					d.GetMessage() == diagnostic
				);
			}
		}

	}
}

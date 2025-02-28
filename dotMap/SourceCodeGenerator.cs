/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Text;
using dotMap.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotMap;

internal class SourceCodeGenerator
{
	private readonly MappingModels _mappingModels;
	private readonly SourceProductionContext _context;

	public SourceCodeGenerator(MappingModels models, SourceProductionContext spc)
	{
		_mappingModels = models;
		_context = spc;
	}

	public string GenerateExtensionClass()
	{
		var sb = new StringBuilder();
		foreach (var @using in _mappingModels.Models.SelectMany(m => m.TypeUsings).Append("using System;")
			.Append("using System.Collections.Generic;").Append("using System.Linq;").Distinct())
		{
			sb.AppendLine(@using);
		}

		GenerateMappingsForModel(sb);
		return sb.ToString();
	}

	private void GenerateMappingsForModel(StringBuilder sb)
	{
		if (_mappingModels.Namespace != null) sb.AppendLine($"namespace {_mappingModels.Namespace}");
		sb.AppendLine($"{{ public static class {_mappingModels.ExtensionTypeName}").AppendLine("{");
		foreach (var model in _mappingModels.Models)
		{
			GenerateMapMethod(sb, model);
			GenerateEnumerableMapMethod(sb, model);
			if (model.MapFromType.IsValueType)
			{
				GenerateMapMethod(sb, model, "nullableSource", "?");
				GenerateEnumerableMapMethod(sb, model, "?");
			}
		}

		sb.AppendLine("}").AppendLine("}");
	}

	private void GenerateMapMethod(StringBuilder sb, MappingModel model, string srcPrmName = "source", string sourceNullability = "")
	{
		var def = GetContractData(model, sourceNullability);
		sb.AppendLine($"{def.AccessModifier} static {model.MapToType.GetFullName()}{def.DestNullability} {def.MapToTypeName}(this {model.MapFromType.GetFullName()}{sourceNullability} {srcPrmName}{def.Parameters})").AppendLine("{");
		if (sourceNullability != "") sb.AppendLine($"if ({srcPrmName} == null) {{ return null; }} var source = {srcPrmName}.Value;");

		MappingGenerator mappingGen = model.MapToType.EnumUnderlyingType != null
			? new EnumMappingGenerator(sb, model)
			: new TypeMappingGenerator(_context, sb, model);
		mappingGen.Generate();
	}

	private void GenerateEnumerableMapMethod(StringBuilder sb, MappingModel model, string sourceNullability = "")
	{
		var def = GetContractData(model, sourceNullability);
		var memberName = sourceNullability != "" ? "item.Value" : "item";
		sb.AppendLine($"{def.AccessModifier} static IEnumerable<{model.MapToType.GetFullName()}{def.DestNullability}> {def.MapToTypeName}(this IEnumerable<{model.MapFromType.GetFullName()}{sourceNullability}> source{def.Parameters})").AppendLine("{");
		var parameters = def.Parameters != "" ? ",parameter" : "";
		if (!model.MapFromType.IsValueType)
		{
			sb.Append($"return source?.Select(item => item == null ? null : {_mappingModels.ExtensionTypeName}.{def.MapToTypeName}({memberName}{parameters}));");
		}
		else
		{
			sb.Append($"return source?.Select(item => {_mappingModels.ExtensionTypeName}.{def.MapToTypeName}(item{parameters}));");
		}
		sb.Append("}");
	}

	private (string AccessModifier, string DestNullability, string MapToTypeName, string Parameters) GetContractData(MappingModel model, string sourceNullability)
	{
		var accessLevel = Math.Min((int)model.MapFromType.DeclaredAccessibility, (int)model.MapToType.DeclaredAccessibility);
		var accessModifier = SyntaxFacts.GetText((Accessibility)accessLevel);
		string destNullability;
		if (sourceNullability == "?" && !model.MapToType.IsValueType)
		{
			destNullability = "";
		}
		else if (sourceNullability == "" && model.MapToType.IsValueType && !model.MapFromType.IsValueType)
		{
			destNullability = "?";
		}
		else
		{
			destNullability = sourceNullability;
		}
		var parameters = model.Parameter != null ? $",{model.Parameter.GetFullName()} parameter" : "";
		return (accessModifier, destNullability, model.MapToType.GetMapToTypeName(), parameters);
	}
}

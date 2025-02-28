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

internal class TypeMappingGenerator(SourceProductionContext context, StringBuilder mappingSb, MappingModel mappingModel) : MappingGenerator
{
	public override void Generate()
	{
		if (TryApplyManualMapping(mappingSb, mappingModel)) return;
		Prepare();
		GenerateConstructor();

		if (mappingModel.MethodGroupMembers.Any() || mappingModel.LambdaMembers.Any() || mappingModel.Members.Any())
		{
			mappingSb.Append("{");
			GenerateLambdaMembers();
			GenerateMethodGroupMembers();
			GenerateRegularMembers();
			mappingSb.Append("}");
		}

		mappingSb.Append(";");
		if (mappingModel.FinallyBlock != null)
		{
			if (mappingModel.Parameter == null)
			{
				mappingSb.Append("finallyBlock(source, result);");
			}
			else
			{
				mappingSb.Append("finallyBlock(source, result, parameter);");
			}
		}
		mappingSb.Append("return result; }");
	}

	private void GenerateLambdaMembers()
	{
		foreach (var prop in mappingModel.LambdaMembers)
		{
			if (mappingModel.Parameter == null)
			{
				mappingSb.AppendLine($"{prop.Key.Symbol.Name} = {prop.Key.Symbol.Name}Lambda(source),");
			}
			else
			{
				mappingSb.AppendLine($"{prop.Key.Symbol.Name} = {prop.Key.Symbol.Name}Lambda(source,parameter),");
			}
		}
	}

	private void GenerateMethodGroupMembers()
	{
		foreach (var prop in mappingModel.MethodGroupMembers)
		{
			if (mappingModel.Parameter == null)
			{
				mappingSb.AppendLine($"{prop.Key.Symbol.Name} = {prop.Value}(source),");
			}
			else
			{
				mappingSb.AppendLine($"{prop.Key.Symbol.Name} = {prop.Value}(source,parameter),");
			}
		}
	}

	private void GenerateRegularMembers()
	{
		if (mappingModel.Members.Any())
		{
			foreach (var member in mappingModel.Members.Take(mappingModel.Members.Count - 1))
			{
				GenerateMember(member.Value, member.Key);
				mappingSb.AppendLine(",");
			}

			var last = mappingModel.Members.Last();
			GenerateMember(last.Value, last.Key);
		}
	}
	
	private void GenerateMember(MemberMappingContext mappedMember, TypedMemberSymbol member)
	{
		mappingSb.Append($"{mappedMember.DestinationMember.Symbol.Name} = source.{member.Symbol.Name}");
		if(mappedMember.MappedFromMethod) mappingSb.Append("()");
			
		if (mappedMember.CascadeMapping == CascadeMapping.ValueType)
		{
			mappingSb.Append($".{mappedMember.DestinationMember.Type.GetMapToTypeName()}()");
		}
		else if (mappedMember.CascadeMapping == CascadeMapping.ReferenceType)
		{
			mappingSb.Append($"?.{mappedMember.DestinationMember.Type.GetMapToTypeName()}()");
		}
	}
	
	private void Prepare()
	{
		if (mappingModel.FinallyBlock != null)
		{
			if (mappingModel.Parameter == null)
			{
				mappingSb.AppendLine($"Action<{mappingModel.MapFromType.GetFullName()}, {mappingModel.MapToType.GetFullName()}> finallyBlock = {mappingModel.FinallyBlock};");
			}
			else
			{
				mappingSb.AppendLine($"Action<{mappingModel.MapFromType.GetFullName()}, {mappingModel.MapToType.GetFullName()}, {mappingModel.Parameter.GetFullName()}> finallyBlock = {mappingModel.FinallyBlock};");
			}
		}

		foreach (var prop in mappingModel.LambdaMembers)
		{
			if (mappingModel.Parameter == null)
			{
				mappingSb.AppendLine($"Func<{mappingModel.MapFromType.GetFullName()}, {prop.Key.Type.GetFullName()}> {prop.Key.Symbol.Name}Lambda = {prop.Value};");
			}
			else
			{
				mappingSb.AppendLine($"Func<{mappingModel.MapFromType.GetFullName()}, {mappingModel.Parameter.GetFullName()}, {prop.Key.Type.GetFullName()}> {prop.Key.Symbol.Name}Lambda = {prop.Value};");
			}
		}
	}
	
	private void GenerateCtorParameter(MemberMappingContext mappedMember, TypedMemberSymbol member)
	{
		mappingSb.Append($"source.{member.Symbol.Name}");
		if (mappedMember.MappedFromMethod) mappingSb.Append("()");

		if (mappedMember.CascadeMapping == CascadeMapping.ValueType)
		{
			mappingSb.Append($".{mappedMember.DestinationMember.Type.GetMapToTypeName()}()");
		}
		else if (mappedMember.CascadeMapping == CascadeMapping.ReferenceType)
		{
			mappingSb.Append($"?.{mappedMember.DestinationMember.Type.GetMapToTypeName()}()");
		}
	}

	private void GenerateCtorParameters()
	{
		mappingSb.Append($"var result = new {mappingModel.MapToType.GetFullName()}(");
		if (mappingModel.ConstructorParameters.Any())
		{
			foreach (var ctor in mappingModel.ConstructorParameters.Take(mappingModel.ConstructorParameters.Count - 1))
			{
				GenerateCtorParameter(ctor.Value, ctor.Key);
				mappingSb.Append(",");
			}
			var last = mappingModel.ConstructorParameters.Last();
			GenerateCtorParameter(last.Value, last.Key);
		}
		mappingSb.Append(")");
	}

	private void GenerateConstructor()
	{
		if (mappingModel.Constructor != null)
		{
			var nodes = mappingModel.Constructor.ChildNodes().ToArray();
			if (mappingModel.Parameter == null)
			{
				if (nodes.Length == 2 && nodes[0] is ParameterSyntax prm && nodes[1] is ObjectCreationExpressionSyntax ctor)
				{
					ctor = ReplaceParameter(ctor, prm, "source");
					mappingSb.Append($"var result = {ctor}");
				}
				else
				{
					context.ReportDiagnostic(Diagnostic.Create(dotMapGen.InvalidConstructFromDiagnosticDescriptor, mappingModel.Constructor.GetLocation(), "invalid"));
				}
			}
			else
			{
				if (nodes.Length == 2 && nodes[0] is ParameterListSyntax prm && prm.Parameters.Count == 2 && nodes[1] is ObjectCreationExpressionSyntax ctor)
				{
					ctor = ReplaceParameter(ctor, prm.Parameters[0], "source");
					ctor = ReplaceParameter(ctor, prm.Parameters[1], "parameter");
					mappingSb.Append($"var result = {ctor}");
				}
				else
				{
					context.ReportDiagnostic(Diagnostic.Create(dotMapGen.InvalidConstructFromDiagnosticDescriptor, mappingModel.Constructor.GetLocation(), "invalid"));
				}
			}
		}
		else
		{
			GenerateCtorParameters();
		}
	}

	private static ObjectCreationExpressionSyntax ReplaceParameter(ObjectCreationExpressionSyntax ctor, ParameterSyntax prm, string newToken)
	{
		while (ctor.DescendantNodes().OfType<SimpleNameSyntax>().FirstOrDefault(s => s.Identifier.ValueText == prm.Identifier.ValueText) is SimpleNameSyntax memAccess)
		{
			ctor = ctor.ReplaceToken(memAccess.Identifier, SyntaxFactory.Identifier(newToken));
		}

		return ctor;
	}
}
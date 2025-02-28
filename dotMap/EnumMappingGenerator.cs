/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Text;
using dotMap.Extensions;

namespace dotMap;

internal class EnumMappingGenerator(StringBuilder mappingSb, MappingModel mappingModel) : MappingGenerator
{
	public override void Generate()
	{
		if (TryApplyManualMapping(mappingSb, mappingModel)) return;
		mappingSb.Append("switch (").Append("source").Append(") {");
		GenerateRegularMembers();
		mappingSb.Append("} throw new NotSupportedException(); }");
	}

	private void GenerateRegularMembers()
	{
		foreach (var member in mappingModel.Members)
		{
			AppendCase(member.Key, $"{mappingModel.MapToType.GetFullName()}.{member.Value.DestinationMember.Symbol.Name}");
		}
	}

	private void AppendCase(TypedMemberSymbol source, string dest)
	{
		mappingSb.Append("case ")
			.Append(source.Type.GetFullName()).Append(".").Append(source.Symbol.Name)
			.Append(": return ").Append(dest).Append(";");
	}
}
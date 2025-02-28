/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Text;
using dotMap.Extensions;

namespace dotMap;

internal abstract class MappingGenerator()
{
	public abstract void Generate();

	protected bool TryApplyManualMapping(StringBuilder mappingSb, MappingModel mappingModel)
	{
		if (mappingModel.ManualMappingLambda != null)
		{
			if (mappingModel.Parameter == null)
			{
				mappingSb
					.AppendLine($"Func<{mappingModel.MapFromType.GetFullName()}, {mappingModel.MapToType.GetFullName()}> map = {mappingModel.ManualMappingLambda};")
					.Append("return map(source); }");
			}
			else
			{
				mappingSb
					.AppendLine($"Func<{mappingModel.MapFromType.GetFullName()}, {mappingModel.Parameter.GetFullName()}, {mappingModel.MapToType.GetFullName()}> map = {mappingModel.ManualMappingLambda};")
					.Append("return map(source, parameter); }");
			}
			return true;
		}

		return false;
	}
}
/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Text.RegularExpressions;

namespace dotMap.NamingConventions;

public class SnakeCaseConvention : OriginalNamingConvention
{
	public override string Convert(string source)
	{
		var converted = Regex.Replace(source, SOURCE_PATTERN, match => match.Value.ToLower().Replace("_", "") + "_");
		return converted.Length > 0 ? converted.Remove(converted.Length - 1, 1) : converted;
	}
}

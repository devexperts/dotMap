/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Text.RegularExpressions;

namespace dotMap.NamingConventions;

public class PascalCaseConvention : OriginalNamingConvention
{
	public override string Convert(string source)
	{
		return Regex.Replace(source, SOURCE_PATTERN, match => match.Value.Length > 0 ? char.ToUpper(match.Value[0]) + match.Value.Substring(1).ToLower().Replace("_", "") : string.Empty);
	}
}

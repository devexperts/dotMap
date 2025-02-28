/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace dotMap.NamingConventions;

public class CamelCaseConvention : PascalCaseConvention
{
	public override string Convert(string source)
	{
		var res = base.Convert(source);
		return res.Length > 0 ? char.ToLower(res[0]) + res.Substring(1) : string.Empty;
	}
}

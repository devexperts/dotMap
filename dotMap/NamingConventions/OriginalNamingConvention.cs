/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace dotMap.NamingConventions;

public class OriginalNamingConvention : INamingConvention
{
	protected const string SOURCE_PATTERN = @"/[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*[_]*|[A-Z]|[0-9]+/g";

	public virtual string Convert(string source) => source;
}

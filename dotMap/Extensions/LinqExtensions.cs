/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace dotMap.Extensions;

internal static class LinqExtensions
{
	public static IEnumerable<TResult> SelectNonNullable<TSource, TResult>(this IEnumerable<TSource> items, Func<TSource, TResult?> select)
	{
		return items.Select(select).OfType<TResult>();
	}
}

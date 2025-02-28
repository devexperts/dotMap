/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.NamingConventions;
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace dotMap;

public static class dotMapDefaults
{
	private static readonly Dictionary<string, Defaults> _defaults = new();

	internal static void Reset() => _defaults.Clear();

	internal static Defaults GetOrAdd(IAssemblySymbol assembly)
	{
		if (!_defaults.TryGetValue(assembly.Name, out Defaults defaults))
		{
			defaults = new Defaults();
			_defaults.Add(assembly.Name, defaults);
		}

		return defaults;
	}

	public static void Configure(Action<DefaultableMap<object, object>> options)
	{
	}

	public static void Configure(string scope, Action<DefaultableMap<object, object>> options)
	{
	}

	public sealed class Defaults
	{
		internal Defaults()
		{
		}

		private Stack<(string, INamingConvention)> ScopedDestMembersNamingConventions { get; } = new();
		private INamingConvention DestMembersNamingConvention { get; set; } = new OriginalNamingConvention();

		private Stack<(string, INamingConvention)> ScopedDestConstructorNamingConventions { get; } = new();
		private INamingConvention DestConstructorNamingConvention { get; set; } = new CamelCaseConvention();

		private Stack<(string, SourceMappingMode)> ScopedSourceMappingModes { get; } = new();
		private SourceMappingMode SourceMappingMode { get; set; } = SourceMappingMode.MapPropsAndFields;

		private Stack<(string, DestMappingMode)> ScopedDestMappingModes { get; } = new();
		private DestMappingMode DestMappingMode { get; set; } = DestMappingMode.MapToPropsAndFields;

		internal void SetDestMembersNamingConvention(string? scope, INamingConvention convention)
		{
			if (scope != null)
			{
				ScopedDestMembersNamingConventions.Push((scope, convention));
			}
			else
			{
				DestMembersNamingConvention = convention;
			}
		}

		internal INamingConvention GetDestMembersNamingConvention(string typeFullName)
			=> GetDefault(typeFullName, DestMembersNamingConvention, ScopedDestMembersNamingConventions);

		internal void SetDestConstructorNamingConvention(string? scope, INamingConvention convention)
		{
			if (scope != null)
			{
				ScopedDestConstructorNamingConventions.Push((scope, convention));
			}
			else
			{
				DestConstructorNamingConvention = convention;
			}
		}

		internal INamingConvention GetDestConstructorNamingConvention(string typeFullName)
			=> GetDefault(typeFullName, DestConstructorNamingConvention, ScopedDestConstructorNamingConventions);

		internal void SetSourceMappingMode(string? scope, SourceMappingMode mappingMode)
		{
			if (scope != null)
			{
				ScopedSourceMappingModes.Push((scope, mappingMode));
			}
			else
			{
				SourceMappingMode = mappingMode;
			}
		}

		internal SourceMappingMode GetSourceMappingMode(string typeFullName)
			=> GetDefault(typeFullName, SourceMappingMode, ScopedSourceMappingModes);

		internal void SetDestMappingMode(string? scope, DestMappingMode mappingMode)
		{
			if (scope != null)
			{
				ScopedDestMappingModes.Push((scope, mappingMode));
			}
			else
			{
				DestMappingMode = mappingMode;
			}
		}

		internal DestMappingMode GetDestMappingMode(string typeFullName)
			=> GetDefault(typeFullName, DestMappingMode, ScopedDestMappingModes);

		private T GetDefault<T>(string typeFullName, T unscoped, Stack<(string, T)> scoped)
		{
			if (scoped.Count > 0)
			{
				foreach (var scope in scoped)
				{
					if (Regex.IsMatch(typeFullName, scope.Item1))
					{
						return scope.Item2;
					}
				}
			}

			return unscoped;
		}
	}
}

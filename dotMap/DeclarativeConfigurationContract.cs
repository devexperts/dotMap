/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace dotMap;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = true)]
public class MapToAttribute : MapAttribute
{
	public MapToAttribute(Type destType)
	{
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = true)]
public class MapFromAttribute : MapAttribute
{
	public MapFromAttribute(Type destType)
	{
	}
}

public class MapAttribute : Attribute
{
	public Type? DestinationMembersNamingConvention { get; set; }
	public Type? DestinationConstructorNamingConvention { get; set; }
	public SourceMappingMode SourceMappingMode { get; set; } = SourceMappingMode.MapPropsAndFields;
	public DestMappingMode DestMappingMode { get; set; } = DestMappingMode.MapToPropsAndFields;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class IgnoreAttribute : Attribute
{
	public Type? For { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class IncludeTypeAttribute : Attribute
{
	public IncludeTypeAttribute(Type type)
	{
	}
}

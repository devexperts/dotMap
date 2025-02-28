/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.NamingConventions;
using FluentAssertions;
using Xunit;

namespace dotMap.Tests;

public class EnumTests
{
	[Theory]
	[InlineData(Enum1.First, Enum2.First)]
	[InlineData(Enum1.Second, Enum2.Second)]
	public void When_enum_declarative_config_Should_map(Enum1 source, Enum2 expectedDest)
	{
		source.MapToEnumTestsEnum2()
			.Should().Be(expectedDest);
	}
	
	[Fact]
	public void When_enum_imperative_config_Should_map()
	{
		Enum2.Second.MapToEnumTestsEnum1()
			.Should().Be(Enum1.Second);
	}

	[Fact]
	public void When_enum_cascade_mapping_Should_map()
	{
		new Model1 { Enum = Enum1.Second }.MapToEnumTestsModel2().Enum
			.Should().Be(Enum2.Second);
	}
	
	[Fact]
	public void When_enum_with_different_convention_Should_map()
	{
		Enum2.Second.MapToEnumTestsEnum3()
			.Should().Be(Enum3.second);
	}

	[MapTo(typeof(Enum2))]
	public enum Enum1 { First = -6, Second = -5 }
	public enum Enum2 { First = 1, Second = 2 }
	public enum Enum3 { first = 1, second = 2 }

	public class Model1
	{
		public Enum1 Enum { get; init; }
	}
	
	public class Model2
	{
		public Enum2 Enum { get; init; }
	}

	private class Configuration : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Enum2>().To<Enum1>();
			config.Map<Model1>().To<Model2>();
			config.Map<Enum2>().To<Enum3>().WithDestinationNamingConvention<CamelCaseConvention>();
		}
	}
}

/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using FluentAssertions;
using Xunit;

namespace dotMap.Tests;

public class ManualMappingTests
{
	[Theory]
	[InlineData(23, 1)]
	[InlineData(-23, -1)]
	public void When_manual_mapping_Should_map(int prop, int expected)
	{
		var m1 = new Model1 { Prop = prop };

		var m2 = m1.MapToManualMappingTestsModel2();

		m2.Prop.Should().Be(expected);
	}
	
	[Fact]
	public void When_manual_mapping_with_parameter_Should_map()
	{
		var m2 = new Model2 { Prop = 3 };

		var m1 = m2.MapToManualMappingTestsModel1(4);

		m1.Prop.Should().Be(7);
	}
	
	[Fact]
	public void When_manual_mapping_Should_support_cascade_mapping()
	{
		var m3 = new Model3 { Model = new Model1 { Prop = 3 } };

		var m4 = m3.MapToManualMappingTestsModel4();

		m4.Model?.Prop.Should().Be(1);
	}
	
	[Fact]
	public void When_manual_mapping_Should_map_enums()
	{
		Enum1.First.MapToManualMappingTestsEnum2()
			.Should().Be(Enum2.Second);
	}
	
	public class Model1
	{
		public int Prop { get; set; }
	}
	
	public class Model2
	{
		public int Prop { get; set; }
	}
	
	[MapTo(typeof(Model4))]
	public class Model3
	{
		public Model1? Model { get; set; }
	}
	
	public class Model4
	{
		public Model2? Model { get; set; }
	}
	
	public enum Enum1 { First, Second }
	
	public enum Enum2 { First, Second }
	
	public class Config : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model1>().To<Model2>().With(m1 => new Model2 { Prop = m1.Prop > 0 ? 1 : -1 });
			config.Map<Model1>().From<Model2>()
				.WithParameter<int>()
				.With((m2, prm) => new Model1 { Prop = m2.Prop + prm });
			config.Map<Enum1>().To<Enum2>().With(e1 => Enum2.Second);
		}
	}
}
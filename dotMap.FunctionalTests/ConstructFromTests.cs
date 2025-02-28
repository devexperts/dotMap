/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using FluentAssertions;
using Xunit;

namespace dotMap.Tests;

public class ConstructFromTests
{
	[Fact]
	public void When_simple_construct_from_Should_call_ctor()
	{
		var m1 = new Model1();

		var m2 = ConstructFromTests_ConfigExtensions.MapToConstructFromTestsModel2(m1);

		m2.Prop.Should().Be(5);
	}

	[Fact]
	public void When_construct_from_with_parameter_Should_call_ctor_with_parameter()
	{
		var m1 = new Model1();

		var m2 = ConstructFromTests_ConfigWithParameterExtensions.MapToConstructFromTestsModel2(m1, 7);

		m2.Prop.Should().Be(12);
	}

	[Fact]
	public void When_construct_from_with_tuple_parameter_Should_call_ctor_with_parameter()
	{
		var m1 = new Model1();

		var m2 = ConstructFromTests_ConfigWithTupleParameterExtensions.MapToConstructFromTestsModel2(m1, (7, "2"));

		m2.Prop.Should().Be(14);
	}

	private class Config : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model1>().To<Model2>()
				.ConstructFrom(m => new Model2(m.Prop))
				.Ignore(m => m.Prop);
		}
	}

	private class ConfigWithParameter : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model1>().To<Model2>()
				.WithParameter<int>()
				.ConstructFrom((m, prm) => new Model2(m.Prop + prm))
				.Ignore(m => m.Prop);
		}
	}

	private class ConfigWithTupleParameter : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model1>().To<Model2>()
				.WithParameter<(int x, string y)>()
				.ConstructFrom((m, prm) => new Model2(m.Prop + prm.x + int.Parse(prm.y)))
				.Ignore(m => m.Prop);
		}
	}

	public class Model1
	{
		public int Prop { get; set; } = 5;
	}

	public record Model2(int Prop);
}

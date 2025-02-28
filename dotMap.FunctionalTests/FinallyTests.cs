/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using FluentAssertions;
using Xunit;

namespace dotMap.Tests;

public class FinallyTests
{
	[Fact]
	public void When_finally_Should_execute()
	{
		var m1 = new Model1();

		var m2 = FinallyTests_ConfigExtensions.MapToFinallyTestsModel2(m1);

		m2.Prop.Should().Be(10);
	}

	[Fact]
	public void When_finally_with_parameter_Should_execute_with_parameter()
	{
		var m1 = new Model1();

		var m2 = FinallyTests_ConfigWithParameterExtensions.MapToFinallyTestsModel2(m1, 7);

		m2.Prop.Should().Be(12);
	}

	[Fact]
	public void When_finally_with_tuple_parameter_Should_execute_with_parameter()
	{
		var m1 = new Model1();

		var m2 = FinallyTests_ConfigWithTupleParameterExtensions.MapToFinallyTestsModel2(m1, (7, "2"));

		m2.Prop.Should().Be(14);
	}

	private class Config : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model1>().To<Model2>().Finally((src, dst) => dst.Prop += 5);
		}
	}

	private class ConfigWithParameter : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model1>().To<Model2>()
				.WithParameter<int>()
				.Finally((src, dst, prm) => dst.Prop += prm);
		}
	}

	private class ConfigWithTupleParameter : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model1>().To<Model2>()
				.WithParameter<(int x, string y)>()
				.Finally((src, dst, prm) => dst.Prop += prm.x + int.Parse(prm.y));
		}
	}

	public class Model1
	{
		public int Prop { get; set; } = 5;
	}

	public class Model2
	{
		public int Prop { get; set; }
	}
}

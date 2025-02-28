/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using FluentAssertions;
using System.Linq;
using Xunit;

namespace dotMap.Tests;

public class EnumerableMappingTests
{
	[Fact]
	public void When_map_configured_Should_map_sequences()
	{
		var m1 = new[] { new Model1 { Prop = 1 }, new Model1 { Prop = 2 } };

		var m2 = m1.MapToEnumerableMappingTestsModel2();

		m2.Select(m => m.Prop).Should().ContainInOrder(m1.Select(m => m.Prop));
	}

	[Fact]
	public void When_map_nullable_Should_succeed()
	{
		var m1 = new[] { new Model1 { Prop = 1 }, null, new Model1 { Prop = 2 } };

		var m2 = m1.MapToEnumerableMappingTestsModel2();

		m2.OfType<Model2>().Select(m => m.Prop).Should().ContainInOrder(m1.OfType<Model1>().Select(m => m.Prop));
		m2.Where(m => m == null).Should().ContainSingle();
	}

	[Fact]
	public void When_map_non_nullable_structs_Should_map_sequences()
	{
		var m1 = new[] { new StructModel1 { Prop = 1 }, new StructModel1 { Prop = 2 } };

		var m2 = m1.MapToEnumerableMappingTestsStructModel2();

		m2.Select(m => m.Prop).Should().ContainInOrder(m1.Select(m => m.Prop));
	}

	[Fact]
	public void When_map_nullable_structs_Should_map_sequences()
	{
		var m1 = new[] { new StructModel1 { Prop = 1 }, (StructModel1?)null, new StructModel1 { Prop = 2 } };

		var m2 = m1.MapToEnumerableMappingTestsStructModel2();

		m2.OfType<StructModel2>().Select(m => m.Prop).Should().ContainInOrder(m1.OfType<StructModel1>().Select(m => m.Prop));
		m2.Where(m => m == null).Should().ContainSingle();
	}

	private class Configuration : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model1>().To<Model2>();
			config.Map<StructModel1>().To<StructModel2>();
		}
	}

	public class Model1
	{
		public int Prop { get; set; }
	}

	public class Model2
	{
		public int Prop { get; set; }
	}

	public struct StructModel1
	{
		public int Prop { get; set; }
	}

	public struct StructModel2
	{
		public int Prop { get; set; }
	}
}

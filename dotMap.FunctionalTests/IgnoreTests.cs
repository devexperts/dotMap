/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using FluentAssertions;
using Xunit;

namespace dotMap.Tests
{
	public class IgnoreTests
	{
		[Fact]
		public void When_map_to_with_the_same_props_Should_match()
		{
			var m1 = new Model1 { Prop1 = "test", Prop2 = 5, Prop3 = true };

			var m2 = m1.MapToIgnoreTestsModel2();

			m2.Prop1.Should().BeNull();
			m2.Prop2.Should().Be(m1.Prop2);
			m2.Prop3.Should().Be(m1.Prop3);
		}

		[Fact]
		public void When_map_from_with_the_same_props_Should_match()
		{
			var m2 = new Model2 { Prop1 = "test", Prop2 = 5, Prop3 = true };

			var m1 = m2.MapToIgnoreTestsModel1();

			m1.Prop1.Should().BeNull();
			m1.Prop2.Should().Be(m2.Prop2);
			m1.Prop3.Should().Be(m2.Prop3);
		}

		private class Configuration : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Model1>().To<Model2>().Ignore(m => m.Prop1);
				config.Map<Model1>().From<Model2>().Ignore(m => m.Prop1);
			}
		}

		public class Model1
		{
			public string? Prop1 { get; set; }
			public int Prop2 { get; set; }
			public bool Prop3 { get; set; }
		}

		public class Model2
		{
			public string? Prop1 { get; set; }
			public int Prop2 { get; set; }
			public bool Prop3 { get; set; }
		}
	}
}

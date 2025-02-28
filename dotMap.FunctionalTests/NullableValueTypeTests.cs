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
	public class NullableValueTypeTests
	{
		[Fact]
		public void When_nullable_int_Should_map_properly()
		{
			var m1 = new Model1 { Prop = null };

			var m2 = m1.MapToNullableValueTypeTestsModel2();

			m2.Prop.Should().BeNull();
		}

		[Fact]
		public void When_nullable_structs_Should_map_properly()
		{
			var m3 = new Model3 { Struct = null };

			var m4 = m3.MapToNullableValueTypeTestsModel4();

			m4.Struct.Should().BeNull();
		}

		[Fact]
		public void When_Non_nullable_then_nullable_Should_map_properly()
		{
			var m5 = new Model5 { Struct = new Struct1 { Prop = 5 } };

			var m6 = m5.MapToNullableValueTypeTestsModel6();

			m6.Struct?.Prop.Should().Be(m5.Struct.Prop);
		}

		private class Config : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Model1>().To<Model2>();
				config.Map<Struct1>().To<Struct2>();
				config.Map<Model3>().To<Model4>();
				config.Map<Model5>().To<Model6>();
			}
		}

		public class Model1
		{
			public int? Prop { get; set; }
		}

		public class Model2
		{
			public int? Prop { get; set; }
		}

		public class Model3
		{
			public Struct1? Struct { get; set; }
		}

		public class Model4
		{
			public Struct2? Struct { get; set; }
		}

		public class Model5
		{
			public Struct1 Struct { get; set; }
		}

		public class Model6
		{
			public Struct2? Struct { get; set; }
		}

		public struct Struct1
		{
			public int Prop { get; set; }
		}

		public struct Struct2
		{
			public int Prop { get; set; }
		}
	}
}

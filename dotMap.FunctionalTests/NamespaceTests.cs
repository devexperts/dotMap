/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap;
using dotMap.Tests.Nested1;
using dotMap.Tests.Nested2;
using FluentAssertions;
using Xunit;

namespace dotMap.Tests
{
	public class NamespaceTests
	{
		[Fact]
		public void When_nested_namespaces_Should_succeed()
		{
			var m1 = new Model1 { Prop = 1 };

			var m2 = NamespaceTests_NestedNamespaceConfigurationExtensions.MapToModel2(m1);

			m2.Prop.Should().Be(m1.Prop);
		}

		[Fact]
		public void When_nested_types_Should_succeed()
		{
			var m1 = new Model1 { Prop = 1, NestedProp = new Nested3.Nested.Model1 { Prop = 2 } };

			var m2 = NamespaceTests_NestedTypeConfigurationExtensions.MapToModel2(m1);

			m2.Prop.Should().Be(m1.Prop);
			m2.NestedProp.As<Nested3.Nested.Model2>().Prop.Should().Be(m1.NestedProp.Prop);
		}
		
		[Fact]
		public void When_map_global_model_Should_succeed()
		{
			var m1 = new NoNamespaceTestsModel1 { Prop = 5 };

			var m2 = m1.MapToNoNamespaceTestsModel2();

			m2.Prop.Should().Be(5);
		}
		
		[Fact]
		public void When_map_global_nested_model_Should_succeed()
		{
			var m1 = new NoNamespaceTestsModel1 { Prop = 5 };

			var m2 = m1.MapToNoNamespaceTestsModel2NoNamespaceTestsNestedModel2();

			m2.Prop.Should().Be(5);
		}

		private class NestedNamespaceConfiguration : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Model1>().To<Model2>().Ignore(m => m.NestedProp);
			}
		}

		private class NestedTypeConfiguration : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Nested3.Nested.Model1>().To<Nested3.Nested.Model2>();
				config.Map<Model1>().To<Model2>();
			}
		}
	}

	namespace Nested1
	{
		internal class Model1
		{
			public int Prop { get; set; }
			public Nested3.Nested.Model1? NestedProp { get; set; }
		}
	}

	namespace Nested2
	{
		internal class Model2
		{
			public int Prop { get; set; }
			public Nested3.Nested.Model2? NestedProp { get; set; }
		}
	}

	namespace Nested3
	{
		internal class Nested
		{
			internal class Model1
			{
				public int Prop { get; set; }
			}

			internal class Model2
			{
				public int Prop { get; set; }
			}
		}
	}
}

[MapTo(typeof(NoNamespaceTestsModel2))]
public class NoNamespaceTestsModel1
{
	public int Prop { get; set; }
}

public class NoNamespaceTestsModel2
{
	public int Prop { get; set; }
	
	[MapFrom(typeof(NoNamespaceTestsModel1))]
	public class NoNamespaceTestsNestedModel2
	{
		public int Prop { get; set; }
	}
}

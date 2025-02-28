/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.NamingConventions;
using FluentAssertions;
using Xunit;

namespace dotMap.Tests
{
	public class ImperativeDetachedConfigurationTests
	{
		[Fact]
		public void When_simple_map_to_Should_succeed()
		{
			var m1 = new SimpleModel1 { SomeProp1 = 5 };

			var m2 = m1.MapToImperativeDetachedConfigurationTestsSimpleModel2();

			m1.SomeProp1.Should().Be(m2.SomeProp1);
		}

		[Fact]
		public void When_simple_map_from_Should_succeed()
		{
			var m0 = new SimpleModel0 { SomeProp1 = 5 };

			var m1 = m0.MapToImperativeDetachedConfigurationTestsSimpleModel1();

			m1.SomeProp1.Should().Be(m0.SomeProp1);
		}

		[Fact]
		public void When_naming_convention_Should_match_properties()
		{
			var m1 = new SimpleModel1 { SomeProp1 = 5 };

			var m2 = m1.MapToImperativeDetachedConfigurationTestsSnakeModel2();

			m1.SomeProp1.Should().Be(m2.some_prop1);
		}

		[Fact]
		public void When_mapping_mode_Should_map_from_methods()
		{
			var m1 = new MethodModel1();

			var m2 = m1.MapToImperativeDetachedConfigurationTestsSimpleModel2();

			m2.SomeProp1.Should().Be(m1.SomeProp1());
		}

		[Fact]
		public void When_mapping_with_init_Should_call_ctor_lambda()
		{
			var m1 = new SimpleModel1 { SomeProp1 = 5 };

			var m2 = m1.MapToImperativeDetachedConfigurationTestsInitModel2();

			m2.SomeProp1.Should().Be(m1.SomeProp1);
		}

		[Fact]
		public void When_mapping_to_ctor_Should_call_ctor()
		{
			var m1 = new SimpleModel1 { SomeProp1 = 5 };

			var m3 = m1.MapToImperativeDetachedConfigurationTestsCtorModel3();

			m3.SomeProp1.Should().Be(m1.SomeProp1);
		}

		private class Configuration : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<SimpleModel1>().To<SimpleModel2>();
				config.Map<SimpleModel1>().From<SimpleModel0>();
				config.Map<SimpleModel1>().To<SnakeModel2>().WithDestinationNamingConvention<SnakeCaseConvention>().Ignore(m => m.some_prop2);
				config.Map<MethodModel1>().To<SimpleModel2>().WithMappingMode(SourceMappingMode.MapMethods, DestMappingMode.MapToPropsAndFields);
				config.Map<SimpleModel1>().To<InitModel2>().ConstructFrom(m1 => new InitModel2(m1.SomeProp1));
				config.Map<SimpleModel1>().To<CtorModel3>()
					.WithMappingMode(SourceMappingMode.MapPropsAndFields, DestMappingMode.MapToConstructor)
					.WithDestinationNamingConvention<PascalCaseConvention, SnakeCaseConvention>();
			}
		}

		public class SimpleModel0
		{
			public int SomeProp1 { get; set; }
		}

		public class SimpleModel1
		{
			public int SomeProp1 { get; set; }
		}

		public class MethodModel1
		{
			public int SomeProp1() => 5;
		}

		public class SimpleModel2
		{
			public int SomeProp1 { get; set; }
		}

		public class SnakeModel2
		{
			public int some_prop1 { get; set; }
			public int some_prop2 { get; set; }
		}

		public class InitModel2
		{
			public InitModel2(int prop1)
			{
				SomeProp1 = prop1;
			}

			public int SomeProp1 { get; }
		}

		public class CtorModel3
		{
			public CtorModel3(int some_prop1)
			{
				SomeProp1 = some_prop1;
			}

			public int SomeProp1 { get; }
		}
	}
}

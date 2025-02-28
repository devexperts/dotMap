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
	public class DeclarativeConfigurationTests
	{
		[Fact]
		public void When_simple_map_to_Should_succeed()
		{
			var m1 = new SimpleModel1 { SomeProp1 = 5 };

			var m2 = m1.MapToDeclarativeConfigurationTestsSimpleModel2();

			m1.SomeProp1.Should().Be(m2.SomeProp1);
		}

		[Fact]
		public void When_simple_map_from_Should_succeed()
		{
			var m0 = new SimpleModel0 { SomeProp1 = 5 };

			var m1 = m0.MapToDeclarativeConfigurationTestsSimpleModel1();

			m1.SomeProp1.Should().Be(m0.SomeProp1);
		}

		[Fact]
		public void When_naming_convention_Should_match_properties()
		{
			var m1 = new SimpleModel1 { SomeProp1 = 5 };

			var m2 = m1.MapToDeclarativeConfigurationTestsSnakeModel2();

			m1.SomeProp1.Should().Be(m2.some_prop1);
		}

		[Fact]
		public void When_mapping_mode_Should_map_from_methods()
		{
			var m1 = new MethodModel1();

			var m2 = m1.MapToDeclarativeConfigurationTestsSimpleModel2();

			m2.SomeProp1.Should().Be(m1.SomeProp1());
		}

		[Fact]
		public void When_mapping_to_ctor_Should_call_ctor()
		{
			var m1 = new SimpleModel1 { SomeProp1 = 5 };

			var m3 = m1.MapToDeclarativeConfigurationTestsCtorModel3();

			m3.SomeProp1.Should().Be(m1.SomeProp1);
		}

		[Fact]
		public void When_ignore_for_Should_succeed()
		{
			var m0 = new SimpleModel1 { SomeProp1 = 5 };
			var m1 = new SimpleModel1 { SomeProp1 = 5 };

			m0.MapToDeclarativeConfigurationTestsIgnoreModel1().SomeProp1.Should().Be(m0.SomeProp1);
			m1.MapToDeclarativeConfigurationTestsIgnoreModel1().SomeProp1.Should().Be(m0.SomeProp1);
		}

		public class SimpleModel0
		{
			public int SomeProp1 { get; set; }
		}

		[MapTo(typeof(SimpleModel2))]
		[MapTo(typeof(SnakeModel2), DestinationMembersNamingConvention = typeof(SnakeCaseConvention))]
		[MapFrom(typeof(SimpleModel0))]
		[MapTo(typeof(CtorModel3), DestMappingMode = DestMappingMode.MapToConstructor, DestinationConstructorNamingConvention = typeof(SnakeCaseConvention))]
		public class SimpleModel1
		{
			public int SomeProp1 { get; set; }
		}

		[MapTo(typeof(SimpleModel2), SourceMappingMode = SourceMappingMode.MapMethods)]
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
			[Ignore]
			public int some_prop2 { get; set; }
		}

		public class CtorModel3
		{
			public CtorModel3(int some_prop1)
			{
				SomeProp1 = some_prop1;
			}

			public int SomeProp1 { get; }
		}

		[MapFrom(typeof(SimpleModel0))]
		[MapFrom(typeof(SimpleModel1))]
		public class IgnoreModel1
		{
			public int SomeProp1 { get; set; }
			[Ignore(For = typeof(SimpleModel0))]
			[Ignore(For = typeof(SimpleModel1))]
			public int SomeProp2 { get; set; }
		}
	}
}

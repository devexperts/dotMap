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
	public class SourceMappingModeTests
	{
		[Fact]
		public void When_map_pros_and_fields_Should_ignore_methods()
		{
			var m1 = new Model1();

			var m2 = SourceMappingModeTests_PropsAndFieldsConfigExtensions.MapToSourceMappingModeTestsModel2(m1);

			m2.Field.Should().Be(m1.Field);
			m2.ReadOnlyProperty.Should().Be(m1.ReadOnlyProperty);
			m2.Property.Should().Be(m1.Property);
			m2.Method.Should().Be(0);
		}

		[Fact]
		public void When_map_methods_Should_ignore_pros_and_fields()
		{
			var m1 = new Model1();

			var m2 = SourceMappingModeTests_MethodsConfigExtensions.MapToSourceMappingModeTestsModel2(m1);

			m2.Field.Should().Be(0);
			m2.ReadOnlyProperty.Should().Be(0);
			m2.Property.Should().Be(0);
			m2.Method.Should().Be(m1.Method());
		}

		[Fact]
		public void When_map_all_members_Should_not_ignore_anything()
		{
			var m1 = new Model1();

			var m2 = SourceMappingModeTests_AllMembersConfigExtensions.MapToSourceMappingModeTestsModel2(m1);

			m2.Field.Should().Be(m1.Field);
			m2.ReadOnlyProperty.Should().Be(m1.ReadOnlyProperty);
			m2.Property.Should().Be(m1.Property);
			m2.Method.Should().Be(m1.Method());
		}

		private class PropsAndFieldsConfig : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Model1>().To<Model2>()
					.WithMappingMode(SourceMappingMode.MapPropsAndFields)
					.Ignore(m => m.Method);
			}
		}

		private class MethodsConfig : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Model1>().To<Model2>()
					.WithMappingMode(SourceMappingMode.MapMethods)
					.Ignore(m => m.Field)
					.Ignore(m => m.ReadOnlyProperty)
					.Ignore(m => m.Property);
			}
		}

		private class AllMembersConfig : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Model1>().To<Model2>().WithMappingMode(SourceMappingMode.MapAllMembers);
			}
		}

		public class Model1
		{
			public int Field = 5;
			public int ReadOnlyProperty { get; } = 6;
			public int Property { get; set; } = 7;
			public int Method() => 8;
		}

		public class Model2
		{
			public int Field { get; set; }
			public int ReadOnlyProperty { get; set; }
			public int Property { get; set; }
			public int Method { get; set; }
		}
	}
}

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
	public class DestMappingModeTests
	{
		[Fact]
		public void When_map_to_ctor_Should_map_to_ctor_only()
		{
			var m1 = new Model1();

			var m2 = m1.MapToDestMappingModeTestsModel2();

			m2.Field.Should().Be(m1.Field);
			m2.ReadOnlyProperty.Should().Be(m1.ReadOnlyProperty);
			m2.Property.Should().Be(m1.Property);
			m2.Method.Should().Be(m1.Method());
		}

		[Fact]
		public void When_map_to_members_Should_ignore_ctor()
		{
			var m1 = new Model1();

			var m2 = m1.MapToDestMappingModeTestsModel3();

			m2.Field.Should().Be(m1.Field);
			m2.ReadOnlyProperty.Should().Be(m1.ReadOnlyProperty);
			m2.Property.Should().Be(m1.Property);
			m2.Method.Should().Be(m1.Method());
		}

		[Fact]
		public void When_map_to_all_members_Should_not_ignore_anything()
		{
			var m1 = new Model1();

			var m2 = m1.MapToDestMappingModeTestsModel4();

			m2.Field.Should().Be(m1.Field);
			m2.ReadOnlyProperty.Should().Be(m1.ReadOnlyProperty);
			m2.Property.Should().Be(m1.Property);
			m2.Method.Should().Be(m1.Method());
		}

		[Fact]
		public void When_map_to_ctor_Should_apply_naming_convention()
		{
			var m1 = new Model1();

			var m2 = m1.MapToDestMappingModeTestsRecordModel();

			m2.Field.Should().Be(m1.Field);
			m2.ReadOnlyProperty.Should().Be(m1.ReadOnlyProperty);
			m2.Property.Should().Be(m1.Property);
			m2.Method.Should().Be(m1.Method());
		}

		private class Config : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Model1>().To<Model2>().WithMappingMode(SourceMappingMode.MapAllMembers, DestMappingMode.MapToConstructor);
				config.Map<Model1>().To<Model3>()
					.ConstructFrom(src => new Model3(-1, -1, -1, -1))
					.WithMappingMode(SourceMappingMode.MapAllMembers, DestMappingMode.MapToPropsAndFields);
				config.Map<Model1>().To<Model4>().WithMappingMode(SourceMappingMode.MapAllMembers, DestMappingMode.MapToAllAvailableMembers);
				config.Map<Model1>().To<RecordModel>()
					.WithDestinationNamingConvention<PascalCaseConvention, PascalCaseConvention>()
					.WithMappingMode(SourceMappingMode.MapAllMembers, DestMappingMode.MapToConstructor);
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
			public Model2(int field, int readOnlyProperty, int property, int method)
			{
				Field = field;
				ReadOnlyProperty = readOnlyProperty;
				Property = property;
				Method = method;
			}

			public int Field { get; }
			public int ReadOnlyProperty { get; }
			public int Property { get; }
			public int Method { get; }
		}

		public class Model3
		{
			public Model3(int field, int readOnlyProperty, int property, int method)
			{
				Field = field;
				ReadOnlyProperty = readOnlyProperty;
				Property = property;
				Method = method;
			}

			public int Field { get; set; }
			public int ReadOnlyProperty { get; set; }
			public int Property { get; set; }
			public int Method { get; set; }
		}

		public class Model4
		{
			public Model4(int field, int readOnlyProperty)
			{
				Field = field;
				ReadOnlyProperty = readOnlyProperty;
			}

			public int Field { get; }
			public int ReadOnlyProperty { get; }
			public int Property { get; set; }
			public int Method { get; set; }
		}

		public record RecordModel(int Field, int ReadOnlyProperty, int Property, int Method);
	}
}

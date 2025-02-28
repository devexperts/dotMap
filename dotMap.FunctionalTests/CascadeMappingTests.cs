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
	public class CascadeMappingTests
	{
		[Fact]
		public void When_cascade_mapping_Should_succeed()
		{
			var m3 = new Model3 { Model = new Model1 { Prop = 5 } };

			var m4 = m3.MapToCascadeMappingTestsModel4();

			m4.Model?.Prop.Should().Be(m3.Model?.Prop);
		}

		[Fact]
		public void When_cascade_mapping_with_ignore_Should_succeed()
		{
			var m5 = new Model5 { Model = new Model1 { Prop = 5 } };

			var m6 = m5.MapToCascadeMappingTestsModel6();

			m6.Model.Prop.Should().Be(m5.Model.Prop);
		}

		[Fact]
		public void When_nullable_cascade_mapping_Should_succeed()
		{
			var m3 = new Model3 { Model = null };

			var m4 = m3.MapToCascadeMappingTestsModel4();

			m4.Model.Should().BeNull();
		}

		[Fact]
		public void When_cascade_mapping_with_structs_Should_succeed()
		{
			var m7 = new Model7 { Model = new StructModel1 { Prop = 5 } };

			var m8 = m7.MapToCascadeMappingTestsModel8();

			m8.Model.Prop.Should().Be(m7.Model.Prop);
		}

		[Fact]
		public void When_cascade_mapping_with_nullable_structs_Should_succeed()
		{
			var m9 = new Model9 { Model = null };

			var m10 = m9.MapToCascadeMappingTestsModel10();

			m10.Model.Should().BeNull();
		}

		[Fact]
		public void When_cascade_mapping_from_method_Should_succeed()
		{
			var m11 = new Model11();

			var m12 = m11.MapToCascadeMappingTestsModel12();

			m12.Model?.Prop.Should().Be(m11.Model().Prop);
		}

		[Fact]
		public void When_cascade_mapping_to_ctor_Should_succeed()
		{
			var m3 = new Model3 { Model = new Model1 { Prop = 5 } };

			var m4 = m3.MapToCascadeMappingTestsCtorModel4();

			m4.Model?.Prop.Should().Be(m3.Model?.Prop);
		}

		[Fact]
		public void When_declarative_config_Should_support_cascade_mapping()
		{
			var m4 = new Model4 { Model = new Model2 { Prop = 5 } };

			var m3 = m4.MapToCascadeMappingTestsModel3();

			m3.Model?.Prop.Should().Be(5);
		}

		private class Config : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Model1>().To<Model2>();
				config.Map<Model3>().To<Model4>();
				config.Map<Model3>().To<CtorModel4>().WithMappingMode(SourceMappingMode.MapPropsAndFields, DestMappingMode.MapToConstructor);
				config.Map<Model5>().To<Model6>().Ignore(m => m.AnotherModel);
				config.Map<StructModel1>().To<StructModel2>();
				config.Map<Model7>().To<Model8>();
				config.Map<Model9>().To<Model10>();
				config.Map<Model11>().To<Model12>().WithMappingMode(SourceMappingMode.MapMethods);
			}
		}

		public class Model1
		{
			public int Prop { get; set; }
		}

		[MapTo(typeof(Model1))]
		public class Model2
		{
			public int Prop { get; set; }
		}

		public class Model3
		{
			public Model1? Model { get; set; }
		}

		[MapTo(typeof(Model3))]
		public class Model4
		{
			public Model2? Model { get; set; }
		}

		public class CtorModel4
		{
			public CtorModel4(Model2? model)
			{
				Model = model;
			}

			public Model2? Model { get; }
		}

		public class Model5
		{
			public Model1 Model { get; set; } = new Model1();
			public AnotherModel1? AnotherModel { get; set; }
		}

		public class Model6
		{
			public Model2 Model { get; set; } = new Model2();
			public AnotherModel2? AnotherModel { get; set; }
		}

		public class Model7
		{
			public StructModel1 Model { get; set; }
		}

		public class Model8
		{
			public StructModel2 Model { get; set; }
		}

		public class Model9
		{
			public StructModel1? Model { get; set; }
		}

		public class Model10
		{
			public StructModel2? Model { get; set; }
		}

		public class Model11
		{
			public Model1 Model() => new Model1 { Prop = 5 };
		}

		public class Model12
		{
			public Model2? Model { get; set; }
		}

		public record AnotherModel1(int Prop);

		public record AnotherModel2(int Prop);

		public struct StructModel1
		{
			public int Prop { get; set; }
		}

		public struct StructModel2
		{
			public int Prop { get; set; }
		}
	}
}

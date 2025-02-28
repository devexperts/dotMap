/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.NamingConventions;
using FluentAssertions;
using Xunit;

namespace dotMap.Tests;

public class ScopedDefaultsTests
{
	[Fact]
	public void When_dest_members_naming_convention_overriden_Should_override_declarative_config()
	{
		dotMapDefaults.Configure("ScopedDefaultsTests.Model2", opts => opts.WithDestinationNamingConvention<SnakeCaseConvention>());
		var m1 = new Model1();

		var m2 = m1.MapToScopedDefaultsTestsModel2();
		var m3 = m1.MapToScopedDefaultsTestsModel3();

		m2.some_prop.Should().Be(m1.SomeProp);
		m3.SomeProp.Should().Be(m1.SomeProp);
	}

	[Fact]
	public void When_dest_members_naming_convention_overriden_Should_override_imperative_config()
	{
		var m3 = new Model3();

		var m2 = m3.MapToScopedDefaultsTestsModel2();
		var m1 = m3.MapToScopedDefaultsTestsModel1();

		m2.some_prop.Should().Be(m3.SomeProp);
		m1.SomeProp.Should().Be(m3.SomeProp);
	}

	[Fact]
	public void When_dest_ctor_naming_convention_overriden_Should_override_imperative_config()
	{
		dotMapDefaults.Configure("ScopedDefaultsTests.CtorModel", opts => opts.WithMappingMode(SourceMappingMode.MapPropsAndFields, DestMappingMode.MapToConstructor));
		dotMapDefaults.Configure("ScopedDefaultsTests.CtorModel2", opts => opts.WithDestinationNamingConvention<PascalCaseConvention, SnakeCaseConvention>());
		var m1 = new CtorModel1(5);

		var m2 = m1.MapToScopedDefaultsTestsCtorModel2();
		var m3 = m1.MapToScopedDefaultsTestsCtorModel3();

		m2.some_prop.Should().Be(m1.SomeProp);
		m3.SomeProp.Should().Be(m1.SomeProp);
	}

	[Fact]
	public void When_dest_ctor_naming_convention_overriden_Should_override_declarative_config()
	{
		dotMapDefaults.Configure("ScopedDefaultsTests.CtorModel1", opts => opts.WithDestinationNamingConvention<SnakeCaseConvention, PascalCaseConvention>());
		var m3 = new CtorModel3(5);

		var m2 = m3.MapToScopedDefaultsTestsCtorModel2();
		var m1 = m3.MapToScopedDefaultsTestsCtorModel1();

		m2.some_prop.Should().Be(m1.SomeProp);
		m1.SomeProp.Should().Be(m1.SomeProp);
	}

	[MapTo(typeof(Model2)), MapTo(typeof(Model3))]
	public class Model1
	{
		public int SomeProp { get; set; } = 5;
	}

	public class Model2
	{
		public int some_prop { get; set; }
	}

	public class Model3 : IMappable<Model3>
	{
		public int SomeProp { get; set; } = 5;

		public void ConfigureMap(MapConfig<Model3> map)
		{
			map.To<Model2>();
			map.To<Model1>();
		}
	}

	[MapTo(typeof(CtorModel2))]
	[MapTo(typeof(CtorModel3))]
	public class CtorModel1
	{
		public CtorModel1(int SomeProp)
		{
			this.SomeProp = SomeProp;
		}

		public int SomeProp { get; }
	}

	public class CtorModel2
	{
		public CtorModel2(int some_prop)
		{
			this.some_prop = some_prop;
		}

		public int some_prop { get; }
	}

	public class CtorModel3 : IMappable<CtorModel3>
	{
		public CtorModel3(int someProp)
		{
			SomeProp = someProp;
		}

		public int SomeProp { get; }

		public void ConfigureMap(MapConfig<CtorModel3> map)
		{
			map.To<CtorModel2>();
			map.To<CtorModel1>();
		}
	}
}

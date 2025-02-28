/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using FluentAssertions;
using Xunit;

namespace dotMap.Tests;

public class ForMemberTests
{
	[Fact]
	public void When_for_member_applied_Should_evaluate_lambda()
	{
		var m1 = new Model1 { Prop1 = 5, Prop2 = true };

		var m2 = m1.MapToForMemberTestsModel2();

		m2.Field.Should().Be(5);
		m2.Prop1.Should().Be(10);
		m2.Prop2.Should().BeFalse();
	}

	[Fact]
	public void When_for_member_with_external_call_Should_evaluate_lambda()
	{
		var m1 = new Model1 { Prop1 = 5, Prop2 = true };

		var m2 = m1.MapToForMemberTestsModel2();

		m2.Prop3.Should().Be("105");
		m2.Prop4.Should().Be("105");
	}

	[Fact]
	public void When_lambda_with_parameter_Should_pass_parameter()
	{
		var m3 = new Model3();

		var m4 = ForMemberTests_LambdaWithParameterExtensions.MapToForMemberTestsModel4(m3, 7);

		m4.Prop.Should().Be(12);
	}

	[Fact]
	public void When_method_group_with_parameter_Should_pass_parameter()
	{
		var m3 = new Model3();

		var m4 = ForMemberTests_MethodGroupWithParameterExtensions.MapToForMemberTestsModel4(m3, 7);

		m4.Prop.Should().Be(12);
	}

	[Fact]
	public void When_lambda_with_tuple_parameter_Should_pass_parameter()
	{
		var m3 = new Model3();

		var m4 = ForMemberTests_MethodGroupWithTupleParameterExtensions.MapToForMemberTestsModel4(m3, (7, "2"));

		m4.Prop.Should().Be(14);
	}

	[Fact]
	public void When_method_group_with_tuple_parameter_Should_pass_parameter()
	{
		var m3 = new Model3();

		var m4 = ForMemberTests_MethodGroupWithTupleParameterExtensions.MapToForMemberTestsModel4(m3, (7, "2"));

		m4.Prop.Should().Be(14);
	}

	internal class Config : IMapConfig
	{
		public static string Transform(Model1 m1) => (m1.Prop1 + 100).ToString();

		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model1>().To<Model2>()
				.ForMember(k => k.Prop1, src => src.Prop1 + 5)
				.ForMember(k => k.Prop2, src => !src.Prop2)
				.ForMember(k => k.Prop3, src => Transform(src))
				.ForMember(k => k.Prop4, Transform)
				.ForMember(k => k.Field, src => src.Prop1);
		}
	}

	private class LambdaWithParameter : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model3>().To<Model4>()
				.WithParameter<int>()
				.ForMember(m => m.Prop, (m, prm) => m.Prop + prm);
		}
	}

	internal class MethodGroupWithParameter : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model3>().To<Model4>()
				.WithParameter<int>()
				.ForMember(m => m.Prop, Transform);
		}

		public static int Transform(Model3 m, int prm) => m.Prop + prm;
	}

	internal class LambdaWithTupleParameter : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model3>().To<Model4>()
				.WithParameter<(int x, string y)>()
				.ForMember(m => m.Prop, (m, prm) => m.Prop + prm.x + int.Parse(prm.y));
		}
	}

	internal class MethodGroupWithTupleParameter : IMapConfig
	{
		public void ConfigureMap(MapConfig config)
		{
			config.Map<Model3>().To<Model4>()
				.WithParameter<(int x, string y)>()
				.ForMember(m => m.Prop, Transform);
		}

		public static int Transform(Model3 m, (int x, string y) prm) => m.Prop + prm.x + int.Parse(prm.y);
	}

	public class Model1
	{
		public int Prop1 { get; set; }
		public bool Prop2 { get; set; }
		public string? Prop3 { get; set; }
		public string? Prop4 { get; set; }
	}

	public class Model2
	{
		public int Prop1 { get; set; }
		public bool Prop2 { get; set; }
		public string? Prop3 { get; set; }
		public string? Prop4 { get; set; }
		public int Field;
	}

	public record Model3
	{
		public int Prop { get; set; } = 5;
	}

	public record Model4
	{
		public int Prop { get; set; }
	}
}

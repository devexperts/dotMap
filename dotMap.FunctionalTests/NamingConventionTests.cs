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
	public class NamingConventionTests
	{
		[Fact]
		public void When_original_and_original_Should_succeed()
		{
			var m1 = new Source();

			var m2 = m1.MapToNamingConventionTestsDest();

			m2.So_mEr_Op.Should().Be(m1.So_mEr_Op);
		}

		[Fact]
		public void When_pascal_and_pascal_Should_succeed()
		{
			var m1 = new SourcePascal();

			var m2 = m1.MapToNamingConventionTestsDestPascal();

			m2.SomeProp.Should().Be(m1.SomeProp);
		}

		[Fact]
		public void When_pascal_and_camel_Should_succeed()
		{
			var m1 = new SourcePascal();

			var m2 = m1.MapToNamingConventionTestsDestCamel();

			m2.someProp.Should().Be(m1.SomeProp);
		}

		[Fact]
		public void When_pascal_and_snake_Should_succeed()
		{
			var m1 = new SourcePascal();

			var m2 = m1.MapToNamingConventionTestsDestSnake();

			m2.some_prop.Should().Be(m1.SomeProp);
		}

		[Fact]
		public void When_camel_and_pascal_Should_succeed()
		{
			var m1 = new SourceCamel();

			var m2 = m1.MapToNamingConventionTestsDestPascal();

			m2.SomeProp.Should().Be(m1.someProp);
		}

		[Fact]
		public void When_camel_and_camel_Should_succeed()
		{
			var m1 = new SourceCamel();

			var m2 = m1.MapToNamingConventionTestsDestCamel();

			m2.someProp.Should().Be(m1.someProp);
		}

		[Fact]
		public void When_camel_and_snake_Should_succeed()
		{
			var m1 = new SourceCamel();

			var m2 = m1.MapToNamingConventionTestsDestSnake();

			m2.some_prop.Should().Be(m1.someProp);
		}

		[Fact]
		public void When_snake_and_pascal_Should_succeed()
		{
			var m1 = new SourceSnake();

			var m2 = m1.MapToNamingConventionTestsDestPascal();

			m2.SomeProp.Should().Be(m1.some_prop);
		}

		[Fact]
		public void When_snake_and_camel_Should_succeed()
		{
			var m1 = new SourceSnake();

			var m2 = m1.MapToNamingConventionTestsDestCamel();

			m2.someProp.Should().Be(m1.some_prop);
		}

		[Fact]
		public void When_snake_and_snake_Should_succeed()
		{
			var m1 = new SourceSnake();

			var m2 = m1.MapToNamingConventionTestsDestSnake();

			m2.some_prop.Should().Be(m1.some_prop);
		}

		private class Config : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Source>().To<Dest>();
				config.Map<SourcePascal>().To<DestPascal>().WithDestinationNamingConvention<PascalCaseConvention>();
				config.Map<SourcePascal>().To<DestCamel>().WithDestinationNamingConvention<CamelCaseConvention>();
				config.Map<SourcePascal>().To<DestSnake>().WithDestinationNamingConvention<SnakeCaseConvention>();
				config.Map<SourceCamel>().To<DestPascal>().WithDestinationNamingConvention<PascalCaseConvention>();
				config.Map<SourceCamel>().To<DestCamel>().WithDestinationNamingConvention<CamelCaseConvention>();
				config.Map<SourceCamel>().To<DestSnake>().WithDestinationNamingConvention<SnakeCaseConvention>();
				config.Map<SourceSnake>().To<DestPascal>().WithDestinationNamingConvention<PascalCaseConvention>();
				config.Map<SourceSnake>().To<DestCamel>().WithDestinationNamingConvention<CamelCaseConvention>();
				config.Map<SourceSnake>().To<DestSnake>().WithDestinationNamingConvention<SnakeCaseConvention>();
			}
		}

		public class Source
		{
			public int So_mEr_Op { get; set; } = 5;
		}

		public class SourcePascal
		{
			public int SomeProp { get; set; } = 5;
		}

		public class SourceCamel
		{
			public int someProp { get; set; } = 5;
		}

		public class SourceSnake
		{
			public int some_prop { get; set; } = 5;
		}

		public class Dest
		{
			public int So_mEr_Op { get; set; }
		}

		public class DestPascal
		{
			public int SomeProp { get; set; }
		}

		public class DestCamel
		{
			public int someProp { get; set; }
		}

		public class DestSnake
		{
			public int some_prop { get; set; }
		}
	}
}

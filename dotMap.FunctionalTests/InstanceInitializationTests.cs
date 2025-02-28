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
	public class InstanceInitializationTests
	{
		[Fact]
		public void When_init_properties_Should_initialize_through_object_initializer()
		{
			var m1 = new Model1 { Prop = 5 };

			var m2 = m1.MapToInstanceInitializationTestsModel2();

			m2.Prop.Should().Be(m1.Prop);
		}

		private class Configuration : IMapConfig
		{
			public void ConfigureMap(MapConfig config)
			{
				config.Map<Model1>().To<Model2>();
			}
		}

		public class Model1
		{
			public int Prop { get; init; }
		}

		public class Model2
		{
			public int Prop { get; init; }
		}
	}
}

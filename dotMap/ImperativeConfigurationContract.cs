/*
   Copyright (C) 2025 - 2025 Devexperts Solutions IE Limited
   This Source Code Form is subject to the terms of the Mozilla Public
   License, v. 2.0. If a copy of the MPL was not distributed with this
   file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using dotMap.NamingConventions;

namespace dotMap;

public class DefaultableMap<TSource, TTarget>
{
	public Map<TSource, TTarget> WithMappingMode(SourceMappingMode source) => new();
	public Map<TSource, TTarget> WithMappingMode(SourceMappingMode source, DestMappingMode dest) => new();
	public Map<TSource, TTarget> WithDestinationNamingConvention<TConvention>() where TConvention : INamingConvention, new() => new();
	public Map<TSource, TTarget> WithDestinationNamingConvention<TMembersConvention, TConstructorConvention>() where TMembersConvention : INamingConvention, new() where TConstructorConvention : INamingConvention, new() => new();
}

public sealed class Map<TSource, TTarget> : DefaultableMap<TSource, TTarget>
{
	public Map<TSource, TTarget> With(Func<TSource, TTarget> map) => new();
	public Map<TSource, TTarget> ConstructFrom(Func<TSource, TTarget> constructor) => new();
	public Map<TSource, TTarget> Ignore<P>(Func<TTarget, P> memberSelector) => new();
	public Map<TSource, TTarget> ForMember<P>(Func<TTarget, P> memberSelector, Func<TSource, P> memberValue) => new();
	public Map<TSource, TTarget, P> WithParameter<P>() => new();
	public void Finally(Action<TSource, TTarget> action) { }
}

public sealed class Map<TSource, TTarget, TParameter> : DefaultableMap<TSource, TTarget>
{
	public Map<TSource, TTarget, TParameter> With(Func<TSource, TParameter, TTarget> map) => new();
	public Map<TSource, TTarget, TParameter> ConstructFrom(Func<TSource, TParameter, TTarget> constructor) => new();
	public Map<TSource, TTarget, TParameter> Ignore<P>(Func<TTarget, P> memberSelector) => new();
	public Map<TSource, TTarget, TParameter> ForMember<P>(Func<TTarget, P> memberSelector, Func<TSource, TParameter, P> memberValue) => new();
	public void Finally(Action<TSource, TTarget, TParameter> action) { }
}

public interface IMappable<TSource>
{
    void ConfigureMap(MapConfig<TSource> map);
}

public interface IMapConfig
{
    void ConfigureMap(MapConfig config);
}

public sealed class MapConfig<T>
{
	public Map<T, TTarget> To<TTarget>() => new();

	public Map<TSource, T> From<TSource>() => new();
}

public sealed class MapConfig
{
	public MapConfig<TSource> Map<TSource>() => new();
}

public enum SourceMappingMode
{
	MapPropsAndFields,
	MapMethods,
	MapAllMembers
}

public enum DestMappingMode
{
	MapToPropsAndFields,
	MapToConstructor,
	MapToAllAvailableMembers
}

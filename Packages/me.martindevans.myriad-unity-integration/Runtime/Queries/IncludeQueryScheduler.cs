using Myriad.ECS.Queries;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeAccessorOwnerBody

namespace Myriad.ECS.Worlds
{
    public static class QueryBuilderExtensions
    {
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6, T7>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
		where T7 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();
		builder.Include<T7>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6, T7, T8>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
		where T7 : struct, IComponent
		where T8 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();
		builder.Include<T7>();
		builder.Include<T8>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
		where T7 : struct, IComponent
		where T8 : struct, IComponent
		where T9 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();
		builder.Include<T7>();
		builder.Include<T8>();
		builder.Include<T9>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
		where T7 : struct, IComponent
		where T8 : struct, IComponent
		where T9 : struct, IComponent
		where T10 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();
		builder.Include<T7>();
		builder.Include<T8>();
		builder.Include<T9>();
		builder.Include<T10>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
		where T7 : struct, IComponent
		where T8 : struct, IComponent
		where T9 : struct, IComponent
		where T10 : struct, IComponent
		where T11 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();
		builder.Include<T7>();
		builder.Include<T8>();
		builder.Include<T9>();
		builder.Include<T10>();
		builder.Include<T11>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
		where T7 : struct, IComponent
		where T8 : struct, IComponent
		where T9 : struct, IComponent
		where T10 : struct, IComponent
		where T11 : struct, IComponent
		where T12 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();
		builder.Include<T7>();
		builder.Include<T8>();
		builder.Include<T9>();
		builder.Include<T10>();
		builder.Include<T11>();
		builder.Include<T12>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
		where T7 : struct, IComponent
		where T8 : struct, IComponent
		where T9 : struct, IComponent
		where T10 : struct, IComponent
		where T11 : struct, IComponent
		where T12 : struct, IComponent
		where T13 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();
		builder.Include<T7>();
		builder.Include<T8>();
		builder.Include<T9>();
		builder.Include<T10>();
		builder.Include<T11>();
		builder.Include<T12>();
		builder.Include<T13>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
		where T7 : struct, IComponent
		where T8 : struct, IComponent
		where T9 : struct, IComponent
		where T10 : struct, IComponent
		where T11 : struct, IComponent
		where T12 : struct, IComponent
		where T13 : struct, IComponent
		where T14 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();
		builder.Include<T7>();
		builder.Include<T8>();
		builder.Include<T9>();
		builder.Include<T10>();
		builder.Include<T11>();
		builder.Include<T12>();
		builder.Include<T13>();
		builder.Include<T14>();

		return builder;
	}
	/// <summary>
	/// Include only entities which have all of these components
	/// </summary>
	/// <returns>The query builder</returns>
	// ReSharper disable once UnusedTypeParameter (Justification: Used for checking the query against the type signature)
	public static QueryBuilder IncludeScheduledQuery<TQ, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this QueryBuilder builder)
		where TQ : WorldJobExtensions.IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
		where T0 : struct, IComponent
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
		where T7 : struct, IComponent
		where T8 : struct, IComponent
		where T9 : struct, IComponent
		where T10 : struct, IComponent
		where T11 : struct, IComponent
		where T12 : struct, IComponent
		where T13 : struct, IComponent
		where T14 : struct, IComponent
		where T15 : struct, IComponent
	{
		builder.Include<T0>();
		builder.Include<T1>();
		builder.Include<T2>();
		builder.Include<T3>();
		builder.Include<T4>();
		builder.Include<T5>();
		builder.Include<T6>();
		builder.Include<T7>();
		builder.Include<T8>();
		builder.Include<T9>();
		builder.Include<T10>();
		builder.Include<T11>();
		builder.Include<T12>();
		builder.Include<T13>();
		builder.Include<T14>();
		builder.Include<T15>();

		return builder;
	}
	}
}



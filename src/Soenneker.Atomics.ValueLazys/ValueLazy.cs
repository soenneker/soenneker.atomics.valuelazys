using Soenneker.Atomics.ValueLocks;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Soenneker.Atomics.ValueLazys;

/// <summary>
/// Provides inline lazy storage for a non-null reference value without allocating a <see cref="Lazy{T}"/> wrapper.
/// </summary>
/// <typeparam name="T">The non-null reference type stored by the lazy value.</typeparam>
/// <remarks>
/// <para>
/// The default value is ready to use and occupies one reference-sized field. A <see cref="ValueAtomicLock"/> can be shared
/// by several lazy fields on the same owner, avoiding a separate synchronization object for every value.
/// </para>
/// <para>
/// This is a mutable <see langword="struct"/> intended for use as a private field. Avoid copying it because each copy has
/// independent initialization state. Exceptions thrown by a factory are not cached, and a later call may retry initialization.
/// </para>
/// </remarks>
[DebuggerDisplay("IsValueCreated = {IsValueCreated}")]
public struct ValueLazy<T> where T : class
{
    private T? _value;

    /// <summary>
    /// Gets a value indicating whether initialization has completed successfully.
    /// </summary>
    public bool IsValueCreated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _value) is not null;
    }

    /// <summary>
    /// Attempts to read the initialized value without invoking a factory.
    /// </summary>
    /// <param name="value">Replacement value to store atomically.</param>
    /// <returns>true if the requested update was applied; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = Volatile.Read(ref _value);
        return value is not null;
    }

    /// <summary>
    /// Gets the initialized value or invokes <paramref name="factory"/> exactly once using execution-and-publication semantics.
    /// </summary>
    /// <param name="sync">Synchronization object guarding one-time initialization.</param>
    /// <param name="factory">Factory used to create a value when one is needed.</param>
    /// <returns>The requested value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrCreate(ref ValueAtomicLock sync, Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return GetOrCreate(ref sync, factory, static valueFactory => valueFactory());
    }

    /// <summary>
    /// Gets the initialized value or invokes <paramref name="factory"/> exactly once using execution-and-publication semantics.
    /// Supplying state allows callers to use a static factory and avoid a closure allocation.
    /// </summary>
    /// <typeparam name="TState">Type of state passed to the callback.</typeparam>
    /// <param name="sync">Synchronization object guarding one-time initialization.</param>
    /// <param name="state">State value used by the variant.</param>
    /// <param name="factory">Factory used to create a value when one is needed.</param>
    /// <returns>The requested value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrCreate<TState>(ref ValueAtomicLock sync, TState state, Func<TState, T> factory)
    {
        T? value = Volatile.Read(ref _value);
        return value ?? Initialize(ref sync, state, factory);
    }

    /// <summary>
    /// Gets or creates the value without locking. This method is only safe when the caller provides external synchronization
    /// or guarantees single-threaded access.
    /// </summary>
    /// <param name="factory">Factory used to create a value when one is needed.</param>
    /// <returns>The requested value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrCreateUnsafe(Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return GetOrCreateUnsafe(factory, static valueFactory => valueFactory());
    }

    /// <summary>
    /// Gets or creates the value without locking. Supplying state allows callers to use a static factory and avoid a closure allocation.
    /// </summary>
    /// <typeparam name="TState">Type of state passed to the callback.</typeparam>
    /// <param name="state">State value used by the variant.</param>
    /// <param name="factory">Factory used to create a value when one is needed.</param>
    /// <returns>The requested value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrCreateUnsafe<TState>(TState state, Func<TState, T> factory)
    {
        T? value = _value;
        return value ?? (_value = Create(state, factory));
    }

    /// <summary>
    /// Gets the initialized value or atomically publishes one factory result. During a race the factory may run more than once,
    /// but every caller receives the single published value.
    /// </summary>
    /// <param name="factory">Factory used to create a value when one is needed.</param>
    /// <returns>The requested value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrCreatePublicationOnly(Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return GetOrCreatePublicationOnly(factory, static valueFactory => valueFactory());
    }

    /// <summary>
    /// Gets the initialized value or atomically publishes one factory result. Supplying state allows callers to use a static
    /// factory and avoid a closure allocation.
    /// </summary>
    /// <typeparam name="TState">Type of state passed to the callback.</typeparam>
    /// <param name="state">State value used by the variant.</param>
    /// <param name="factory">Factory used to create a value when one is needed.</param>
    /// <returns>The requested value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrCreatePublicationOnly<TState>(TState state, Func<TState, T> factory)
    {
        T? value = Volatile.Read(ref _value);
        if (value is not null)
            return value;

        T created = Create(state, factory);
        return Interlocked.CompareExchange(ref _value, created, null) ?? created;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private T Initialize<TState>(ref ValueAtomicLock sync, TState state, Func<TState, T> factory)
    {
        lock (sync.Get())
        {
            T? value = _value;
            if (value is not null)
                return value;

            value = Create(state, factory);
            Volatile.Write(ref _value, value);
            return value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Create<TState>(TState state, Func<TState, T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory(state) ?? throw new InvalidOperationException(
            $"The {nameof(ValueLazy<T>)} factory returned null. Use ValueNullableLazy<T> when null is a valid result.");
    }
}

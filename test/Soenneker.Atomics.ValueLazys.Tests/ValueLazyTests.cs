using AwesomeAssertions;
using Soenneker.Atomics.ValueLocks;
using Soenneker.Tests.Unit;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Atomics.ValueLazys.Tests;

public sealed class ValueLazyTests : UnitTest
{
    [Test]
    public void Default_should_be_uninitialized_and_reference_sized()
    {
        var value = new ValueLazy<object>();

        value.IsValueCreated.Should().BeFalse();
        value.TryGetValue(out _).Should().BeFalse();
        Unsafe.SizeOf<ValueLazy<object>>().Should().Be(IntPtr.Size);
    }

    [Test]
    public void GetOrCreate_should_cache_one_value()
    {
        var holder = new Holder();

        Payload first = holder.Value.GetOrCreate(ref holder.Sync, static () => new Payload(42));
        Payload second = holder.Value.GetOrCreate(ref holder.Sync, static () => new Payload(43));

        ReferenceEquals(first, second).Should().BeTrue();
        second.Number.Should().Be(42);
        holder.Value.IsValueCreated.Should().BeTrue();
        holder.Value.TryGetValue(out Payload? cached).Should().BeTrue();
        ReferenceEquals(first, cached).Should().BeTrue();
    }

    [Test]
    public void Stateful_factory_should_avoid_capturing_the_owner()
    {
        var holder = new Holder();

        Payload value = holder.Value.GetOrCreate(ref holder.Sync, 42, static number => new Payload(number));

        value.Number.Should().Be(42);
    }

    [Test]
    public void Concurrent_execution_and_publication_should_invoke_factory_once()
    {
        var holder = new Holder();
        var values = new Payload[128];

        Parallel.For(0, values.Length, i =>
        {
            values[i] = holder.Value.GetOrCreate(ref holder.Sync, holder, static state =>
            {
                Interlocked.Increment(ref state.FactoryCalls);
                Thread.SpinWait(20_000);
                return new Payload(42);
            });
        });

        holder.FactoryCalls.Should().Be(1);
        values.All(value => ReferenceEquals(values[0], value)).Should().BeTrue();
    }

    [Test]
    public void Publication_only_should_publish_one_result()
    {
        var holder = new Holder();
        var values = new Payload[128];

        Parallel.For(0, values.Length, i =>
        {
            values[i] = holder.Value.GetOrCreatePublicationOnly(holder, static state =>
            {
                Interlocked.Increment(ref state.FactoryCalls);
                Thread.SpinWait(20_000);
                return new Payload(42);
            });
        });

        holder.FactoryCalls.Should().BeGreaterThanOrEqualTo(1);
        values.All(value => ReferenceEquals(values[0], value)).Should().BeTrue();
    }

    [Test]
    public void Unsafe_initialization_should_not_create_the_lock()
    {
        var holder = new Holder();

        Payload value = holder.Value.GetOrCreateUnsafe(static () => new Payload(42));

        value.Number.Should().Be(42);
        holder.Sync.IsValueCreated.Should().BeFalse();
    }

    [Test]
    public void Null_factory_result_should_throw_and_remain_uninitialized()
    {
        var holder = new Holder();

        Action action = () => holder.Value.GetOrCreate(ref holder.Sync, static () => null!);

        action.Should().Throw<InvalidOperationException>();
        holder.Value.IsValueCreated.Should().BeFalse();
    }

    private sealed class Holder
    {
        public ValueLazy<Payload> Value;
        public ValueAtomicLock Sync;
        public int FactoryCalls;
    }

    private sealed record Payload(int Number);
}

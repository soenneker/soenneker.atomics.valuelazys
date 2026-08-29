[![](https://img.shields.io/nuget/v/soenneker.atomics.valuelazys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.atomics.valuelazys/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.atomics.valuelazys/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.atomics.valuelazys/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.atomics.valuelazys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.atomics.valuelazys/)

# Soenneker.Atomics.ValueLazys

Provides inline lazy storage for a non-null reference value without allocating a `Lazy{T}` wrapper.

## Install

```bash
dotnet add package Soenneker.Atomics.ValueLazys
```

## What you get

- `ValueLazy<T>` — Provides inline lazy storage for a non-null reference value without allocating a `Lazy{T}` wrapper.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `ValueLazy<T>.IsValueCreated` | Gets a value indicating whether initialization has completed successfully. | Gets a value indicating whether initialization has completed successfully. |
| `ValueLazy<T>.TryGetValue(value)` | Attempts to read the initialized value without invoking a factory. | true if the requested update was applied; otherwise, false. |
| `ValueLazy<T>.GetOrCreate(sync, factory)` | Gets the initialized value or invokes `factory` exactly once using execution-and-publication semantics. | The requested value. |
| `ValueLazy<T>.GetOrCreate(sync, state, factory)` | Gets the initialized value or invokes `factory` exactly once using execution-and-publication semantics. Supplying state allows callers to use a static factory and avoid a closure allocation. | The requested value. |
| `ValueLazy<T>.GetOrCreateUnsafe(factory)` | Gets or creates the value without locking. This method is only safe when the caller provides external synchronization or guarantees single-threaded access. | The requested value. |
| `ValueLazy<T>.GetOrCreateUnsafe(state, factory)` | Gets or creates the value without locking. Supplying state allows callers to use a static factory and avoid a closure allocation. | The requested value. |
| `ValueLazy<T>.GetOrCreatePublicationOnly(factory)` | Gets the initialized value or atomically publishes one factory result. During a race the factory may run more than once, but every caller receives the single published value. | The requested value. |
| `ValueLazy<T>.GetOrCreatePublicationOnly(state, factory)` | Gets the initialized value or atomically publishes one factory result. Supplying state allows callers to use a static factory and avoid a closure allocation. | The requested value. |

## Important behavior

- `ValueLazy<T>`: The default value is ready to use and occupies one reference-sized field. A `ValueAtomicLock` can be shared by several lazy fields on the same owner, avoiding a separate synchronization object for every value. This is a mutable `struct` intended for use as a private field. Avoid copying it because each copy has independent initialization state. Exceptions thrown by a factory are not cached, and a later call may retry initialization.

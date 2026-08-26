[![](https://img.shields.io/nuget/v/soenneker.atomics.valuelazys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.atomics.valuelazys/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.atomics.valuelazys/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.atomics.valuelazys/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.atomics.valuelazys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.atomics.valuelazys/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Atomics.ValueLazys
### Thread-safe lazy initialization without Lazy wrapper allocations.

## Installation

```
dotnet add package Soenneker.Atomics.ValueLazys
```

## Usage

```csharp
using Soenneker.Atomics.ValueLazys;
using Soenneker.Atomics.ValueLocks;

public sealed class Service
{
    private ValueLazy<Client> _client;
    private ValueAtomicLock _initializationLock;

    public Client GetClient() =>
        _client.GetOrCreate(ref _initializationLock, this,
            static service => new Client(service.GetConnectionString()));
}
```

`ValueLazy<T>` occupies one reference-sized field. Several lazy fields on an owner can share one `ValueAtomicLock`, avoiding the wrapper, factory closure, and synchronization object normally allocated for every `Lazy<T>`.

- `GetOrCreate` provides execution-and-publication semantics.
- `GetOrCreatePublicationOnly` may run the factory concurrently but atomically publishes one result.
- `GetOrCreateUnsafe` performs no synchronization.

The factory must return a non-null value. Use `Soenneker.Atomics.ValueNullableLazys` when `null` is a valid initialized result.

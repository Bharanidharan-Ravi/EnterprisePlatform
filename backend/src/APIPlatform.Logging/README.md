# APIPlatform.Logging

This module provides the generic logging abstraction for APIPlatform. It encapsulates `Microsoft.Extensions.Logging` behind a custom `IPlatformLogger<T>` interface to ensure that business modules are not directly coupled to the framework's logging implementation.

## Guidelines
- Do **not** inject `ILogger<T>` directly into your services. Always use `IPlatformLogger<T>`.
- Do **not** implement database sinks, file sinks, or other providers directly here. They will be added as provider extensions later.

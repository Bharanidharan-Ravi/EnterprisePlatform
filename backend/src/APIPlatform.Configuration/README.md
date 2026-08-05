# APIPlatform.Configuration

This module provides the generic configuration infrastructure for APIPlatform.

## Features
- Provides helper methods like `BindPlatformOptions<T>` to easily bind options from `IConfiguration`.
- Exposes `IConfiguration` for generic use.
- Allows consumption via `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`.

## Guidelines
- Do **not** place application-specific configuration classes (e.g. `JwtOptions`, `DatabaseOptions`) here. Those belong in their respective modules.
- Ensure that validation rules (like `DataAnnotations`) are correctly applied when calling the binder.

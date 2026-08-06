# APIPlatform.Validation

This module provides a generic, decoupled validation pipeline for EnterprisePlatform.

## Features
- Provides `IValidator<T>` for implementing strongly-typed validators.
- Provides `IValidationService` to execute all registered validators for a given instance.
- Completely decoupled from ASP.NET MVC and business-specific validation rules.

## Guidelines
- Do **not** place business validators (e.g. `CustomerValidator`) inside this module.
- Always use the `IValidationService` to validate objects, rather than manually instantiating or injecting specific validators directly.

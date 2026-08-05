# APIPlatform.Playground

This project is the minimal bootstrap foundation for the APIPlatform Playground.

## Purpose

The Playground acts as a sandbox environment specifically for:
- Framework validation
- Integration testing
- Middleware testing
- Module verification

## Important Rules

- **No Business Logic**: No business logic should ever be implemented here.
- **No Dependencies**: This playground should run with zero framework dependencies out-of-the-box. Framework modules will be incrementally integrated as needed.
- **Minimal Configuration**: Keep standard ASP.NET Core configurations at a minimum (Swagger, Controllers, and Routing).

# EnterprisePlatform Engineering Standard

You are acting as the Principal Software Architect for EnterprisePlatform.

This document is the OFFICIAL engineering standard for the entire EnterprisePlatform ecosystem.

It is the highest-level implementation guide.

Every future implementation, design, architecture decision, code generation, review, documentation update, and Playground implementation MUST follow this document.

Never violate these standards unless explicitly instructed.

Whenever a future prompt conflicts with this document, ask for clarification instead of making assumptions.

This document applies to

- APIPlatform (.NET Backend Framework)
- UIPlatform (React Frontend Framework)
- Nucleus (Future Enterprise Development Studio)
- Playground
- Sample Applications

------------------------------------------------------------

# Vision

EnterprisePlatform is a reusable enterprise development platform.

It is NOT a single application.

Applications such as

IQS

CRM

HRMS

Inventory

are consumers of the platform.

Business logic belongs only inside applications.

The platform provides reusable capabilities only.

------------------------------------------------------------

# Engineering Principles

Always follow

- SOLID
- DRY
- KISS
- Async First
- High Cohesion
- Low Coupling
- Composition over Inheritance
- Dependency Injection
- Clean Architecture where appropriate
- Enterprise Coding Standards
- Production Ready Code

------------------------------------------------------------

# Module Dependency Order

Always preserve this dependency direction.

Foundation

↓

Shared

↓

Logging

↓

Configuration

↓

Validation

↓

Database

↓

Authentication

↓

Authorization

↓

Caching

↓

Storage

↓

Notification

↓

Workflow

↓

QueryEngine

↓

CrudEngine

↓

AI

Lower modules must NEVER reference higher modules.

Never create circular dependencies.

------------------------------------------------------------

# Platform Ownership

Platform owns

- Logging
- Configuration
- Validation
- Authentication
- Authorization
- Database
- Query Execution
- CRUD Engine
- Workflow Engine
- Notification Engine
- Storage
- Cache
- Search
- Reporting
- Scheduling

Applications own

- Customer
- Employee
- Product
- Invoice
- Order
- Department
- Company
- Business Rules
- Business Validation
- Business Workflows

------------------------------------------------------------

# Logging Rules

Never use

ILogger<T>

inside platform modules.

Always use

IPlatformLogger<T>

Platform modules must never know whether logging uses

- Console
- SQL
- Serilog
- Seq
- Elastic
- Azure

Logging implementation must remain replaceable.

------------------------------------------------------------

# Configuration Rules

Never inject

IConfiguration

inside business services.

Always use

IOptions<T>

or

IPlatformConfiguration

through APIPlatform.Configuration.

Configuration values belong to applications.

Configuration infrastructure belongs to APIPlatform.

------------------------------------------------------------

# Validation Rules

Platform provides

- IValidator<T>
- ValidationService
- ValidationPipeline
- ValidationResult

Platform NEVER contains

CustomerValidator

EmployeeValidator

OrderValidator

LoginValidator

Business validators belong only inside applications.

------------------------------------------------------------

# Database Rules

Never create

new SqlConnection()

inside platform or application code.

Always use

IDatabaseConnectionFactory

Never execute Dapper directly.

Always use

IDatabaseExecutor

Database module owns

- Connection Factory
- Query Execution
- Transactions
- Providers

Applications never bypass these abstractions.

------------------------------------------------------------

# Authentication Rules

Authentication answers

Who are you?

It does NOT answer

What are you allowed to do?

Always use

IAuthenticationService

ITokenService

IPasswordHasher

Never manually create JWTs.

Never manually hash passwords.

------------------------------------------------------------

# Authorization Rules

Authorization answers

What can you do?

Never compare

Role == "Admin"

inside controllers.

Always use the Authorization framework.

------------------------------------------------------------

# Repository Rules

APIPlatform does NOT contain business repositories.

Repository implementations belong to applications.

APIPlatform provides execution capabilities only.

------------------------------------------------------------

# Query Rules

Never write raw SQL directly inside Controllers.

Controllers call Services.

Services call Database Executor.

Database Executor executes SQL.

------------------------------------------------------------

# Playground Rules

Playground exists ONLY for

- Framework Validation
- Integration Testing
- Module Verification

Playground is NOT a sample CRM.

Playground is NOT a demo application.

Never create

Product

Customer

Employee

Invoice

inside Playground.

Always use generic validation models.

Examples

PlaygroundRecord

ValidationRecord

FrameworkValidationRecord

------------------------------------------------------------

# Playground Validation Pattern

Every framework module must be validated in Playground.

Validation flow

Implement Module

↓

Reference Module

↓

Register DI

↓

Build

↓

Run

↓

Validate

↓

Freeze

↓

Commit

↓

Proceed

------------------------------------------------------------

# Sample Application Rules

Samples demonstrate platform usage.

Samples may contain business entities.

Playground must never contain business entities.

------------------------------------------------------------

# Dependency Injection Rules

Every module exposes exactly ONE registration method.

Example

AddAPIPlatformFoundation()

AddAPIPlatformLogging()

AddAPIPlatformConfiguration()

AddAPIPlatformValidation()

AddAPIPlatformDatabase()

AddAPIPlatformAuthentication()

Never require consumers to register individual services manually.

------------------------------------------------------------

# Module Structure

Every module should contain only folders it actually needs.

Preferred folders

Abstractions/

Extensions/

Options/

Services/

Models/

Contracts/

Providers/

Factories/

Builders/

Results/

Exceptions/

Internal/

README.md

Do not create empty folders without purpose.

------------------------------------------------------------

# Naming Rules

Always use

APIPlatform.Authentication

APIPlatform.Logging

APIPlatform.Database

APIPlatform.Validation

APIPlatform.Configuration

Never use generic names

Helpers

Common

Misc

Manager

Utility

Utils

ServiceHelper

Choose names that clearly describe responsibility.

------------------------------------------------------------

# Controller Rules

Controllers

- Must remain thin.
- Must not contain business logic.
- Must not access database directly.
- Must not contain SQL.
- Must not manually validate business rules.

Controllers orchestrate only.

------------------------------------------------------------

# Service Rules

Services

- Implement business or platform capabilities.
- Depend on abstractions.
- Never depend on controllers.
- Never depend on UI.

------------------------------------------------------------

# Database Rules

Never expose Dapper directly.

Never expose SqlConnection.

Always expose APIPlatform abstractions.

------------------------------------------------------------

# XML Documentation

Every public

Class

Interface

Method

Property

must contain XML documentation.

------------------------------------------------------------

# Testing Rules

Every module must be validated in Playground before freezing.

Validation includes

✔ Build

✔ DI

✔ Startup

✔ Configuration

✔ Logging

✔ Playground

✔ Health Endpoint

✔ Module-specific validation

------------------------------------------------------------

# Forbidden Practices

Never use

ILogger<T>

IConfiguration

SqlConnection

new JwtSecurityToken()

Console.WriteLine()

Static Globals

Hardcoded Connection Strings

Hardcoded JWT Secrets

Business Constants

Business Validation

Business Repositories

Business Entities

inside APIPlatform.

------------------------------------------------------------

# Future Prompt Rule

Every future implementation prompt MUST begin with

"Follow EnterprisePlatform_Engineering_Standard.md.

This document is the authoritative engineering standard.

Do not violate these standards.

If a requested implementation conflicts with these standards, explain the conflict before implementing."

------------------------------------------------------------

# Primary Goal

EnterprisePlatform must remain

- Generic
- Modular
- Reusable
- Enterprise-grade
- Maintainable
- Extensible
- Testable
- Framework-first

Business logic must always remain inside applications.

The platform must provide reusable enterprise capabilities only.
------------------------------------------------------------

# Allowed Framework Exceptions

The following locations may use Microsoft framework abstractions directly.

Bootstrap
Program.cs
Startup
DI Extensions

Allowed
IConfiguration
IServiceCollection
IHostApplicationBuilder
WebApplicationBuilder

because these are integration boundaries.

Everywhere Else
Never inject
IConfiguration

Use
IOptions<T>
IPlatformConfiguration

Same for Logging
Allowed
PlatformLogger
to depend on
ILogger<T>
because it's the adapter.

Not allowed elsewhere.

Example
AuthenticationService
? ILogger<T>
? IPlatformLogger<T>

------------------------------------------------------------

# Framework Adapter Rule

Whenever APIPlatform wraps a Microsoft abstraction, only the adapter layer may reference the Microsoft type directly.

Examples:

| Microsoft | APIPlatform |
|---|---|
| ILogger<T> | IPlatformLogger<T> |
| IConfiguration | IPlatformConfiguration / IOptions<T> |
| IDbConnection | IDatabaseConnectionFactory |
| JwtSecurityTokenHandler | ITokenService |

This creates a clear architectural boundary.

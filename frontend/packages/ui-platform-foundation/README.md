# UIPlatform.Foundation

Root dependency of every Nucleus UIPlatform package. Provides only generic,
reusable engineering infrastructure — no business logic, no CRM/HRMS/IQS or
other application-specific code, no Auth/Forms/Grid/CRUD/Workflow/Search/
Storage/Notification/Dashboard/SignalR (those live in their own packages,
built on top of this one).

## Naming Convention

npm package name: `@nucleus/uiplatform-foundation`, mirroring the .NET module
name `UIPlatform.Foundation` in lowercase-kebab form. Every future UI package
follows the same pattern: `@nucleus/uiplatform-<name>` (e.g.
`@nucleus/uiplatform-auth`, `@nucleus/uiplatform-forms`,
`@nucleus/uiplatform-grid`). Use this convention for all future UIPlatform
packages without variation.

## Responsibilities

- **API Client** — configured axios instance (`createApiClient`, `getApiClient`)
- **HTTP Pipeline** — request/response interceptors for tenant + auth header injection
  (`attachRequestInterceptor`, `attachErrorInterceptor`)
- **Response Parsing** — unwraps the `ApiResponse<T>` envelope, throws normalized `ApiError`
- **Query / Mutation Infrastructure** — `useApiQuery`, `useApiMutation` (typed TanStack Query wrappers)
- **Route Infrastructure** — `AppRouterProvider` (base `BrowserRouter` only; guards belong to UIPlatform.Auth)
- **App Configuration** — `configureApp`, `getAppConfig` (env-driven, override at bootstrap)
- **Multi-tenancy** — `TenantProvider` / `useTenant` (`ITenantContext`), present from day one per platform rule
- **Shared Types** — `ApiResponse<T>`, `ApiError`, `PagedResult<T>`, `QueryParams`, `AppConfig`
- **Shared Utilities** — `toQueryString`, `isApiError`, `toErrorMessage`, `HttpStatus`
- **Store Factory** — `createStore` (thin Zustand wrapper; no concrete stores defined here)
- **Error Boundary** — `AppErrorBoundary` (generic, fallback-render-prop based; not coupled to any app)
- **Logging Extension Point** — `Logger` interface + `getLogger`/`setLogger` (no-op by default; a future
  diagnostics package plugs in a real implementation)
- **Theme Extension Point** — `AppProvider`'s optional `themeProvider` prop (`ThemeProviderComponent` type);
  Foundation implements no theming, only the slot a future UIPlatform.Theme package fills

## Usage

```tsx
// main.tsx
import { AppProvider } from '@nucleus/uiplatform-foundation';

function App() {
  return (
    <AppProvider config={{ apiBaseUrl: '/api' }}>
      <YourApp />
    </AppProvider>
  );
}
```

```tsx
// Reading data
import { useApiQuery } from '@nucleus/uiplatform-foundation';

function useWidgets() {
  return useApiQuery<Widget[]>({
    queryKey: ['widgets'],
    requestConfig: { url: '/widgets', method: 'GET' },
  });
}
```

```tsx
// Writing data
import { useApiMutation } from '@nucleus/uiplatform-foundation';

function useCreateWidget() {
  return useApiMutation<Widget, CreateWidgetInput>({
    buildRequest: (input) => ({ url: '/widgets', method: 'POST', data: input }),
  });
}
```

```tsx
// Error boundary — generic, caller supplies fallback UI
import { AppErrorBoundary } from '@nucleus/uiplatform-foundation';

<AppErrorBoundary fallback={(error, reset) => <ErrorScreen error={error} onRetry={reset} />}>
  <AppProvider>
    <YourApp />
  </AppProvider>
</AppErrorBoundary>
```

```tsx
// Logging — plug in a real implementation once one exists; no-op until then
import { setLogger } from '@nucleus/uiplatform-foundation';

setLogger({
  debug: (msg, ctx) => console.debug(msg, ctx),
  info: (msg, ctx) => console.info(msg, ctx),
  warn: (msg, ctx) => console.warn(msg, ctx),
  error: (msg, err, ctx) => console.error(msg, err, ctx),
});
```

## Integration Notes for Future Packages

- `UIPlatform.Auth` supplies `config.getAuthToken` to `AppProvider`/`configureApp` and adds
  route guards on top of `AppRouterProvider`. It does not modify Foundation.
- `UIPlatform.Forms`, `.Grid`, `.CRUD`, `.Search`, `.Workflow`, `.Storage`, `.Notification`,
  `.SignalR`, `.Dashboard` all consume `useApiQuery`/`useApiMutation`/`apiRequest` and the
  shared `ApiResponse<T>` / `PagedResult<T>` / `QueryParams` types — they must not redefine them.
- A future `UIPlatform.Theme` package implements `ThemeProviderComponent` and passes itself to
  `AppProvider`'s `themeProvider` prop. No change to `AppProvider`'s signature is required.
- A future diagnostics/logging package calls `setLogger(...)` once at bootstrap; `AppErrorBoundary`
  and any Foundation-internal error paths automatically route through it.
- Tenant resolution: once `UIPlatform.Auth` establishes a real session, it should call
  `useTenant().setTenantId(...)`, which automatically flows into every subsequent API request
  via the existing `X-Tenant-Id` header interceptor. No changes to Foundation required.

## Non-Goals

This package deliberately does **not** contain authentication, forms, grids, CRUD,
workflow, search, storage, notifications, dashboards, or SignalR. Those are separate,
independently-installable UIPlatform packages that depend on Foundation, never the reverse.

---
name: kepler-service-builder
description: Use when someone wants to create, scaffold, deploy, call, or troubleshoot a custom Kepler service in Relativity aiR, meaning a custom REST endpoint or .NET API hosted inside Relativity, a custom Kepler module, a backend for a custom page, or an agent's Workload Discovery endpoint. Produces compiling Interfaces and Services projects plus a console test client from templates, and covers routes, request bodies, permissions, exceptions, deployment, and "endpoint could not be found" errors.
---

# Relativity aiR Kepler Service Builder

You are helping a developer build a custom **Kepler service**: their own REST and .NET API, hosted by Relativity aiR, that runs with the caller's identity and can use Relativity's APIs on the server.

**Relativity aiR only.** This covers the cloud platform, not Relativity Server. If the user is on Server, say so.

**What this skill does and does not do.** It produces two class libraries and a test client that compile, starting from the templates in this skill. It does **not** deploy anything. Uploading the DLLs and calling the live service are steps the user performs; walk them through and never say the service is live.

## Step 1: Confirm Kepler is the right tool

A Kepler service fits a request/response call that should return promptly: a custom page's backend, an integration endpoint, a computed lookup. For work that takes minutes, have the service queue it and let an agent process it (agent-builder skill). Kepler calls time out after 20 minutes.

## Step 2: Scaffold from the templates

Copy `templates/` from this skill into the user's solution. It has three projects:

| Project | What it holds | Upload to Relativity? |
|---|---|---|
| `Interfaces/` | The contract: module and service interfaces, request/response models, exceptions | Yes |
| `Services/` | The implementation | Yes |
| `TestClient/` | A console app that calls the deployed service through the Kepler .NET proxy | No |

The contract is its own assembly because callers share it: the test client (and any .NET consumer) references `Interfaces` directly, never `Services`.

Then make it theirs:

1. Replace `MyCompany.MyApp` (namespaces, project and assembly names, file names, and the solution) with the user's root namespace, for example `Northwind.Insights`. Namespaces then read `Northwind.Insights.Interfaces.<Module>.v1`. Keep the `Interfaces`, `Services` and `TestClient` folder names, or update the `ProjectReference` paths if you rename them.
2. Rename the module (`MyModule`, route `my-module`) and the service (`SampleService`, route `sample-service`). Use lowercase, hyphenated routes.
3. **Replace every `00000000-0000-0000-0000-000000000000` with a new GUID** (`[guid]::NewGuid()` in PowerShell, or `uuidgen`). Each custom exception needs its own.
4. Build from the folder with `MyCompany.MyApp.sln`: `dotnet build -c Release`. It must build with no errors or warnings.

## Step 3: Define the contract (rules Relativity enforces)

Each of these causes a deployment failure, a 404, or a 500 if missed:

- **Module:** one interface per module namespace, with `[ServiceModule("Name")]` and `[RoutePrefix("route", VersioningStrategy.Namespace)]`, plus one empty class implementing it in Services. Every service in that namespace or below belongs to the module. Only one module per namespace.
- **Service interface:** `[WebService("Name")]`, `[ServiceAudience(Audience.Public)]` and `[RoutePrefix("route")]`, and it must inherit `IDisposable`. Missing `ServiceAudience` fails deployment with `(555) ... Service Audience is not specified`. An empty `RoutePrefix` gives a 404 and an empty `WebService` a 500. Route prefixes must not contain the word "Relativity". Use `Audience.Private` for endpoints only Relativity itself calls, such as Workload Discovery.
- **Versioning:** the `.v1.` segment of the namespace produces `/v1/` in the URL. To make a breaking change, add a `v2` namespace alongside `v1`; don't edit `v1` in place.
- **Methods:** every method returns `Task` or `Task<T>`, ends in `Async`, and is public in the implementation. Mark verbs with `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` and set `[Route("...")]`. A method with no verb is a POST. **Every parameter of a GET must be in the route**; parameters not in the route are read from the body.
- **Unique service names:** give every service interface a specific name (`IInvoiceExportService`, not `IService` or `IWorkloadDiscoveryService`). Kepler refuses to host two services with the same interface name in an application, and in testing on Relativity aiR that error stopped **every** Kepler service in the application, including ones that had been working.
- **Implementation:** exactly one class per service interface, with a constructor taking what it needs from dependency injection: `IHelper` (Relativity.API) and `ILog` (`Relativity.Kepler.Logging`) are always available. `IHelper` gives `GetServicesManager()`, `GetDBContext()`, `GetSecretStore()` and more.
- **Exceptions:** put custom exceptions in Interfaces so clients can catch them by type. Derive from `ServiceException`, give each a unique `[ExceptionIdentifier("guid")]`, and set the HTTP status with `[FaultCode(HttpStatusCode.X)]` (a plain `ServiceException` is a 500). For common cases throw the built-ins from `Relativity.Services.Exceptions`: `ValidationException` (400), `NotAuthorizedException` (403), `NotFoundException` (404), `ConflictException` (409).
- **Packages (nuget.org):** `Relativity.Kepler` on Interfaces; `Relativity.API`, `Relativity.Kepler` and, to call Relativity APIs, `Relativity.Services.Interfaces.SDK` on Services (Object Manager: `Relativity.Services.Objects.IObjectManager`, models in `Relativity.Services.Objects.DataContracts`). All with `ExcludeAssets="runtime"`, so `bin/Release/net462` holds only the user's own DLLs, which is exactly what to upload. Target `net462` (Relativity's documented framework for extensibility points). The Object Manager package's DLLs (`Relativity.Services.Interfaces.dll`, `Relativity.Services.DataContracts.dll`) are on Relativity's list of assemblies it provides at runtime, so there is nothing extra to upload. Do not use the newer `Relativity.ObjectManager.SDK` (namespace `Relativity.ObjectManager.V1`) in server-side code: its DLLs are not on that list. Never use RSAPI (`kCura.Relativity.Client`). (A separate .NET client outside Relativity, like the TestClient, can use any client SDK it likes.)

## Step 4: Write the implementation

- **Run as the caller.** Relativity authenticates every call. Create API proxies with `ExecutionIdentity.CurrentUser` so Relativity's own permissions decide what the caller can read or change, as the template does. Use `ExecutionIdentity.System` only after your code has confirmed the caller is allowed to do what it is about to do. A `System` proxy behind a `Public` endpoint otherwise lets any signed-in user act with service-account rights.
- **Validate input** and throw `ValidationException` with a message that says what was wrong.
- **Be async all the way.** `await` with `.ConfigureAwait(false)`; never block with `.Result` in a service.
- **Log with message templates** (`_logger.LogWarning("Workspace {WorkspaceID} not found", id)`), never string interpolation, and log identifiers rather than document content. Relativity's documentation calls out interpolation in log templates as a memory leak.
- **Long-running calls:** a method that takes a `CancellationToken` or `IProgress<T>` is treated as long-running. For large uploads or downloads use `IKeplerStream` as a direct parameter or return type (never nested in a model).
- **Keep it stateless.** Any instance of the service may handle any call; keep no state between calls in memory.
- **Cap list inputs.** If a method takes a list of IDs, set and document a maximum (for example 1,000) and throw `ValidationException` above it.

### Object Manager quick reference

These were checked against a live Relativity aiR instance:

| Need | How |
|---|---|
| Object type of a query | `new ObjectTypeRef { ArtifactTypeID = n }`: Workspace 8 (query workspace `-1`), Document 10, Saved Search 15, Field 14, Object Type 25, Choice 7. For your own objects use `Guid`. |
| Count without fetching rows | `QueryAsync(workspaceId, query, 1, 1)` and read `TotalCount`. |
| Paging | `start` is **1-based**; `length` is the page size. |
| Documents in a saved search | `Condition = "'ArtifactID' IN SAVEDSEARCH 1234567"` |
| A list of IDs | `Condition = "'ArtifactID' IN [1001, 1002]"` |
| A choice value | `Condition = "'Status' IN CHOICE [1038001]"`; field and choice can both be GUIDs: `"'<field guid>' IN CHOICE [<choice guid>]"` |
| Field by GUID | `new FieldRef { Guid = ... }` in `Fields`; `'<field guid>'` in a condition |

An invalid condition throws `ValidationException` with the parse position, so a query that returns zero rows has valid syntax. With `ExecutionIdentity.CurrentUser`, objects the caller cannot see are simply absent from results; if the caller must be told "not found or not allowed", check for the object first and throw `NotFoundException` (do not reveal whether it exists). The Document identifier field is often "Control Number" but is renamed in some workspaces; reference it by GUID or ask the user.

## Step 5: Calling the service

**URL:** `{host}/Relativity.REST/api/{module-route}/v1/{service-route}/{method-route}`, for example `https://tenant.relativity.one/Relativity.REST/api/my-module/v1/sample-service/workspace/1234567`.

**REST requests need:**
- `Authorization: Bearer <token>` (see below), or, from a custom page in the same Relativity instance, the signed-in user's session cookies (the browser sends them automatically).
- `X-CSRF-Header: -` on every call. **Without it a POST returns an empty 404**, which looks exactly like "service not found".
- `Content-Type: application/json` for bodies.

**Body wrapping:** a POST body is one JSON object whose keys are the **parameter names** (matched case-insensitively). `EchoAsync(EchoRequest request)` takes `{"request": {"Message": "hello"}}`, not `{"Message": "hello"}`. `DoAsync(int a, string name)` takes `{"a": 1, "name": "x"}`. Parameters that appear in the route come from the URL and are left out of the body, so a POST can mix both. A method with no body parameters takes an empty body. A body that does not match returns **400** `ServiceSerializationException`: "There was no matching service method for the supplied arguments."

**Responses** are JSON with **PascalCase** property names, matching the C# models (`{"ArtifactID": 1, "Name": "..."}`). Errors return the `FaultCode` status with a body like `{"ErrorType": "...", "Message": "...", ...}`.

**From a custom page's JavaScript**, call with a path relative to the instance; the user's session authenticates it and the call runs as that user:
```js
fetch("/Relativity.REST/api/my-module/v1/sample-service/echo", {
    method: "POST",
    headers: { "X-CSRF-Header": "-", "Content-Type": "application/json" },
    body: JSON.stringify({ request: { Message: "hello" } })
}).then(function (r) {
    if (!r.ok) { return r.text().then(function (t) { throw new Error(r.status + ": " + t); }); }
    return r.json();
}).then(function (result) { console.log(result.Message); });
```

**.NET clients** (the template's TestClient): reference the Interfaces project and `Relativity.Kepler.Client.SDK`, then
```csharp
var factory = new ServiceFactory(new ServiceFactorySettings(
    new Uri("https://tenant.relativity.one/Relativity.REST/api"),
    new BearerTokenCredentials(token)));
using (var service = factory.CreateProxy<ISampleService>()) { ... }
```
The proxy adds `X-CSRF-Header` itself and turns error responses back into the typed exceptions.

**Tokens:** create an OAuth2 client (client credentials flow) in Relativity, then POST `grant_type=client_credentials`, `client_id`, `client_secret`, `scope=UserInfoAccess` as form data to `{host}/Relativity/Identity/connect/token`. The service runs with that OAuth2 client's user, so that user needs access to whatever the service touches. The TestClient reads these from environment variables; never hard-code secrets.

**From inside Relativity** (an agent, event handler, or another Kepler service): `Helper.GetServicesManager().CreateProxy<ISampleService>(ExecutionIdentity.CurrentUser)`.

## Step 6: Deploy (the user does this in Relativity)

1. In **Relativity Applications**, open or create the application and make sure it has been **published to the Application Library** first.
2. In **Resource Files**, upload the Interfaces DLL and the Services DLL from `Services/bin/Release/net462` (plus any genuine third-party DLLs, and `.pdb` files for readable stack traces), linked to that application. Never upload `Relativity.*.dll` or `kCura.*.dll`.
3. Relativity detects the service when the Services DLL is uploaded. It is hosted after a delay of a few minutes; until then calls return 404 / `ServiceNotFoundException`.
4. **Updating:** upload the new DLLs over the existing resource files. Note that a service stays hosted even after its application is deleted.

## Step 7: Verify

Set `RELATIVITY_INSTANCE_URL`, `RELATIVITY_CLIENT_ID`, `RELATIVITY_CLIENT_SECRET` and `RELATIVITY_WORKSPACE_ID`, then run the TestClient. It calls each method and prints OK or the exception type and message. Exit code 0 means every call passed. Also check the service is hosted: a **GET** to `{host}/Relativity.REST/api/{module-route}/v1/{service-route}/GetKeplerStatusAsync` (with the Authorization and `X-CSRF-Header` headers) returns `{"Status":"OK"}` once it is.

## Troubleshooting

- **404 or `ServiceNotFoundException: The service endpoint could not be found`**, in order: (1) REST call missing `X-CSRF-Header`; (2) deployed less than a few minutes ago; (3) DLLs not linked to the application, or the application not in the Application Library first; (4) missing `[ServiceAudience]`, empty `RoutePrefix`, interface not `IDisposable`, method not public, or a GET with a parameter missing from its route; (5) wrong module/service route or version in the URL. This is a server-side registration problem far more often than a client typo.
- **Every service in the application went down together** (all 404, including ones that worked before): one assembly in the application failed Kepler bootstrapping, which stops all of them. Usual causes are a service interface name used twice, two `[ServiceModule]` interfaces in one namespace, or a missing `[ServiceAudience]`. Remove or fix the newest upload; the others come back within a minute or so.
- **500 on every call**: empty `[WebService]`, or an unhandled exception; check the logs.
- **400 "There was no matching service method for the supplied arguments"**: the body was not wrapped by parameter name, or a key is misspelled.
- **405 "does not support HTTP method"**: wrong verb for the route (for example POST to a `[HttpGet]` method).
- **403**: the caller (the OAuth2 client's user) lacks permission for what the service did as `CurrentUser`.
- **429**: Relativity's rate limiter; honour `Retry-After`.

## Reference documentation

- Kepler framework: https://platform.relativity.com/RelativityaiR/Content/Kepler_framework/Kepler_framework.htm
- Kepler service structure and components: https://platform.relativity.com/RelativityaiR/Content/Kepler_framework/Kepler_service_structure_and_components.htm
- Create a RESTful API (tutorial): https://platform.relativity.com/RelativityaiR/Content/Get_started_RelativityaiR/Create_a_RESTful_API.htm
- Deploy Kepler services through ADS: https://platform.relativity.com/RelativityaiR/Content/Kepler_framework/Deploy_Kepler_services_through_ADS.htm
- HTTP headers, verbs, and request bodies: https://platform.relativity.com/RelativityaiR/Content/Kepler_framework/HTTP_headers__verbs__and_related_information.htm
- Dependency injection: https://platform.relativity.com/RelativityaiR/Content/Kepler_framework/Dependency_injection.htm
- Proxies and authentication: https://platform.relativity.com/RelativityaiR/Content/Kepler_framework/Proxies_and_authentication.htm
- Troubleshooting Kepler services: https://platform.relativity.com/RelativityaiR/Content/Kepler_framework/Troubleshooting_Kepler_services.htm

## Output style

- Generate the files, build both projects, and fix any error before showing the result. Show the build output.
- Show the resulting REST URL for each method and an example request body for each POST.
- Give the exact upload list, then walk through deployment and the TestClient run one step at a time.

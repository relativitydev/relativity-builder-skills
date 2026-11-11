# kepler-service-builder

Builds a custom Kepler service for Relativity aiR: your own REST and .NET API, hosted inside
Relativity, running as the caller and able to use Relativity's APIs on the server.

> **Relativity aiR only.** Not for Relativity Server.

## What you get

- `templates/Interfaces/`: the contract (module, service interface, models, a custom exception)
- `templates/Services/`: the implementation, with a GET that queries Relativity as the calling
  user and a POST that shows how request bodies are wrapped
- `templates/TestClient/`: a console app that gets an OAuth2 token and calls every method
  through the Kepler .NET proxy

All three target .NET Framework 4.6.2 and build from nuget.org alone.

**It does not deploy anything for you.** The skill builds the DLLs and walks you through
uploading them and running the test client against your instance.

## What it saves you from

- `(555) Service Audience is not specified` at deployment
- 404s from a missing `X-CSRF-Header`, a GET parameter outside the route, or a non-public method
- POST bodies that aren't wrapped by parameter name
- Endpoints that quietly run with service-account rights instead of the caller's
- Mistaking "not hosted yet" for a code problem

## Using it

```
/relativity-builder-skills:kepler-service-builder
```

See the [repository README](../../README.md) for installation, and
[`SKILL.md`](SKILL.md) for the instructions themselves.

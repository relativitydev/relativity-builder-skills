# Relativity aiR API Coverage Map

> **Purpose:** Cross-cutting rules for the SQL to Relativity aiR API migration that apply
> regardless of which specific API is in play: authentication, headers, URL patterns, and
> the full Object Manager query condition syntax. This file does not map individual APIs to
> endpoints; that comes from the documentation catalogs fetched at run time (see `SKILL.md` Step 3b).
> **Platform API reference:** https://platform.relativity.com/RelativityaiR/Content/Explore/Platform-APIs.htm#RelativityOneAPIs

---

## Authentication

All API calls require a Bearer Token (OAuth2 Client Credentials flow).

**Step 1: Get a token:**
```
POST https://{your-instance}.relativity.one/Relativity/Identity/connect/token

Body (form-encoded):
  grant_type=client_credentials
  client_id={client_id}
  client_secret={client_secret}
  scope=SystemUserInfo
```

**Step 2: Use the token in every request:**
```
Authorization: Bearer {access_token}
```

OAuth2 client setup: Home → Authentication → OAuth2 Client Manager. Required fields: Name, Enabled, Flow Type = Client Credentials, Context User, Access Token Lifetime.

Full reference: https://platform.relativity.com/RelativityaiR/Content/REST_API/REST_API_authentication.htm#Bearertokenauthentication

---

## Required Headers

Every REST call must include these headers.

| Header | Value | Notes |
|---|---|---|
| `Content-Type` | `application/json` | Required for POST bodies |
| `X-CSRF-Header` | Any non-empty string (e.g. `-`) | Cannot be omitted or empty; the request will fail |
| `Authorization` | `Bearer {access_token}` | From OAuth2 token endpoint above |
| `X-Kepler-Referrer` | The calling application's GUID | Object Manager identifies rate-limit consumers by this header. Without it, requests join a shared unknown bucket and are throttled unpredictably. Handle HTTP 429 by waiting for the reset the response gives. |

Full reference: https://platform.relativity.com/RelativityaiR/Content/REST_API/HTTP_headers.htm

---

## Base URL Patterns

Two URL patterns are in active use. Both are valid:

```
# Legacy / ROMAWR pattern
https://{your-instance}.relativity.one/Relativity.REST/api/Relativity.Objects/workspace/{workspaceID}/objects/{operation}

# v1 pattern (used in most sample files)
https://{your-instance}.relativity.one/Relativity.Rest/api/Relativity.ObjectManager/v1/workspace/{workspaceID}/object/{operation}
```

Use `workspaceID = -1` for instance-level (admin/EDDS scope) queries.
Use `workspaceID = {artifactID}` for workspace-scoped queries.

---

## Paging, and Mass Update's three forms

> **Reference:** https://platform.relativity.com/RelativityaiR/Content/BD_Object_Manager/Object_Manager_service.htm

Queries page with `start` (one-based) and `length`. There is **no documented 1000-row cap**; a
`length` of 0 or less substitutes the `PDVDefaultQueryCacheSize` instance setting, 10,000 by
default, and a very large `length` is documented as a performance risk. For result sets large
enough that paging is the bottleneck, use the Export API sequence on the same service:
`initializeexport`, then `retrievenextresultsblockfromexport`.

Mass update has **no `massupdate` path**. It is `POST .../object/update`, the same endpoint as the
single-object update, and the body selects the form:

| Body | Behavior | Converts |
|---|---|---|
| `massRequestByCriteria` | one value on everything matching a query condition | `UPDATE … WHERE`; documented as the fastest option for many objects |
| `massRequestByObjectIdentifiers` | one value on a list of Artifact IDs | a known document list |
| `massRequestPerObjects` | a different value per object: `Fields`, then `ObjectValues` carrying `object` and `values`, in the same order as the fields | a SET expression that differs per row |

The identifier field cannot be written by any mass update form. Documentation advises Artifact IDs
over GUIDs here, and warns that mass update can fail where several artifacts share a text
identifier value.

---

## Query Condition Syntax

> **Full reference:** https://platform.relativity.com/RelativityaiR/Content/REST_API/Resource_Tasks/Querying_for_resources.htm#Syntaxforqueryconditions

Conditions are passed as strings in the `"condition"` field of Object Manager queries and as string-based conditions in Audit `UI/query` calls.

### Field Selection Keywords

| Value | Returns |
|---|---|
| `["*"]` | All fields |
| `["*NOFIELDS*"]` | Artifact ID, GUIDs, ArtifactTypeID, ArtifactTypeName only |
| `["*SELECTEDFIELDS*"]` | Fields from the associated Saved Search or View only |

### Operators by Data Type

| Data type | Supported operators | Example |
|---|---|---|
| **Whole Number** | `==` `<>` `>` `>=` `<` `<=` `IN` | `'Foo' == 42` / `'Foo' IN [42, 53]` |
| **Decimal** | `==` `<>` `>` `>=` `<` `<=` `IN` | `'Foo' == 3.14` / `'Foo' IN [3.14, 7.50]` |
| **Boolean** | `==` `<>` | `'Foo' == true` / `'Foo' <> false` |
| **Text** | `==` `<>` `>` `>=` `<` `<=` `STARTSWITH` `ENDSWITH` `LIKE` `CONTAINS` `IN` | `'Foo' LIKE 'Bar%'` / `'Foo' IN ['Foo','Bar']` |
| **Date** | `==` `<>` `>` `>=` `<` `<=` `MonthOf` | `'Foo' >= 2025-01-01T00:00:00.00Z` / `'Foo' in MonthOf May` |
| **Choice** | `==` `<>` `IN` | `'Foo' == CHOICE <ArtifactID>` / `'Foo' IN CHOICE [id1, id2]` |
| **Multichoice** | `CONTAINS` `INTERSECTS` | `'Foo' CONTAINS MULTICHOICE [id1, id2]` |
| **Object** | `==` `<>` `IN` | `'Foo' == OBJECT <ArtifactID>` / `'Foo' IN OBJECT [id1, id2]` |
| **Multiobject** | `CONTAINS` `INTERSECTS` | `'Foo' INTERSECTS MULTIOBJECT [id1, id2]` |
| **User** | `==` `<>` `LIKE` | `'Foo' == USER 'Ralph'` / `'Foo' LIKE USER '%Ralph'` |
| **Saved Search** | `IN SAVEDSEARCH` | `'ArtifactID' IN SAVEDSEARCH <ArtifactID>` |
| **View** | `IN VIEW` | `'ArtifactID' IN VIEW <ArtifactID>` |
| **Any type** | `ISSET` `NOT ISSET` | `'Foo' ISSET` / `NOT 'Foo' ISSET` |

> **CONTAINS vs INTERSECTS (multiobject/multichoice):** `CONTAINS` = field must have ALL listed values. `INTERSECTS` = field must have AT LEAST ONE of the listed values.

### Logical Operators

```
'Foo' == 1 AND 'Bar' == 2
'Foo' == 1 OR 'Bar' == 2
NOT ('Foo' == 1)
```

Parentheses are optional but supported for grouping.

### Wildcards (LIKE / STARTSWITH / ENDSWITH)

| Character | Matches |
|---|---|
| `%` | One or more characters |
| `_` | Exactly one character |

To use `%` or `_` literally, escape inside square brackets: `'Foo' LIKE 'yearly [%] increase'`

### Special Characters: Escape in String Values

| Escape sequence | Character |
|---|---|
| `\'` | Single quote |
| `\"` | Double quote |
| `\\` | Backslash |
| `\n` | Newline |
| `\r` | Carriage return |
| `\t` | Tab |
| `\uxxxx` | Unicode |

### Constraints & Known Limitations

- `BETWEEN` is not supported for dates; use `>=` and `<=` together instead
- Saved Search and View conditions cannot be combined with other conditions in a compound query
- Avoid underscores in condition strings
- Data Grid fields support `IS SET` / `IS NOT SET`; `IS LIKE` is not supported on Data Grid fields

---

## No API Equivalent (Blocked)

| Script | Why blocked |
|---|---|
| Globally Administer Keyboard Shortcuts | No public API for cross-workspace keyboard shortcut writes |
| Report Sample-Based Statistics | Feature deprecated, no replacement |
| Get Database Size | SQL Server system catalog (`sp_helpdb`, `sys.databases`); no Relativity API surface |


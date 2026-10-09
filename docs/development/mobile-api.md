# Mobile API Contract

This document describes the Food and Observation API available to the Triggered
mobile application. Use the API base URL supplied for the current environment
and send and receive JSON with `Content-Type: application/json`.

JSON property names are camelCase. IDs are GUID strings. `consumedAt` is an
ISO 8601 date-time string with an offset, for example
`"2026-10-09T11:30:00+00:00"`. The API preserves the submitted offset when
returning the timestamp.

## Endpoint inventory

| Method | Path | Purpose | Success | Other documented status |
| --- | --- | --- | --- | --- |
| `POST` | `/api/foods` | Create a food | `201` | `400` |
| `GET` | `/api/foods` | List foods | `200` | — |
| `GET` | `/api/foods/{id}` | Get one food | `200` | `400`, `404` |
| `GET` | `/api/foods/{foodId}/observations` | List observations for a food | `200` | `400`, `404` |
| `GET` | `/api/foods/{foodId}/trigger-pattern` | Get the calculated pattern for a food | `200` | `400`, `404` |
| `POST` | `/api/observations` | Create an observation | `201` | `400`, `404` |
| `GET` | `/api/observations` | List observations | `200` | — |
| `PUT` | `/api/observations/{observationId}` | Replace an observation's time and reaction | `200` | `400`, `404` |
| `DELETE` | `/api/observations/{observationId}` | Delete an observation | `204` | `404` |

The food collection routes use `{id}` or `{foodId}` GUID parameters without a
route constraint: an invalid GUID returns `400`, while an unknown valid GUID
returns `404`. Observation update and delete routes have a GUID route constraint,
so an invalid GUID does not match the route and returns `404`.

## Foods

### Create a food

`POST /api/foods`

Request:

```json
{
  "name": "Ketchup"
}
```

`name` is required and must not be empty or whitespace. The domain trims
surrounding whitespace.

`201 Created` response:

```json
{
  "id": "65fbe76e-aea0-4c42-a9ab-6a27e507d215",
  "name": "Ketchup"
}
```

### List foods

`GET /api/foods`

`200 OK` response:

```json
[
  {
    "id": "65fbe76e-aea0-4c42-a9ab-6a27e507d215",
    "name": "Ketchup"
  }
]
```

An empty collection is returned as `[]`.

### Get a food

`GET /api/foods/{id}`

`200 OK` returns the same food object as the list response. A valid but unknown
ID returns an empty `404 Not Found`.

### List observations for a food

`GET /api/foods/{foodId}/observations`

`200 OK` returns an array of observation objects (see the Observation response
below). A valid but unknown food ID returns an empty `404 Not Found`; a food
with no observations returns `[]`.

### Get a food's trigger pattern

`GET /api/foods/{foodId}/trigger-pattern`

`200 OK` response:

```json
{
  "foodId": "65fbe76e-aea0-4c42-a9ab-6a27e507d215",
  "pattern": "Insufficient Data"
}
```

`pattern` is one of `Insufficient Data`, `Low`, `Possible Trigger`, `Moderate`,
`Likely Trigger`, or `Strong Trigger Pattern`. These labels describe patterns
in the user's reported history only; they are not medical diagnoses or
assessments of medical risk. A valid but unknown food ID returns an empty
`404 Not Found`.

## Observations

Observation response object:

```json
{
  "id": "8379c0a1-810e-44a8-8fc6-6f4e2b7770c1",
  "foodId": "65fbe76e-aea0-4c42-a9ab-6a27e507d215",
  "consumedAt": "2026-10-09T11:30:00+00:00",
  "reaction": "Heartburn"
}
```

### Create an observation

`POST /api/observations`

Request:

```json
{
  "foodId": "65fbe76e-aea0-4c42-a9ab-6a27e507d215",
  "consumedAt": "2026-10-09T11:30:00+00:00",
  "reaction": "Heartburn"
}
```

All three fields are required. `foodId` must be a non-empty GUID,
`consumedAt` must be a non-default date-time, and `reaction` must not be empty
or whitespace.

`201 Created` returns the observation object above. If `foodId` identifies no
food, the API returns an empty `404 Not Found`.

### List observations

`GET /api/observations`

`200 OK` returns an array of observation objects. An empty collection is
returned as `[]`.

### Update an observation

`PUT /api/observations/{observationId}`

The request replaces the observation's consumption time and reaction; it does
not change its food.

```json
{
  "consumedAt": "2026-10-09T12:30:00+00:00",
  "reaction": "No reaction"
}
```

Both fields are required and follow the same validation rules as when creating
an observation. `200 OK` returns the updated observation object. A valid but
unknown observation ID returns an empty `404 Not Found`.

### Delete an observation

`DELETE /api/observations/{observationId}`

`204 No Content` has no response body. An unknown observation ID returns an
empty `404 Not Found`.

## Errors and status codes

Request validation failures return `400 Bad Request` with the existing
`ValidationProblemDetails` response (`application/problem+json`). Field errors
are grouped by request property in the `errors` object, for example:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Reaction": ["Reaction is required."]
  }
}
```

Malformed JSON or values that cannot be bound to a request type can also return
`400 Bad Request`; the detail is produced by ASP.NET Core's request binding.
Not-found responses currently have no body. Handle status codes per endpoint
as listed above; a `204` response has no JSON to parse.

The generated OpenAPI document is available at `/swagger/v1/swagger.json` in
Development.

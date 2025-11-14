# API Specification - Hull Sizing Service

## Base URL
- **Development:** `http://localhost:5002/api/v1/hull-sizing`
- **Production:** `https://api.navarch.studio/api/v1/hull-sizing`

## Authentication
All endpoints require JWT token in `Authorization` header:
```
Authorization: Bearer <jwt_token>
```

## Common Headers

### Request Headers
```
Authorization: Bearer <jwt_token>
Content-Type: application/json
X-Idempotency-Key: <key> (for POST push-to-hydrostatics)
```

### Response Headers
```
Content-Type: application/json
X-Correlation-Id: <correlation_id>
X-Compute-Time-Ms: <ms> (for sizing runs)
```

---

## Endpoints

### 1. Mission Cases

#### POST /mission-cases
Create a new mission case.

**Request:**
```json
{
  "name": "6000 TEU Feeder",
  "missionCategory": "Commercial",
  "missionType": "container",
  "cargoBasis": "teu",
  "teuCount": 6000,
  "serviceSpeedKn": 24.0,
  "seaMarginPct": 0.15,
  "serviceMarginPct": 0.15,
  "envHsM": 3.5,
  "envTzS": 8.0,
  "capDraftM": 12.0,
  "notes": "Feeder vessel for Asia-Europe route"
}
```

**Response:** `201 Created`
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "userId": "user-guid",
  "tenantId": "tenant-123",
  "name": "6000 TEU Feeder",
  "missionCategory": "Commercial",
  "missionType": "container",
  "cargoBasis": "teu",
  "teuCount": 6000,
  "serviceSpeedKn": 24.0,
  "seaMarginPct": 0.15,
  "serviceMarginPct": 0.15,
  "envHsM": 3.5,
  "envTzS": 8.0,
  "capDraftM": 12.0,
  "notes": "Feeder vessel for Asia-Europe route",
  "createdAt": "2024-11-02T10:30:00Z",
  "updatedAt": "2024-11-02T10:30:00Z"
}
```

---

#### GET /mission-cases
List user's mission cases (paginated).

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)
- `missionType` (string, optional filter)

**Response:** `200 OK`
```json
{
  "data": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "6000 TEU Feeder",
      "missionType": "container",
      "serviceSpeedKn": 24.0,
      "createdAt": "2024-11-02T10:30:00Z"
    }
  ],
  "total": 15,
  "page": 1,
  "pageSize": 20
}
```

---

#### GET /mission-cases/{id}
Get mission case details.

**Response:** `200 OK`
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "6000 TEU Feeder",
  "missionCategory": "Commercial",
  "missionType": "container",
  "cargoBasis": "teu",
  "teuCount": 6000,
  "serviceSpeedKn": 24.0,
  "seaMarginPct": 0.15,
  "serviceMarginPct": 0.15,
  "envHsM": 3.5,
  "envTzS": 8.0,
  "capDraftM": 12.0,
  "notes": "Feeder vessel for Asia-Europe route",
  "createdAt": "2024-11-02T10:30:00Z",
  "updatedAt": "2024-11-02T10:30:00Z"
}
```

---

#### PUT /mission-cases/{id}
Update mission case.

**Request:** Same as POST (all fields)

**Response:** `200 OK` (same as GET)

---

#### DELETE /mission-cases/{id}
Soft delete mission case.

**Response:** `204 No Content`

---

### 2. Sizing Runs

#### POST /mission-cases/{missionCaseId}/runs
Execute sizing computation.

**Request:**
```json
{
  "mode": "first_principles",
  "locks": {
    "keepFn": true,
    "keepLOverB": false,
    "keepBOverT": false,
    "keepDOverT": false,
    "keepCb": false
  },
  "options": {
    "familyHint": null,
    "maxCandidates": 5
  }
}
```

**Response:** `200 OK`
```json
{
  "run": {
    "id": "run-guid",
    "missionCaseId": "mission-guid",
    "mode": "first_principles",
    "status": "completed",
    "computeTimeMs": 1850,
    "createdAt": "2024-11-02T10:35:00Z"
  },
  "candidates": [
    {
      "id": "cand-1-guid",
      "sizingRunId": "run-guid",
      "hullFamily": "container",
      "rank": 1,
      "lppM": 232.5,
      "lwlM": 238.7,
      "loaM": 245.9,
      "bM": 32.5,
      "tM": 11.2,
      "dM": 15.4,
      "cb": 0.651,
      "cp": 0.670,
      "cwp": 0.890,
      "cm": 0.972,
      "displacementT": 58200,
      "fn": 0.260,
      "lwlOverLambda": 2.384,
      "ehpKw": 21500,
      "shpKw": 41200,
      "gmEstM": 1.85,
      "kbM": 5.9,
      "lcbPctLpp": -2.5,
      "score": 0.9245,
      "scoresJson": {
        "deltaBalance": 0.98,
        "installedPower": 0.92,
        "constraintsOk": 1.0,
        "stabilityScreen": 0.95,
        "teuFit": 0.88
      },
      "flagsJson": {},
      "geometryJson": {
        "stations": [
          {"x": 0, "waterlines": [{"z": 0, "y": 0}, {"z": 2.24, "y": 14.2}, ...]},
          ...
        ]
      },
      "createdAt": "2024-11-02T10:35:01Z"
    },
    {
      "id": "cand-2-guid",
      "hullFamily": "container",
      "rank": 2,
      "score": 0.9102,
      ...
    }
  ]
}
```

**Headers:**
```
X-Compute-Time-Ms: 1850
```

---

#### GET /runs/{runId}
Get run details.

**Response:** `200 OK` (same as POST /runs response)

---

#### GET /runs/{runId}/candidates
List candidates for a run (filtered/sorted).

**Query Parameters:**
- `sortBy` (score | ehp | displacement | rank, default: rank)
- `sortOrder` (asc | desc, default: asc for rank)
- `familyFilter` (string, optional)

**Response:** `200 OK`
```json
{
  "candidates": [
    {
      "id": "cand-1-guid",
      "rank": 1,
      "hullFamily": "container",
      "score": 0.9245,
      "lppM": 232.5,
      "bM": 32.5,
      "tM": 11.2,
      "displacementT": 58200,
      "ehpKw": 21500
    },
    ...
  ],
  "total": 5
}
```

---

### 3. Candidates

#### GET /candidates/{id}
Get candidate full details.

**Response:** `200 OK` (full candidate object as shown in POST /runs)

---

#### POST /candidates/{id}/recompute
Recompute candidate with adjusted parameters.

**Request:**
```json
{
  "adjustments": {
    "serviceSpeedKn": 25.0,
    "locks": {
      "keepFn": true,
      "keepLOverB": true,
      "keepBOverT": false,
      "keepDOverT": false,
      "keepCb": false
    }
  }
}
```

**Response:** `200 OK` (updated candidate object)

**Headers:**
```
X-Compute-Time-Ms: 245
```

---

#### POST /candidates/{id}/push-to-hydrostatics
Create vessel in DataService from candidate.

**Required Header:**
```
X-Idempotency-Key: push-hydro-{candidateId}-{timestamp}
```

**Request:**
```json
{
  "vesselName": "Container 6000 TEU - Candidate 1"
}
```

**Response:** `201 Created`
```json
{
  "vesselId": "vessel-guid",
  "vesselName": "Container 6000 TEU - Candidate 1",
  "status": "created",
  "message": "Vessel created successfully in Hydrostatics module"
}
```

**Headers:**
```
Location: /api/v1/hydrostatics/vessels/{vesselId}
```

**Idempotency Behavior:**
- If same idempotency key sent twice → returns existing vessel ID (no duplicate creation)
- Idempotency keys expire after 24 hours

---

### 4. Reference Data

#### GET /reference/hull-families
List all hull family presets.

**Response:** `200 OK`
```json
{
  "families": [
    {
      "id": "family-guid",
      "family": "container",
      "displayName": "Container Ship",
      "lOverBMin": 6.5,
      "lOverBMax": 8.5,
      "bOverTMin": 2.3,
      "bOverTMax": 3.0,
      "dOverTMin": 1.3,
      "dOverTMax": 1.55,
      "cbMin": 0.60,
      "cbMax": 0.70,
      "cpMin": 0.60,
      "cpMax": 0.70,
      "cwpMin": 0.85,
      "cwpMax": 0.95,
      "fnMin": 0.23,
      "fnMax": 0.30,
      "generatorType": "wigley",
      "notes": "Fine/full mix; high Cwp"
    },
    ...
  ]
}
```

---

#### GET /reference/iso-containers
List ISO container types.

**Response:** `200 OK`
```json
{
  "containers": [
    {
      "type": "20GP",
      "lengthMm": 6058,
      "widthMm": 2438,
      "heightMm": 2591,
      "maxGrossKg": 30480
    },
    {
      "type": "40GP",
      "lengthMm": 12192,
      "widthMm": 2438,
      "heightMm": 2591,
      "maxGrossKg": 30480
    },
    {
      "type": "40HC",
      "lengthMm": 12192,
      "widthMm": 2438,
      "heightMm": 2896,
      "maxGrossKg": 30480
    },
    {
      "type": "45HC",
      "lengthMm": 13716,
      "widthMm": 2438,
      "heightMm": 2896,
      "maxGrossKg": 32500
    }
  ]
}
```

---

#### GET /reference/kpi-weights
Get scoring weights (system default or user-specific).

**Query Parameters:**
- `userId` (uuid, optional - if omitted, returns system defaults)

**Response:** `200 OK`
```json
{
  "weights": [
    {"metric": "delta_balance", "weight": 0.35},
    {"metric": "installed_power", "weight": 0.25},
    {"metric": "constraints_ok", "weight": 0.20},
    {"metric": "stability_screen", "weight": 0.10},
    {"metric": "teu_or_volume_fit", "weight": 0.10}
  ]
}
```

---

## Error Responses

### Standard Error Format (ProblemDetails)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
  "errors": {
    "serviceSpeedKn": ["Speed must be greater than 0"],
    "cargoBasis": ["Cargo basis must be 'volume', 'weight', or 'teu'"]
  }
}
```

### Common Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | OK | Successful GET, PUT, POST (non-creation) |
| 201 | Created | POST /mission-cases, POST /push-to-hydrostatics |
| 204 | No Content | DELETE /mission-cases/{id} |
| 400 | Bad Request | Validation errors, invalid JSON |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | Tenant ID missing, insufficient permissions |
| 404 | Not Found | Resource doesn't exist or deleted |
| 422 | Unprocessable Entity | Solver failed to converge |
| 429 | Too Many Requests | Rate limit exceeded (100 req/min) |
| 500 | Internal Server Error | Unexpected server error |
| 503 | Service Unavailable | DataService circuit breaker open |

---

## Rate Limiting

**Global Limit:** 100 requests per minute per IP

**Response (429):**
```json
{
  "error": "Too many requests",
  "message": "Rate limit exceeded. Please try again later.",
  "retryAfter": 45
}
```

**Headers:**
```
Retry-After: 45
```

---

## OpenAPI / Swagger

**Swagger UI:** `http://localhost:5004/swagger`

**OpenAPI JSON:** `http://localhost:5004/swagger/v1/swagger.json`

**Generate TypeScript client:**
```bash
npx openapi-typescript http://localhost:5004/swagger/v1/swagger.json --output frontend/src/types/hull-sizing-api.generated.ts
```

---

## Example Workflows

### Workflow 1: Create Mission → Run Sizing → Select Candidate → Push to Hydrostatics

```bash
# 1. Create mission case
curl -X POST http://localhost:5002/api/v1/hull-sizing/mission-cases \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "6000 TEU Feeder",
    "missionType": "container",
    "cargoBasis": "teu",
    "teuCount": 6000,
    "serviceSpeedKn": 24.0,
    "envHsM": 3.5,
    "envTzS": 8.0,
    "capDraftM": 12.0
  }'

# Response: {"id": "mission-123", ...}

# 2. Run sizing
curl -X POST http://localhost:5002/api/v1/hull-sizing/mission-cases/mission-123/runs \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "mode": "first_principles",
    "locks": {"keepFn": true},
    "options": {}
  }'

# Response: {"run": {...}, "candidates": [{id: "cand-1", rank: 1, score: 0.92}, ...]}

# 3. Get candidate details
curl -X GET http://localhost:5002/api/v1/hull-sizing/candidates/cand-1 \
  -H "Authorization: Bearer $JWT"

# Response: {full candidate with geometry_json}

# 4. Push to Hydrostatics
curl -X POST http://localhost:5002/api/v1/hull-sizing/candidates/cand-1/push-to-hydrostatics \
  -H "Authorization: Bearer $JWT" \
  -H "X-Idempotency-Key: push-cand-1-20241102" \
  -H "Content-Type: application/json" \
  -d '{"vesselName": "Container 6000 TEU - Best Candidate"}'

# Response: {"vesselId": "vessel-456", "status": "created"}
# Location header: /api/v1/hydrostatics/vessels/vessel-456
```

---

### Workflow 2: Recompute with Slider Adjustment

```bash
# User drags speed slider from 24 kn to 25 kn

curl -X POST http://localhost:5002/api/v1/hull-sizing/candidates/cand-1/recompute \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "adjustments": {
      "serviceSpeedKn": 25.0,
      "locks": {"keepFn": false, "keepLOverB": true}
    }
  }'

# Response: {updated candidate with new dimensions}
# X-Compute-Time-Ms: 245
```

---

## Versioning Strategy

**Current:** v1 (all routes `/api/v1/hull-sizing/*`)

**Future:**
- v2 will introduce breaking changes (new required fields, response format changes)
- v1 will remain supported for 12 months after v2 release
- Deprecation warnings in response headers: `X-API-Deprecated: true; sunset=2025-12-31`

---

## Next: Read `07-TESTING-STRATEGY.md` for QA plan

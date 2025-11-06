# Data-Driven Mode - API Reference

**Last Updated:** November 6, 2025  
**Version:** v1.0  
**Base URL:** `https://api.navarch.studio` (production) or `http://localhost:5003` (dev)

---

## Endpoints

### 1. Search Similar Vessels (KNN)

**POST** `/api/v1/catalog/vessels/search-similar`

Search for K most similar vessels based on mission requirements.

#### Request

```typescript
{
  "vesselType": "Container",
  "targetDisplacement": 50000.0,
  "serviceSpeed": 12.34,  // m/s
  "maxBeam": 35.0,  // Optional constraint
  "maxDraft": 12.0,  // Optional constraint
  "k": 5
}
```

#### Response (200 OK)

```typescript
{
  "similarVessels": [
    {
      "vesselId": "abc-123-def-456",
      "vesselName": "KCS",
      "vesselType": "Container",
      "lppM": 230.0,
      "beamM": 32.2,
      "draftM": 10.8,
      "depthM": 19.0,
      "displacementT": 52030.0,
      "cb": 0.6505,
      "cp": 0.66,
      "cm": 0.9849,
      "cw": 0.83,
      "serviceSpeedMs": 12.34,
      "similarityScore": 0.873  // 0-1, higher is better
    },
    // ... 4 more vessels
  ],
  "totalCatalogSize": 600,
  "catalogSource": "RealWorld"
}
```

#### Validation Rules

- `vesselType`: Required, non-empty
- `targetDisplacement`: Required, must be > 0
- `serviceSpeed`: Required, must be > 0
- `k`: Must be between 1 and 20 (default: 5)
- `maxBeam`, `maxDraft`: Optional, used for distance weighting

#### Error Responses

**400 Bad Request**
```json
{
  "error": "VesselType is required"
}
```

**500 Internal Server Error**
```json
{
  "error": "Internal server error during KNN search"
}
```

**499 Client Closed Request** (user cancelled)

---

### 2. Create Sizing Run (with mode selection)

**POST** `/api/v1/hull-sizing/runs`

Run hull sizing solver with selected mode.

#### Request

```typescript
{
  "missionCaseId": "mission-uuid-here",
  "mode": "data_driven_real",  // NEW: "first_principles" | "data_driven_real" | "data_driven_ml"
  "locks": {  // Optional
    "keepFn": false,
    "keepLOverB": false,
    "keepBOverT": false,
    "keepDOverT": false,
    "keepCbBand": false
  },
  "options": {  // Optional
    "maxCandidates": 5,
    "minFn": 0.15,
    "maxFn": 0.35,
    "familyHints": ["container", "tanker"]
  }
}
```

#### Response (201 Created)

```typescript
{
  "id": "run-uuid-here",
  "missionCaseId": "mission-uuid",
  "mode": "data_driven_real",
  "runStatus": "completed",
  "computeTimeMs": 847,
  "candidateCount": 5,
  "createdAt": "2025-11-06T10:30:00Z"
}
```

**Location Header:** `/api/v1/hull-sizing/runs/{id}`

---

### 3. Get Candidates (with provenance)

**GET** `/api/v1/hull-sizing/runs/{id}/candidates`

Retrieve candidate designs for a sizing run.

#### Response (200 OK)

```typescript
[
  {
    "id": "candidate-uuid-1",
    "sizingRunId": "run-uuid",
    "hullFamily": "container",
    "lppM": 235.6,
    "beamM": 33.1,
    "draftM": 11.2,
    // ... other dimensions
    "score": 0.92,
    "rank": 1,
    
    // NEW: Provenance fields (only for data-driven)
    "referenceVesselId": "abc-123-def",
    "referenceVesselName": "KCS",
    "similarityScore": 0.87,
    "solverMode": "DataDrivenRealWorld"
  },
  // ... 4 more candidates
]
```

**Provenance Fields (null for First-Principles):**
- `referenceVesselId`: UUID from catalog (for future drill-down)
- `referenceVesselName`: Human-readable name ("KCS", "KVLCC2", etc.)
- `similarityScore`: 0-1, how similar to reference before scaling
- `solverMode`: "FirstPrinciples" | "DataDrivenRealWorld" | "DataDrivenML" | "FirstPrinciples_Fallback"

---

### 4. Clear Catalog Cache

**POST** `/api/v1/catalog/vessels/clear-cache`

Clear the in-memory catalog cache (for testing or after updates).

#### Response (200 OK)

```json
{
  "message": "Cache cleared successfully"
}
```

**Use Cases:**
- Testing: Force reload from database
- After adding user vessels: Refresh cache
- Memory pressure: Free up memory

---

## Data Models

### KnnSearchRequest

```typescript
interface KnnSearchRequest {
  vesselType: string;           // "Container", "Tanker", etc.
  targetDisplacement: number;   // tonnes
  serviceSpeed: number;         // m/s
  maxBeam?: number;             // m (optional)
  maxDraft?: number;            // m (optional)
  k: number;                    // 1-20, default 5
}
```

### SimilarVesselDto

```typescript
interface SimilarVesselDto {
  vesselId: string;             // UUID
  vesselName: string;           // "KCS", "Emma Maersk", etc.
  vesselType: string;
  lppM: number;
  beamM: number;
  draftM: number;
  depthM?: number;
  displacementT: number;
  cb: number;
  cp?: number;
  cm?: number;
  cw?: number;
  serviceSpeedMs?: number;
  similarityScore: number;      // 0-1
}
```

### CandidateDesignDto (Enhanced)

```typescript
interface CandidateDesignDto {
  // ... existing fields ...
  
  // NEW: Provenance
  referenceVesselId?: string;
  referenceVesselName?: string;
  similarityScore?: number;
  solverMode?: string;
}
```

---

## Authentication & Authorization

All endpoints require JWT authentication:

```http
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Required Claims:**
- `sub`: User ID (UUID)
- `tenant_id`: Tenant ID (for multi-tenancy)

---

## Rate Limiting

| Endpoint | Limit | Window |
|----------|-------|--------|
| `/catalog/vessels/search-similar` | 100 requests | Per minute |
| `/hull-sizing/runs` (POST) | 10 requests | Per minute |
| `/hull-sizing/runs/{id}/candidates` (GET) | 60 requests | Per minute |

---

## CORS

**Allowed Origins:**
- `http://localhost:3000` (development)
- `https://app.navarch.studio` (production)

---

## Versioning

API version is in URL path: `/api/v1/...`

**Current Version:** v1  
**Breaking Changes:** Require new version (v2)

---

## Error Codes

| Code | Meaning | Action |
|------|---------|--------|
| 400 | Bad Request | Check request validation |
| 401 | Unauthorized | Provide valid JWT token |
| 404 | Not Found | Mission case or run doesn't exist |
| 499 | Client Closed Request | User cancelled, retry if needed |
| 500 | Internal Server Error | Check logs, contact support |

---

## Examples

### Example 1: Find Similar Containers

```bash
curl -X POST http://localhost:5003/api/v1/catalog/vessels/search-similar \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "vesselType": "Container",
    "targetDisplacement": 55000,
    "serviceSpeed": 12.5,
    "maxBeam": 35.0,
    "k": 5
  }'
```

**Response:**
```json
{
  "similarVessels": [
    {
      "vesselId": "...",
      "vesselName": "KCS",
      "similarityScore": 0.91,
      ...
    },
    ...
  ],
  "totalCatalogSize": 600
}
```

---

### Example 2: Run Data-Driven Solver

```bash
curl -X POST http://localhost:5004/api/v1/hull-sizing/runs \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "missionCaseId": "abc-123-...",
    "mode": "data_driven_real",
    "options": {
      "maxCandidates": 5
    }
  }'
```

**Response:**
```json
{
  "id": "run-uuid-...",
  "mode": "data_driven_real",
  "runStatus": "completed",
  "computeTimeMs": 847,
  "candidateCount": 5
}
```

---

### Example 3: Get Candidates with Provenance

```bash
curl -X GET http://localhost:5004/api/v1/hull-sizing/runs/{runId}/candidates \
  -H "Authorization: Bearer $JWT_TOKEN"
```

**Response:**
```json
[
  {
    "id": "candidate-1",
    "lppM": 235.6,
    "beamM": 33.1,
    "score": 0.92,
    "referenceVesselName": "KCS",
    "similarityScore": 0.87,
    "solverMode": "DataDrivenRealWorld"
  },
  ...
]
```

---

## WebSocket Support (Future)

For real-time progress updates:

```typescript
// Phase 2 enhancement
const ws = new WebSocket('wss://api.navarch.studio/api/v1/hull-sizing/runs/stream');

ws.onmessage = (event) => {
  const update = JSON.parse(event.data);
  // { step: "knn_search", progress: 25, message: "Searching catalog..." }
  // { step: "scaling", progress: 50, message: "Scaling 3/5 vessels..." }
  // { step: "refinement", progress: 75, message: "Refining candidate 2/3..." }
  // { step: "complete", progress: 100, result: {...} }
};
```

---

**API Version:** v1.0  
**Status:** Production Ready  
**Documentation:** Complete


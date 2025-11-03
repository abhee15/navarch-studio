# HullSizingService

Mission→Hull Sizing microservice for NavArch Studio.

## Overview
Transforms mission requirements (cargo, speed, environment) into preliminary hull designs using first-principles physics and Holtrop-Mennen resistance calculations.

## Features
- First-principles solver (displacement closure, Froude targeting)
- Multiple hull families (container, tanker, bulk, fishing, yacht, HSC)
- Parametric hull generation (Wigley, Series 60)
- Integration with DataService for water properties and vessel creation
- Polly resilience patterns (retry, circuit breaker, timeout)
- OpenTelemetry distributed tracing
- Multi-tenancy support

## Port
5004 (container: 8080)

## Schema
`sizing` (PostgreSQL)

## Dependencies
- Shared project (models, DTOs, middleware)
- DataService (water properties, vessel creation via HTTP)

## Development
```bash
dotnet run --project backend/HullSizingService
```

## Docker
```bash
docker-compose up hull-sizing-service
```

## API Documentation
Swagger UI: http://localhost:5004/swagger

## Related Documentation
- `.plan/hull-sizing/plan/00-OVERVIEW.md` - Executive overview
- `.plan/hull-sizing/plan/01-ARCHITECTURE.md` - Service architecture
- `.plan/hull-sizing/plan/02-DATABASE-SCHEMA.md` - Database schema (to be created)

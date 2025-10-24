# System Architecture

This document describes the high-level architecture, design decisions, and technical details of the sri-subscription full-stack template.

## Table of Contents

- [Overview](#overview)
- [Architecture Diagram](#architecture-diagram)
- [Microservices Design](#microservices-design)
- [Data Flow](#data-flow)
- [Infrastructure](#infrastructure)
- [Security](#security)
- [Scalability](#scalability)
- [Design Decisions](#design-decisions)

## Overview

sri-subscription is a modern, production-ready full-stack application template built with:

- **Frontend**: Single Page Application (SPA) using React
- **Backend**: Microservices architecture using .NET 8
- **Database**: PostgreSQL with schema-per-service pattern
- **Infrastructure**: AWS-based, containerized deployment
- **Authentication**: AWS Cognito with JWT tokens

### Key Principles

1. **Separation of Concerns**: Each microservice handles a specific domain
2. **API Gateway Pattern**: Single entry point for all client requests
3. **Database-per-Service**: Each service has its own schema
4. **Stateless Services**: Horizontally scalable services
5. **Infrastructure as Code**: All AWS resources defined in Terraform
6. **CI/CD Automation**: Automated testing and deployment

## Architecture Diagram

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              AWS Cloud                                   │
│                                                                          │
│  ┌────────────────┐       ┌──────────────────────────────────────────┐ │
│  │  CloudFront    │       │         App Runner (Containers)           │ │
│  │     (CDN)      │       │                                           │ │
│  └────────┬───────┘       │  ┌──────────┐  ┌──────────┐  ┌─────────┐│ │
│           │               │  │ Identity │  │   API    │  │  Data   ││ │
│           │               │  │ Service  │  │ Gateway  │  │ Service ││ │
│    ┌──────▼──────┐        │  │  :5001   │  │  :5002   │  │  :5003  ││ │
│    │   S3 (SPA)  │        │  └────┬─────┘  └────┬─────┘  └────┬────┘│ │
│    │  Frontend   │        │       │             │             │      │ │
│    │   React     │        │       └─────────────┼─────────────┘      │ │
│    └─────────────┘        └───────────────────────────────────────────┘ │
│                                               │                          │
│                                    ┌──────────▼──────────┐               │
│                                    │   RDS PostgreSQL    │               │
│                                    │   ┌──────────────┐  │               │
│                                    │   │ identity_db  │  │               │
│                                    │   │  data_db     │  │               │
│                                    │   └──────────────┘  │               │
│                                    └─────────────────────┘               │
│                                                                          │
│  ┌──────────────┐       ┌──────────────┐       ┌───────────────┐       │
│  │   Cognito    │       │     ECR      │       │    Secrets    │       │
│  │  User Pool   │       │ (Container   │       │   Manager     │       │
│  │              │       │  Registry)   │       │               │       │
│  └──────────────┘       └──────────────┘       └───────────────┘       │
└─────────────────────────────────────────────────────────────────────────┘

                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           Client Browser                                 │
│                                                                          │
│  React App → CloudFront → S3 (Static Assets)                           │
│       │                                                                  │
│       └──────→ API Gateway (App Runner) → Microservices                │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Request Flow

```
User Browser
    │
    ├─→ Static Assets (HTML/JS/CSS)
    │       └─→ CloudFront (CDN)
    │              └─→ S3 Bucket
    │
    └─→ API Requests (/api/*)
            └─→ API Gateway (App Runner)
                    │
                    ├─→ /api/v1/auth/* → Identity Service
                    │       ├─→ Cognito (User Management)
                    │       └─→ RDS (identity schema)
                    │
                    ├─→ /api/v1/users/* → Identity Service
                    │       └─→ RDS (identity schema)
                    │
                    └─→ /api/v1/products/* → Data Service
                            └─→ RDS (data schema)
```

## Microservices Design

### 1. Identity Service (Port 5001)

**Responsibilities:**
- User registration and authentication
- User profile management
- Integration with AWS Cognito
- JWT token validation

**Endpoints:**
- `POST /api/v1/auth/signup` - User registration
- `POST /api/v1/auth/login` - User login
- `POST /api/v1/auth/logout` - User logout
- `GET /api/v1/users` - List users
- `GET /api/v1/users/{id}` - Get user by ID
- `PUT /api/v1/users/{id}` - Update user
- `DELETE /api/v1/users/{id}` - Delete user (soft delete)

**Database Schema:**
```sql
identity.users
  - id (uuid)
  - email (string)
  - name (string)
  - created_at (timestamp)
  - updated_at (timestamp)
  - deleted_at (timestamp, nullable)
```

### 2. API Gateway (Port 5002)

**Responsibilities:**
- Single entry point for all API requests
- Request routing to appropriate microservices
- JWT authentication middleware
- CORS handling
- Request/response logging
- Error handling and standardization

**Routing:**
```
/api/v1/auth/*     → Identity Service
/api/v1/users/*    → Identity Service
/api/v1/products/* → Data Service
```

**Features:**
- Health check endpoint: `GET /health`
- Swagger/OpenAPI documentation: `/swagger`
- Correlation ID injection for request tracing

### 3. Data Service (Port 5003)

**Responsibilities:**
- Business logic and data management
- CRUD operations for domain entities
- Data validation and business rules

**Endpoints:**
- `GET /api/v1/products` - List products
- `GET /api/v1/products/{id}` - Get product by ID
- `POST /api/v1/products` - Create product
- `PUT /api/v1/products/{id}` - Update product
- `DELETE /api/v1/products/{id}` - Delete product (soft delete)

**Database Schema:**
```sql
data.products
  - id (uuid)
  - name (string)
  - description (string)
  - price (decimal)
  - created_at (timestamp)
  - updated_at (timestamp)
  - deleted_at (timestamp, nullable)
```

### 4. Shared Library

**Purpose:** Code reuse across microservices

**Contents:**
- **Models**: Common data models (User, Product)
- **DTOs**: Data Transfer Objects for API contracts
- **Middleware**: JWT authentication middleware
- **Services**: JWT validation service (Cognito)
- **Utilities**: Password hashing, error handling
- **Test Data**: Factory classes for test data generation

## Data Flow

### User Registration Flow

```
1. User submits signup form on frontend
2. Frontend sends POST /api/v1/auth/signup to API Gateway
3. API Gateway routes request to Identity Service
4. Identity Service:
   - Validates input
   - Registers user in AWS Cognito
   - Stores user in PostgreSQL (identity schema)
   - Returns user data
5. Frontend stores auth token and redirects to dashboard
```

### Authenticated Request Flow

```
1. User makes request from frontend (with JWT token)
2. Frontend adds Authorization: Bearer <token> header
3. API Gateway receives request:
   - Validates JWT token with Cognito
   - Extracts user identity
   - Routes to appropriate service
4. Service processes request:
   - Validates user permissions
   - Performs business logic
   - Returns response
5. API Gateway returns response to frontend
```

### Data Query Flow

```
1. Frontend requests product list
2. API Gateway routes to Data Service
3. Data Service:
   - Queries PostgreSQL (data schema)
   - Applies filters, sorting, pagination
   - Returns products
4. Frontend displays products in UI
```

## Infrastructure

### AWS Services

#### Compute

- **AWS App Runner**: Managed container hosting for microservices
  - Auto-scaling based on traffic
  - Built-in load balancing
  - Automatic deployments from ECR
  - Health checks and auto-restart

#### Storage

- **Amazon RDS (PostgreSQL)**: Managed relational database
  - Multi-AZ deployment for high availability (production)
  - Automated backups and point-in-time recovery
  - Encryption at rest and in transit
  - Connection pooling

- **Amazon S3**: Object storage
  - Frontend hosting (static assets)
  - Terraform state storage
  - Versioning enabled for state files
  - Lifecycle policies for cost optimization

#### Content Delivery

- **Amazon CloudFront**: CDN for frontend
  - Global edge locations for low latency
  - HTTPS enforcement
  - Origin Access Control (OAC) for secure S3 access
  - SPA routing (404 → index.html)

#### Containers

- **Amazon ECR**: Container registry
  - Private Docker image hosting
  - Lifecycle policies for image cleanup
  - Image scanning for vulnerabilities
  - Integration with App Runner

#### Authentication

- **Amazon Cognito**: User management
  - User pools for authentication
  - JWT token generation
  - Email verification
  - Password policies

#### Networking

- **Amazon VPC**: Virtual private cloud
  - Public subnets for cost optimization
  - Security groups for access control
  - Internet Gateway for public access

#### Monitoring & Logging

- **Amazon CloudWatch**: Centralized logging
  - Log groups for each service
  - Log retention policies
  - Metrics and alarms

#### Secrets Management

- **AWS Secrets Manager**: Secure secrets storage
  - Database credentials
  - API keys
  - Automatic rotation support

### Terraform Modules

#### Setup Module (`terraform/setup/`)

**Purpose:** One-time infrastructure setup

**Resources:**
- VPC and networking
- Security groups
- ECR repositories
- S3 bucket for Terraform state
- DynamoDB table for state locking
- Cognito User Pool
- CloudWatch log groups
- Cost monitoring and budgets

#### Deploy Module (`terraform/deploy/`)

**Purpose:** Environment-specific application deployment

**Modules:**
- **RDS Module**: PostgreSQL database
- **App Runner Module**: Microservices deployment
- **S3/CloudFront Module**: Frontend hosting and CDN

**Environments:**
- `dev.tfvars` - Development environment
- `staging.tfvars` - Staging environment
- `prod.tfvars` - Production environment

## Security

### Authentication & Authorization

1. **AWS Cognito**: Centralized user management
   - User registration and verification
   - Password policies and MFA support
   - JWT token generation

2. **JWT Tokens**: Stateless authentication
   - Signed by Cognito
   - Validated by microservices
   - Short expiration (1 hour)
   - Refresh token support

3. **Middleware**: Request-level authentication
   - JWT validation in API Gateway
   - User identity extraction
   - Request correlation tracking

### Network Security

1. **VPC Security Groups**:
   - App Runner: Outbound only (managed by AWS)
   - RDS: Inbound from App Runner only

2. **HTTPS**: Enforced for all external traffic
   - CloudFront to browser
   - App Runner endpoints

3. **Private Subnets**: RDS not publicly accessible
   - Accessible only from VPC
   - No direct internet access

### Data Security

1. **Encryption at Rest**:
   - RDS storage encryption
   - S3 bucket encryption
   - EBS volumes encrypted

2. **Encryption in Transit**:
   - TLS 1.2+ for all connections
   - PostgreSQL SSL connections

3. **Secrets Management**:
   - AWS Secrets Manager for credentials
   - No secrets in code or environment variables
   - Automatic rotation support

### Application Security

1. **Input Validation**: All inputs validated and sanitized
2. **SQL Injection Prevention**: Parameterized queries (EF Core)
3. **XSS Prevention**: Output encoding in React
4. **CSRF Protection**: Token-based authentication
5. **Dependency Scanning**: Trivy in CI/CD pipeline
6. **Soft Deletes**: Data retention for audit trails

## Scalability

### Horizontal Scaling

- **App Runner**: Auto-scales based on CPU/memory utilization
- **Stateless Services**: No session state in services
- **Connection Pooling**: Efficient database connections

### Database Scaling

- **Read Replicas**: For read-heavy workloads (production)
- **Connection Pooling**: Npgsql connection pooling
- **Indexing**: Strategic indexes for query performance

### Caching Strategy

- **CloudFront**: Static asset caching at edge locations
- **Browser Caching**: Cache-Control headers
- **Application Caching**: In-memory caching for reference data (future)

### Performance Optimization

- **Lazy Loading**: React Router lazy loading
- **Code Splitting**: Webpack/Vite code splitting
- **Image Optimization**: WebP format, responsive images
- **API Pagination**: Limit results for large datasets
- **Database Indexes**: Query optimization

## Design Decisions

### Why Microservices?

**Pros:**
- ✅ Independent deployment and scaling
- ✅ Technology flexibility per service
- ✅ Team autonomy (separate services)
- ✅ Fault isolation

**Cons:**
- ❌ Increased complexity
- ❌ Network latency between services
- ❌ Distributed transactions complexity

**Decision:** Microservices for learning and scalability, with API Gateway to minimize complexity.

### Why AWS App Runner?

**Alternatives:** ECS, EKS, EC2, Lambda

**Decision:** App Runner for simplicity and cost
- ✅ Fully managed (no cluster management)
- ✅ Auto-scaling included
- ✅ Built-in load balancing
- ✅ Cost-effective for small/medium workloads
- ✅ Easy container deployment

**Trade-offs:**
- ❌ Less control than ECS/EKS
- ❌ Limited configuration options

### Why PostgreSQL?

**Alternatives:** MySQL, DynamoDB, MongoDB

**Decision:** PostgreSQL for reliability and features
- ✅ ACID compliance
- ✅ Rich data types (JSON, arrays, UUID)
- ✅ Mature ecosystem
- ✅ Excellent performance
- ✅ Open source

### Why Schema-per-Service?

**Alternative:** Shared database

**Decision:** Schema-per-service for isolation
- ✅ Service autonomy
- ✅ Independent schema evolution
- ✅ Clear boundaries
- ✅ Easier to migrate to separate databases

**Trade-offs:**
- ❌ No foreign keys across schemas
- ❌ Distributed queries more complex

### Why Cognito?

**Alternatives:** Auth0, Firebase, Custom auth

**Decision:** Cognito for AWS integration
- ✅ Fully managed
- ✅ AWS ecosystem integration
- ✅ Free tier (50,000 MAU)
- ✅ JWT tokens
- ✅ MFA support

**Trade-offs:**
- ❌ AWS vendor lock-in
- ❌ Less flexible than custom auth

### Why MobX over Redux?

**Decision:** MobX for simplicity
- ✅ Less boilerplate
- ✅ Easier to learn
- ✅ Reactive programming
- ✅ Good TypeScript support

**Trade-offs:**
- ❌ Less predictable than Redux
- ❌ Smaller ecosystem

## Future Enhancements

### Short-term

- [ ] Add Redis for caching
- [ ] Implement rate limiting
- [ ] Add WebSocket support for real-time features
- [ ] Comprehensive E2E testing

### Long-term

- [ ] Event-driven architecture with SNS/SQS
- [ ] CQRS pattern for complex queries
- [ ] API versioning strategy
- [ ] Multi-region deployment
- [ ] GraphQL API layer

---

**Last Updated:** Phase 8 - Architecture Documentation

**Contributors:** See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines.







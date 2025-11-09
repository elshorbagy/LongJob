LongJob API + UI (Dockerized Test Task)
Overview

This is a full-stack test project built with ASP.NET Core 9 (Minimal API) and Angular.
It simulates a long-running text processing job that streams incremental output.
The system is fully containerized using Docker Compose, with an Nginx reverse proxy providing SSL termination and Basic Authentication.

Key Features

Real-time streaming of processed text
Job start, stream, and cancel endpoints

Unified exception handling middleware

Structured logging (Serilog-style with ILogger)

Clean Architecture separation:

Domain: Core logic (no framework dependencies)

Application: Use case contracts

Infrastructure: In-memory implementation

API: Presentation & routing

Client: Angular UI

Architecture
LongJob/
 => LongJob.Domain/          → Core text processing logic
 => LongJob.Application/     → Interfaces and use-case definitions
 => LongJob.Infrastructure/  → Actual implementation (In-Memory)
 => LongJob.Api/             → Minimal API endpoints + middleware
 => Client/                  → Angular UI consuming the API
deploy => proxy/             → nginx.conf, certs, htpasswd

Running with Docker

1) Generate development SSL certificates

Use mkcert:

mkcert localhost

Then rename and move the generated files:

deploy/proxy/certs/dev.crt
deploy/proxy/certs/dev.key

2) Create Basic Auth credentials
docker run --rm httpd:alpine htpasswd -nbm admin StrongPass123 > deploy/proxy/htpasswd

3) Build and run all services
docker compose up --build

4 Open in browser

URL: https://localhost:8443

Username: admin

Password: StrongPass123

API Endpoints
Method	Endpoint	Description
POST	/api/jobs	Start a new long-running job, returns jobId
GET	/api/jobs/{jobId}/stream	Stream output of the job via Server-Sent Events
DELETE	/api/jobs/{jobId}	Cancel a running job
🧪 Testing

Unit tests use MSTest with NSubstitute to verify:

Text processing logic

Job creation and cancellation

Exception handling behavior

Run locally:

dotnet test

Tech Stack

.NET 9 (Minimal API)

Angular 20.1.6

Docker & Docker Compose

Nginx (SSL + Basic Auth)

MSTest + NSubstitute

C#

Notes

Self-signed certificates are for local development only.

All services share an internal Docker network (appnet).

To change ports or credentials, edit docker-compose.yml.

The architecture is framework-agnostic and can easily be extended with Redis or SQL persistence.
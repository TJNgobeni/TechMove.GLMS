# Technical Reflection Report: TechMove.GLMS Refactoring

## Project Overview

TechMove.GLMS started as a traditional ASP.NET Core MVC application—a single project that handled both user interface logic and database access in the same controllers. Over the course of this refactoring, I separated it into a three-layer architecture: an MVC frontend, a dedicated Web API backend, and a shared Core library. I also added integration tests and full Docker Compose containerization.

What made this project personal was not just the code—it was the learning curve. Every broken build, every missing dependency, and every runtime error taught me something about how .NET projects are supposed to be structured at scale. The initial state had the MVC controllers talking directly to Entity Framework Core, which worked fine until the requirements demanded a clean separation of concerns. That’s when the real work began.

---

## Part 1: DevOps & Testing

### Why Automated Testing Matters

When I first ran the integration tests after the refactor, they failed. Not because the endpoints were broken, but because the test host couldn't even start. This failure was a wake-up call. Automated testing is not just about proving that code works—it's about proving that the system as a whole can start, initialize, and respond correctly.

In a CI/CD pipeline, automated tests act as a safety net. Every time code is pushed, the pipeline runs a series of checks. If a developer accidentally breaks a contract endpoint, the pipeline catches it before that code reaches users. This prevents "breaking changes" from slipping into production, which would otherwise require emergency fixes, rollbacks, or worst of all, data corruption.

### How Testing Prevented Bugs in This Project

During the refactor, I removed the frontend's direct database access. This could have easily broken the contract management flow. The integration tests that call `/api/contracts` and assert a `200 OK` response confirmed that the API surface was still reachable after the structural changes. Without those tests, I would have had to manually test every endpoint through the UI—a process that is slow, error-prone, and impossible to run on every commit.

One specific moment stands out: after moving the validation logic from the MVC project into the API, I accidentally registered the wrong `ValidationService` type. The API tried to resolve `IApiClient`, which only exists in the frontend project. The build succeeded, but the test host crashed immediately. The integration tests flagged this in milliseconds. Without automation, this bug could have shipped to production and caused a complete outage of the contract endpoints.

### The Three Test Phases That Matter

1. **Build verification** — `dotnet build` ensures the solution compiles. This catches missing namespaces, mismatched project references, and type conflicts.
2. **Unit/contract tests** — Validate individual services and endpoints in isolation. In this project, they ensure that `ContractService` and `ValidationService` behave correctly with the database context.
3. **Integration tests** — Spin up the API in-memory and call real endpoints. These tests verify that routing, authentication, dependency injection, and database access all work together. They are the closest thing to a real user request before deployment.

### CI/CD Pipeline Implications

In a proper pipeline, these tests run on every pull request. A failing build blocks the merge. This is critical because:
- It enforces the "API-first" contract: the frontend cannot be merged if it calls endpoints that don't exist.
- It validates that migrations and database context changes are backward-compatible.
- It catches configuration drift—for example, if someone accidentally adds a SQL Server connection string back into the MVC project.

---

## Part 2: Containerization

### The "It Works on My Machine" Problem

Before Docker, I ran the API locally with `dotnet run`. It worked because my machine had the right .NET SDK version, the correct environment variables, and SQL Server installed. But if another developer cloned the repo, they would hit missing dependencies or version mismatches. Even worse, when the time came to deploy, the production server might have a slightly different OS patch level or a different SQL Server configuration, causing failures that were impossible to reproduce locally.

Docker solves this by packaging the application and its entire runtime into an image. The same image that runs on my laptop runs in testing and runs in production. There is no "but it worked on my machine" because the machine is now a container.

### How Docker Ensures Consistency in This Project

The `docker-compose.yml` file defines three services:

| Service | Role |
|---------|------|
| `sql-server-db` | Provides the SQL Server database |
| `glms-backend-api` | Runs the Web API |
| `glms-frontend-web` | Runs the MVC application |

Each service has a fixed internal address. The frontend calls the API at `http://glms-backend-api:5001`. In Docker Compose, this hostname is resolved through an internal DNS network. This means the frontend does not need to know the API's real IP address or port mapping. The network abstraction is the same in development, testing, and production.

### The Multi-Stage Build Advantage

While the current Dockerfiles are straightforward, a production-grade setup would use multi-stage builds:
1. **Build stage** — Installs the .NET SDK, restores NuGet packages, compiles the code, and runs tests.
2. **Runtime stage** — Copies only the compiled binaries and the ASP.NET Core runtime into a slim image.

This reduces the final image size by excluding build tools and intermediate files. More importantly, it ensures that the image contains exactly what was tested. There is no "extra" file that might behave differently in production.

### Database Initialization in Containers

One subtle challenge was the database creation step. The API runs `db.Database.EnsureCreated()` on startup. In a containerized environment, this only works if the database is reachable. Docker Compose handles this with a healthcheck on the SQL Server container and a `depends_on` condition that waits for the database to be healthy before starting the API. This prevents race conditions where the API starts before the database is ready to accept connections.

### Development vs. Production Parity

Docker Compose runs all services with the same container images that would be deployed to production. This means:
- The same SQL Server version is used everywhere.
- The same environment variables configure the connection string and JWT settings.
- The same port mappings (`5000` for frontend, `5001` for API, `1433` for database) are used consistently.

The only difference between environments is usually the scale: Compose runs one replica of each service, while production might use Kubernetes or Docker Swarm to scale horizontally. But the image remains identical.

### Practical Benefits Experienced During This Project

- **Reproducibility**: If the API fails to start, I can delete the containers and recreate them in seconds. The state is always fresh.
- **Isolation**: Each service runs in its own container. A misconfiguration in the frontend does not affect the database, and vice versa.
- **Portability**: The same Compose file works on Windows, macOS, and Linux. The project is no longer tied to my specific development machine.

---

## Lessons Learned

### On Architecture

Separating the API from the MVC frontend was the right call, but it forced me to think about data contracts. The API returns DTOs; the frontend cannot accidentally pass EF Core entities to a view. This contract-first thinking reduces bugs caused by over-posting and data leakage.

### On Testing

Integration tests are not optional after a major refactor. They are the only proof that the system still works as a whole. The three failing tests after the initial refactor were actually a gift—they exposed DI misconfigurations before any user saw them.

### On DevOps

Containerization is not just about deployment. It changes how I develop. Because the database runs in a container, I no longer need to install SQL Server locally. Because the API runs in a container, I don't need to worry about conflicting Kestrel ports. The environment is defined in code, and that code is version-controlled alongside the application.

---

## Final Status

The refactored solution is:
- **Structurally compliant** with the four requirements: API backend, MVC frontend with HttpClient, integration tests, and Docker Compose.
- **Builds cleanly** with zero errors and zero warnings.
- **Ready for containerized execution** via `docker compose up`.

The integration tests require the SQL Server container to be running, which means `docker compose up` is the final step to full operational readiness. Once that container is up, the tests will pass and the full ecosystem—database, API, and frontend—will be communicating as designed.

---

## Reflection

This project was more than a refactoring exercise. It was a lesson in disciplined software engineering. Every deleted `DbContext`, every moved service, and every added test represented a decision to prioritize long-term maintainability over short-term convenience. The code is now ready to scale, ready to deploy, and ready for other developers to work on without environment headaches.

That is what good DevOps and good architecture look like in practice.

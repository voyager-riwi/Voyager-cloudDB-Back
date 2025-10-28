# Project Architecture: CrudCloudDb

This document outlines the architecture of the CrudCloudDb backend, which is based on the principles of **Clean Architecture**.

## 1. Core Philosophy

The architecture is designed to be:
-   **Independent of Frameworks**: The core business logic isn't tied to ASP.NET Core.
-   **Testable** Each layer can be tested in isolation.
-   **Independent of UI** The same backend logic can support a web app, a mobile app, or a console client.
-   **Independent of Database** The core logic doesn't know about PostgreSQL or EF Core.
-   **Independent of External Services** The core is decoupled from external services like Docker or email providers.

## 2. Project Layers

The solution is divided into four main projects, each representing a layer in the architecture.

### `CrudCloudDb.Core`
-   **Purpose** Contains the most central and high-level business rules.
-   **Contents**:
    -   `Entities` The domain models of the application (e.g., `User`, `DatabaseInstance`, `Plan`). These are plain C# objects.
    -   `Enums` Enumerations used throughout the application (e.g., `DatabaseEngine`, `PlanType`).
-   **Dependencies** None. This project is the core of the application and depends on nothing.

### `CrudCloudDb.Application`
-   **Purpose**: Contains the application-specific business logic. It orchestrates the domain entities to perform use cases.
-   **Contents**:
    -   `Interfaces/Repositories` Defines the contracts (interfaces) that the data access layer must implement (e.g., `IUserRepository`, `IPlanRepository`).
    -   `Services/Interfaces` Defines the contracts for business logic services (e.g., `IAuthService`, `IDatabaseService`).
    -   `Services/Implementation` The concrete implementation of the service interfaces. This is where the main business logic resides.
    -   `DTOs` (Data Transfer Objects) Models used to transfer data between the API layer and the Application layer.
    -   `Utilities` Helper classes for application-level tasks like password hashing or token generation.
-   **Dependencies** `CrudCloudDb.Core`.

### `CrudCloudDb.Infrastructure`
-   **Purpose** Implements the interfaces defined in the Application layer. This layer is responsible for all interactions with the "outside world."
-   **Contents**
    -   `Data` Contains the `ApplicationDbContext` (Entity Framework Core).
    -   `Migrations` EF Core database migration files.
    -   `Repositories` Concrete implementations of the repository interfaces (e.g., `UserRepository`, `PlanRepository`). This is where the database queries are made.
    -   `Services` Implementations of interfaces that deal with external systems (e.g., `DockerService`, `EmailService`).
-   **Dependencies** `CrudCloudDb.Application`, `CrudCloudDb.Core`.

### `CrudCloudDb.API`
-   **Purpose** The entry point of the application. It exposes the business logic as a RESTful API.
-   **Contents**
    -   `Controllers` The API endpoints that receive HTTP requests and return responses. They are responsible for routing requests to the appropriate application services.
    -   `Middleware` Custom middleware for handling cross-cutting concerns like error handling, logging, and auditing.
    -   `Configuration` Classes for binding configuration settings from `appsettings.json`.
    -   `Program.cs` The main application startup file where services are registered (Dependency Injection), the HTTP pipeline is configured, and middleware is added.
-   **Dependencies** `CrudCloudDb.Application`, `CrudCloudDb.Infrastructure`.

## 3. Dependency Flow

The dependencies flow inwards, towards the `Core` project. This is the **Dependency Rule**.
```bash
+-------------------+       +----------------------+       +--------------------------+
| CrudCloudDb.API   | --->  | CrudCloudDb.Application | ---> | CrudCloudDb.Core         |
+-------------------+       +----------------------+       +--------------------------+
^                           |                              ^
|                           v                              |
|               +--------------------------+               |
+---------------| CrudCloudDb.Infrastructure |--------------+
+--------------------------+
```

-   `API` depends on `Application` to execute use cases.
-   `Infrastructure` depends on `Application` to implement its interfaces.
-   `Application` depends on `Core` to use the domain entities.
-   `Core` depends on nothing.

This structure ensures that changes in outer layers (like the database or a third-party service) do not affect the core business logic.
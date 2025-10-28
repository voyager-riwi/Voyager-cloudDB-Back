# CrudCloudDb Platform

Cloud database management platform for PostgreSQL, MySQL, and MongoDB, developed as part of the Voyager program at Riwi.

## Architecture

This project follows the principles of Clean Architecture to ensure a decoupled, scalable, and maintainable codebase.

- **CrudCloudDb.Core** Contains the domain entities, enums, and core business rules. It has no dependencies on other projects.
- **CrudCloudDb.Application** Implements the business logic (services), defines interfaces for repositories, and contains DTOs.
- **CrudCloudDb.Infrastructure** Handles data access (DbContext, repositories) and interacts with external services like Docker and email providers.
- **CrudCloudDb.API** Exposes the application's functionality through a RESTful API, containing controllers, middleware, and startup configuration.

## Tech Stack

- **Backend** ASP.NET Core 8.0
- **ORM** Entity Framework Core
- **Database (Platform)** PostgreSQL
- **Containerization** Docker & Docker.DotNet
- **Authentication** JWT (JSON Web Token)
- **Logging** Serilog
- **API Documentation** Swashbuckle (Swagger)

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Docker Desktop (or Docker Engine on Linux)
- PostgreSQL for the main platform database.

### Installation & Setup
1.  **Clone the repository**
    ```bash
    git clone https://github.com/voyager-riwi/Voyager-cloudDB-Back.git
    cd Voyager-cloudDB-Back
    ```

2.  **Configure Environment Variables**
    For local development, sensitive data like connection strings and email passwords are set in `CrudCloudDb.API/Properties/launchSettings.json`. Ensure your local copy has the correct credentials. **Do not commit secrets to the repository.**

3.  **Apply Database Migrations**
    The initial migration is already created. You just need to apply it to your database.
    ```bash
    # Navigate to the Infrastructure project folder
    cd CrudCloudDb.Infrastructure

    # Apply the migration (make sure the API project is specified for startup configuration)
    dotnet ef database update --startup-project ../CrudCloudDb.API
    ```

4.  **Run the application**
    ```bash
    # Navigate back to the API project
    cd ../CrudCloudDb.API

    # Run the project
    dotnet run
    ```

### Accessing the API
-   **API Base URL** `http://localhost:5191`
-   **Swagger UI** `http://localhost:5191/swagger`

## Team & Roles
-   **Miguel (Dev 1)** Authentication, User Management, and Core Docker Logic.
-   **Brahiam (Dev 2)** Database Management, Payments, and Webhooks.
-   **Vanessa (Dev 3)** Middleware, Infrastructure Support, Health Checks, and Documentation.
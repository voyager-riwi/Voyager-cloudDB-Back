# 🧙‍♂️ PotterCloud - Backend API

<div align="center">

![PotterCloud](https://img.shields.io/badge/PotterCloud-Backend-7B68EE?style=for-the-badge&logo=dotnet)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Nginx](https://img.shields.io/badge/Nginx-009639?style=for-the-badge&logo=nginx&logoColor=white)

**La plataforma mágica para gestionar bases de datos en la nube** ✨

[Explorar API](https://service.voyager.andrescortes.dev) • [Frontend](https://voyager.andrescortes.dev) • [Documentación](#-documentación)

</div>

---

## 📖 Tabla de Contenidos

- [¿Qué es PotterCloud?](#-qué-es-pottercloud)
- [Problema que Resuelve](#-problema-que-resuelve)
- [Características Principales](#-características-principales)
- [Ventajas Competitivas](#-ventajas-competitivas)
- [Seguridad](#-seguridad)
- [Arquitectura](#%EF%B8%8F-arquitectura)
- [Stack Tecnológico](#%EF%B8%8F-stack-tecnológico)
- [Requisitos Previos](#-requisitos-previos)
- [Instalación](#-instalación)
- [Configuración](#%EF%B8%8F-configuración)
- [Despliegue](#-despliegue)
- [API Endpoints](#-api-endpoints)
- [Lógica de Negocio](#-lógica-de-negocio)
- [Testing](#-testing)
- [CI/CD](#-cicd)
- [Monitoreo y Logs](#-monitoreo-y-logs)
- [Contribución](#-contribución)
- [Equipo](#-equipo)
- [Diagramas](#-diagramas)
- [Licencia](#-licencia)

---

## 🧙 ¿Qué es PotterCloud?

**PotterCloud** es una plataforma SaaS de última generación que democratiza el acceso a bases de datos en la nube. Inspirada en la magia y simplicidad de servicios como Clever Cloud, PotterCloud permite a desarrolladores, startups y empresas crear, gestionar y escalar instancias de bases de datos con un simple click.

Con PotterCloud, no necesitas ser un experto en DevOps para tener bases de datos profesionales en producción. Nuestra plataforma se encarga de toda la infraestructura, seguridad y configuración automáticamente.

### 🎯 Misión

Proporcionar una experiencia de gestión de bases de datos tan simple y mágica como lanzar un hechizo, eliminando la complejidad técnica y permitiendo que los desarrolladores se enfoquen en lo que realmente importa: construir productos increíbles.

---

## 🔥 Problema que Resuelve

### Desafíos Tradicionales

1. **Complejidad en la Configuración**
   - Configurar servidores de bases de datos requiere conocimientos especializados
   - Gestión manual de puertos, usuarios y permisos
   - Configuraciones propensas a errores de seguridad

2. **Costos Elevados**
   - Proveedores tradicionales cobran tarifas elevadas desde el inicio
   - Falta de planes gratuitos para desarrollo y testing
   - Escalabilidad costosa y compleja

3. **Mantenimiento Operacional**
   - Respaldos manuales y gestión de versiones
   - Monitoreo y alertas requieren herramientas adicionales
   - Actualizaciones de seguridad y parches manuales

4. **Aislamiento y Seguridad**
   - Difícil garantizar el aislamiento entre bases de datos
   - Gestión manual de credenciales y certificados
   - Vulnerabilidades en configuraciones personalizadas

### Solución PotterCloud ✨

PotterCloud elimina todos estos puntos de fricción mediante:
- **Creación instantánea** de bases de datos con un click
- **Plan gratuito** generoso para desarrollo
- **Gestión automática** de credenciales y seguridad
- **Aislamiento total** entre usuarios y bases de datos
- **Notificaciones inteligentes** vía email y webhooks
- **Integración perfecta** con sistemas de pago

---

## ⚡ Características Principales

### 🎨 Gestión Multi-Motor

Soporte nativo para los motores de bases de datos más populares:

| Motor | Versión | Estado |
|-------|---------|--------|
| 🐘 **PostgreSQL** | 15+ | ✅ Disponible |
| 🐬 **MySQL** | 8.0+ | ✅ Disponible |
| 🍃 **MongoDB** | 6.0+ | ✅ Disponible |
| 📊 **SQL Server** | 2022 | ✅ Disponible |
| 🔴 **Redis** | 7.0+ | 🚧 Próximamente |
| 🌌 **Cassandra** | 4.0+ | 🚧 Próximamente |

### 🎁 Planes Flexibles

```
┌─────────────────────────────────────────────────────────┐
│  🆓 PLAN GRATUITO                                       │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  ✓ 2 bases de datos por motor                          │
│  ✓ Gestión completa de credenciales                    │
│  ✓ Notificaciones por email                            │
│  ✓ Soporte comunitario                                 │
│  ✓ Sin tarjeta de crédito                              │
│                                                          │
│  💎 PLAN INTERMEDIO - $5.000 COP/mes                    │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  ✓ 5 bases de datos por motor                          │
│  ✓ Webhooks personalizados                             │
│  ✓ Soporte prioritario                                 │
│  ✓ Métricas avanzadas                                  │
│                                                          │
│  🚀 PLAN AVANZADO - $10.000 COP/mes                     │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  ✓ 10 bases de datos por motor                         │
│  ✓ API con rate limits extendidos                      │
│  ✓ Soporte 24/7                                        │
│  ✓ Backups automáticos                                │
└─────────────────────────────────────────────────────────┘
```

### 🔐 Seguridad de Clase Empresarial

- **Autenticación JWT** robusta con tokens de larga duración
- **Aislamiento total** entre usuarios mediante contenedores Docker
- **Credenciales únicas** generadas automáticamente por instancia
- **Encriptación end-to-end** para datos sensibles
- **Auditoría completa** de todas las acciones
- **Rate limiting** para prevenir abusos
- **HTTPS obligatorio** en todos los endpoints

### 📧 Sistema de Notificaciones Inteligente

#### Emails Automatizados
- ✅ Confirmación de registro
- ✅ Credenciales de nueva base de datos
- ✅ Confirmación de eliminación
- ✅ Cambio de plan exitoso
- ✅ Alertas de seguridad

#### Webhooks Personalizables
```json
{
  "event": "database.created",
  "timestamp": "2025-11-13T10:30:00Z",
  "data": {
    "userId": "uuid-here",
    "databaseName": "my_postgres_db",
    "engine": "PostgreSQL",
    "status": "active"
  }
}
```

### 💳 Integración de Pagos

- **Mercado Pago** como pasarela principal
- Procesamiento de pagos a planes intermedio y/o max.
- Webhooks para confirmación de pagos
- Facturación transparente

---

## 🏆 Ventajas Competitivas

### vs. Clever Cloud
- ✅ **Plan gratuito más generoso** (2 DBs por motor vs 1 DB total)
- ✅ **Precio 70% más bajo** en planes pagos
- ✅ **API REST completa** con documentación Swagger
- ✅ **Webhooks nativos** para integración con terceros

### vs. Heroku Postgres
- ✅ **Multi-motor** desde el inicio (no solo PostgreSQL)
- ✅ **Sin hibernación** de bases de datos inactivas
- ✅ **Configuración automática** de credenciales
- ✅ **Soporte latinoamericano** con pagos locales

### vs. AWS RDS
- ✅ **Simplicidad extrema** sin curva de aprendizaje
- ✅ **Sin costos ocultos** ni facturación por hora
- ✅ **Onboarding en minutos** vs días
- ✅ **Interfaz en español** nativa

### vs. MongoDB Atlas
- ✅ **Multi-motor** en una sola plataforma
- ✅ **Plan gratuito sin límite de tiempo**
- ✅ **Gestión unificada** de todas las DBs
- ✅ **Integración con ecosistema local**

---

## 🔒 Seguridad

### Arquitectura de Seguridad por Capas

```
┌─────────────────────────────────────────────────────┐
│  CAPA 1: INFRAESTRUCTURA                            │
│  • Nginx con SSL/TLS 1.3                            │
│  • Certificados Let's Encrypt renovados auto        │
│  • Headers de seguridad HTTP                        │
└─────────────────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────┐
│  CAPA 2: APLICACIÓN                                 │
│  • Autenticación JWT (HS256)                        │
│  • Middleware de autorización                       │
│  • Rate limiting por endpoint                       │
│  • CORS configurado restrictivamente                │
└─────────────────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────┐
│  CAPA 3: DATOS                                      │
│  • Passwords hasheados (BCrypt)                     │
│  • Secrets en variables de entorno                  │
│  • Credenciales rotables                            │
│  • Backups encriptados                              │
└─────────────────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────┐
│  CAPA 4: AISLAMIENTO                                │
│  • Contenedores Docker independientes               │
│  • Redes virtuales segregadas                       │
│  • Usuarios de BD con permisos mínimos              │
│  • Sin acceso cross-tenant                          │
└─────────────────────────────────────────────────────┘
```

### Prácticas de Seguridad Implementadas

1. **Autenticación y Autorización**
   - JWT con expiración de 24 horas
   - Refresh tokens para sesiones extendidas
   - Verificación de email obligatoria
   - Passwords con política de complejidad

2. **Protección de Datos**
   - Credenciales nunca en logs
   - Secrets en GitHub Actions
   - Variables de entorno segregadas
   - Auditoría completa de accesos

3. **Seguridad en Contenedores**
   - Imágenes oficiales verificadas
   - Sin privilegios root
   - Redes bridge aisladas
   - Health checks obligatorios

4. **Monitoreo y Respuesta**
   - Logs centralizados con Serilog
   - Alertas automáticas por Discord
   - Auditoría de todas las operaciones
   - Detección de anomalías

---

## 🏗️ Arquitectura

### Clean Architecture (Onion Architecture)

El proyecto sigue los principios de **Clean Architecture**, garantizando:
- ✅ Independencia de frameworks
- ✅ Testabilidad completa
- ✅ Independencia de UI
- ✅ Independencia de base de datos
- ✅ Independencia de servicios externos

```
┌───────────────────────────────────────────────────────────┐
│                    CrudCloudDb.API                        │
│  (Controllers, Middleware, Configuration)                 │
│  • AuthController, DatabasesController                    │
│  • PaymentsController, WebhooksController                 │
│  • JWT Middleware, Error Handling                         │
└─────────────────────┬─────────────────────────────────────┘
                      │ depends on
┌─────────────────────▼─────────────────────────────────────┐
│              CrudCloudDb.Application                      │
│  (Business Logic, Services, DTOs, Interfaces)             │
│  • IAuthService, IDatabaseService                         │
│  • IPaymentService, IEmailService                         │
│  • DTOs for Auth, Database, Payment                       │
└─────────────────────┬─────────────────────────────────────┘
                      │ depends on
┌─────────────────────▼─────────────────────────────────────┐
│               CrudCloudDb.Core                            │
│  (Entities, Enums, Business Rules)                        │
│  • User, DatabaseInstance, Plan                           │
│  • Subscription, AuditLog, EmailLog                       │
│  • DatabaseEngine, PlanType, Status                       │
└───────────────────────────────────────────────────────────┘
                      ▲
                      │ implements
┌─────────────────────┴─────────────────────────────────────┐
│            CrudCloudDb.Infrastructure                     │
│  (Data Access, External Services)                         │
│  • ApplicationDbContext (EF Core)                         │
│  • Repositories (User, Plan, Database)                    │
│  • DockerService, EmailService                            │
│  • MasterContainerService                                 │
└───────────────────────────────────────────────────────────┘
```

### Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────┐
│                         FRONTEND                            │
│                    (Vue.js SPA)                             │
│             voyager.andrescortes.dev                        │
└─────────────────────┬───────────────────────────────────────┘
                      │ HTTPS/REST
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                         NGINX                               │
│                  (Reverse Proxy + SSL)                      │
│  • SSL Termination (Let's Encrypt)                          │
│  • Load Balancing                                           │
│  • Static Asset Caching                                     │
└─────────────────────┬───────────────────────────────────────┘
                      │
        ┌─────────────┴─────────────┐
        ▼                           ▼
┌──────────────────┐       ┌──────────────────┐
│   Backend API    │       │   Frontend App   │
│   Port: 5191     │       │   Port: 3011     │
│  ASP.NET Core    │       │     Vue.js       │
└────────┬─────────┘       └──────────────────┘
         │
         ├─────────────────┬─────────────────┬─────────────────┐
         ▼                 ▼                 ▼                 ▼
┌─────────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   PostgreSQL    │ │  Docker API  │ │ Mercado Pago │ │ SMTP Server  │
│  (Main DB)      │ │ (Containers) │ │    (API)     │ │  (Email)     │
│  Port: 5432     │ │              │ │              │ │              │
└─────────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│              USER DATABASE CONTAINERS                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  PostgreSQL  │  │    MySQL     │  │   MongoDB    │      │
│  │  User_1      │  │  User_1      │  │  User_1      │      │
│  │  Port: 5433  │  │  Port: 3306  │  │  Port: 27017 │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  PostgreSQL  │  │    MySQL     │  │   MongoDB    │      │
│  │  User_2      │  │  User_2      │  │  User_2      │      │
│  │  Port: 5434  │  │  Port: 3307  │  │  Port: 27018 │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

### Flujo de Creación de Base de Datos

```
┌────────────┐
│   Usuario  │
└─────┬──────┘
      │ 1. POST /api/databases
      ▼
┌────────────────────────────┐
│   DatabasesController      │
└─────┬──────────────────────┘
      │ 2. ValidateUserQuota()
      ▼
┌────────────────────────────┐
│   DatabaseService          │ ──────┐
└─────┬──────────────────────┘       │ 3. CreateContainer()
      │                               ▼
      │                      ┌────────────────────┐
      │                      │   DockerService    │
      │                      └────────────────────┘
      │ 4. GenerateCredentials()
      ▼
┌────────────────────────────┐
│   CredentialService        │
└─────┬──────────────────────┘
      │ 5. SaveToDatabase()
      ▼
┌────────────────────────────┐
│   DatabaseInstanceRepo     │
└─────┬──────────────────────┘
      │ 6. SendCredentialsEmail()
      ▼
┌────────────────────────────┐
│   EmailService             │
└─────┬──────────────────────┘
      │ 7. TriggerWebhook()
      ▼
┌────────────────────────────┐
│   WebhookService           │
└─────┬──────────────────────┘
      │ 8. Return Response
      ▼
┌────────────────────────────┐
│   Usuario recibe:          │
│   • Credentials            │
│   • Email                  │
│   • Webhook notification   │
└────────────────────────────┘
```

Para más detalles sobre la arquitectura, consulta [ARCHITECTURE.md](./ARCHITECTURE.md).

---

## 🛠️ Stack Tecnológico

### Backend Core

```yaml
Framework: ASP.NET Core 8.0 (LTS)
Lenguaje: C# 12
Arquitectura: Clean Architecture (4 capas)
API: RESTful con Swagger/OpenAPI
Autenticación: JWT (JSON Web Tokens)
```

### Base de Datos

```yaml
Principal: PostgreSQL 15
ORM: Entity Framework Core 8.0
Migraciones: EF Core Migrations
Naming: Snake Case (Npgsql.EntityFrameworkCore.PostgreSQL.Snake)
```

### Infraestructura

```yaml
Containerización: Docker + Docker Compose
Reverse Proxy: Nginx (Alpine)
SSL/TLS: Let's Encrypt (Certbot)
Orquestación: Docker Engine API
```

### Servicios Externos

```yaml
Pagos: Mercado Pago SDK
Email: SMTP (Gmail/SendGrid)
Webhooks: Discord Integration
Logs: Serilog (Console + File)
```

### DevOps & CI/CD

```yaml
VCS: Git + GitHub
CI/CD: GitHub Actions
Secrets: GitHub Secrets
Deployment: Docker en VPS
Monitoring: Serilog + Discord Alerts
```

### Librerías Destacadas

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0 | Autenticación JWT |
| `Microsoft.EntityFrameworkCore` | 8.0 | ORM para PostgreSQL |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0 | Provider de PostgreSQL |
| `Docker.DotNet` | 3.125 | API de Docker |
| `MercadoPago` | 2.5.0 | SDK de pagos |
| `Serilog.AspNetCore` | 8.0 | Logging estructurado |
| `Swashbuckle.AspNetCore` | 6.5 | Documentación Swagger |
| `BCrypt.Net-Next` | 4.0.3 | Hashing de contraseñas |

---

## 📋 Requisitos Previos

### Software Requerido

| Software | Versión Mínima | Propósito |
|----------|---------------|-----------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 | Compilar y ejecutar el proyecto |
| [Docker](https://www.docker.com/get-started) | 24.0+ | Contenedores de aplicación y BD |
| [Docker Compose](https://docs.docker.com/compose/install/) | 2.20+ | Orquestación de servicios |
| [PostgreSQL](https://www.postgresql.org/download/) | 15+ | Base de datos principal |
| [Git](https://git-scm.com/downloads) | 2.40+ | Control de versiones |

### Conocimientos Recomendados

- ✅ C# y programación orientada a objetos
- ✅ ASP.NET Core y Web APIs
- ✅ Entity Framework Core
- ✅ Docker y contenedores
- ✅ SQL y bases de datos relacionales
- ✅ Autenticación JWT
- ✅ Git y GitHub

### Herramientas Opcionales

- **Visual Studio 2022** o **Rider** para desarrollo
- **Postman** o **Insomnia** para testing de APIs
- **DBeaver** o **pgAdmin** para gestión de BD
- **Docker Desktop** para gestión visual de contenedores

---

## 🚀 Instalación

### Opción 1: Desarrollo Local (Sin Docker)

#### 1. Clonar el Repositorio

```bash
git clone https://github.com/voyager-riwi/Voyager-cloudDB-Back.git
cd Voyager-cloudDB-Back
```

#### 2. Configurar PostgreSQL

```bash
# Crear base de datos
psql -U postgres
CREATE DATABASE crud_cloud_db;
\q
```

#### 3. Configurar Variables de Entorno

Crear archivo `.env` en la raíz del proyecto:

```bash
# Base de datos principal
DB_HOST=localhost
DB_PORT=5432
DB_NAME=crud_cloud_db
DB_USER=postgres
DB_PASSWORD=tu_password_seguro

# JWT
JWT_SECRET=tu_secreto_jwt_muy_largo_y_seguro_minimo_32_caracteres
JWT_ISSUER=CrudCloudDb.API
JWT_AUDIENCE=CrudCloudDb.Frontend
JWT_EXPIRY_MINUTES=1440

# Mercado Pago
MERCADOPAGO_ACCESS_TOKEN=TEST-tu-token-aqui
MERCADOPAGO_PUBLIC_KEY=TEST-tu-public-key-aqui

# Email (Gmail)
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_EMAIL=tu-email@gmail.com
SMTP_PASSWORD=tu-app-password
SMTP_SENDER_NAME=PotterCloud

# Webhooks
DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/tu-webhook-aqui

# Docker (para gestión de contenedores de usuario)
DOCKER_HOST=unix:///var/run/docker.sock
```

#### 4. Restaurar Dependencias

```bash
dotnet restore
```

#### 5. Aplicar Migraciones

```bash
cd CrudCloudDb.Infrastructure
dotnet ef database update --startup-project ../CrudCloudDb.API
```

#### 6. Ejecutar la Aplicación

```bash
cd ../CrudCloudDb.API
dotnet run
```

La API estará disponible en:
- **HTTP:** http://localhost:5191
- **Swagger:** http://localhost:5191/swagger

---

### Opción 2: Con Docker (Recomendado)

#### 1. Clonar el Repositorio

```bash
git clone https://github.com/voyager-riwi/Voyager-cloudDB-Back.git
cd Voyager-cloudDB-Back
```

#### 2. Configurar Variables de Entorno

Crear archivo `.env` igual que en la opción 1.

#### 3. Construir y Ejecutar

```bash
# Construir la imagen
docker build -t pottercloud-backend:latest .

# Ejecutar el contenedor
docker run -d \
  --name pottercloud-api \
  -p 5191:5191 \
  --env-file .env \
  -v /var/run/docker.sock:/var/run/docker.sock \
  pottercloud-backend:latest
```

#### 4. Verificar Estado

```bash
# Ver logs
docker logs -f pottercloud-api

# Verificar salud
curl http://localhost:5191/health
```

---

### Opción 3: Docker Compose (Producción Local)

#### 1. Preparar Docker Compose

Crear `docker-compose.local.yml`:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: pottercloud-postgres
    environment:
      POSTGRES_DB: crud_cloud_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build: .
    container_name: pottercloud-api
    ports:
      - "5191:5191"
    depends_on:
      postgres:
        condition: service_healthy
    env_file:
      - .env
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - ./logs:/app/logs
    restart: unless-stopped

volumes:
  postgres_data:
```

#### 2. Ejecutar

```bash
docker-compose -f docker-compose.local.yml up -d
```

---

## ⚙️ Configuración

### Archivo `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=crud_cloud_db;Username=postgres;Password=placeholder;Port=5432"
  },
  "JwtSettings": {
    "Secret": "placeholder_secret_minimum_32_characters_long",
    "Issuer": "CrudCloudDb.API",
    "Audience": "CrudCloudDb.Frontend",
    "ExpiryMinutes": 1440
  },
  "MercadoPagoSettings": {
    "AccessToken": "placeholder",
    "PublicKey": "placeholder"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "placeholder@gmail.com",
    "SenderName": "PotterCloud",
    "Username": "placeholder@gmail.com",
    "Password": "placeholder",
    "EnableSsl": true
  },
  "WebhookSettings": {
    "DiscordUrl": "placeholder"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/crudclouddb-.log",
          "rollingInterval": "Day",
          "fileSizeLimitBytes": 10485760,
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

### Configuración de Nginx

Ver archivo completo en [`nginx.conf`](./nginx.conf).

**Características destacadas:**
- ✅ SSL/TLS 1.3 con certificados Let's Encrypt
- ✅ HTTP/2 habilitado
- ✅ Compresión GZIP para assets
- ✅ Proxy reverso para API y Frontend
- ✅ Headers de seguridad
- ✅ Cache de assets estáticos

### Configuración de SSL (Let's Encrypt)

```bash
# Instalar certbot
sudo apt install certbot

# Obtener certificados para Backend
sudo certbot certonly --standalone -d service.voyager.andrescortes.dev

# Obtener certificados para Frontend
sudo certbot certonly --standalone -d voyager.andrescortes.dev

# Copiar certificados a la ubicación de Nginx
sudo cp /etc/letsencrypt/live/service.voyager.andrescortes.dev/fullchain.pem ./ssl/
sudo cp /etc/letsencrypt/live/service.voyager.andrescortes.dev/privkey.pem ./ssl/
sudo cp /etc/letsencrypt/live/voyager.andrescortes.dev/fullchain.pem ./ssl/voyager-fullchain.pem
sudo cp /etc/letsencrypt/live/voyager.andrescortes.dev/privkey.pem ./ssl/voyager-privkey.pem

# Renovación automática (cada 3 meses)
sudo certbot renew --dry-run
```

---

## 🌐 Despliegue

### Despliegue en Producción (VPS)

#### Prerrequisitos en el Servidor

```bash
# Actualizar sistema
sudo apt update && sudo apt upgrade -y

# Instalar Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Instalar Docker Compose
sudo apt install docker-compose -y

# Instalar Nginx
sudo apt install nginx -y

# Instalar Certbot
sudo apt install certbot -y
```

#### 1. Preparar el Proyecto

```bash
# Conectar al servidor
ssh usuario@tu-servidor.com

# Clonar repositorio
cd /opt
sudo git clone https://github.com/voyager-riwi/Voyager-cloudDB-Back.git
cd Voyager-cloudDB-Back
```

#### 2. Configurar Secrets

```bash
# Crear archivo .env con variables de producción
sudo nano .env

# Asegurarse de que contenga TODAS las variables necesarias
# NUNCA commitear el archivo .env al repositorio
```

#### 3. Construir y Desplegar

```bash
# Construir imagen
sudo docker build -t pottercloud-backend:latest .

# Ejecutar contenedor
sudo docker run -d \
  --name pottercloud-api \
  --restart unless-stopped \
  -p 5191:5191 \
  --env-file .env \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v /opt/Voyager-cloudDB-Back/logs:/app/logs \
  pottercloud-backend:latest
```

#### 4. Configurar Nginx

```bash
# Copiar configuración
sudo cp nginx.conf /etc/nginx/nginx.conf

# Obtener certificados SSL
sudo certbot certonly --standalone -d service.voyager.andrescortes.dev
sudo certbot certonly --standalone -d voyager.andrescortes.dev

# Crear directorio SSL en el proyecto
sudo mkdir -p ssl
sudo cp /etc/letsencrypt/live/service.voyager.andrescortes.dev/fullchain.pem ssl/
sudo cp /etc/letsencrypt/live/service.voyager.andrescortes.dev/privkey.pem ssl/
sudo cp /etc/letsencrypt/live/voyager.andrescortes.dev/fullchain.pem ssl/voyager-fullchain.pem
sudo cp /etc/letsencrypt/live/voyager.andrescortes.dev/privkey.pem ssl/voyager-privkey.pem

# Iniciar Nginx con Docker
sudo docker-compose up -d
```

#### 5. Verificar Despliegue

```bash
# Verificar contenedores
sudo docker ps

# Ver logs de la API
sudo docker logs -f pottercloud-api

# Verificar Nginx
sudo docker logs -f voyager-backend-nginx

# Test de endpoints
curl https://service.voyager.andrescortes.dev/health
```

---

### GitHub Actions (CI/CD Automático)

Crear `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Production

on:
  push:
    branches: [main, master]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
      
      - name: Test
        run: dotnet test --no-build --verbosity normal
      
      - name: Deploy to Server
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.SERVER_HOST }}
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          script: |
            cd /opt/Voyager-cloudDB-Back
            git pull origin main
            docker build -t pottercloud-backend:latest .
            docker stop pottercloud-api || true
            docker rm pottercloud-api || true
            docker run -d \
              --name pottercloud-api \
              --restart unless-stopped \
              -p 5191:5191 \
              --env-file .env \
              -v /var/run/docker.sock:/var/run/docker.sock \
              -v /opt/Voyager-cloudDB-Back/logs:/app/logs \
              pottercloud-backend:latest
```

#### Configurar GitHub Secrets

En el repositorio de GitHub, ir a `Settings > Secrets and variables > Actions` y agregar:

| Secret | Valor |
|--------|-------|
| `SERVER_HOST` | IP o dominio del servidor |
| `SERVER_USER` | Usuario SSH |
| `SSH_PRIVATE_KEY` | Clave privada SSH |
| `DB_PASSWORD` | Password de PostgreSQL |
| `JWT_SECRET` | Secret JWT |
| `MERCADOPAGO_ACCESS_TOKEN` | Token de Mercado Pago |
| `MERCADOPAGO_PUBLIC_KEY` | Public key de Mercado Pago |
| `SMTP_PASSWORD` | Password de email |
| `DISCORD_WEBHOOK_URL` | URL del webhook de Discord |

---

## 📡 API Endpoints

### Autenticación

#### `POST /api/auth/register`
Registrar nuevo usuario.

**Request:**
```json
{
  "email": "usuario@ejemplo.com",
  "password": "Password123!",
  "firstName": "Juan",
  "lastName": "Pérez"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Usuario registrado exitosamente",
  "data": {
    "userId": "uuid-here",
    "email": "usuario@ejemplo.com",
    "planType": "Free"
  }
}
```

---

#### `POST /api/auth/login`
Iniciar sesión.

**Request:**
```json
{
  "email": "usuario@ejemplo.com",
  "password": "Password123!"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-11-14T10:00:00Z",
    "user": {
      "id": "uuid-here",
      "email": "usuario@ejemplo.com",
      "firstName": "Juan",
      "lastName": "Pérez",
      "currentPlan": "Free"
    }
  }
}
```

---

### Bases de Datos

#### `GET /api/databases`
Listar bases de datos del usuario autenticado.

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid-here",
      "name": "my_postgres_db",
      "engine": "PostgreSQL",
      "status": "Active",
      "host": "localhost",
      "port": 5433,
      "createdAt": "2025-11-13T10:00:00Z"
    }
  ]
}
```

---

#### `POST /api/databases`
Crear nueva base de datos.

**Headers:**
```
Authorization: Bearer {token}
```

**Request:**
```json
{
  "name": "my_postgres_db",
  "engine": "PostgreSQL"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Base de datos creada exitosamente",
  "data": {
    "id": "uuid-here",
    "name": "my_postgres_db",
    "engine": "PostgreSQL",
    "credentials": {
      "host": "localhost",
      "port": 5433,
      "username": "db_user_xyz",
      "password": "generated_password_123",
      "database": "my_postgres_db"
    },
    "connectionString": "Host=localhost;Port=5433;Database=my_postgres_db;Username=db_user_xyz;Password=generated_password_123"
  }
}
```

---

#### `DELETE /api/databases/{id}`
Eliminar base de datos.

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Base de datos eliminada exitosamente"
}
```

---

### Planes y Suscripciones

#### `GET /api/plans`
Obtener planes disponibles.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid-here",
      "name": "Free Plan",
      "planType": "Free",
      "price": 0,
      "databaseLimitPerEngine": 2
    },
    {
      "id": "uuid-here",
      "name": "Intermediate Plan",
      "planType": "Intermediate",
      "price": 5000,
      "databaseLimitPerEngine": 5
    },
    {
      "id": "uuid-here",
      "name": "Advanced Plan",
      "planType": "Advanced",
      "price": 10000,
      "databaseLimitPerEngine": 10
    }
  ]
}
```

---

### Health Check

#### `GET /health`
Verificar estado de la API.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-13T10:30:00Z",
  "environment": "Production",
  "version": "1.0.0"
}
```

---

### Documentación Completa

Acceder a Swagger para explorar TODOS los endpoints:

**Producción:** https://service.voyager.andrescortes.dev/swagger  
**Local:** http://localhost:5191/swagger

---

## 💼 Lógica de Negocio

### Sistema de Planes y Cuotas

El sistema valida automáticamente las cuotas de cada usuario antes de permitir la creación de bases de datos. Cada plan tiene límites específicos por motor de base de datos.

### Generación Automática de Credenciales

Las credenciales se generan de forma aleatoria y segura para cada instancia de base de datos, incluyendo:
- Usuario único
- Contraseña segura (16 caracteres)
- Puerto dinámico disponible
- String de conexión completo

### Creación de Contenedores Docker

Cada base de datos se ejecuta en su propio contenedor Docker aislado, garantizando:
- Aislamiento total entre usuarios
- Gestión independiente de recursos
- Facilidad de eliminación y limpieza
- Reinicio automático en caso de fallos

### Sistema de Notificaciones

Notificaciones automáticas vía email y webhooks para:
- Creación de cuenta
- Nueva base de datos (incluye credenciales)
- Eliminación de base de datos
- Cambio de plan
- Errores críticos

### Integración con Mercado Pago

Sistema completo de pagos con:
- Creación de preferencias de pago
- Webhooks para confirmación
- Actualización automática de plan
- Manejo de estados de suscripción

### Auditoría Completa

Registro detallado de todas las operaciones con:
- Timestamp UTC
- Usuario que ejecutó la acción
- Tipo de acción
- Detalles de la operación
- IP del cliente

---

## 🧪 Testing

### Ejecutar Tests

```bash
# Todos los tests
dotnet test

# Tests de una categoría
dotnet test --filter Category=Unit

# Con cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 🔄 CI/CD

El proyecto utiliza GitHub Actions para:
- ✅ Compilación automática
- ✅ Ejecución de tests
- ✅ Análisis de código
- ✅ Construcción de imágenes Docker
- ✅ Despliegue automático a producción

---

## 📊 Monitoreo y Logs

### Serilog Configuration

Los logs se almacenan en:
- **Console:** Para desarrollo y debugging
- **Archivos:** `logs/crudclouddb-YYYY-MM-DD.log`

### Ver Logs en Tiempo Real

```bash
# Logs de la aplicación
tail -f logs/crudclouddb-$(date +%Y-%m-%d).log

# Logs de Docker
docker logs -f pottercloud-api

# Logs de Nginx
docker logs -f voyager-backend-nginx
```

### Alertas de Discord

Errores críticos se notifican automáticamente vía webhook de Discord.

---

## 🤝 Contribución

### Cómo Contribuir

1. **Fork** el repositorio
2. **Crea** una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. **Commit** tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. **Push** a la rama (`git push origin feature/AmazingFeature`)
5. **Abre** un Pull Request

### Estándares de Código

- ✅ Seguir principios SOLID
- ✅ Documentar métodos públicos con XML comments
- ✅ Escribir tests para nuevas funcionalidades
- ✅ Mantener cobertura de tests >80%
- ✅ Usar nombres descriptivos en inglés
- ✅ Seguir convenciones de C# y .NET

### Checklist de PR

- [ ] El código compila sin errores ni warnings
- [ ] Todos los tests pasan
- [ ] Se agregaron tests para nuevas funcionalidades
- [ ] Se actualizó la documentación
- [ ] Se siguieron los estándares de código
- [ ] No hay credenciales hardcodeadas

---

## 👥 Equipo

Este proyecto fue desarrollado por el equipo **Voyager** como proyecto final del bootcamp de desarrollo web en RIWI:

### Desarrolladores

- **Andrés Cortés** - Tech Lead & DevOps
- **Miguel** - Backend Developer & Authentication
- **Brahiam** - Backend Developer & Payments Integration
- **Vanessa** - Backend Developer & Infrastructure

### Agradecimientos

- **RIWI** por la formación y mentoría
- **Clever Cloud** por la inspiración
- **Comunidad .NET** por las herramientas y recursos

---

## 📊 Diagramas

### Casos de Uso
Visualiza todos los casos de uso del sistema organizados por módulos funcionales:
- **[Diagrama de Casos de Uso](https://drive.google.com/file/d/1I3EXjk6hH1IEkvMZj81HPc-8Xj7RZtF4/view?usp=drive_link)**

### Diagrama de Clases
Explora la arquitectura completa del sistema con todas las entidades, servicios y relaciones:
- **[Diagrama de Clases](https://drive.google.com/file/d/1nZDh1Ft-oZjAmSf4CgdqU1SIJqoKLvt0/view?usp=drive_link)**

Estos diagramas complementan la documentación de arquitectura disponible en [ARCHITECTURE.md](./ARCHITECTURE.md).

---

## 📄 Licencia

Este proyecto es software educativo desarrollado como proyecto final. Se permite su uso con fines educativos y de demostración.

---

## 📞 Contacto y Soporte

### Enlaces

- **Frontend:** https://voyager.andrescortes.dev
- **Backend API:** https://service.voyager.andrescortes.dev
- **Documentación API:** https://service.voyager.andrescortes.dev/swagger
- **GitHub:** https://github.com/voyager-riwi/Voyager-cloudDB-Back

### Soporte

Para reportar bugs o solicitar features, por favor abre un issue en GitHub.

---

<div align="center">

**Hecho con ❤️ y ☕ por el equipo Voyager**

✨ *"La magia está en los detalles"* ✨

![PotterCloud](https://img.shields.io/badge/PotterCloud-Production_Ready-success?style=for-the-badge)

</div>




# 🧙‍♂️ PotterCloud - Backend API

<div align="center">

![PotterCloud](https://img.shields.io/badge/PotterCloud-Backend-7B68EE?style=for-the-badge&logo=dotnet)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Nginx](https://img.shields.io/badge/Nginx-009639?style=for-the-badge&logo=nginx&logoColor=white)

**La plataforma mágica para gestionar bases de datos en la nube** ✨

[Backend API](https://service.voyager.andrescortes.dev) • [Frontend](https://voyager.andrescortes.dev)

</div>

---

## 📖 Tabla de Contenidos

- [¿Qué es PotterCloud?](#qué-es-pottercloud)
- [Problema que Resuelve](#problema-que-resuelve)
- [Características Principales](#características-principales)
- [Ventajas Competitivas](#ventajas-competitivas)
- [Seguridad](#seguridad)
- [Arquitectura](#arquitectura)
- [Stack Tecnológico](#stack-tecnológico)
- [Requisitos Previos](#requisitos-previos)
- [Instalación](#instalación)
- [Configuración](#configuración)
- [Despliegue](#despliegue)
- [API Endpoints](#api-endpoints)
- [Lógica de Negocio](#lógica-de-negocio)
- [Testing](#testing)
- [CI/CD](#cicd)
- [Monitoreo y Logs](#monitoreo-y-logs)
- [Contribución](#contribución)
- [Equipo](#equipo)
- [Contacto](#contacto)
- [Licencia](#licencia)

---

## ¿Qué es PotterCloud?

**PotterCloud** es una plataforma SaaS de última generación que democratiza el acceso a bases de datos en la nube. Inspirada en la magia y simplicidad de servicios como Clever Cloud, PotterCloud permite a desarrolladores, startups y empresas crear, gestionar y escalar instancias de bases de datos con un simple click.

Con PotterCloud, no necesitas ser un experto en DevOps para tener bases de datos profesionales en producción. Nuestra plataforma se encarga de toda la infraestructura, seguridad y configuración automáticamente.

### 🎯 Misión

Proporcionar una experiencia de gestión de bases de datos tan simple y mágica como lanzar un hechizo, eliminando la complejidad técnica y permitiendo que los desarrolladores se enfoquen en lo que realmente importa: construir productos increíbles.

---

## Problema que Resuelve

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

## Características Principales

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
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│  ✓ 2 bases de datos por motor                          │
│  ✓ Gestión completa de credenciales                    │
│  ✓ Notificaciones por email                            │
│  ✓ Soporte comunitario                                 │
│  ✓ Sin tarjeta de crédito                              │
│                                                         │
│  💎 PLAN INTERMEDIO - $5.000 COP/mes                   │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│  ✓ 5 bases de datos por motor                          │
│  ✓ Webhooks personalizados                             │
│  ✓ Soporte prioritario                                 │
│  ✓ Métricas avanzadas                                  │
│                                                         │
│  🚀 PLAN AVANZADO - $10.000 COP/mes                    │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│  ✓ 10 bases de datos por motor                         │
│  ✓ API con rate limits extendidos                      │
│  ✓ Soporte 24/7                                        │
│  ✓ Backups automáticos                                 │
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
- Procesamiento de pagos a planes intermedio y/o max
- Webhooks para confirmación de pagos
- Facturación transparente

---

## Ventajas Competitivas

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

## Seguridad

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

## Arquitectura

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

---

## Stack Tecnológico

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
Naming: Snake Case
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

## Requisitos Previos

### Software Requerido

| Software | Versión Mínima | Propósito |
|----------|---------------|-----------|
| .NET SDK | 8.0 | Compilar y ejecutar el proyecto |
| Docker | 24.0+ | Contenedores de aplicación y BD |
| Docker Compose | 2.20+ | Orquestación de servicios |
| PostgreSQL | 15+ | Base de datos principal |
| Git | 2.40+ | Control de versiones |

---

## Instalación

### Opción 1: Desarrollo Local (Sin Docker)

#### 1. Clonar el Repositorio

```bash
git clone https://github.com/voyager-riwi/Voyager-cloudDB-Back.git
cd Voyager-cloudDB-Back
```

#### 2. Configurar PostgreSQL

```bash
psql -U postgres
CREATE DATABASE crud_cloud_db;
\q
```

#### 3. Configurar Variables de Entorno

Crear archivo `.env`:

```bash
DB_HOST=localhost
DB_PORT=5432
DB_NAME=crud_cloud_db
DB_USER=postgres
DB_PASSWORD=tu_password

JWT_SECRET=tu_secreto_jwt_minimo_32_caracteres
JWT_ISSUER=CrudCloudDb.API
JWT_AUDIENCE=CrudCloudDb.Frontend

MERCADOPAGO_ACCESS_TOKEN=TEST-tu-token
MERCADOPAGO_PUBLIC_KEY=TEST-tu-public-key

SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_EMAIL=tu-email@gmail.com
SMTP_PASSWORD=tu-app-password

DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/tu-webhook
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

#### 6. Ejecutar

```bash
cd ../CrudCloudDb.API
dotnet run
```

API disponible en: http://localhost:5191

---

## Configuración

### SSL (Let's Encrypt)

```bash
sudo apt install certbot
sudo certbot certonly --standalone -d service.voyager.andrescortes.dev
sudo certbot certonly --standalone -d voyager.andrescortes.dev
```

---

## Despliegue

### Producción con Docker

```bash
docker build -t pottercloud-backend:latest .

docker run -d \
  --name pottercloud-api \
  --restart unless-stopped \
  -p 5191:5191 \
  --env-file .env \
  -v /var/run/docker.sock:/var/run/docker.sock \
  pottercloud-backend:latest
```

---

## API Endpoints

### Autenticación

#### POST `/api/auth/register`

```json
{
  "email": "usuario@ejemplo.com",
  "password": "Password123!",
  "firstName": "Juan",
  "lastName": "Pérez"
}
```

#### POST `/api/auth/login`

```json
{
  "email": "usuario@ejemplo.com",
  "password": "Password123!"
}
```

### Bases de Datos

#### GET `/api/databases`

Lista todas las bases de datos del usuario.

#### POST `/api/databases`

```json
{
  "name": "my_postgres_db",
  "engine": "PostgreSQL"
}
```

#### DELETE `/api/databases/{id}`

Elimina una base de datos.

### Health Check

#### GET `/health`

Verifica el estado de la API.

**Documentación completa:** https://service.voyager.andrescortes.dev/swagger

---

## Lógica de Negocio

### Sistema de Planes y Cuotas

Validación automática de cuotas antes de crear bases de datos.

### Generación de Credenciales

- Usuario único aleatorio
- Contraseña segura (16 caracteres)
- Puerto dinámico
- String de conexión completo

### Contenedores Docker

Cada base de datos en su propio contenedor aislado.

### Notificaciones

- Emails automáticos
- Webhooks configurables
- Alertas de Discord

---

## Testing

```bash
dotnet test
dotnet test /p:CollectCoverage=true
```

---

## CI/CD

GitHub Actions para:
- ✅ Build automático
- ✅ Tests unitarios
- ✅ Deploy a producción

---

## Monitoreo y Logs

### Ver Logs

```bash
# Aplicación
tail -f logs/crudclouddb-$(date +%Y-%m-%d).log

# Docker
docker logs -f pottercloud-api
```

---

## Contribución

1. Fork el repositorio
2. Crear rama: `git checkout -b feature/nueva-funcionalidad`
3. Commit: `git commit -m "feat: agregar funcionalidad"`
4. Push: `git push origin feature/nueva-funcionalidad`
5. Crear Pull Request

### Convenciones

- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/)
- **Naming:** PascalCase para clases, camelCase para variables
- **Tests:** Escribir tests para nueva funcionalidad

---

## Equipo

Proyecto desarrollado por el equipo **Voyager** en RIWI:

- **Denis Sanchez** - Frontend Developer
- **Miguel Arias** - Backend Developer & Authentication
- **Brahiam Ruiz** - Backend Developer & Payments
- **Vanessa Gomez** - Backend Developer & Infrastructure

---

## Contacto

- **Frontend:** https://voyager.andrescortes.dev
- **Backend API:** https://service.voyager.andrescortes.dev
- **GitHub:** https://github.com/voyager-riwi/Voyager-cloudDB-Back

---

## Licencia

MIT License - Copyright (c) 2025 Voyager Team - RIWI

---

<div align="center">

**Hecho con ❤️ y ☕ por el equipo Voyager**

✨ *"La magia está en los detalles"* ✨

![Status](https://img.shields.io/badge/Status-Production_Ready-success?style=for-the-badge)

</div>


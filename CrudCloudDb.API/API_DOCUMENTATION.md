# 📚 Documentación de API - CloudDB Backend

## 🌐 Base URL
```
https://service.voyager.andrescortes.dev/api
```

## 🔑 Autenticación

La mayoría de los endpoints requieren autenticación JWT. Incluye el token en el header:
```
Authorization: Bearer {tu_token_jwt}
```

---

## 📋 Tabla de Contenidos

1. [Autenticación](#1-autenticación-auth)
2. [Usuarios](#2-usuarios-users)
3. [Bases de Datos](#3-bases-de-datos-databases)
4. [Planes](#4-planes-plans)
5. [Pagos](#5-pagos-payments)
6. [Webhooks](#6-webhooks-webhooks)
7. [Health Check](#7-health-check-health)

---

## 1. Autenticación (`/Auth`)

### 1.1 Registrar Usuario
```http
POST /api/Auth/register
```

**Auth requerida:** ❌ No

**Body:**
```json
{
  "firstName": "Juan",
  "lastName": "Pérez",
  "email": "juan@example.com",
  "password": "Password123!"
}
```

**Validaciones:**
- `firstName`: Requerido, máximo 100 caracteres
- `lastName`: Requerido, máximo 100 caracteres
- `email`: Requerido, formato email válido
- `password`: Requerido, mínimo 8 caracteres

**Respuesta exitosa (200):**
```json
{
  "succeeded": true,
  "message": "Usuario registrado exitosamente.",
  "data": {
    "userId": "3f37fae6-a063-4994-9f67-b63a72fb4a2b",
    "fullName": "Juan Pérez",
    "email": "juan@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

**Respuesta de error (400):**
```json
{
  "succeeded": false,
  "message": "El correo electrónico ya está en uso.",
  "errors": []
}
```

---

### 1.2 Iniciar Sesión
```http
POST /api/Auth/login
```

**Auth requerida:** ❌ No

**Body:**
```json
{
  "email": "juan@example.com",
  "password": "Password123!"
}
```

**Respuesta exitosa (200):**
```json
{
  "succeeded": true,
  "message": "Inicio de sesión exitoso.",
  "data": {
    "userId": "3f37fae6-a063-4994-9f67-b63a72fb4a2b",
    "fullName": "Juan Pérez",
    "email": "juan@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

**Respuesta de error (401):**
```json
{
  "succeeded": false,
  "message": "Credenciales inválidas."
}
```

---

### 1.3 Olvidé mi Contraseña
```http
POST /api/Auth/forgot-password
```

**Auth requerida:** ❌ No

**Body:**
```json
{
  "email": "juan@example.com"
}
```

**Respuesta (200):**
```json
{
  "succeeded": true,
  "message": "Si existe una cuenta con este correo, se ha enviado un código de recuperación."
}
```

**Nota:** Por seguridad, siempre retorna 200 aunque el email no exista.

---

### 1.4 Resetear Contraseña
```http
POST /api/Auth/reset-password
```

**Auth requerida:** ❌ No

**Body:**
```json
{
  "token": "123456",
  "newPassword": "NewPassword123!"
}
```

**Validaciones:**
- `token`: Código de 6 dígitos enviado por email
- `newPassword`: Mínimo 8 caracteres

**Respuesta exitosa (200):**
```json
{
  "succeeded": true,
  "message": "Tu contraseña ha sido restablecida exitosamente."
}
```

**Respuesta de error (400):**
```json
{
  "succeeded": false,
  "message": "El código es inválido o ha expirado."
}
```

---

## 2. Usuarios (`/Users`)

### 2.1 Obtener Mi Perfil
```http
GET /api/Users/profile
```

**Auth requerida:** ✅ Sí

**Respuesta exitosa (200):**
```json
{
  "succeeded": true,
  "data": {
    "id": "3f37fae6-a063-4994-9f67-b63a72fb4a2b",
    "firstName": "Juan",
    "lastName": "Pérez",
    "email": "juan@example.com",
    "currentPlanName": "Free Plan",
    "databaseLimitPerEngine": 2,
    "memberSince": "2025-01-09T12:00:00Z"
  }
}
```

---

### 2.2 Actualizar Mi Perfil
```http
PUT /api/Users/profile
```

**Auth requerida:** ✅ Sí

**Body:**
```json
{
  "firstName": "Juan Carlos",
  "lastName": "Pérez García"
}
```

**Validaciones:**
- `firstName`: Requerido, máximo 100 caracteres
- `lastName`: Requerido, máximo 100 caracteres

**Respuesta exitosa (200):**
```json
{
  "succeeded": true,
  "message": "Perfil actualizado exitosamente."
}
```

---

### 2.3 Cambiar Mi Contraseña
```http
POST /api/Users/change-password
```

**Auth requerida:** ✅ Sí

**Body:**
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!"
}
```

**Validaciones:**
- `currentPassword`: Requerido
- `newPassword`: Requerido, mínimo 8 caracteres

**Respuesta exitosa (200):**
```json
{
  "succeeded": true,
  "message": "Contraseña actualizada exitosamente."
}
```

**Respuesta de error (400):**
```json
{
  "succeeded": false,
  "message": "La contraseña actual es incorrecta."
}
```

---

## 3. Bases de Datos (`/Databases`)

### 3.1 Crear Base de Datos
```http
POST /api/Databases
```

**Auth requerida:** ✅ Sí

**Body:**
```json
{
  "engine": 1
}
```

**Valores de `engine`:**
- `1` = PostgreSQL
- `2` = MySQL
- `3` = MongoDB
- `4` = SQLServer

**Respuesta exitosa (200):**
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "db_random_name_12345",
  "engine": "PostgreSQL",
  "status": "Running",
  "port": 5432,
  "host": "91.98.42.248",
  "username": "user_abc123",
  "connectionString": "Host=91.98.42.248;Port=5432;Database=db_random_name_12345;Username=user_abc123;Password=xyz789",
  "createdAt": "2025-01-09T12:00:00Z",
  "credentialsViewed": false,
  "containerId": "a1b2c3d4e5f6",
  "isRunning": true
}
```

**⚠️ IMPORTANTE:** Las credenciales se envían **UNA SOLA VEZ**. Guárdalas inmediatamente. También se envían por email.

**Respuesta de error (400):**
```json
{
  "success": false,
  "message": "You have reached the maximum number of PostgreSQL databases allowed in your plan (2). You currently have 2 active database(s). To create a new database, you must either:\n1. Deactivate and then wait 30 days for permanent deletion, OR\n2. Upgrade your plan to get more database slots."
}
```

---

### 3.2 Listar Mis Bases de Datos
```http
GET /api/Databases
```

**Auth requerida:** ✅ Sí

**Respuesta exitosa (200):**
```json
[
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "db_postgres_12345",
    "engine": "PostgreSQL",
    "status": "Running",
    "port": 5432,
    "host": "91.98.42.248",
    "username": "user_abc123",
    "connectionString": "Host=91.98.42.248;Port=5432;Database=...",
    "createdAt": "2025-01-09T12:00:00Z",
    "credentialsViewed": true,
    "containerId": "a1b2c3d4e5f6",
    "isRunning": true
  },
  {
    "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "name": "db_mysql_67890",
    "engine": "MySQL",
    "status": "Running",
    "port": 3306,
    "host": "91.98.42.248",
    "username": "user_xyz789",
    "connectionString": "Server=91.98.42.248;Port=3306;Database=...",
    "createdAt": "2025-01-08T15:30:00Z",
    "credentialsViewed": true,
    "containerId": "b2c3d4e5f6a7",
    "isRunning": true
  }
]
```

**Nota:** Solo retorna bases de datos **activas** (no eliminadas).

---

### 3.3 Obtener Detalles de una Base de Datos
```http
GET /api/Databases/{id}
```

**Auth requerida:** ✅ Sí

**Parámetros URL:**
- `id` (GUID): ID de la base de datos

**Ejemplo:**
```http
GET /api/Databases/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**Respuesta exitosa (200):**
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "db_postgres_12345",
  "engine": "PostgreSQL",
  "status": "Running",
  "port": 5432,
  "host": "91.98.42.248",
  "username": "user_abc123",
  "connectionString": "Host=91.98.42.248;Port=5432;Database=...",
  "createdAt": "2025-01-09T12:00:00Z",
  "credentialsViewed": true,
  "containerId": "a1b2c3d4e5f6",
  "isRunning": true
}
```

**Respuesta de error (404):**
```json
{
  "success": false,
  "message": "Database not found"
}
```

**Respuesta de error (403):**
```json
{
  "success": false,
  "message": "You don't have access to this database"
}
```

---

### 3.4 Eliminar Base de Datos (Soft Delete)
```http
DELETE /api/Databases/{id}
```

**Auth requerida:** ✅ Sí

**Parámetros URL:**
- `id` (GUID): ID de la base de datos

**⚠️ IMPORTANTE:** 
- Soft delete con período de gracia de **30 días**
- Durante 30 días, puedes restaurar la base de datos
- Después de 30 días, se elimina permanentemente
- El acceso se bloquea inmediatamente (password cambiado)

**Respuesta exitosa (200):**
```json
{
  "success": true,
  "message": "Database deleted successfully"
}
```

**Respuesta de error (404):**
```json
{
  "success": false,
  "message": "Database not found"
}
```

---

### 3.5 Resetear Contraseña de Base de Datos
```http
POST /api/Databases/{id}/reset-password
```

**Auth requerida:** ✅ Sí

**Parámetros URL:**
- `id` (GUID): ID de la base de datos

**Respuesta exitosa (200):**
```json
{
  "success": true,
  "message": "Password reset successfully. Check your email for the new password."
}
```

**Nota:** La nueva contraseña se envía por **email**. No se retorna en la respuesta por seguridad.

---

### 3.6 Restaurar Base de Datos Eliminada
```http
POST /api/Databases/{id}/restore
```

**Auth requerida:** ✅ Sí

**Parámetros URL:**
- `id` (GUID): ID de la base de datos eliminada

**⚠️ Condiciones:**
- Solo funciona si la base de datos fue eliminada hace **menos de 30 días**
- Genera una **nueva contraseña** (se envía por email)
- La base de datos vuelve al estado `Running`

**Respuesta exitosa (200):**
```json
{
  "success": true,
  "message": "Database restored successfully"
}
```

**Respuesta de error (400):**
```json
{
  "success": false,
  "message": "Cannot restore database: grace period expired (more than 30 days). The database will be permanently deleted soon."
}
```

**Respuesta de error (404):**
```json
{
  "success": false,
  "message": "Database not found or already active"
}
```

---

## 4. Planes (`/Plans`)

### 4.1 Obtener Todos los Planes
```http
GET /api/Plans
```

**Auth requerida:** ❌ No

**Respuesta exitosa (200):**
```json
[
  {
    "id": "b1b108e5-fcbc-4a91-8967-b545ff937016",
    "name": "Free Plan",
    "planType": 1,
    "price": 0.00,
    "databaseLimitPerEngine": 2
  },
  {
    "id": "0b2a601a-1269-4818-9161-2797f54a7100",
    "name": "Intermediate Plan",
    "planType": 2,
    "price": 5000.00,
    "databaseLimitPerEngine": 5
  },
  {
    "id": "7be9fe44-7454-4055-8a5f-eff194532a2e",
    "name": "Advanced Plan",
    "planType": 3,
    "price": 10000.00,
    "databaseLimitPerEngine": 10
  }
]
```

**Valores de `planType`:**
- `1` = Free
- `2` = Intermediate
- `3` = Advanced

---

### 4.2 Obtener Plan por ID
```http
GET /api/Plans/{id}
```

**Auth requerida:** ❌ No

**Parámetros URL:**
- `id` (GUID): ID del plan

**Ejemplo:**
```http
GET /api/Plans/b1b108e5-fcbc-4a91-8967-b545ff937016
```

**Respuesta exitosa (200):**
```json
{
  "id": "b1b108e5-fcbc-4a91-8967-b545ff937016",
  "name": "Free Plan",
  "planType": 1,
  "price": 0.00,
  "databaseLimitPerEngine": 2
}
```

**Respuesta de error (404):**
```json
{
  "message": "Plan no encontrado"
}
```

---

## 5. Pagos (`/Payments`)

### 5.1 Crear Preferencia de Pago (MercadoPago)
```http
POST /api/Payments/create-preference
```

**Auth requerida:** ✅ Sí

**Body:**
```json
{
  "planId": "0b2a601a-1269-4818-9161-2797f54a7100"
}
```

**Respuesta exitosa (200):**
```json
{
  "succeeded": true,
  "message": "Preferencia creada exitosamente.",
  "data": {
    "preferenceId": "1234567890-abcd1234-efgh5678-ijkl9012",
    "initPoint": "https://www.mercadopago.com/mco/checkout/start?pref_id=..."
  }
}
```

**Uso:**
1. Llamas a este endpoint
2. Obtienes el `initPoint`
3. Rediriges al usuario a esa URL
4. El usuario completa el pago en MercadoPago
5. MercadoPago envía webhook automáticamente
6. El plan del usuario se actualiza automáticamente

**Respuesta de error (400):**
```json
{
  "succeeded": false,
  "message": "El plan seleccionado no existe."
}
```

---

### 5.2 Verificar Configuración de MercadoPago (Debug)
```http
GET /api/Payments/config-test
```

**Auth requerida:** ✅ Sí

**Respuesta (200):**
```json
{
  "configured": true,
  "mode": "PRODUCTION",
  "accessTokenLength": 105,
  "accessTokenPrefix": "APP_USR-12345678901...",
  "publicKeyConfigured": true,
  "notificationUrl": "https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago",
  "timestamp": "2025-01-09T12:00:00Z"
}
```

**Nota:** Útil para debugging. Verifica que MercadoPago esté correctamente configurado.

---

### 5.3 Consultar Estado de Orden de MercadoPago (Debug)
```http
GET /api/Payments/debug-order/{orderId}
```

**Auth requerida:** ✅ Sí

**Parámetros URL:**
- `orderId` (long): ID de la orden de MercadoPago

**Ejemplo:**
```http
GET /api/Payments/debug-order/35452151330
```

**Respuesta exitosa (200):**
```json
{
  "orderId": 35452151330,
  "status": "closed",
  "orderStatus": "paid",
  "externalReference": "user:3f37fae6-a063-4994-9f67-b63a72fb4a2b;plan:0b2a601a-1269-4818-9161-2797f54a7100",
  "totalAmount": 5000.00,
  "paidAmount": 5000.00,
  "refundedAmount": 0.00,
  "payments": [
    {
      "id": 123456789,
      "status": "approved",
      "statusDetail": "accredited",
      "transactionAmount": 5000.00,
      "currencyId": "COP"
    }
  ],
  "paymentsCount": 1,
  "dateCreated": "2025-01-09T12:00:00Z",
  "lastUpdated": "2025-01-09T12:05:00Z",
  "timestamp": "2025-01-09T12:10:00Z"
}
```

**Nota:** Útil para debugging. Consulta el estado real de una orden en MercadoPago.

---

## 6. Webhooks (`/Webhooks`)

### 6.1 Webhook de MercadoPago
```http
POST /api/Webhooks/mercadopago
```

**Auth requerida:** ❌ No (pero debe venir de MercadoPago)

**Body (ejemplo):**
```json
{
  "topic": "merchant_order",
  "resource": "https://api.mercadolibre.com/merchant_orders/35452151330",
  "action": "payment.updated"
}
```

**Respuesta (200):**
```
OK
```

**Nota:** Este endpoint es llamado **automáticamente** por MercadoPago cuando:
- Se crea un pago
- Se actualiza un pago
- Se completa un pago

**Proceso automático:**
1. MercadoPago envía webhook
2. Backend consulta estado de la orden
3. Si el pago está `paid`, actualiza el plan del usuario
4. Envía email de confirmación al usuario

---

## 7. Health Check (`/Health`)

### 7.1 Verificar Estado del Servidor
```http
GET /api/Health
```

**Auth requerida:** ❌ No

**Respuesta (200):**
```json
{
  "status": "healthy",
  "timestamp": "2025-01-09T12:00:00Z",
  "service": "CrudCloudDb API"
}
```

**Uso:** Útil para monitoreo y verificar que el servidor está funcionando.

---

### 7.2 Probar Manejo de Errores (Debug)
```http
GET /api/Health/error
```

**Auth requerida:** ❌ No

**Respuesta (500):**
```json
{
  "type": "InvalidOperationException",
  "message": "Este es un error de prueba para verificar el middleware.",
  "stackTrace": "..."
}
```

**Nota:** Endpoint de prueba para verificar que el middleware de errores funciona correctamente.

---

## 📧 Notificaciones por Email

El sistema envía emails automáticamente en estos casos:

### 1. **Cuenta Creada** (`AccountCreated`)
- Cuando: Al registrarse
- Contenido: Bienvenida y detalles de la cuenta

### 2. **Base de Datos Creada** (`DatabaseCreated`)
- Cuando: Al crear una base de datos
- Contenido: Credenciales completas (usuario, password, connection string)
- ⚠️ **IMPORTANTE:** Las credenciales solo se muestran UNA VEZ en la API

### 3. **Base de Datos Eliminada** (`DatabaseDeleted`)
- Cuando: Al eliminar una base de datos
- Contenido: Notificación con período de gracia de 30 días

### 4. **Password Reseteado** (`PasswordReset`)
- Cuando: Al resetear password de BD o restaurar BD eliminada
- Contenido: Nuevas credenciales

### 5. **Plan Cambiado** (`PlanChanged`)
- Cuando: Al comprar/cambiar de plan via MercadoPago
- Contenido: Detalles del nuevo plan

### 6. **Recuperación de Contraseña** (`AccountPasswordReset`)
- Cuando: Al solicitar "forgot password"
- Contenido: Código de 6 dígitos (válido 15 minutos)

---

## 🔒 Códigos de Estado HTTP

| Código | Significado | Cuándo Aparece |
|--------|-------------|----------------|
| `200` | OK | Operación exitosa |
| `400` | Bad Request | Validación fallida o regla de negocio violada |
| `401` | Unauthorized | Token JWT faltante o inválido |
| `403` | Forbidden | Usuario sin permisos para el recurso |
| `404` | Not Found | Recurso no encontrado |
| `500` | Internal Server Error | Error del servidor (se notifica al equipo) |

---

## 🎯 Flujos Completos

### Flujo 1: Registro e Inicio de Sesión
```
1. POST /api/Auth/register
   → Recibe token JWT
   → Se crea con plan Free por defecto

2. (Opcional) POST /api/Auth/login
   → Obtiene nuevo token JWT

3. GET /api/Users/profile
   → Ve su información con plan actual
```

### Flujo 2: Crear y Gestionar Base de Datos
```
1. GET /api/Plans
   → Ve límites de su plan actual

2. POST /api/Databases
   → Crea BD con credenciales
   → ⚠️ GUARDAR CREDENCIALES INMEDIATAMENTE
   → También llegan por email

3. GET /api/Databases
   → Lista sus BDs

4. GET /api/Databases/{id}
   → Ve detalles específicos

5. (Opcional) POST /api/Databases/{id}/reset-password
   → Nueva password por email

6. (Opcional) DELETE /api/Databases/{id}
   → Soft delete (30 días de gracia)

7. (Opcional) POST /api/Databases/{id}/restore
   → Restaura BD eliminada (si < 30 días)
```

### Flujo 3: Cambiar de Plan
```
1. GET /api/Plans
   → Ve planes disponibles

2. POST /api/Payments/create-preference
   → Obtiene initPoint de MercadoPago

3. Redirigir usuario a initPoint
   → Usuario paga en MercadoPago

4. (Automático) Webhook POST /api/Webhooks/mercadopago
   → Plan se actualiza automáticamente
   → Usuario recibe email de confirmación

5. GET /api/Users/profile
   → Confirma nuevo plan
```

---

## 🚨 Límites y Restricciones

### Límites por Plan

| Plan | Precio (COP) | BD por Motor | Total Máximo |
|------|--------------|--------------|--------------|
| Free | $0 | 2 | 8 (2×4 motores) |
| Intermediate | $5,000 | 5 | 20 (5×4 motores) |
| Advanced | $10,000 | 10 | 40 (10×4 motores) |

### Restricciones Importantes

1. **Límite por motor:** Si tienes plan Free, puedes tener:
   - 2 PostgreSQL + 2 MySQL + 2 MongoDB + 2 SQLServer = 8 total

2. **BDs eliminadas cuentan:** Durante 30 días, las BDs eliminadas ocupan tu cuota

3. **Credenciales una sola vez:** Al crear BD, guarda las credenciales. No se mostrarán de nuevo en la API (solo por email)

4. **Período de gracia:** 30 días para restaurar BDs eliminadas

5. **Token JWT:** Expira en 24 horas (1440 minutos)

6. **Código de recuperación:** Expira en 15 minutos

---

## 🛠️ Ejemplos de Integración

### JavaScript/TypeScript (Axios)

```typescript
import axios from 'axios';

const API_URL = 'https://service.voyager.andrescortes.dev/api';

// Cliente con token
const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Interceptor para agregar token
api.interceptors.request.use(config => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Ejemplo: Registro
async function register(data: {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}) {
  const response = await api.post('/Auth/register', data);
  if (response.data.succeeded) {
    localStorage.setItem('token', response.data.data.token);
  }
  return response.data;
}

// Ejemplo: Crear BD
async function createDatabase(engine: number) {
  const response = await api.post('/Databases', { engine });
  return response.data;
}

// Ejemplo: Listar BDs
async function getMyDatabases() {
  const response = await api.get('/Databases');
  return response.data;
}
```

### Python (Requests)

```python
import requests

API_URL = "https://service.voyager.andrescortes.dev/api"

class CloudDBClient:
    def __init__(self, token=None):
        self.token = token
        self.headers = {
            "Content-Type": "application/json"
        }
        if token:
            self.headers["Authorization"] = f"Bearer {token}"
    
    def register(self, first_name, last_name, email, password):
        response = requests.post(
            f"{API_URL}/Auth/register",
            json={
                "firstName": first_name,
                "lastName": last_name,
                "email": email,
                "password": password
            },
            headers=self.headers
        )
        data = response.json()
        if data.get("succeeded"):
            self.token = data["data"]["token"]
            self.headers["Authorization"] = f"Bearer {self.token}"
        return data
    
    def create_database(self, engine):
        response = requests.post(
            f"{API_URL}/Databases",
            json={"engine": engine},
            headers=self.headers
        )
        return response.json()
    
    def get_my_databases(self):
        response = requests.get(
            f"{API_URL}/Databases",
            headers=self.headers
        )
        return response.json()

# Uso
client = CloudDBClient()
client.register("Juan", "Pérez", "juan@example.com", "Pass123!")
db = client.create_database(1)  # PostgreSQL
print(f"BD creada: {db['name']}")
```

---

## 📝 Notas Finales

1. **Todos los endpoints con [Authorize]** requieren token JWT en el header
2. **Las fechas** están en formato UTC (ISO 8601)
3. **Los precios** están en pesos colombianos (COP)
4. **Los GUIDs** son en formato: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
5. **Los emails** se envían automáticamente, no requieren acción del frontend
6. **Las credenciales de BD** se muestran UNA SOLA VEZ en la respuesta de creación

---

**Última actualización:** 2025-01-09  
**Versión de API:** v1  
**Documentación generada automáticamente desde los controllers**


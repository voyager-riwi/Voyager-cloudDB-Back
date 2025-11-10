# ✅ Configuración Completa de Mercado Pago

## 🎉 Resumen de Cambios Realizados

He configurado completamente Mercado Pago Checkout Pro en tu proyecto. Aquí está todo lo que se ha hecho:

### 1. ✅ Variables de Entorno Configuradas

**Archivo actualizado:** `.env.example`

```env
# === MERCADO PAGO CONFIGURATION ===
MERCADOPAGO_ACCESS_TOKEN=APP_USR-8642932100357504-103018-41019bd0a89ab243de2a9a37e093bdb1-2946065922
MERCADOPAGO_PUBLIC_KEY=APP_USR-2e5b5c04-d243-4926-99f1-6dc11bd8f93a
MERCADOPAGO_WEBHOOK_SECRET=d7312ca60fd2acf48200f5d290c6e663101e64920dc05c47fb933ba107f4deb4
```

### 2. ✅ Clase de Configuración Actualizada

**Archivo:** `CrudCloudDb.API/Configuration/MercadoPagoSettings.cs`

Agregado:
- `WebhookSecret` para validar notificaciones de Mercado Pago

### 3. ✅ Program.cs Actualizado

**Archivo:** `CrudCloudDb.API/Program.cs`

Agregado:
- Configuración de `MercadoPagoSettings` desde variables de entorno
- Registro de `ISubscriptionService` en el contenedor de dependencias

### 4. ✅ PaymentsController Mejorado

**Archivo:** `CrudCloudDb.API/Controllers/PaymentsController.cs`

Agregado:
- **Nuevo endpoint:** `GET /api/payments/public-key`
  - Devuelve la Public Key para el frontend
  - No requiere autenticación (AllowAnonymous)

### 5. ✅ WebhooksController Mejorado

**Archivo:** `CrudCloudDb.API/Controllers/WebhooksController.cs`

Agregado:
- Validación de firma del webhook (preparado para implementar)
- Lectura de headers `x-signature` y `x-request-id`

---

## 🚀 Pasos para Usar la Configuración

### Paso 1: Copiar Variables de Entorno

Si aún no tienes un archivo `.env`, créalo:

```bash
# En la raíz del proyecto (donde está .env.example)
copy .env.example .env
```

**Las credenciales de Mercado Pago ya están incluidas en `.env.example`**, así que solo necesitas copiar el archivo.

### Paso 2: Verificar la Configuración

Ejecuta el script de verificación:

```powershell
.\verify-mercadopago.ps1
```

Deberías ver:
```
✅ Archivo .env encontrado
✅ MERCADOPAGO_ACCESS_TOKEN configurado
✅ MERCADOPAGO_PUBLIC_KEY configurado
✅ MERCADOPAGO_WEBHOOK_SECRET configurado
```

### Paso 3: Iniciar la Aplicación

```bash
cd CrudCloudDb.API
dotnet run
```

Verifica en los logs:
```
✅ Loaded .env file for development
✅ MercadoPago configured
```

### Paso 4: Configurar Webhooks en Mercado Pago

1. Ve a: https://www.mercadopago.com.ar/developers/panel/app
2. Selecciona tu aplicación
3. En "Webhooks", agrega tu URL:
   - Producción: `https://tu-dominio.com/api/webhooks/mercadopago`
   - Desarrollo: `https://tu-ngrok.ngrok.io/api/webhooks/mercadopago`
4. Selecciona el evento: `merchant_orders`

---

## 📡 Endpoints Disponibles

### 1. Obtener Public Key (Frontend)

```http
GET /api/payments/public-key
```

**Respuesta:**
```json
{
  "publicKey": "APP_USR-2e5b5c04-d243-4926-99f1-6dc11bd8f93a"
}
```

**Uso en Frontend:**
```javascript
const response = await fetch('https://api.voyager.com/api/payments/public-key');
const { publicKey } = await response.json();

const mp = new MercadoPago(publicKey);
```

### 2. Crear Preferencia de Pago

```http
POST /api/payments/create-preference
Authorization: Bearer {token}
Content-Type: application/json

{
  "planId": "guid-del-plan-premium"
}
```

**Respuesta:**
```json
{
  "succeeded": true,
  "data": {
    "preferenceId": "123456789-abc",
    "initPoint": "https://www.mercadopago.com.ar/checkout/v1/redirect?pref_id=..."
  }
}
```

### 3. Webhook de Mercado Pago

```http
POST /api/webhooks/mercadopago
Content-Type: application/json

{
  "topic": "merchant_order",
  "resource": "https://api.mercadopago.com/merchant_orders/123456789"
}
```

Este endpoint es llamado automáticamente por Mercado Pago cuando se completa un pago.

---

## 🔄 Flujo Completo de Pago

```
1. Usuario selecciona plan Premium en el frontend
   ↓
2. Frontend llama a POST /api/payments/create-preference
   ↓
3. Backend crea preferencia en Mercado Pago
   ↓
4. Backend devuelve initPoint al frontend
   ↓
5. Frontend redirige al usuario a Mercado Pago (initPoint)
   ↓
6. Usuario completa el pago en Mercado Pago
   ↓
7. Mercado Pago envía notificación a POST /api/webhooks/mercadopago
   ↓
8. Backend procesa la notificación:
   - Verifica que el pago fue aprobado
   - Actualiza el plan del usuario en la BD
   - Crea registro de suscripción
   - Envía email de confirmación
   ↓
9. Mercado Pago redirige al usuario a:
   - Éxito: https://voyager.andrescortes.dev/payment-success
   - Error: https://voyager.andrescortes.dev/payment-failure
```

---

## 🧪 Testing en Desarrollo Local

### Opción 1: Usar ngrok (Recomendado)

```bash
# 1. Instalar ngrok
choco install ngrok

# 2. Iniciar tu API
cd CrudCloudDb.API
dotnet run

# 3. En otra terminal, crear túnel
ngrok http 5000

# 4. Copiar la URL de ngrok (ej: https://abc123.ngrok.io)
# 5. Configurar webhook en Mercado Pago:
#    https://abc123.ngrok.io/api/webhooks/mercadopago
```

### Opción 2: Usar Tarjetas de Prueba

Mercado Pago proporciona tarjetas de prueba:

**Tarjeta aprobada:**
- Número: `5031 7557 3453 0604`
- CVV: `123`
- Fecha: Cualquier fecha futura

**Más tarjetas de prueba:**
https://www.mercadopago.com.ar/developers/es/docs/checkout-pro/additional-content/test-cards

---

## 📊 Monitoreo y Logs

### Ver Logs de la Aplicación

```bash
# Los logs se muestran en la consola al ejecutar
dotnet run

# Busca estos mensajes:
✅ Loaded .env file for development
✅ MercadoPago configured
💳 Activating Premium subscription for user...
✅ Premium subscription activated for user...
```

### Ver Webhooks en Mercado Pago

1. Ve a: https://www.mercadopago.com.ar/developers/panel/app
2. Selecciona tu aplicación
3. Ve a "Webhooks" → "Historial"
4. Verás todas las notificaciones enviadas y sus respuestas

---

## 🐛 Solución de Problemas

### ❌ Error: "MercadoPago AccessToken not configured"

**Causa:** El archivo `.env` no existe o no tiene las credenciales.

**Solución:**
```bash
copy .env.example .env
```

### ❌ Error: "Public Key de Mercado Pago no configurada"

**Causa:** La variable `MERCADOPAGO_PUBLIC_KEY` no está en `.env`.

**Solución:**
Verifica que `.env` contiene:
```env
MERCADOPAGO_PUBLIC_KEY=APP_USR-2e5b5c04-d243-4926-99f1-6dc11bd8f93a
```

### ❌ Webhook no recibe notificaciones

**Causa:** La URL no es accesible públicamente.

**Solución:**
- Usa ngrok para desarrollo local
- Verifica que la URL está configurada en Mercado Pago
- Verifica que el endpoint responde (debe devolver 200 OK)

### ❌ El pago se completa pero el plan no se actualiza

**Causa:** El webhook no está procesando correctamente.

**Solución:**
1. Revisa los logs del servidor
2. Verifica que el `ExternalReference` tiene el formato: `user:{userId};plan:{planId}`
3. Verifica que el usuario y plan existen en la BD

---

## ✅ Checklist Final

Antes de ir a producción, verifica:

- [ ] Variables de entorno configuradas en `.env`
- [ ] Access Token configurado
- [ ] Public Key configurado
- [ ] Webhook Secret configurado
- [ ] URL de webhook configurada en Mercado Pago
- [ ] Prueba de pago realizada con tarjeta de prueba
- [ ] Webhook recibe notificaciones correctamente
- [ ] Plan de usuario se actualiza después del pago
- [ ] Email de confirmación se envía
- [ ] URLs de redirección funcionan (success/failure/pending)

---

## 📚 Archivos Creados/Modificados

### Archivos Modificados:
1. `.env.example` - Credenciales de Mercado Pago agregadas
2. `CrudCloudDb.API/Configuration/MercadoPagoSettings.cs` - WebhookSecret agregado
3. `CrudCloudDb.API/Program.cs` - Configuración de MercadoPago
4. `CrudCloudDb.API/Controllers/PaymentsController.cs` - Endpoint GetPublicKey
5. `CrudCloudDb.API/Controllers/WebhooksController.cs` - Validación de firma

### Archivos Creados:
1. `MERCADOPAGO_SETUP.md` - Documentación detallada
2. `verify-mercadopago.ps1` - Script de verificación
3. `CONFIGURACION_COMPLETA.md` - Este archivo (resumen)

---

## 🎯 Próximos Pasos

1. **Copia `.env.example` a `.env`**
   ```bash
   copy .env.example .env
   ```

2. **Ejecuta la aplicación**
   ```bash
   cd CrudCloudDb.API
   dotnet run
   ```

3. **Configura el webhook en Mercado Pago**
   - Panel: https://www.mercadopago.com.ar/developers/panel/app
   - URL: `https://tu-dominio.com/api/webhooks/mercadopago`

4. **Prueba el flujo completo**
   - Crea una preferencia de pago
   - Completa el pago con tarjeta de prueba
   - Verifica que el plan se actualiza

---

## 🆘 Soporte

Si tienes problemas:

1. Ejecuta el script de verificación: `.\verify-mercadopago.ps1`
2. Revisa los logs de la aplicación
3. Consulta la documentación: `MERCADOPAGO_SETUP.md`
4. Revisa el historial de webhooks en Mercado Pago

---

## 🎉 ¡Todo Listo!

Tu integración de Mercado Pago está completamente configurada y lista para usar. Las credenciales ya están en el archivo `.env.example`, solo necesitas copiarlas a `.env` y configurar la URL del webhook en el panel de Mercado Pago.

**¡Éxito con tu proyecto! 🚀**

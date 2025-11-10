# 🛒 Configuración de Mercado Pago - Checkout Pro

Este documento explica cómo configurar Mercado Pago Checkout Pro en el proyecto Voyager CloudDB.

## 📋 Credenciales Necesarias

Ya tienes las siguientes credenciales configuradas en el archivo `.env.example`:

```env
# === MERCADO PAGO CONFIGURATION ===
# Access Token (para crear preferencias de pago)
MERCADOPAGO_ACCESS_TOKEN=APP_USR-8642932100357504-103018-41019bd0a89ab243de2a9a37e093bdb1-2946065922

# Public Key (para el frontend)
MERCADOPAGO_PUBLIC_KEY=APP_USR-2e5b5c04-d243-4926-99f1-6dc11bd8f93a

# Webhook Secret (para validar notificaciones)
MERCADOPAGO_WEBHOOK_SECRET=d7312ca60fd2acf48200f5d290c6e663101e64920dc05c47fb933ba107f4deb4
```

## 🔧 Pasos de Configuración

### 1. Configurar Variables de Entorno

Copia el archivo `.env.example` a `.env` (si no lo has hecho):

```bash
cp .env.example .env
```

Las credenciales de Mercado Pago ya están incluidas en el `.env.example`, así que solo necesitas copiar el archivo.

### 2. Configurar Webhooks en Mercado Pago

Para que Mercado Pago notifique a tu aplicación cuando se complete un pago:

1. **Accede al Panel de Mercado Pago:**
   - Ve a: https://www.mercadopago.com.ar/developers/panel/app
   - Selecciona tu aplicación

2. **Configura la URL del Webhook:**
   - En la sección "Webhooks", agrega la siguiente URL:
   ```
   https://tu-dominio.com/api/webhooks/mercadopago
   ```
   - Para desarrollo local con ngrok:
   ```
   https://tu-subdominio.ngrok.io/api/webhooks/mercadopago
   ```

3. **Selecciona los Eventos:**
   - Marca: `merchant_orders` (Órdenes de comercio)
   - Esto es lo que el sistema espera recibir

4. **Guarda la Configuración**

### 3. Verificar la Configuración en el Código

El sistema ya está configurado para:

✅ **Leer las credenciales desde variables de entorno**
- `MERCADOPAGO_ACCESS_TOKEN` - Para crear preferencias de pago
- `MERCADOPAGO_PUBLIC_KEY` - Para el frontend
- `MERCADOPAGO_WEBHOOK_SECRET` - Para validar notificaciones

✅ **Configurar MercadoPago SDK automáticamente**
- Se configura en `Program.cs` al iniciar la aplicación

✅ **Endpoint para obtener la Public Key**
- `GET /api/payments/public-key` - Devuelve la Public Key para el frontend

✅ **Endpoint para recibir notificaciones**
- `POST /api/webhooks/mercadopago` - Recibe notificaciones de Mercado Pago

## 🎯 Flujo de Pago

### 1. Crear Preferencia de Pago (Frontend → Backend)

```http
POST /api/payments/create-preference
Authorization: Bearer {token}
Content-Type: application/json

{
  "planId": "guid-del-plan"
}
```

**Respuesta:**
```json
{
  "succeeded": true,
  "data": {
    "preferenceId": "123456789-abc-def",
    "initPoint": "https://www.mercadopago.com.ar/checkout/v1/redirect?pref_id=..."
  }
}
```

### 2. Redirigir al Usuario (Frontend)

```javascript
// Obtener la Public Key
const response = await fetch('/api/payments/public-key');
const { publicKey } = await response.json();

// Inicializar Mercado Pago
const mp = new MercadoPago(publicKey);

// Crear preferencia y abrir checkout
const preference = await createPreference(planId);
mp.checkout({
  preference: {
    id: preference.preferenceId
  }
});
```

### 3. Procesar Pago (Mercado Pago → Backend)

Cuando el usuario completa el pago:

1. **Mercado Pago envía notificación al webhook:**
   ```
   POST /api/webhooks/mercadopago
   ```

2. **El sistema procesa la notificación:**
   - Verifica que el pago fue aprobado
   - Actualiza el plan del usuario
   - Crea registro de suscripción
   - Envía email de confirmación

3. **El usuario es redirigido:**
   - Éxito: `https://voyager.andrescortes.dev/payment-success`
   - Error: `https://voyager.andrescortes.dev/payment-failure`
   - Pendiente: `https://voyager.andrescortes.dev/payment-pending`

## 🔍 Testing Local con ngrok

Para probar webhooks en desarrollo local:

1. **Instala ngrok:**
   ```bash
   choco install ngrok
   ```

2. **Inicia tu aplicación:**
   ```bash
   dotnet run --project CrudCloudDb.API
   ```

3. **Crea un túnel ngrok:**
   ```bash
   ngrok http 5000
   ```

4. **Configura el webhook en Mercado Pago:**
   - Usa la URL de ngrok: `https://abc123.ngrok.io/api/webhooks/mercadopago`

5. **Realiza un pago de prueba**

## 📊 Monitoreo

### Ver Logs de Webhooks

Los logs se guardan automáticamente con Serilog. Busca:

```
✅ MercadoPago configured
💳 Activating Premium subscription for user...
✅ Premium subscription activated for user...
```

### Verificar Configuración

```bash
# Verificar que las variables están cargadas
dotnet run --project CrudCloudDb.API

# Deberías ver en los logs:
# ✅ Loaded .env file for development
# ✅ MercadoPago configured
```

## 🚨 Solución de Problemas

### Error: "MercadoPago AccessToken not configured"

**Causa:** Las variables de entorno no se están cargando.

**Solución:**
1. Verifica que el archivo `.env` existe en la raíz del proyecto
2. Verifica que contiene las credenciales de Mercado Pago
3. Reinicia la aplicación

### Error: "Public Key de Mercado Pago no configurada"

**Causa:** La variable `MERCADOPAGO_PUBLIC_KEY` no está en el `.env`

**Solución:**
```env
MERCADOPAGO_PUBLIC_KEY=APP_USR-2e5b5c04-d243-4926-99f1-6dc11bd8f93a
```

### Webhook no recibe notificaciones

**Causa:** La URL del webhook no es accesible públicamente.

**Solución:**
1. Usa ngrok para desarrollo local
2. Verifica que la URL está configurada en Mercado Pago
3. Verifica que el endpoint `/api/webhooks/mercadopago` está activo

### El pago se completa pero el plan no se actualiza

**Causa:** El webhook no está procesando correctamente la notificación.

**Solución:**
1. Revisa los logs del servidor
2. Verifica que el `ExternalReference` tiene el formato correcto: `user:{userId};plan:{planId}`
3. Verifica que el usuario y el plan existen en la base de datos

## 📚 Recursos Adicionales

- [Documentación de Mercado Pago - Checkout Pro](https://www.mercadopago.com.ar/developers/es/docs/checkout-pro/landing)
- [Documentación de Webhooks](https://www.mercadopago.com.ar/developers/es/docs/your-integrations/notifications/webhooks)
- [SDK de Mercado Pago para .NET](https://github.com/mercadopago/sdk-dotnet)

## ✅ Checklist de Configuración

- [x] Variables de entorno configuradas en `.env`
- [x] Access Token configurado
- [x] Public Key configurado
- [x] Webhook Secret configurado
- [ ] URL de webhook configurada en Mercado Pago
- [ ] Prueba de pago realizada
- [ ] Webhook recibe notificaciones correctamente
- [ ] Plan de usuario se actualiza después del pago

## 🎉 ¡Listo!

Tu integración de Mercado Pago está configurada. Ahora puedes:

1. Crear preferencias de pago desde el frontend
2. Procesar pagos con Checkout Pro
3. Recibir notificaciones de pago completado
4. Actualizar automáticamente el plan del usuario

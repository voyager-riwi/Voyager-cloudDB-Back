# 📍 Dónde Configurar la URL del Webhook de Mercado Pago

## 🎯 Resumen Rápido

La URL del webhook **NO se configura en tu código**, se configura en el **Panel de Mercado Pago**.

---

## 🔧 Pasos para Configurar el Webhook

### 1. Accede al Panel de Mercado Pago

Ve a: **https://www.mercadopago.com.ar/developers/panel/app**

O si estás en Colombia: **https://www.mercadopago.com.co/developers/panel/app**

### 2. Selecciona tu Aplicación

- Inicia sesión con tu cuenta de Mercado Pago
- Verás una lista de tus aplicaciones
- Selecciona la aplicación que estás usando (la que tiene las credenciales que configuraste)

### 3. Ve a la Sección "Webhooks"

En el menú lateral, busca y haz clic en **"Webhooks"** o **"Notificaciones"**

### 4. Agrega la URL del Webhook

Dependiendo de tu entorno:

#### 🌐 **Producción** (Cuando tu API esté en un servidor)

```
https://tu-dominio.com/api/webhooks/mercadopago
```

Ejemplo:
```
https://voyager.andrescortes.dev/api/webhooks/mercadopago
```

#### 💻 **Desarrollo Local** (Usando ngrok)

Si estás probando en tu computadora local, necesitas usar **ngrok**:

1. **Instala ngrok:**
   ```bash
   choco install ngrok
   ```

2. **Inicia tu API:**
   ```bash
   cd CrudCloudDb.API
   dotnet run
   ```
   (Supongamos que corre en el puerto 5000)

3. **Crea un túnel con ngrok:**
   ```bash
   ngrok http 5000
   ```

4. **Copia la URL de ngrok:**
   ```
   Forwarding: https://abc123.ngrok.io -> http://localhost:5000
   ```

5. **Usa esta URL en Mercado Pago:**
   ```
   https://abc123.ngrok.io/api/webhooks/mercadopago
   ```

### 5. Selecciona los Eventos

Marca la casilla de:
- ✅ **`merchant_orders`** (Órdenes de comercio)

Este es el evento que tu aplicación espera recibir.

### 6. Guarda la Configuración

Haz clic en **"Guardar"** o **"Crear"**

---

## 🔍 Verificar que Funciona

### Opción 1: Ver el Historial de Webhooks

1. En el panel de Mercado Pago, ve a **Webhooks → Historial**
2. Realiza un pago de prueba
3. Verás las notificaciones enviadas y sus respuestas

### Opción 2: Ver los Logs de tu Aplicación

Cuando Mercado Pago envíe una notificación, verás en los logs:

```
[Information] Notificación de Webhook recibida de Mercado Pago para el recurso: ...
[Information] Procesando notificación de Mercado Pago para el recurso: ...
[Information] 💳 Activating Premium subscription for user...
[Information] ✅ Premium subscription activated for user...
```

---

## 🚨 Problemas Comunes

### ❌ Error: "No se puede conectar al webhook"

**Causa:** La URL no es accesible públicamente.

**Solución:**
- Si estás en desarrollo local, usa **ngrok**
- Si estás en producción, verifica que tu servidor esté accesible desde internet
- Verifica que no haya firewall bloqueando las conexiones

### ❌ Error: "404 Not Found"

**Causa:** La ruta del webhook es incorrecta.

**Solución:**
- Verifica que la URL sea exactamente: `/api/webhooks/mercadopago`
- Verifica que tu API esté corriendo
- Prueba acceder a la URL manualmente con un POST

### ❌ El webhook recibe notificaciones pero no actualiza el plan

**Causa:** Error en el procesamiento de la notificación.

**Solución:**
1. Revisa los logs de tu aplicación
2. Verifica que el `ExternalReference` tenga el formato correcto
3. Verifica que el usuario y el plan existan en la base de datos

---

## 📊 Ejemplo Visual

```
┌─────────────────────────────────────────┐
│   Panel de Mercado Pago                 │
│   https://mercadopago.com/developers    │
└─────────────────────────────────────────┘
              │
              │ Configuras aquí
              ▼
┌─────────────────────────────────────────┐
│  Webhooks                                │
│  ┌─────────────────────────────────┐   │
│  │ URL:                             │   │
│  │ https://tu-dominio.com/api/      │   │
│  │       webhooks/mercadopago       │   │
│  └─────────────────────────────────┘   │
│                                          │
│  Eventos:                                │
│  ☑ merchant_orders                      │
│  ☐ payments                             │
│  ☐ chargebacks                          │
└─────────────────────────────────────────┘
              │
              │ Mercado Pago envía notificaciones
              ▼
┌─────────────────────────────────────────┐
│   Tu API (Backend)                       │
│   POST /api/webhooks/mercadopago        │
│                                          │
│   1. Recibe notificación                │
│   2. Verifica el pago                   │
│   3. Actualiza el plan del usuario      │
│   4. Crea suscripción                   │
│   5. Envía email de confirmación        │
└─────────────────────────────────────────┘
```

---

## 🎯 URLs Importantes

### Panel de Mercado Pago:
- **Argentina:** https://www.mercadopago.com.ar/developers/panel/app
- **Colombia:** https://www.mercadopago.com.co/developers/panel/app
- **México:** https://www.mercadopago.com.mx/developers/panel/app

### Documentación:
- **Webhooks:** https://www.mercadopago.com.ar/developers/es/docs/your-integrations/notifications/webhooks
- **Checkout Pro:** https://www.mercadopago.com.ar/developers/es/docs/checkout-pro/landing

### Herramientas:
- **ngrok:** https://ngrok.com/download

---

## ✅ Checklist

Antes de probar:

- [ ] API corriendo (local o en servidor)
- [ ] URL del webhook configurada en Mercado Pago
- [ ] Evento `merchant_orders` seleccionado
- [ ] Si es local, ngrok está corriendo
- [ ] Credenciales de Mercado Pago configuradas en `.env`

---

## 🎉 ¡Listo!

Una vez configurada la URL del webhook en el panel de Mercado Pago, tu aplicación recibirá automáticamente las notificaciones de pago y actualizará los planes de los usuarios.

**No necesitas configurar nada más en tu código, el endpoint ya está listo.**

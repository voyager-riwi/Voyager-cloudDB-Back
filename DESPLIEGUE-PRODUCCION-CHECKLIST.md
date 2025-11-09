# 📋 CHECKLIST DE DESPLIEGUE A PRODUCCIÓN

## ✅ **RESUMEN DE LO QUE CAMBIA:**

### **DESARROLLO (localhost con ngrok):**
```
Webhook en MercadoPago: https://abc123.ngrok-free.app/api/webhooks/mercadopago
                         ↑ Esta URL cambia cada vez que reinicias ngrok
```

### **PRODUCCIÓN (servidor real):**
```
Webhook en MercadoPago: https://api.tudominio.com/api/webhooks/mercadopago
                         ↑ Esta URL es fija y permanente
```

---

## 🚀 **PASOS PARA DESPLEGAR A PRODUCCIÓN**

### **PASO 1: Desplegar tu aplicación en un servidor**

Opciones comunes:
- ✅ Azure App Service
- ✅ AWS EC2 / Elastic Beanstalk
- ✅ DigitalOcean Droplet
- ✅ Railway / Render
- ✅ Tu propio servidor VPS

**Dominio final:** `https://api.tudominio.com` (o el que tengas)

---

### **PASO 2: Actualizar el webhook en MercadoPago**

1. Ve a: https://www.mercadopago.com.co/developers/panel/app/2955353636/webhooks

2. **Opción A: Si aún no creaste el webhook** (solo usaste ngrok):
   - Click en **"Crear webhook"**
   - URL: `https://api.tudominio.com/api/webhooks/mercadopago`
   - Eventos: `merchant_order`
   - Modo: `Producción`
   - Guardar

3. **Opción B: Si ya creaste el webhook con ngrok:**
   - Busca el webhook existente
   - Click en **"Editar"**
   - Cambia la URL de ngrok por: `https://api.tudominio.com/api/webhooks/mercadopago`
   - Guardar

---

### **PASO 3: Verificar las variables de entorno en producción**

Asegúrate de que estas variables estén configuradas en tu servidor:

```env
# Base de datos (puede ser la misma o diferente)
DB_HOST=91.98.42.248
DB_PORT=5432
DB_NAME=crud_cloud_db
DB_USER=postgres
DB_PASSWORD=cambiarestapassword

# JWT
JWT_SECRET=A7bC9dE2fG5hI1jK3lM4nO6pQ8rS0tUvXyZ!@#$%^

# Email
SMTP_SERVER=smtp.gmail.com
SMTP_SENDER_EMAIL=brahiamdelaipuc77@gmail.com
SMTP_PASSWORD=orilgnygnxoselnt

# MercadoPago (MISMAS credenciales de producción)
MERCADOPAGO_ACCESS_TOKEN=APP_USR-2690172310788738-103018-...
MERCADOPAGO_PUBLIC_KEY=APP_USR-53a9d6f5-0c48-44ad-8387-...

# Discord
DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/1436469778184409248/...
```

---

### **PASO 4: Verificar que el endpoint funcione**

Prueba el endpoint de webhook desde internet:

```bash
curl -X POST https://api.tudominio.com/api/webhooks/mercadopago \
  -H "Content-Type: application/json" \
  -d '{"resource":"/merchant_orders/123","topic":"merchant_order"}'
```

Debería retornar `200 OK`.

---

### **PASO 5: Probar el flujo completo**

1. Crear una preferencia de pago desde producción
2. Hacer un pago de prueba
3. Verificar en los logs que el webhook llegó
4. Verificar que el plan se actualizó

---

## 📊 **COMPARACIÓN: DESARROLLO vs PRODUCCIÓN**

| Aspecto | Desarrollo (ngrok) | Producción (servidor) |
|---------|-------------------|----------------------|
| **URL del webhook** | https://abc123.ngrok-free.app/api/webhooks/mercadopago | https://api.tudominio.com/api/webhooks/mercadopago |
| **Estabilidad URL** | ❌ Cambia cada vez | ✅ Permanente |
| **Requiere ngrok** | ✅ Sí | ❌ No |
| **Accesible 24/7** | ❌ Solo cuando corres ngrok | ✅ Siempre |
| **Credenciales MP** | APP_USR (Producción) | APP_USR (Producción) |
| **Para desarrollo** | ✅ Perfecto | ❌ Sobrecarga |
| **Para usuarios reales** | ❌ No recomendado | ✅ Necesario |

---

## ⚠️ **IMPORTANTE: CORS Y DOMINIOS**

Cuando despliegues, asegúrate de configurar CORS en tu backend para aceptar peticiones de tu frontend:

```csharp
// En Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://voyager.andrescortes.dev") // Tu dominio frontend
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ...

app.UseCors("Production");
```

---

## 🔒 **SEGURIDAD EN PRODUCCIÓN**

### **1. Variables de entorno:**
- ❌ NO uses `.env` en producción
- ✅ Usa variables de entorno del servidor
- ✅ O un servicio como Azure Key Vault, AWS Secrets Manager

### **2. HTTPS obligatorio:**
- ✅ Tu dominio DEBE tener certificado SSL (https://)
- ❌ MercadoPago NO enviará webhooks a http://

### **3. BackUrls de MercadoPago:**
Actualiza las URLs de redirección en `PaymentService.cs`:

```csharp
BackUrls = new PreferenceBackUrlsRequest
{
    Success = "https://voyager.andrescortes.dev/payment-success",
    Failure = "https://voyager.andrescortes.dev/payment-failure",
    Pending = "https://voyager.andrescortes.dev/payment-pending",
}
```

Estas ya están bien configuradas en tu código actual. ✅

---

## 📝 **CHECKLIST FINAL ANTES DE DESPLEGAR**

### **Configuración:**
- [ ] Aplicación desplegada en servidor
- [ ] Dominio configurado (ej: api.tudominio.com)
- [ ] Certificado SSL activo (https)
- [ ] Variables de entorno configuradas
- [ ] CORS configurado para tu frontend

### **MercadoPago:**
- [ ] Webhook actualizado con URL de producción
- [ ] Evento `merchant_order` seleccionado
- [ ] Modo `Producción` seleccionado
- [ ] Webhook probado y funcionando

### **Testing:**
- [ ] Endpoint /api/Health responde
- [ ] Endpoint /api/Plans responde
- [ ] Crear preferencia funciona
- [ ] Pago de prueba funciona
- [ ] Webhook llega correctamente
- [ ] Plan se actualiza automáticamente

---

## 🎯 **RESUMEN ULTRA SIMPLE**

### **AHORA (Desarrollo):**
```
1. Corres tu app localmente
2. Corres ngrok
3. Configuras webhook con URL de ngrok
4. Funciona para pruebas
```

### **DESPUÉS (Producción):**
```
1. Despliegas tu app en un servidor
2. YA NO necesitas ngrok
3. Cambias webhook a tu dominio real (https://api.tudominio.com/api/webhooks/mercadopago)
4. Funciona permanentemente
```

---

## 🆘 **CUANDO DESPLIEGUES A PRODUCCIÓN**

Si tienes dudas o problemas al desplegar, compárteme:
1. En qué servicio desplegaste (Azure, AWS, etc.)
2. Cuál es tu dominio
3. Cualquier error que aparezca

Y te ayudaré a configurarlo correctamente. 🚀

---

## ✅ **POR AHORA:**

**Para desarrollo local:**
- ✅ Usa ngrok (ya lo tienes configurado)
- ✅ Registra el webhook con la URL de ngrok
- ✅ Funciona perfectamente para pruebas

**Cuando despliegues:**
- ✅ Solo cambia la URL del webhook en MercadoPago
- ✅ Todo lo demás sigue igual
- ✅ No necesitas cambiar código


# 🎯 SOLUCIÓN COMPLETA - MERCADOPAGO WEBHOOKS

## 📋 Problemas encontrados y solucionados

### ❌ Problema 1: NotificationUrl faltante
**Síntoma:** Los pagos se completaban pero el plan del usuario nunca se actualizaba.

**Causa:** El código no configuraba la `NotificationUrl` en la preferencia de pago, por lo que MercadoPago no sabía dónde enviar las notificaciones del webhook.

**Solución:** Agregada `NotificationUrl` en `PaymentService.cs`

### ❌ Problema 2: Disposed Context
**Síntoma:** Error `System.ObjectDisposedException: Cannot access a disposed context instance`

**Causa:** El webhook se procesaba en segundo plano con `Task.Run()`, pero el `ApplicationDbContext` y otros servicios scoped ya estaban dispuestos al finalizar el request HTTP.

**Solución:** Cambiado `WebhookService.cs` para usar `IServiceScopeFactory` y crear un nuevo scope dentro del procesamiento en background.

---

## ✅ Cambios aplicados

### 1. PaymentService.cs - Agregada NotificationUrl

```csharp
var notificationUrl = "https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago";
_logger.LogInformation("🔔 Configurando NotificationUrl para webhooks: {NotificationUrl}", notificationUrl);

var preferenceRequest = new PreferenceRequest
{
    // ...existing code...
    NotificationUrl = notificationUrl,  // ← LÍNEA CRÍTICA AGREGADA
    ExternalReference = $"user:{userId};plan:{plan.Id}",
};
```

### 2. WebhookService.cs - Uso de IServiceScopeFactory

**Constructor actualizado:**
```csharp
private readonly IServiceScopeFactory _serviceScopeFactory;

public WebhookService(
    ILogger<WebhookService> logger,
    IServiceScopeFactory serviceScopeFactory,  // ← Inyectar factory
    IHttpClientFactory httpClientFactory,
    IOptions<WebhookSettings> webhookSettings)
{
    _logger = logger;
    _serviceScopeFactory = serviceScopeFactory;
    _httpClientFactory = httpClientFactory;
    _webhookSettings = webhookSettings.Value;
}
```

**Método actualizado para crear scope:**
```csharp
public async Task ProcessMercadoPagoNotificationAsync(MercadoPagoNotification notification)
{
    // Crear nuevo scope para dependencias scoped
    using var scope = _serviceScopeFactory.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var planRepository = scope.ServiceProvider.GetRequiredService<IPlanRepository>();
    var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // ... resto del procesamiento usando las variables locales
}
```

### 3. WebhooksController.cs - Logs mejorados

```csharp
[HttpPost("mercadopago")]
[AllowAnonymous] 
public IActionResult MercadoPagoWebhook([FromBody] MercadoPagoNotification notification)
{
    _logger.LogInformation("🎯 ===== WEBHOOK RECIBIDO DE MERCADOPAGO =====");
    _logger.LogInformation("📨 Topic: {Topic}", notification.Topic);
    _logger.LogInformation("📨 Resource: {Resource}", notification.Resource);
    _logger.LogInformation("📨 Action: {Action}", notification.Action ?? "N/A");
    // ... más logs para debugging
}
```

### 4. MercadoPagoNotification DTO - Campos completos

```csharp
public class MercadoPagoNotification
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("data")]
    public NotificationData? Data { get; set; }

    // Campos legacy para compatibilidad
    [JsonPropertyName("resource")]
    public string Resource { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;
}
```

---

## 🚀 Instrucciones de Deploy

```bash
git status
git add .
git commit -m "fix: Add NotificationUrl to MercadoPago preferences and fix disposed context issue

- Added NotificationUrl in PaymentService so MercadoPago sends webhook notifications
- Fixed disposed context error by using IServiceScopeFactory in WebhookService
- Improved webhook logging for better debugging
- Extended MercadoPagoNotification DTO to support both new and legacy formats

This resolves the issue where payments completed successfully but user plans were not updated."

git push origin deployment/docker-nginx
```

---

## ✅ Verificación post-deploy

### 1. Ver logs en tiempo real
```bash
docker logs crudclouddb_backend -f
```

### 2. Crear preferencia de pago

Debes ver en los logs:
```
🔔 Configurando NotificationUrl para webhooks: https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago
Preferencia de pago creada exitosamente con ID: xxx-xxx
```

### 3. Completar pago

Después de pagar, debes ver:
```
🎯 ===== WEBHOOK RECIBIDO DE MERCADOPAGO =====
📨 Topic: merchant_order
📨 Resource: /merchant_orders/123456789
✅ Webhook aceptado - Respondiendo 200 OK a MercadoPago
---
📨 ===== PROCESANDO WEBHOOK DE MERCADOPAGO =====
🔍 Consultando orden 123456789 en MercadoPago...
📋 Orden obtenida - Status: closed, OrderStatus: paid
✅ Orden de pago fue aprobada y cerrada. Procesando...
👤 Usuario ID: xxx, Plan ID: xxx
Plan del usuario actualizado
🎉 ¡Éxito! El usuario ahora tiene el plan Premium
```

### 4. Sin errores de disposed context

Ya NO deberías ver:
```
❌ System.ObjectDisposedException: Cannot access a disposed context instance
```

---

## 📊 Archivos modificados

- ✅ `CrudCloudDb.Infrastructure/Services/PaymentService.cs`
  - Agregada NotificationUrl
  - Agregados logs de configuración

- ✅ `CrudCloudDb.Infrastructure/Services/WebhookService.cs`
  - Cambiado constructor para usar IServiceScopeFactory
  - Creación de scope dentro del procesamiento
  - Actualizado procesamiento para soportar ambos formatos

- ✅ `CrudCloudDb.API/Controllers/WebhooksController.cs`
  - Logs mejorados y detallados

- ✅ `CrudCloudDb.Application/DTOs/Webhook/WebhookConfigDto.cs`
  - DTO completo con todos los campos de MercadoPago

---

## 🎯 ¿Por qué fallaba antes?

### Flujo INCORRECTO (antes):
```
Usuario paga → MercadoPago completa pago → (no envía webhook porque no tiene URL) → Plan nunca se actualiza ❌
```

### Flujo CORRECTO (ahora):
```
Usuario paga → MercadoPago completa pago → Envía webhook a NotificationUrl → 
Webhook recibido → Nuevo scope creado → Plan actualizado exitosamente ✅
```

---

## 💡 Lecciones aprendidas

1. **Siempre configurar NotificationUrl:** Sin esto, MercadoPago no sabe dónde enviar notificaciones.

2. **Cuidado con scoped services en background:** Cuando procesas en `Task.Run()` o similar, el request HTTP original ya terminó y sus servicios scoped están dispuestos. Solución: Usar `IServiceScopeFactory`.

3. **Logs detallados son esenciales:** Los logs agregados permitieron identificar rápidamente el problema del disposed context.

---

**🎉 ¡Ahora todo debería funcionar perfectamente!**


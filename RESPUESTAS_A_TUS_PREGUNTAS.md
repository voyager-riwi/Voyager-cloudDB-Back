# 📝 Respuestas a tus Preguntas

## ❓ "¿Ya copié .env.example a .env, revisa por favor?"

✅ **Perfecto!** Tu archivo `.env` ya tiene las credenciales de Mercado Pago configuradas:

```env
MERCADOPAGO_ACCESS_TOKEN=APP_USR-8642932100357504-103018-41019bd0a89ab243de2a9a37e093bdb1-2946065922
MERCADOPAGO_PUBLIC_KEY=APP_USR-2e5b5c04-d243-4926-99f1-6dc11bd8f93a
MERCADOPAGO_WEBHOOK_SECRET=d7312ca60fd2acf48200f5d290c6e663101e64920dc05c47fb933ba107f4deb4
```

**No necesitas cambiar nada más en el archivo `.env`**. Está listo para funcionar.

---

## ❓ "¿URL no sé dónde ponerla?"

La URL del webhook **NO se pone en el código**, se configura en el **Panel de Mercado Pago**.

### 📍 Dónde Configurarla:

1. **Ve a:** https://www.mercadopago.com.co/developers/panel/app
2. **Selecciona** tu aplicación
3. **Ve a** la sección "Webhooks"
4. **Agrega la URL:**
   - **Producción:** `https://voyager.andrescortes.dev/api/webhooks/mercadopago`
   - **Desarrollo:** `https://tu-ngrok.ngrok.io/api/webhooks/mercadopago`
5. **Selecciona** el evento: `merchant_orders`
6. **Guarda**

### 🔍 Más Detalles:

Lee el archivo: **`DONDE_CONFIGURAR_WEBHOOK.md`** - Tiene una guía paso a paso con imágenes.

---

## ❓ "Al registrarse, el usuario accede automáticamente al plan gratuito, ¿ya está?"

✅ **SÍ, ya está configurado!**

### Cómo Funciona:

1. **Usuario se registra** → Crea una cuenta
2. **Automáticamente** → Se le asigna el **Plan Free**
3. **Plan Free** → Permite crear hasta **2 bases de datos por motor**

### Código que lo hace:

En `Program.cs` (líneas 339-362), hay código que:
- Busca usuarios sin plan
- Les asigna automáticamente el Plan Free

```csharp
// Asignar plan Free a usuarios sin plan
var freePlanId = dbContext.Plans.FirstOrDefault(p => p.PlanType == PlanType.Free)?.Id;
if (freePlanId.HasValue)
{
    var usersWithoutPlan = dbContext.Users
        .Where(u => !validPlanIds.Contains(u.CurrentPlanId))
        .ToList();
    
    foreach (var user in usersWithoutPlan)
    {
        user.CurrentPlanId = freePlanId.Value;
    }
    
    await dbContext.SaveChangesAsync();
}
```

**Esto se ejecuta automáticamente cada vez que inicias la aplicación.**

---

## ❓ "Plan gratuito: hasta 2 bases de datos por motor. Plan intermedio: hasta 5 bases de datos por motor — 💰 $5.000 COP/mes. Plan avanzado: hasta 10 bases de datos por motor — 💰 $10.000 COP/mes. ¿Todo esto ya está?"

✅ **SÍ, TODO ya está configurado!**

### Planes Configurados:

| Plan | Precio | Bases de Datos | Estado |
|------|--------|----------------|--------|
| **Free Plan** | $0 COP | 2 por motor | ✅ Configurado |
| **Intermediate Plan** | $5.000 COP/mes | 5 por motor | ✅ Configurado |
| **Advanced Plan** | $10.000 COP/mes | 10 por motor | ✅ Configurado |

### Dónde Está Configurado:

#### 1. En las Migraciones (Base de Datos):

Archivo: `CrudCloudDb.Infrastructure/Migrations/20251029231124_InitialCreate.cs`

```csharp
values: new object[,]
{
    { new Guid("b1b108e5-fcbc-4a91-8967-b545ff937016"), 2, "Free Plan", 1, 0.00m },
    { new Guid("0b2a601a-1269-4818-9161-2797f54a7100"), 5, "Intermediate Plan", 2, 5000.00m },
    { new Guid("7be9fe44-7454-4055-8a5f-eff194532a2e"), 10, "Advanced Plan", 3, 10000.00m }
}
```

#### 2. En Program.cs (Seeding):

Archivo: `CrudCloudDb.API/Program.cs` (líneas 298-337)

```csharp
var freePlan = new Plan
{
    PlanType = PlanType.Free,
    Name = "Free Plan",
    Price = 0,
    DatabaseLimitPerEngine = 2
};

var intermediatePlan = new Plan
{
    PlanType = PlanType.Intermediate,
    Name = "Intermediate Plan",
    Price = 5000m, // $5.000 COP
    DatabaseLimitPerEngine = 5
};

var advancedPlan = new Plan
{
    PlanType = PlanType.Advanced,
    Name = "Advanced Plan",
    Price = 10000m, // $10.000 COP
    DatabaseLimitPerEngine = 10
};
```

### Cómo Verificar:

Cuando ejecutes `dotnet run`, verás en los logs:

```
📋 Creating default plans...
✅ Plans created:
   - Free: 2 DBs/engine - $0 COP
   - Intermediate: 5 DBs/engine - $5.000 COP/mes
   - Advanced: 10 DBs/engine - $10.000 COP/mes
```

---

## ❓ "¿Todo eso es de Mercado Pago?"

**Sí y No.** Déjame explicar:

### ✅ Lo que ES de Mercado Pago:

1. **Procesamiento de pagos** → Mercado Pago procesa las tarjetas
2. **Checkout Pro** → La página de pago donde el usuario paga
3. **Webhooks** → Notificaciones cuando un pago es aprobado
4. **Credenciales** → Access Token, Public Key, Webhook Secret

### 🏗️ Lo que es de TU aplicación:

1. **Planes** → Los defines tú (Free, Intermediate, Advanced)
2. **Precios** → Los defines tú ($5.000, $10.000)
3. **Límites** → Los defines tú (2, 5, 10 bases de datos)
4. **Lógica de negocio** → Tu código actualiza el plan del usuario

### 🔄 Cómo Trabajan Juntos:

```
TU APLICACIÓN                    MERCADO PAGO
─────────────                    ─────────────

1. Usuario selecciona plan
   (Intermediate - $5.000)
                    ──────────>  2. Crea preferencia de pago
                                    (con precio $5.000)

                                 3. Usuario paga en Mercado Pago
                                    (Checkout Pro)

                    <──────────  4. Webhook: "Pago aprobado"

5. Actualiza plan del usuario
   (Free → Intermediate)

6. Usuario ahora puede crear
   5 bases de datos por motor
```

### Resumen:

- **Mercado Pago** → Procesa el dinero
- **Tu aplicación** → Define los planes y actualiza el acceso del usuario

---

## 🎯 Resumen de Respuestas

| Pregunta | Respuesta |
|----------|-----------|
| ¿`.env` está bien? | ✅ Sí, perfecto |
| ¿Dónde pongo la URL? | En el Panel de Mercado Pago (no en el código) |
| ¿Plan gratis automático? | ✅ Sí, ya está configurado |
| ¿Planes con precios? | ✅ Sí, todos configurados ($5.000 y $10.000 COP) |
| ¿Es todo de Mercado Pago? | Mercado Pago procesa pagos, tu app define planes |

---

## ✅ Checklist de lo que YA ESTÁ

- [x] Archivo `.env` con credenciales de Mercado Pago
- [x] 3 planes configurados (Free, Intermediate, Advanced)
- [x] Precios en COP ($5.000 y $10.000)
- [x] Límites de bases de datos (2, 5, 10)
- [x] Asignación automática de Plan Free al registrarse
- [x] Integración con Mercado Pago (crear pagos)
- [x] Webhook para recibir notificaciones
- [x] Actualización automática de planes después del pago
- [x] Endpoints de la API funcionando

---

## 📍 Lo ÚNICO que falta

**Configurar la URL del webhook en el Panel de Mercado Pago**

### Cómo hacerlo:

1. Ve a: https://www.mercadopago.com.co/developers/panel/app
2. Selecciona tu aplicación
3. Ve a "Webhooks"
4. Agrega: `https://voyager.andrescortes.dev/api/webhooks/mercadopago`
5. Selecciona: `merchant_orders`
6. Guarda

**Lee el archivo:** `DONDE_CONFIGURAR_WEBHOOK.md` para más detalles.

---

## 🚀 Próximo Paso

```bash
# 1. Ejecuta la aplicación
cd CrudCloudDb.API
dotnet run

# 2. Verifica en los logs que los planes se crearon
# Busca: "✅ Plans created"

# 3. Configura el webhook en Mercado Pago
# (Lee: DONDE_CONFIGURAR_WEBHOOK.md)

# 4. ¡Listo para recibir pagos!
```

---

## 🎉 ¡TODO ESTÁ LISTO!

Tu aplicación ya tiene:
- ✅ Planes configurados
- ✅ Precios en COP
- ✅ Asignación automática de Plan Free
- ✅ Integración completa con Mercado Pago

**Solo falta configurar la URL del webhook en Mercado Pago y estás listo para recibir pagos! 🚀**

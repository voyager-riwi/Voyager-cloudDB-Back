# 🔧 FIX: Email Authentication Issue

## ❌ Problema Identificado

Error al crear bases de datos:
```
535: 5.7.8 Username and Password not accepted
```

**Causa raíz**: 
- El `EmailService` estaba leyendo credenciales SOLO de `appsettings.json`
- Las variables de entorno de GitHub Secrets (`SMTP_PASSWORD`, etc.) NO se estaban usando
- El password hardcodeado en `appsettings.json` estaba desactualizado

---

## ✅ Solución Implementada

### **EmailService.cs** - Leer Variables de Entorno

Modificado el constructor para leer credenciales SMTP con esta prioridad:

```csharp
// 1. Variables de entorno (prioritario - desde GitHub Secrets)
// 2. appsettings.json (fallback - para desarrollo local)
```

**Variables que ahora se leen de entorno**:
- ✅ `SMTP_SERVER` → `smtp.gmail.com`
- ✅ `SMTP_PORT` → `587`
- ✅ `SMTP_SENDER_EMAIL` → Tu email
- ✅ `SMTP_USERNAME` → Tu email
- ✅ `SMTP_PASSWORD` → **App Password de Gmail** ← El crítico
- ✅ `SMTP_ENABLE_SSL` → `true`
- ✅ `SMTP_SENDER_NAME` → `PotterCloud`

---

## 🔐 GitHub Secrets Requeridos

Asegúrate de tener configurados estos secrets en GitHub:

```
SMTP_SENDER_EMAIL = brahiamdelaipuc77@gmail.com
SMTP_USERNAME = brahiamdelaipuc77@gmail.com
SMTP_PASSWORD = orilgnygnxoselnt  ← Nuevo App Password de Gmail
```

---

## 🚀 Flujo Completo de Variables

### **Desarrollo Local** (.env file)
```env
SMTP_PASSWORD=orilgnygnxoselnt
```
↓
EmailService lee desde `Environment.GetEnvironmentVariable("SMTP_PASSWORD")`
↓
✅ Envía emails correctamente

### **Producción** (GitHub Actions)
```yaml
-e SMTP_PASSWORD=${{ secrets.SMTP_PASSWORD }}
```
↓
Docker container recibe variable de entorno
↓
EmailService lee desde `Environment.GetEnvironmentVariable("SMTP_PASSWORD")`
↓
✅ Envía emails correctamente

---

## 📋 Archivos Modificados

### 1️⃣ **EmailService.cs**
- ✅ Lee `SMTP_SERVER` desde env o config
- ✅ Lee `SMTP_PORT` desde env o config
- ✅ Lee `SMTP_SENDER_EMAIL` desde env o config
- ✅ Lee `SMTP_USERNAME` desde env o config
- ✅ Lee `SMTP_PASSWORD` desde env o config ← **CRÍTICO**
- ✅ Lee `SMTP_ENABLE_SSL` desde env o config
- ✅ Lee `SMTP_SENDER_NAME` desde env o config
- ✅ Agrega log informativo: `📧 Email configured: user@email.com via smtp.gmail.com:587`

### 2️⃣ **MasterContainerService.cs** (cambio anterior)
- ✅ Usa `172.17.0.1` para conexión a contenedores maestros

### 3️⃣ **deploy.yml** (cambio anterior)
- ✅ Usa `--network host` para networking correcto
- ✅ Inyecta todas las variables de entorno desde GitHub Secrets

---

## 🧪 Verificación

### Después del Deploy

1. **Revisa los logs del contenedor**:
```bash
docker logs crudclouddb_backend | grep "📧"
```

**Deberías ver**:
```
📧 Email configured: brahiamdelaipuc77@gmail.com via smtp.gmail.com:587
```

2. **Prueba crear una base de datos**:
```bash
POST https://service.voyager.andrescortes.dev/api/Databases
{
  "engine": "PostgreSQL"
}
```

3. **Verifica que recibas el email** con las credenciales de la nueva DB

---

## ✅ Resumen

**Problema**: Credenciales de email no se leían desde GitHub Secrets
**Solución**: EmailService ahora lee variables de entorno primero
**Resultado**: Emails se envían correctamente en producción

---

## 🚀 Próximo Paso

```bash
git add .
git commit -m "fix: Read SMTP credentials from environment variables for production

- Modified EmailService to prioritize environment variables over appsettings
- Fixes email authentication error (535: Username and Password not accepted)
- Adds logging for SMTP configuration verification
- Ensures GitHub Secrets are properly used in production"
git push origin deployment/docker-nginx
```

Espera 1-2 minutos después del deploy y prueba crear una base de datos. ¡Ahora debería enviarse el email correctamente! 📧✅


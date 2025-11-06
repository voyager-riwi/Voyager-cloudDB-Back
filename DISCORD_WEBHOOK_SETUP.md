# 🔔 Discord Webhook Setup

## 📋 GitHub Secrets Required

Debes agregar los siguientes secrets en tu repositorio de GitHub:

### 1. Discord Webhook
**Secret Name:**
```
DISCORD_WEBHOOK_URL
```

**Secret Value:**
```
https://discord.com/api/webhooks/1435449866732961974/n_Sstu5ZA0SdG0NXiqvS4ipLnLTmdVnR2li0PyPTXvZgEsxhqyO8YgDWJiupsN1iFXqs
```

### 2. MercadoPago Access Token
**Secret Name:**
```
MERCADOPAGO_ACCESS_TOKEN
```

**Secret Value:**
```
APP_USR-2690172310788738-103018-7a859811edad6b1a51a04850d85e7660-2955353636
```

### 3. MercadoPago Public Key
**Secret Name:**
```
MERCADOPAGO_PUBLIC_KEY
```

**Secret Value:**
```
APP_USR-53a9d6f5-0c48-44ad-8387-46cf52dba4c2
```

## 🚀 How to Add the Secrets

1. Ve a tu repositorio en GitHub
2. Click en **Settings** (Configuración)
3. En el menú izquierdo, click en **Secrets and variables** → **Actions**
4. Click en **New repository secret**
5. Agrega cada uno de los secrets mencionados arriba
6. Click en **Add secret**

## ✅ Verification

Después de agregar los secrets, el archivo `.github/workflows/deploy.yml` automáticamente usará estas variables durante el deploy.

## 📝 Files Modified

Los siguientes archivos han sido actualizados para usar variables de entorno:

- ✅ `.env` - Configuración local con valores reales
- ✅ `.env.example` - Template con placeholders
- ✅ `.github/workflows/deploy.yml` - Workflow de deploy usa los secrets
- ✅ `appsettings.json` - SOLO placeholders (se sube a GitHub)
- ✅ `appsettings.Production.json` - SOLO placeholders (se sube a GitHub)
- ✅ `Program.cs` - Lee de variables de entorno primero, fallback a appsettings
- ✅ `Configuration/EnvironmentConfig.cs` - Métodos helper agregados

## 🔐 Security Best Practices

### ✅ LO QUE SE SUBE A GITHUB:
- `appsettings.json` → Con PLACEHOLDERS solamente
- `appsettings.Production.json` → Con PLACEHOLDERS solamente
- `.env.example` → Template público sin credenciales reales
- `.github/workflows/deploy.yml` → Referencias a `${{ secrets.XXX }}`

### ❌ LO QUE NO SE SUBE A GITHUB:
- `.env` → Archivo con credenciales reales (está en `.gitignore`)
- `appsettings.Development.json` → Si tienes uno (está en `.gitignore`)
- Cualquier archivo con credenciales reales

### 🔄 FLUJO DE TRABAJO:

#### En Desarrollo (localhost):
1. Copias `.env.example` a `.env`
2. Llenas `.env` con tus credenciales reales
3. La app lee de `.env` primero
4. El archivo `.env` NUNCA se sube a GitHub

#### En Producción (servidor):
1. GitHub Actions hace el deploy
2. Lee los secrets de GitHub
3. Los pasa como variables de entorno al contenedor Docker
4. La app lee esas variables de entorno
5. Los `appsettings.json` solo tienen placeholders

## ⚠️ IMPORTANTE

- **NUNCA** hagas commit del archivo `.env`
- **SIEMPRE** verifica que `.env` esté en `.gitignore`
- **SOLO** usa placeholders en `appsettings.json` y `appsettings.Production.json`
- Los valores reales SOLO en `.env` (local) o GitHub Secrets (producción)



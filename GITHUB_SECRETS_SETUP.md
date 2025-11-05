# 🎯 GUÍA COMPLETA: Configuración de GitHub Secrets

## 📋 Valores que Debes Configurar

Antes de hacer push a `deployment/docker-nginx`, **DEBES** configurar estos secrets en GitHub:

---

## 🔐 GitHub Secrets a Crear

Ve a: **Tu Repositorio → Settings → Secrets and variables → Actions → New repository secret**

### 1️⃣ Database Configuration

| Secret Name | Valor Sugerido | Descripción |
|------------|----------------|-------------|
| `DB_HOST` | `172.17.0.2` | Host de la base de datos del backend (interno Docker) |
| `DB_PORT` | `5432` | Puerto de PostgreSQL |
| `DB_NAME` | `crud_cloud_db` | Nombre de la base de datos del backend |
| `DB_USER` | `postgres` | Usuario de PostgreSQL |
| `DB_PASSWORD` | `cambiarestapassword` | ⚠️ Cambiar por uno seguro |

---

### 2️⃣ JWT Configuration

| Secret Name | Valor Sugerido | Descripción |
|------------|----------------|-------------|
| `JWT_SECRET` | `A7bC9dE2fG5hI1jK3lM4nO6pQ8rS0tUvXyZ!@#$%^` | ⚠️ Cambiar por uno único |

**⚠️ Importante**: El JWT_SECRET debe tener al menos 32 caracteres.

---

### 3️⃣ Email Configuration

| Secret Name | Valor Sugerido | Descripción |
|------------|----------------|-------------|
| `SMTP_SENDER_EMAIL` | `brahiamdelaipuc77@gmail.com` | Email que envía los correos |
| `SMTP_USERNAME` | `brahiamdelaipuc77@gmail.com` | Usuario SMTP (mismo que sender) |
| `SMTP_PASSWORD` | `tfjvojddwgfeqytp` | **App Password** de Gmail |

**📝 Cómo obtener Gmail App Password**:
1. Ve a: https://myaccount.google.com/security
2. Activa "Verificación en 2 pasos"
3. Ve a "Contraseñas de aplicaciones"
4. Genera una contraseña para "Otra (nombre personalizado)"
5. Copia el código de 16 dígitos (sin espacios)

---

### 4️⃣ Database Hosts (Para usuarios finales)

Estas IPs son las que recibirán los usuarios en sus connection strings:

| Secret Name | Valor | Descripción |
|------------|-------|-------------|
| `DB_HOST_POSTGRESQL` | `91.98.42.248` | IP pública del servidor PostgreSQL |
| `DB_HOST_MYSQL` | `91.98.42.248` | IP pública del servidor MySQL |
| `DB_HOST_MONGODB` | `91.98.42.248` | IP pública del servidor MongoDB |

---

## 🚀 Flujo de Deploy

### Paso 1: Configurar Secrets (SOLO UNA VEZ)

```
1. Ve a tu repo en GitHub
2. Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Agrega cada secret de la lista anterior
```

### Paso 2: Hacer Push

```bash
cd C:\Users\Brahiam\Documents\CloudDb-Back\Voyager-cloudDB-Back

# Agregar cambios
git add .

# Commit
git commit -m "feat: Implementar variables de entorno seguras"

# Push a la rama de deployment
git push origin deployment/docker-nginx
```

### Paso 3: GitHub Actions se Ejecuta Automáticamente

```
1. GitHub Actions hace pull del código
2. Build de la imagen Docker
3. Inyecta las variables desde GitHub Secrets
4. Deploy del contenedor en 91.98.42.248
```

### Paso 4: Verificación

```bash
# Conectarse al servidor
ssh user@91.98.42.248

# Ver logs del contenedor
docker logs crudclouddb_backend

# Deberías ver:
# ✅ Loaded .env file for development
# 🗄️ Database: postgres@172.17.0.2:5432/crud_cloud_db
# 🌐 Building connection string with host: 91.98.42.248
```

---

## ⚠️ MUY IMPORTANTE

### ❌ NO HAGAS PUSH SIN CONFIGURAR SECRETS

Si haces push **SIN** configurar los GitHub Secrets, el deploy **FALLARÁ** porque:
- No habrá credenciales de base de datos
- No habrá JWT Secret
- No habrá credenciales de email

### ✅ Verifica que el .env NO se suba

Antes de hacer push, verifica:

```bash
git status
```

**NO debe aparecer** `.env` en la lista de archivos a subir.

Si aparece, significa que el `.gitignore` no está funcionando:

```bash
# Removerlo del staging
git reset HEAD .env

# Verificar que está en .gitignore
cat .gitignore | grep ".env"
```

---

## 📊 Checklist Pre-Push

Antes de hacer `git push origin deployment/docker-nginx`:

- [ ] ✅ Todos los GitHub Secrets configurados (14 secrets)
- [ ] ✅ Archivo `.env` NO está en `git status`
- [ ] ✅ Archivo `.env.example` SÍ está en `git status`
- [ ] ✅ `appsettings.Production.json` tiene solo placeholders
- [ ] ✅ Código compila sin errores: `dotnet build`
- [ ] ✅ Tests locales funcionan con `.env`

---

## 🆘 Troubleshooting

### Error: "JWT Secret is not configured"

**Causa**: Secret `JWT_SECRET` no configurado en GitHub
**Solución**: Agregar secret en GitHub → Settings → Secrets

### Error: "Cannot connect to database"

**Causa**: Secrets de DB no configurados o incorrectos
**Solución**: Verificar `DB_HOST`, `DB_PORT`, `DB_PASSWORD` en GitHub Secrets

### Error: "Email sending failed"

**Causa**: `SMTP_PASSWORD` incorrecto o no es App Password
**Solución**: 
1. Generar nuevo App Password en Google
2. Actualizar secret `SMTP_PASSWORD` en GitHub

---

## 📝 Resumen de Archivos Creados/Modificados

### ✅ Archivos Nuevos
- `.env` → **NO se sube a GitHub** (contiene tus datos)
- `.env.example` → **SÍ se sube** (template público)
- `ENVIRONMENT_SETUP.md` → Documentación
- `GITHUB_SECRETS_SETUP.md` → Esta guía
- `CrudCloudDb.API/Configuration/EnvironmentConfig.cs` → Helper

### ✅ Archivos Modificados
- `.gitignore` → Protege `.env`
- `Program.cs` → Lee variables de entorno
- `DockerService.cs` → Usa variables de entorno para hosts
- `appsettings.Production.json` → Datos sensibles eliminados
- `.github/workflows/deploy.yml` → Inyecta secrets en Docker

---

## 🎉 ¿Listo para Deploy?

Si configuraste todos los secrets, ahora puedes:

```bash
git add .
git commit -m "feat: Implementar variables de entorno seguras"
git push origin deployment/docker-nginx
```

¡GitHub Actions se encargará del resto! 🚀


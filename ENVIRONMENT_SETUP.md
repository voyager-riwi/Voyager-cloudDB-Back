# 🔐 Configuración de Variables de Entorno

## 📋 Setup Inicial (Desarrollo Local)

### 1️⃣ Crear archivo `.env`

```bash
cp .env.example .env
```

### 2️⃣ Editar `.env` con tus credenciales

Abre `.env` y configura:
- Credenciales de base de datos
- JWT Secret
- Credenciales de email (Gmail App Password)
- Hosts de bases de datos para usuarios

### 3️⃣ Ejecutar la aplicación

```bash
dotnet run --project CrudCloudDb.API
```

Las variables de `.env` se cargarán automáticamente en desarrollo.

---

## 🐳 Setup Docker Local (Opcional)

Si usas Docker localmente, crea `docker-compose.yml`:

```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5191:5191"
    env_file:
      - .env
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
```

---

## 🚀 Setup Producción (GitHub Actions)

### 1️⃣ Configurar GitHub Secrets

Ve a tu repositorio: **Settings → Secrets and variables → Actions → New repository secret**

Agrega estos secrets:

#### Database Configuration
- `DB_HOST`: `172.17.0.2` (o tu host de producción)
- `DB_PORT`: `5432`
- `DB_NAME`: `crud_cloud_db`
- `DB_USER`: `postgres`
- `DB_PASSWORD`: `tu_password_seguro`

#### JWT Configuration
- `JWT_SECRET`: `un_secreto_seguro_minimo_32_caracteres`

#### Email Configuration
- `SMTP_SENDER_EMAIL`: `tu_email@gmail.com`
- `SMTP_USERNAME`: `tu_email@gmail.com`
- `SMTP_PASSWORD`: `tu_app_password_de_gmail`

#### Database Hosts (Para usuarios finales)
- `DB_HOST_POSTGRESQL`: `91.98.42.248`
- `DB_HOST_MYSQL`: `91.98.42.248`
- `DB_HOST_MONGODB`: `91.98.42.248`

### 2️⃣ Hacer Push

```bash
git add .
git commit -m "feat: Agregar variables de entorno seguras"
git push origin deployment/docker-nginx
```

GitHub Actions automáticamente:
1. Pull el código
2. Build la imagen Docker
3. Inyecta las variables desde GitHub Secrets
4. Despliega el contenedor

---

## ✅ Verificación

### Desarrollo Local
```bash
dotnet run --project CrudCloudDb.API
```

Verifica en los logs:
```
✅ Loaded .env file for development
🗄️ Database: postgres@91.98.42.248:5432/crud_cloud_db
```

### Producción
Después del deploy, verifica:
```bash
ssh user@91.98.42.248
docker logs crudclouddb_backend
```

Deberías ver:
```
🗄️ Database: postgres@172.17.0.2:5432/crud_cloud_db
🌐 Building connection string with host: 91.98.42.248
```

---

## 🔒 Seguridad

✅ **NUNCA** subir `.env` a GitHub
✅ **NUNCA** exponer passwords en appsettings
✅ Usar GitHub Secrets para producción
✅ Rotar credenciales periódicamente

---

## 📝 Notas Importantes

- `.env` es para **desarrollo local** únicamente
- `.env.example` es un template público sin datos sensibles
- En **producción**, las variables vienen de GitHub Secrets
- `appsettings.Production.json` ahora solo tiene placeholders seguros

---

## 🆘 Troubleshooting

### Error: "JWT Secret is not configured"
**Solución**: Verifica que `JWT_SECRET` esté en `.env` o en GitHub Secrets

### Error: "Cannot connect to database"
**Solución**: Verifica `DB_HOST`, `DB_PORT`, `DB_PASSWORD` en `.env` o GitHub Secrets

### Error: "Email sending failed"
**Solución**: Verifica `SMTP_PASSWORD` (debe ser App Password de Gmail, no tu contraseña)

---

## 📚 Recursos

- [Cómo obtener Gmail App Password](https://support.google.com/accounts/answer/185833)
- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)


# 🎯 Pasos para tu Situación Actual

## 📍 Estado Actual Confirmado

✅ **Lo que YA tienes:**
- Servidor: `91.98.42.248` (misma IP, pero limpio/desde cero)
- 4 contenedores de BD master levantados con `docker-compose.databases.yml`:
  - `postgres_master` (puerto 5432)
  - `mysql_master` (puerto 3306)
  - `mongodb_master` (puerto 27017)
  - `sqlserver_master` (puerto 1433)
- Backend levantado
- Red Docker: `voyager_network`

❌ **Lo que FALTA:**
- Base de datos `crud_cloud_db` en PostgreSQL (no existe)
- Tablas del backend (users, plans, subscriptions, etc.)
- Las migraciones de EF Core no se han ejecutado

---

## 🚀 Plan de Acción (Paso a Paso)

### **Paso 1: Crear la Base de Datos `crud_cloud_db`**

Conéctate al servidor:

```bash
ssh -i "$HOME\.ssh\voyager-deploy\id_ed25519" root@91.98.42.248
```

Crea la base de datos en PostgreSQL:

```bash
# Verificar que postgres_master esté corriendo
docker ps | grep postgres_master

# Crear la base de datos crud_cloud_db
docker exec -it postgres_master psql -U postgres -c "CREATE DATABASE crud_cloud_db;"

# Verificar que se creó correctamente
docker exec -it postgres_master psql -U postgres -c "\l" | grep crud_cloud_db

# Probar conexión a la nueva base de datos
docker exec -it postgres_master psql -U postgres -d crud_cloud_db -c "SELECT version();"
```

**Resultado esperado:**
```
CREATE DATABASE
crud_cloud_db | postgres | UTF8 | ...
PostgreSQL 15.x on x86_64-pc-linux-musl...
```

---

### **Paso 2: Verificar Variables de Entorno del Backend**

El backend necesita estas variables para conectarse:

```bash
DB_HOST=91.98.42.248  # ← Tu IP externa (si backend corre fuera del servidor)
# O
DB_HOST=postgres_master  # ← Si backend corre en contenedor dentro de voyager_network
DB_PORT=5432
DB_NAME=crud_cloud_db
DB_USER=postgres
DB_PASSWORD=cambiarestapassword
```

**Pregunta crítica:** ¿Cómo está corriendo tu backend ahora?

**Opción A:** Backend en contenedor Docker (dentro de `voyager_network`)
→ Usa `DB_HOST=postgres_master`

**Opción B:** Backend en el host directamente (`dotnet run`)
→ Usa `DB_HOST=localhost` o `DB_HOST=91.98.42.248`

**Opción C:** Backend en tu máquina local (desarrollo)
→ Usa `DB_HOST=91.98.42.248`

---

### **Paso 3: Detener el Backend Actual**

```bash
# Si está en contenedor
docker ps | grep crudclouddb
docker stop crudclouddb_backend
docker rm crudclouddb_backend

# Si está corriendo como servicio systemd
sudo systemctl stop crudclouddb-api

# O simplemente matar el proceso
pkill -f CrudCloudDb.API
```

---

### **Paso 4: Actualizar el Código en el Servidor**

```bash
cd ~/Voyager-cloudDB-Back

# Hacer backup del código actual (por si acaso)
cd ~
cp -r Voyager-cloudDB-Back Voyager-cloudDB-Back.backup

cd ~/Voyager-cloudDB-Back

# Verificar rama actual
git branch

# Actualizar código desde GitHub
git pull origin deployment/docker-nginx

# O si no tienes el repo clonado aún
git clone -b deployment/docker-nginx https://github.com/TU_USUARIO/CrudCloudDb-voyager.git tmp
cp -r tmp/* .
cp -r tmp/.github .
rm -rf tmp
```

---

### **Paso 5: Reconstruir la Imagen del Backend**

```bash
cd ~/Voyager-cloudDB-Back

# Reconstruir imagen con el nuevo código (incluye migraciones automáticas)
docker build -t crudclouddb-api:latest .
```

**Esto tomará 3-5 minutos.** El nuevo código incluye:
- ✅ Migraciones automáticas al iniciar (`Database.MigrateAsync()`)
- ✅ Logs de migraciones pendientes
- ✅ Seeding de planes (Free, Intermediate, Advanced)

---

### **Paso 6: Iniciar el Backend con las Variables Correctas**

**Opción A: Backend en Contenedor (Recomendado)**

```bash
# Definir variables de entorno
export DB_HOST="postgres_master"  # ← Nombre del contenedor
export DB_PORT="5432"
export DB_NAME="crud_cloud_db"
export DB_USER="postgres"
export DB_PASSWORD="cambiarestapassword"
export JWT_SECRET="A7bC9dE2fG5hI1jK3lM4nO6pQ8rS0tUvXyZ!@#$%^"
export JWT_ISSUER="CrudCloudDb.API"
export JWT_AUDIENCE="CrudCloudDb.Frontend"
export SMTP_SENDER_EMAIL="brahiamdelaipuc77@gmail.com"
export SMTP_USERNAME="brahiamdelaipuc77@gmail.com"
export SMTP_PASSWORD="orilgnygnxoselnt"
export MERCADOPAGO_ACCESS_TOKEN="APP_USR-5340880978802180-111018-dd05d1cebddd52932d39c3514202e04e-2904931077"
export MERCADOPAGO_PUBLIC_KEY="APP_USR-03ba93f3-a3c0-4035-89b4-33c5dca1437d"
export MERCADOPAGO_NOTIFICATION_URL="https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago"
export MERCADOPAGO_WEBHOOK_SECRET="63d1907380ac91f2c45b6f1a973e8d83788dd95321eaf440cd91f2ff7ea006d3"
export DISCORD_WEBHOOK_URL="https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/d7312ca60fd2acf48200f5d290c6e663101e64920dc05c47fb933ba107f4deb4"

# Iniciar contenedor
docker run -d \
  --name crudclouddb_backend \
  --restart unless-stopped \
  --network voyager_network \
  --cpus="1.0" \
  --memory="512m" \
  --memory-swap="768m" \
  -p 5191:5191 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:5191 \
  -e DB_HOST="$DB_HOST" \
  -e DB_PORT="$DB_PORT" \
  -e DB_NAME="$DB_NAME" \
  -e DB_USER="$DB_USER" \
  -e DB_PASSWORD="$DB_PASSWORD" \
  -e JWT_SECRET="$JWT_SECRET" \
  -e JWT_ISSUER="$JWT_ISSUER" \
  -e JWT_AUDIENCE="$JWT_AUDIENCE" \
  -e SMTP_SENDER_EMAIL="$SMTP_SENDER_EMAIL" \
  -e SMTP_USERNAME="$SMTP_USERNAME" \
  -e SMTP_PASSWORD="$SMTP_PASSWORD" \
  -e MERCADOPAGO_ACCESS_TOKEN="$MERCADOPAGO_ACCESS_TOKEN" \
  -e MERCADOPAGO_PUBLIC_KEY="$MERCADOPAGO_PUBLIC_KEY" \
  -e MERCADOPAGO_NOTIFICATION_URL="$MERCADOPAGO_NOTIFICATION_URL" \
  -e MERCADOPAGO_WEBHOOK_SECRET="$MERCADOPAGO_WEBHOOK_SECRET" \
  -e DISCORD_WEBHOOK_URL="$DISCORD_WEBHOOK_URL" \
  crudclouddb-api:latest
```

---

### **Paso 7: Ver Logs del Backend (Migraciones en Acción)**

```bash
docker logs -f crudclouddb_backend
```

**Deberías ver algo como esto:**

```
🗄️ Checking database migrations...
📦 Applying 4 pending migration(s)...
   - 20251029231124_InitialCreate
   - 20251031212550_AddDeletedAtColumn
   - 20251107213555_UpdatePlanTypesToPremiumAndMax
   - 20251109032201_FixSubscriptionMercadoPagoColumns
✅ Database migrations applied successfully
🌱 Initializing database data...
📋 Creating default plans...
✅ Plans created: Free (2 DBs/engine), Premium (5 DBs/engine), Max (10 DBs/engine)
✅ Database initialization completed
Starting web host...
Now listening on: http://[::]:5191
Application started. Press Ctrl+C to shut down.
```

**Si ves esto, ¡TODO ESTÁ FUNCIONANDO! 🎉**

---

### **Paso 8: Verificar las Tablas Creadas**

```bash
# Listar todas las tablas
docker exec -it postgres_master psql -U postgres -d crud_cloud_db -c "\dt"

# Ver los planes creados
docker exec -it postgres_master psql -U postgres -d crud_cloud_db -c "SELECT * FROM plans;"

# Ver la tabla de migraciones aplicadas
docker exec -it postgres_master psql -U postgres -d crud_cloud_db -c "SELECT * FROM __EFMigrationsHistory ORDER BY migration_id;"
```

**Deberías ver:**
```
 Schema |         Name          | Type  |  Owner   
--------+-----------------------+-------+----------
 public | audit_logs            | table | postgres
 public | database_instances    | table | postgres
 public | email_logs            | table | postgres
 public | plans                 | table | postgres
 public | subscriptions         | table | postgres
 public | users                 | table | postgres
 public | webhook_configs       | table | postgres
```

---

### **Paso 9: Probar el Backend**

```bash
# Health check
curl http://localhost:5191/health

# Debería retornar:
# {"status":"healthy","timestamp":"2025-01-17T...","environment":"Production","version":"1.0.0"}

# Root endpoint
curl http://localhost:5191/

# Ver Swagger (desde tu navegador)
# http://91.98.42.248:5191/swagger
```

---

### **Paso 10: Iniciar NGINX (si aplica)**

```bash
cd ~/Voyager-cloudDB-Back

# Iniciar NGINX con docker-compose
docker compose up -d nginx

# Ver logs
docker logs voyager-backend-nginx

# Probar acceso por HTTPS
curl https://service.voyager.andrescortes.dev/health
```

---

## 🔍 Troubleshooting

### **Error: "Connection refused" o "could not connect to server"**

**Causa:** `DB_HOST` mal configurado.

**Solución:**
- Si backend está en contenedor → `DB_HOST=postgres_master`
- Si backend está en el host → `DB_HOST=localhost` o `DB_HOST=91.98.42.248`

```bash
# Verificar que postgres esté accesible
docker exec -it crudclouddb_backend ping postgres_master
```

---

### **Error: "database 'crud_cloud_db' does not exist"**

**Solución:** Ejecuta el Paso 1 de nuevo.

---

### **Error: "password authentication failed for user postgres"**

**Causa:** Contraseña incorrecta en `DB_PASSWORD`.

**Solución:**
```bash
# Ver la contraseña configurada en el contenedor
docker exec -it postgres_master env | grep POSTGRES_PASSWORD

# Actualizar la variable al iniciar el backend
export DB_PASSWORD="LA_CONTRASEÑA_CORRECTA"
```

---

### **Las migraciones no se aplican**

**Verificar logs:**
```bash
docker logs crudclouddb_backend | grep -i migration
```

**Aplicar manualmente (si es necesario):**
```bash
# Desde tu máquina local (con .NET SDK instalado)
cd C:\Users\maria\RiderProjects\CrudCloudDb-voyager
dotnet ef database update --project CrudCloudDb.Infrastructure --startup-project CrudCloudDb.API

# O desde el servidor con SDK instalado
cd ~/Voyager-cloudDB-Back
dotnet ef database update --project CrudCloudDb.Infrastructure --startup-project CrudCloudDb.API
```

---

## ✅ Checklist de Verificación

- [ ] Base de datos `crud_cloud_db` creada en PostgreSQL
- [ ] Backend reconstruido con nuevo código
- [ ] Backend iniciado con `DB_HOST=postgres_master`
- [ ] Logs muestran "✅ Database migrations applied successfully"
- [ ] Tablas visibles con `\dt` en psql
- [ ] Planes creados (Free, Intermediate, Advanced)
- [ ] `/health` responde correctamente
- [ ] NGINX funcionando (opcional)

---

## 🎯 Próximos Pasos (después de verificar que todo funciona)

1. **Commit y Push del código actualizado:**
   ```bash
   git add .
   git commit -m "feat: add automatic database migrations on startup"
   git push origin deployment/docker-nginx
   ```

2. **Configurar GitHub Actions** para despliegue automático

3. **Monitorear uso de memoria:**
   ```bash
   docker stats --no-stream
   free -h
   ```

---

## 📞 ¿Necesitas Ayuda?

Si encuentras algún error:
1. Comparte el output de `docker logs crudclouddb_backend`
2. Comparte el resultado de `docker ps`
3. Comparte el error exacto que ves

¡Estoy aquí para ayudarte! 🚀


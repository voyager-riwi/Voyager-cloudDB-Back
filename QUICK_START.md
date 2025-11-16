# ⚡ Quick Start - Migración Rápida

## 📌 Para los que tienen prisa

Este documento es un resumen ultra-compacto para migrar el backend rápidamente. **Lee la documentación completa después para entender cada paso.**

---

## 🎯 Objetivos
- Servidor: 4GB RAM
- Backend: .NET 8 API
- Bases de datos: PostgreSQL, MySQL, MongoDB, SQL Server
- CD: GitHub Actions + SSH

---

## 📋 Checklist Rápido

```
[ ] Servidor con Ubuntu 20.04+, 4GB RAM, 20GB disco
[ ] Acceso SSH como root/sudo
[ ] Cuenta GitHub con permisos admin
[ ] 30-45 minutos disponibles
```

---

## 🚀 Pasos Rápidos

### 1️⃣ Generar Claves SSH (Local - 2 min)

```powershell
# PowerShell en Windows
mkdir "$HOME\.ssh\voyager-deploy" -Force
ssh-keygen -t ed25519 -C "github-actions" -f "$HOME\.ssh\voyager-deploy\id_ed25519" -N '""'

# Copiar clave pública
Get-Content "$HOME\.ssh\voyager-deploy\id_ed25519.pub" | clip

# Copiar clave privada (para GitHub Secrets)
Get-Content "$HOME\.ssh\voyager-deploy\id_ed25519" | clip
```

---

### 2️⃣ Configurar Servidor (SSH - 10 min)

```bash
# Conectar al servidor
ssh root@TU_IP_SERVIDOR

# Script de instalación rápida
curl -fsSL https://get.docker.com | sh
apt install docker-compose-plugin -y

# Crear swap (CRÍTICO)
fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab

# Usuario deployer
adduser --disabled-password --gecos "" github-deployer
usermod -aG docker github-deployer

# SSH para deployer
su - github-deployer
mkdir -p ~/.ssh && chmod 700 ~/.ssh
nano ~/.ssh/authorized_keys  # Pegar clave pública aquí
chmod 600 ~/.ssh/authorized_keys
exit

# Firewall
ufw allow 22/tcp && ufw allow 80/tcp && ufw allow 443/tcp
ufw --force enable

# Probar SSH
ssh -i "$HOME\.ssh\voyager-deploy\id_ed25519" github-deployer@TU_IP_SERVIDOR
```

---

### 3️⃣ Estructura en Servidor (5 min)

```bash
# Como github-deployer
cd ~
mkdir -p Voyager-cloudDB-Back/ssl
cd Voyager-cloudDB-Back

# Variables de entorno
nano .env.databases
```

**Contenido de `.env.databases`:**
```env
POSTGRES_USER=admin_pg
POSTGRES_PASSWORD=$(openssl rand -base64 32)
POSTGRES_DB=voyager_main

MYSQL_ROOT_PASSWORD=$(openssl rand -base64 32)
MYSQL_DATABASE=voyager_main
MYSQL_USER=admin_mysql
MYSQL_PASSWORD=$(openssl rand -base64 32)

MONGO_INITDB_ROOT_USERNAME=admin_mongo
MONGO_INITDB_ROOT_PASSWORD=$(openssl rand -base64 32)
MONGO_INITDB_DATABASE=voyager_main

SA_PASSWORD=YourSecure123!Password
ACCEPT_EULA=Y
```

**⚠️ IMPORTANTE:** Reemplaza `$(openssl rand -base64 32)` con passwords reales generados.

```bash
chmod 600 .env.databases
```

---

### 4️⃣ GitHub Secrets (5 min)

GitHub → Repositorio → Settings → Secrets and variables → Actions

Agregar estos 20 secrets:

```
SERVER_HOST           = IP_DEL_SERVIDOR
SERVER_USER           = github-deployer
SSH_PRIVATE_KEY       = [contenido completo de id_ed25519]
DB_HOST               = localhost
DB_PORT               = 5432
DB_NAME               = voyager_main
DB_USER               = admin_pg
DB_PASSWORD           = [password de PostgreSQL]
JWT_SECRET            = [openssl rand -base64 64]
DB_HOST_POSTGRESQL    = postgres_master
DB_HOST_MYSQL         = mysql_master
DB_HOST_MONGODB       = mongodb_master
DB_HOST_SQLSERVER     = sqlserver_master
SMTP_SENDER_EMAIL     = tu-email@gmail.com
SMTP_USERNAME         = tu-email@gmail.com
SMTP_PASSWORD         = [App Password de Gmail]
MERCADOPAGO_ACCESS_TOKEN    = [tu token]
MERCADOPAGO_PUBLIC_KEY      = [tu key]
MERCADOPAGO_NOTIFICATION_URL = https://service.voyager.andrescortes.dev/api/webhooks/mercadopago
MERCADOPAGO_WEBHOOK_SECRET  = [openssl rand -base64 32]
```

---

### 5️⃣ Primer Deploy Manual (15 min)

```bash
# En el servidor como github-deployer
cd ~/Voyager-cloudDB-Back

# Clonar repo
git clone -b deployment/docker-nginx https://github.com/TU_USUARIO/TU_REPO.git tmp
cp -r tmp/* . && cp -r tmp/.github . && rm -rf tmp

# Crear red Docker
docker network create voyager_network

# Iniciar bases de datos
export $(cat .env.databases | xargs)
docker compose -f docker-compose.databases.yml up -d

# Esperar 2 minutos
sleep 120

# Build backend
docker build -t crudclouddb-api:latest .

# Iniciar backend
docker run -d \
  --name crudclouddb_backend \
  --restart unless-stopped \
  --network voyager_network \
  --cpus="1.0" \
  --memory="512m" \
  -p 5191:5191 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:5191 \
  -e DB_HOST=localhost \
  -e DB_PORT=5432 \
  -e DB_NAME=voyager_main \
  -e DB_USER=admin_pg \
  -e DB_PASSWORD=$POSTGRES_PASSWORD \
  -e JWT_SECRET=TU_JWT_SECRET_AQUI \
  crudclouddb-api:latest

# Esperar 30 segundos
sleep 30

# Verificar
curl http://localhost:5191/health  # Debe retornar "Healthy"

# Iniciar NGINX
docker compose up -d nginx
```

---

### 6️⃣ Activar CD Automático (2 min)

```bash
# En tu máquina local
git add .
git commit -m "feat: setup optimized deployment"
git push origin deployment/docker-nginx
```

GitHub Actions se activará automáticamente. Monitorea en: GitHub → Actions

---

## ✅ Verificación Final

```bash
# En el servidor
docker ps  # Debe mostrar 6 contenedores corriendo

./monitor.sh status  # Verificar estado

curl http://localhost:5191/health  # Backend
curl https://service.voyager.andrescortes.dev/health  # Público
```

---

## 📊 Recursos Esperados

```
docker stats --no-stream
```

Deberías ver:
- Backend: ~300-400 MB
- PostgreSQL: ~400-500 MB
- MySQL: ~400-500 MB
- MongoDB: ~250-350 MB
- SQL Server: ~700-800 MB
- NGINX: ~50 MB
- **Total: ~3.5-3.8 GB de 4 GB**

---

## 🆘 Si Algo Sale Mal

### Backend no inicia
```bash
docker logs crudclouddb_backend  # Ver errores
docker restart crudclouddb_backend
```

### Sin memoria
```bash
free -h  # Verificar RAM y swap
docker stats --no-stream  # Ver consumo
```

### Health check falla
```bash
docker exec crudclouddb_backend curl http://localhost:5191/health
```

### Rollback
```bash
docker stop crudclouddb_backend && docker rm crudclouddb_backend
docker run -d --name crudclouddb_backend crudclouddb-api:previous
```

---

## 📚 Documentación Completa

Para detalles completos, lee:
- `DOCUMENTATION_INDEX.md` - Índice general
- `DEPLOYMENT_TUTORIAL.md` - Tutorial detallado
- `MIGRATION_PLAN.md` - Estrategia y arquitectura
- `SECURITY_GUIDE.md` - Seguridad y hardening

---

## 🎯 Comandos Útiles

```bash
# Ver logs
docker logs -f crudclouddb_backend

# Monitoreo completo
./monitor.sh full

# Reiniciar todo
docker restart $(docker ps -q)

# Uso de recursos
docker stats --no-stream

# Estado de contenedores
docker ps --format "table {{.Names}}\t{{.Status}}"
```

---

## ⏱️ Tiempos Estimados

| Fase | Tiempo |
|------|--------|
| 1. Generar claves SSH | 2 min |
| 2. Configurar servidor | 10 min |
| 3. Estructura servidor | 5 min |
| 4. GitHub Secrets | 5 min |
| 5. Primer deploy | 15 min |
| 6. Activar CD | 2 min |
| **TOTAL** | **~40 min** |

---

**✨ ¡Listo! Backend desplegado en 40 minutos.**

**🔒 IMPORTANTE:** Después de esto, implementa las medidas de seguridad de `SECURITY_GUIDE.md`.

---

**📅 Última actualización**: 2025-11-16


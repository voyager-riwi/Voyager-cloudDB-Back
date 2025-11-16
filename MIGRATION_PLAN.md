# 🚀 Plan de Migración y Despliegue Optimizado - Servidor 4GB RAM

## 📋 Índice
1. [Análisis de Recursos y Distribución de Carga](#1-análisis-de-recursos-y-distribución-de-carga)
2. [Arquitectura de Contenedores Optimizada](#2-arquitectura-de-contenedores-optimizada)
3. [Configuración de Seguridad SSH](#3-configuración-de-seguridad-ssh)
4. [Preparación del Servidor](#4-preparación-del-servidor)
5. [Configuración de GitHub Actions](#5-configuración-de-github-actions)
6. [Proceso de Despliegue Paso a Paso](#6-proceso-de-despliegue-paso-a-paso)
7. [Monitoreo y Troubleshooting](#7-monitoreo-y-troubleshooting)
8. [Plan de Rollback](#8-plan-de-rollback)

---

## 1. Análisis de Recursos y Distribución de Carga

### 🔍 Situación Actual
- **RAM Total**: 4 GB (4096 MB)
- **Componentes**:
  - Backend API (.NET 8)
  - NGINX (Reverse Proxy)
  - PostgreSQL (Master)
  - MySQL (Master)
  - MongoDB (Master)
  - SQL Server (Master)

### 📊 Distribución Recomendada de Memoria

```
┌─────────────────────────────────────────────────────┐
│ Total RAM: 4096 MB                                  │
├─────────────────────────────────────────────────────┤
│ Sistema Operativo (Buffer)       : 512 MB  (12.5%) │
│ NGINX                             : 128 MB  (3.1%)  │
│ Backend API (.NET)                : 512 MB  (12.5%) │
│ PostgreSQL                        : 768 MB  (18.8%) │
│ MySQL                             : 768 MB  (18.8%) │
│ MongoDB                           : 512 MB  (12.5%) │
│ SQL Server                        : 896 MB  (21.8%) │
├─────────────────────────────────────────────────────┤
│ TOTAL ASIGNADO                    : 4096 MB (100%)  │
└─────────────────────────────────────────────────────┘
```

### ⚠️ Puntos Críticos
1. **SQL Server es el más pesado**: Requiere configuración especial
2. **Swap necesario**: Mínimo 2 GB de swap para evitar OOM (Out of Memory)
3. **Sin margen de error**: Cada contenedor debe tener límites estrictos

---

## 2. Arquitectura de Contenedores Optimizada

### 🎯 Estrategias Clave

#### A. Multi-Stage Builds (Backend)
- ✅ Ya implementado correctamente en `Dockerfile`
- Reduce imagen de ~1GB a ~200MB
- Usa `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime-only)

#### B. Límites de Recursos
Implementar en `docker-compose.databases.yml`:
```yaml
resources:
  limits:
    cpus: '0.5'
    memory: 768M
  reservations:
    memory: 256M
```

#### C. Health Checks Inteligentes
- Intervalos más largos para reducir CPU
- Reintentos limitados para evitar loops

#### D. Configuraciones de DB Optimizadas
- PostgreSQL: `shared_buffers=256MB`, `max_connections=50`
- MySQL: `innodb_buffer_pool_size=256M`, `max_connections=50`
- MongoDB: `wiredTigerCacheSizeGB=0.25`
- SQL Server: `memory.limit=896m`

---

## 3. Configuración de Seguridad SSH

### 🔐 Paso 1: Generar Par de Claves SSH Dedicadas

En tu máquina local (PowerShell):

```powershell
# Crear directorio para las claves
mkdir -p ~/.ssh/voyager-deploy

# Generar clave ED25519 (más segura y rápida que RSA)
ssh-keygen -t ed25519 -C "github-actions-voyager" -f ~/.ssh/voyager-deploy/id_ed25519 -N ""

# Mostrar la clave pública (para copiar al servidor)
cat ~/.ssh/voyager-deploy/id_ed25519.pub

# Mostrar la clave privada (para GitHub Secrets)
cat ~/.ssh/voyager-deploy/id_ed25519
```

### 🖥️ Paso 2: Configurar el Servidor

Conéctate a tu servidor y ejecuta:

```bash
# Crear usuario dedicado para despliegues (más seguro que usar root)
sudo adduser --disabled-password --gecos "" github-deployer

# Agregar al grupo docker
sudo usermod -aG docker github-deployer

# Configurar SSH
sudo su - github-deployer
mkdir -p ~/.ssh
chmod 700 ~/.ssh
nano ~/.ssh/authorized_keys  # Pega aquí la clave pública generada
chmod 600 ~/.ssh/authorized_keys
exit

# Configurar firewall (UFW)
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw enable
sudo ufw status
```

### 🔒 Paso 3: Hardening SSH (`/etc/ssh/sshd_config`)

```bash
sudo nano /etc/ssh/sshd_config
```

Configuración recomendada:
```
# Deshabilitar login de root
PermitRootLogin no

# Solo autenticación por clave
PasswordAuthentication no
PubkeyAuthentication yes

# Restringir usuario
AllowUsers github-deployer

# Configuraciones de seguridad
Protocol 2
MaxAuthTries 3
LoginGraceTime 30
ClientAliveInterval 300
ClientAliveCountMax 2
```

Reiniciar SSH:
```bash
sudo systemctl restart sshd
```

### 🧪 Paso 4: Probar Conexión

Desde tu máquina local:
```powershell
ssh -i ~/.ssh/voyager-deploy/id_ed25519 github-deployer@NUEVA_IP_SERVIDOR
```

---

## 4. Preparación del Servidor

### 📦 Paso 1: Instalar Dependencias

```bash
# Actualizar sistema
sudo apt update && sudo apt upgrade -y

# Instalar Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Instalar Docker Compose v2
sudo apt install docker-compose-plugin -y

# Verificar instalación
docker --version
docker compose version
```

### 💾 Paso 2: Configurar Swap (CRÍTICO para 4GB RAM)

```bash
# Verificar swap actual
free -h

# Crear archivo swap de 2GB
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile

# Hacer permanente
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab

# Optimizar swappiness (reducir uso de swap)
echo 'vm.swappiness=10' | sudo tee -a /etc/sysctl.conf
sudo sysctl -p

# Verificar
free -h
```

### 📁 Paso 3: Estructura de Directorios

```bash
# Crear estructura como github-deployer
cd ~
mkdir -p Voyager-cloudDB-Back
cd Voyager-cloudDB-Back

# Crear directorios para volúmenes
mkdir -p data/postgres data/mysql data/mongodb data/sqlserver
mkdir -p logs ssl

# Clonar repositorio (temporalmente para configurar)
# Se sobrescribirá en cada deploy
git clone -b deployment/docker-nginx https://github.com/TU_USUARIO/TU_REPO.git tmp
cp -r tmp/* .
rm -rf tmp
```

### 🔧 Paso 4: Configurar Variables de Entorno del Sistema

Crear archivo `.env` (para bases de datos locales):

```bash
nano ~/Voyager-cloudDB-Back/.env.databases
```

Contenido:
```env
# PostgreSQL
POSTGRES_USER=admin_pg
POSTGRES_PASSWORD=TU_PASSWORD_SEGURO_1
POSTGRES_DB=voyager_main

# MySQL
MYSQL_ROOT_PASSWORD=TU_PASSWORD_SEGURO_2
MYSQL_DATABASE=voyager_main
MYSQL_USER=admin_mysql
MYSQL_PASSWORD=TU_PASSWORD_SEGURO_3

# MongoDB
MONGO_INITDB_ROOT_USERNAME=admin_mongo
MONGO_INITDB_ROOT_PASSWORD=TU_PASSWORD_SEGURO_4

# SQL Server
SA_PASSWORD=TuPassword123!
ACCEPT_EULA=Y
```

**⚠️ IMPORTANTE**: Genera passwords seguros:
```bash
# Generar passwords aleatorios
openssl rand -base64 32
```

---

## 5. Configuración de GitHub Actions

### 🔑 Paso 1: Configurar Secrets en GitHub

Ve a: `Settings` → `Secrets and variables` → `Actions` → `New repository secret`

Agrega estos secrets:

| Secret Name | Descripción | Ejemplo/Valor |
|-------------|-------------|---------------|
| `SERVER_HOST` | IP del nuevo servidor | `123.45.67.89` |
| `SERVER_USER` | Usuario SSH | `github-deployer` |
| `SSH_PRIVATE_KEY` | Clave privada completa | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `DB_HOST` | Host de la DB principal | `localhost` |
| `DB_PORT` | Puerto PostgreSQL | `5432` |
| `DB_NAME` | Nombre de la DB | `voyager_main` |
| `DB_USER` | Usuario DB | `admin_pg` |
| `DB_PASSWORD` | Password DB | (generado antes) |
| `JWT_SECRET` | Secret JWT (min 32 chars) | `openssl rand -base64 32` |
| `SMTP_SENDER_EMAIL` | Email SMTP | `tu-email@gmail.com` |
| `SMTP_USERNAME` | Usuario SMTP | `tu-email@gmail.com` |
| `SMTP_PASSWORD` | Password de aplicación Gmail | (ver [guía Gmail](https://support.google.com/accounts/answer/185833)) |
| `DB_HOST_POSTGRESQL` | IP servidor PostgreSQL | `localhost` |
| `DB_HOST_MYSQL` | IP servidor MySQL | `localhost` |
| `DB_HOST_MONGODB` | IP servidor MongoDB | `localhost` |
| `DB_HOST_SQLSERVER` | IP servidor SQL Server | `localhost` |
| `DISCORD_WEBHOOK_URL` | Webhook Discord | (opcional) |
| `MERCADOPAGO_ACCESS_TOKEN` | Token MP | (tu token) |
| `MERCADOPAGO_PUBLIC_KEY` | Public key MP | (tu key) |
| `MERCADOPAGO_NOTIFICATION_URL` | URL notificaciones | `https://service.voyager.andrescortes.dev/api/webhooks/mercadopago` |
| `MERCADOPAGO_WEBHOOK_SECRET` | Secret MP | (tu secret) |

### 📝 Paso 2: Copiar la Clave Privada

```powershell
# En tu máquina local
cat ~/.ssh/voyager-deploy/id_ed25519 | clip
```

Pega el contenido completo (incluye `-----BEGIN...` y `-----END...`) en el secret `SSH_PRIVATE_KEY`.

---

## 6. Proceso de Despliegue Paso a Paso

### 🚀 Flujo de Trabajo

```
┌─────────────────────────────────────────────────────────┐
│ 1. Push a branch deployment/docker-nginx                │
│                ↓                                         │
│ 2. GitHub Actions se activa                             │
│                ↓                                         │
│ 3. Conecta vía SSH al servidor                          │
│                ↓                                         │
│ 4. Pull del código más reciente                         │
│                ↓                                         │
│ 5. Inicia contenedores de DB (si no están corriendo)    │
│                ↓                                         │
│ 6. Construye imagen del backend                         │
│                ↓                                         │
│ 7. Detiene contenedor anterior (graceful shutdown)      │
│                ↓                                         │
│ 8. Inicia nuevo contenedor backend                      │
│                ↓                                         │
│ 9. Health check (espera 30s)                            │
│                ↓                                         │
│ 10. Verifica conectividad                               │
│                ↓                                         │
│ 11. Inicia/recarga NGINX                                │
│                ↓                                         │
│ 12. Health check final vía NGINX                        │
│                ↓                                         │
│ 13. Limpieza de imágenes antiguas                       │
│                ↓                                         │
│ 14. Notificación de éxito/fallo                         │
└─────────────────────────────────────────────────────────┘
```

### ✅ Validaciones en Cada Paso

1. **Pre-deployment**: Verificar espacio en disco, memoria disponible
2. **Durante build**: Timeout de 10 minutos máximo
3. **Post-deployment**: 
   - Health check del backend
   - Verificar logs (sin errores críticos)
   - Test de endpoint crítico

---

## 7. Monitoreo y Troubleshooting

### 📊 Comandos Útiles

```bash
# Ver uso de recursos en tiempo real
docker stats

# Ver logs del backend
docker logs -f --tail 100 crudclouddb_backend

# Ver logs de NGINX
docker logs -f voyager-backend-nginx

# Ver logs de bases de datos
docker logs -f postgres_master
docker logs -f mysql_master
docker logs -f mongodb_master
docker logs -f sqlserver_master

# Verificar salud de contenedores
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Inspeccionar uso de memoria
docker inspect crudclouddb_backend | grep -i memory

# Verificar conectividad interna
docker exec crudclouddb_backend curl -f http://localhost:5191/health
```

### 🚨 Señales de Alerta

1. **OOM (Out of Memory)**:
   ```bash
   dmesg | grep -i "out of memory"
   journalctl -u docker | grep OOM
   ```

2. **Contenedor reiniciándose constantemente**:
   ```bash
   docker ps -a | grep Restarting
   ```

3. **Errores de conexión a DB**:
   ```bash
   docker exec crudclouddb_backend ping postgres_master
   ```

### 🔧 Soluciones Rápidas

```bash
# Reiniciar contenedor específico
docker restart crudclouddb_backend

# Liberar memoria (limpiar caché de Docker)
docker system prune -a --volumes -f

# Ver procesos que consumen memoria
top -o %MEM

# Aumentar swap temporalmente
sudo swapoff -a
sudo dd if=/dev/zero of=/swapfile bs=1M count=3072
sudo mkswap /swapfile
sudo swapon /swapfile
```

---

## 8. Plan de Rollback

### ⏮️ Rollback Automático (en deploy.yml)

Si el health check falla después de 3 intentos:
1. Detener nuevo contenedor
2. Restaurar imagen anterior (`crudclouddb-api:previous`)
3. Iniciar contenedor con imagen anterior
4. Notificar fallo

### 🔄 Rollback Manual

```bash
# Conectarse al servidor
ssh github-deployer@NUEVA_IP_SERVIDOR

cd ~/Voyager-cloudDB-Back

# Ver imágenes disponibles
docker images | grep crudclouddb-api

# Detener contenedor actual
docker stop crudclouddb_backend
docker rm crudclouddb_backend

# Iniciar con imagen anterior (reemplaza TAG con el hash del commit anterior)
docker run -d \
  --name crudclouddb_backend \
  --restart unless-stopped \
  --network host \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:5191 \
  # ... (copiar todas las variables de entorno del deploy.yml)
  crudclouddb-api:TAG_ANTERIOR

# Verificar
docker logs -f --tail 50 crudclouddb_backend
curl http://localhost:5191/health
```

### 📸 Backup Antes de Deploy

```bash
# Automatizar en el servidor (agregar a crontab)
0 3 * * * docker commit crudclouddb_backend crudclouddb-api:backup-$(date +\%Y\%m\%d)
```

---

## 🎯 Checklist Pre-Migración

- [ ] Swap de 2GB configurado
- [ ] Usuario `github-deployer` creado y configurado
- [ ] Claves SSH generadas y probadas
- [ ] Firewall configurado (UFW)
- [ ] Docker y Docker Compose instalados
- [ ] Secrets de GitHub configurados (20 secrets)
- [ ] Estructura de directorios creada
- [ ] Variables de entorno de bases de datos configuradas
- [ ] Certificados SSL copiados a `/home/github-deployer/Voyager-cloudDB-Back/ssl/`
- [ ] Archivo `docker-compose.databases.yml` optimizado
- [ ] Archivo `deploy.yml` actualizado con nueva IP
- [ ] Plan de rollback documentado y probado

---

## 📚 Recursos Adicionales

- [Docker Memory Limits](https://docs.docker.com/config/containers/resource_constraints/)
- [GitHub Actions SSH](https://github.com/appleboy/ssh-action)
- [.NET Performance Tips](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)
- [PostgreSQL Memory Tuning](https://wiki.postgresql.org/wiki/Tuning_Your_PostgreSQL_Server)

---

**📧 Contacto**: Si hay problemas durante la migración, consulta los logs y el plan de rollback.

**✅ Estado**: Documento actualizado - 2025-11-16


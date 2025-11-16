# 🎯 Tutorial: Implementación Paso a Paso - Migración a Nuevo Servidor

## 📌 Prerequisitos
- Acceso SSH al nuevo servidor
- Cuenta de GitHub con permisos de administrador del repositorio
- PowerShell o terminal Bash en tu máquina local
- 30-45 minutos de tiempo estimado

---

## 🚀 FASE 1: Preparación de la Máquina Local

### Paso 1.1: Generar Claves SSH

Abre PowerShell en tu máquina local:

```powershell
# Crear directorio para las claves
New-Item -ItemType Directory -Force -Path "$HOME\.ssh\voyager-deploy"

# Navegar al directorio
cd "$HOME\.ssh\voyager-deploy"

# Generar clave SSH ED25519
ssh-keygen -t ed25519 -C "github-actions-voyager" -f id_ed25519 -N '""'
```

**Resultado esperado:**
```
Your identification has been saved in id_ed25519
Your public key has been saved in id_ed25519.pub
```

### Paso 1.2: Visualizar las Claves

```powershell
# Ver clave pública (para copiar al servidor)
Get-Content id_ed25519.pub

# Ver clave privada (para GitHub Secrets)
Get-Content id_ed25519
```

**⚠️ IMPORTANTE:** 
- Copia TODO el contenido de `id_ed25519` (incluye `-----BEGIN OPENSSH PRIVATE KEY-----` y `-----END OPENSSH PRIVATE KEY-----`)
- Guárdalo temporalmente en un archivo de texto seguro

---

## 🖥️ FASE 2: Configuración del Servidor

### Paso 2.1: Conexión Inicial al Servidor

```bash
# Conectarse como root o usuario con sudo
ssh root@NUEVA_IP_DEL_SERVIDOR

# O si tienes usuario diferente:
ssh tu_usuario@NUEVA_IP_DEL_SERVIDOR
```

### Paso 2.2: Actualizar el Sistema

```bash
sudo apt update && sudo apt upgrade -y
```

### Paso 2.3: Instalar Docker

```bash
# Instalar Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Instalar Docker Compose Plugin
sudo apt install docker-compose-plugin -y

# Verificar instalaciones
docker --version
docker compose version

# Debería mostrar algo como:
# Docker version 24.x.x
# Docker Compose version v2.x.x
```

### Paso 2.4: Configurar Swap (CRÍTICO)

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

# Optimizar swappiness
echo 'vm.swappiness=10' | sudo tee -a /etc/sysctl.conf
sudo sysctl -p

# Verificar que funciona
free -h
```

**Resultado esperado:**
```
              total        used        free      shared  buff/cache   available
Mem:           3.8Gi       1.2Gi       1.5Gi        10Mi       1.1Gi       2.4Gi
Swap:          2.0Gi          0B       2.0Gi
```

### Paso 2.5: Crear Usuario para Despliegues

```bash
# Crear usuario github-deployer
sudo adduser --disabled-password --gecos "" github-deployer

# Agregar al grupo docker
sudo usermod -aG docker github-deployer

# Verificar
groups github-deployer
# Debe mostrar: github-deployer : github-deployer docker
```

### Paso 2.6: Configurar SSH para github-deployer

```bash
# Cambiar a usuario github-deployer
sudo su - github-deployer

# Crear directorio .ssh
mkdir -p ~/.ssh
chmod 700 ~/.ssh

# Crear archivo authorized_keys
nano ~/.ssh/authorized_keys
```

**En el editor nano:**
- Pega la **clave pública** (contenido de `id_ed25519.pub`) que copiaste en Paso 1.2
- Presiona `Ctrl + X`, luego `Y`, luego `Enter` para guardar

```bash
# Configurar permisos
chmod 600 ~/.ssh/authorized_keys

# Salir del usuario github-deployer
exit
```

### Paso 2.7: Configurar Firewall

```bash
# Configurar UFW
sudo ufw allow 22/tcp comment 'SSH'
sudo ufw allow 80/tcp comment 'HTTP'
sudo ufw allow 443/tcp comment 'HTTPS'
sudo ufw enable

# Verificar
sudo ufw status numbered
```

### Paso 2.8: Hardening SSH

```bash
# Hacer backup de configuración original
sudo cp /etc/ssh/sshd_config /etc/ssh/sshd_config.backup

# Editar configuración
sudo nano /etc/ssh/sshd_config
```

**Agregar/modificar estas líneas:**
```
PermitRootLogin no
PasswordAuthentication no
PubkeyAuthentication yes
AllowUsers github-deployer tu_usuario_normal
Protocol 2
MaxAuthTries 3
LoginGraceTime 30
ClientAliveInterval 300
ClientAliveCountMax 2
```

```bash
# Validar configuración
sudo sshd -t

# Si no hay errores, reiniciar SSH
sudo systemctl restart sshd
```

### Paso 2.9: Probar Conexión SSH con Clave

**Desde tu máquina local (PowerShell):**

```powershell
# Probar conexión (cambia NUEVA_IP_DEL_SERVIDOR)
ssh -i "$HOME\.ssh\voyager-deploy\id_ed25519" github-deployer@NUEVA_IP_DEL_SERVIDOR
```

**✅ Deberías conectarte sin pedir contraseña.**

Si funciona, continúa. Si no, revisa los pasos 2.6 y 2.8.

### Paso 2.10: Crear Estructura de Directorios

```bash
# Ya conectado como github-deployer
cd ~
mkdir -p Voyager-cloudDB-Back
cd Voyager-cloudDB-Back

# Crear directorios para datos persistentes
mkdir -p data/postgres data/mysql data/mongodb data/sqlserver
mkdir -p logs ssl init-scripts/postgres init-scripts/mysql init-scripts/mongo init-scripts/sqlserver
```

### Paso 2.11: Configurar Variables de Entorno de Bases de Datos

```bash
nano ~/Voyager-cloudDB-Back/.env.databases
```

**Contenido del archivo:**
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
MONGO_INITDB_DATABASE=voyager_main

# SQL Server
SA_PASSWORD=TuPassword123!ComplexSecure
ACCEPT_EULA=Y
```

**Generar passwords seguros:**
```bash
# Generar 4 passwords diferentes
openssl rand -base64 32
openssl rand -base64 32
openssl rand -base64 32
openssl rand -base64 32
```

Reemplaza `TU_PASSWORD_SEGURO_X` con los passwords generados.

```bash
# Guardar y proteger el archivo
chmod 600 .env.databases
```

### Paso 2.12: Copiar Certificados SSL

Si ya tienes certificados SSL válidos:

```bash
# En el servidor
cd ~/Voyager-cloudDB-Back/ssl

# Usa scp desde tu máquina local para copiarlos:
# (ejecuta en PowerShell local)
```

**En tu máquina local:**
```powershell
# Asumiendo que tienes los certificados localmente
scp -i "$HOME\.ssh\voyager-deploy\id_ed25519" fullchain.pem github-deployer@NUEVA_IP_DEL_SERVIDOR:~/Voyager-cloudDB-Back/ssl/
scp -i "$HOME\.ssh\voyager-deploy\id_ed25519" privkey.pem github-deployer@NUEVA_IP_DEL_SERVIDOR:~/Voyager-cloudDB-Back/ssl/
```

Si necesitas generar certificados con Let's Encrypt:

```bash
# En el servidor
sudo apt install certbot -y
sudo certbot certonly --standalone -d service.voyager.andrescortes.dev
sudo certbot certonly --standalone -d voyager.andrescortes.dev

# Copiar certificados al directorio del proyecto
sudo cp /etc/letsencrypt/live/service.voyager.andrescortes.dev/fullchain.pem ~/Voyager-cloudDB-Back/ssl/
sudo cp /etc/letsencrypt/live/service.voyager.andrescortes.dev/privkey.pem ~/Voyager-cloudDB-Back/ssl/
sudo cp /etc/letsencrypt/live/voyager.andrescortes.dev/fullchain.pem ~/Voyager-cloudDB-Back/ssl/voyager-fullchain.pem
sudo cp /etc/letsencrypt/live/voyager.andrescortes.dev/privkey.pem ~/Voyager-cloudDB-Back/ssl/voyager-privkey.pem
sudo chown github-deployer:github-deployer ~/Voyager-cloudDB-Back/ssl/*
```

---

## 🔧 FASE 3: Configuración de GitHub

### Paso 3.1: Acceder a Configuración de Secrets

1. Ve a tu repositorio en GitHub
2. Click en **Settings** (Configuración)
3. En el menú lateral, click en **Secrets and variables** → **Actions**
4. Click en **New repository secret**

### Paso 3.2: Agregar Secrets (Uno por Uno)

Agrega cada uno de estos secrets:

#### Secrets de Servidor:

| Nombre | Valor |
|--------|-------|
| `SERVER_HOST` | La IP de tu nuevo servidor (ej: `123.45.67.89`) |
| `SERVER_USER` | `github-deployer` |
| `SSH_PRIVATE_KEY` | Contenido COMPLETO de `id_ed25519` (incluye BEGIN y END) |

#### Secrets de Base de Datos Principal:

| Nombre | Valor |
|--------|-------|
| `DB_HOST` | `localhost` |
| `DB_PORT` | `5432` |
| `DB_NAME` | `voyager_main` |
| `DB_USER` | `admin_pg` |
| `DB_PASSWORD` | El password que generaste para PostgreSQL |

#### Secrets de Hosts de Bases de Datos:

| Nombre | Valor |
|--------|-------|
| `DB_HOST_POSTGRESQL` | `postgres_master` |
| `DB_HOST_MYSQL` | `mysql_master` |
| `DB_HOST_MONGODB` | `mongodb_master` |
| `DB_HOST_SQLSERVER` | `sqlserver_master` |

#### Secrets de JWT:

| Nombre | Valor |
|--------|-------|
| `JWT_SECRET` | Generar con: `openssl rand -base64 64` |

#### Secrets de Email (SMTP):

| Nombre | Valor |
|--------|-------|
| `SMTP_SENDER_EMAIL` | Tu email de Gmail (ej: `tu-email@gmail.com`) |
| `SMTP_USERNAME` | Mismo email |
| `SMTP_PASSWORD` | **App Password** de Gmail (ver nota abajo) |

**📧 Cómo obtener App Password de Gmail:**
1. Ve a https://myaccount.google.com/security
2. Activar verificación en 2 pasos
3. Buscar "App passwords" o "Contraseñas de aplicaciones"
4. Generar nueva contraseña para "Mail"
5. Copiar el código de 16 caracteres generado

#### Secrets de MercadoPago:

| Nombre | Valor |
|--------|-------|
| `MERCADOPAGO_ACCESS_TOKEN` | Tu Access Token de MercadoPago |
| `MERCADOPAGO_PUBLIC_KEY` | Tu Public Key de MercadoPago |
| `MERCADOPAGO_NOTIFICATION_URL` | `https://service.voyager.andrescortes.dev/api/webhooks/mercadopago` |
| `MERCADOPAGO_WEBHOOK_SECRET` | Generar: `openssl rand -base64 32` |

#### Secrets Opcionales:

| Nombre | Valor |
|--------|-------|
| `DISCORD_WEBHOOK_URL` | URL de webhook de Discord (opcional) |

### Paso 3.3: Verificar Todos los Secrets

Deberías tener un total de **20 secrets** configurados:

```
✅ SERVER_HOST
✅ SERVER_USER
✅ SSH_PRIVATE_KEY
✅ DB_HOST
✅ DB_PORT
✅ DB_NAME
✅ DB_USER
✅ DB_PASSWORD
✅ DB_HOST_POSTGRESQL
✅ DB_HOST_MYSQL
✅ DB_HOST_MONGODB
✅ DB_HOST_SQLSERVER
✅ JWT_SECRET
✅ SMTP_SENDER_EMAIL
✅ SMTP_USERNAME
✅ SMTP_PASSWORD
✅ MERCADOPAGO_ACCESS_TOKEN
✅ MERCADOPAGO_PUBLIC_KEY
✅ MERCADOPAGO_NOTIFICATION_URL
✅ MERCADOPAGO_WEBHOOK_SECRET
```

---

## 📦 FASE 4: Preparar Repositorio Local

### Paso 4.1: Asegurar que Tienes los Archivos Nuevos

En tu proyecto local, deberías tener estos archivos nuevos:

```
✅ MIGRATION_PLAN.md
✅ docker-compose.databases.yml
✅ mysql-config/my.cnf
✅ .env.databases.example
✅ .github/workflows/deploy.yml (actualizado)
```

### Paso 4.2: Actualizar .gitignore

```powershell
# Abre el archivo .gitignore
notepad .gitignore
```

Asegúrate de que contiene:
```
.env.databases
*.env
ssl/*.pem
ssl/*.key
logs/
data/
```

### Paso 4.3: Commit y Push

```powershell
git add .
git commit -m "feat: add optimized deployment configuration for 4GB RAM server"
git push origin deployment/docker-nginx
```

---

## 🚢 FASE 5: Primer Despliegue Manual

### Paso 5.1: Clonar Repositorio en el Servidor

```bash
# Conectarse al servidor como github-deployer
ssh -i "$HOME\.ssh\voyager-deploy\id_ed25519" github-deployer@NUEVA_IP_DEL_SERVIDOR

cd ~/Voyager-cloudDB-Back

# Configurar Git
git config --global user.name "GitHub Deployer"
git config --global user.email "deployer@voyager.local"

# Clonar el repositorio
git clone -b deployment/docker-nginx https://github.com/TU_USUARIO/TU_REPO.git tmp

# Mover archivos
cp -r tmp/* .
cp -r tmp/.github .
rm -rf tmp

# Verificar archivos
ls -la
# Deberías ver: docker-compose.databases.yml, Dockerfile, nginx.conf, etc.
```

### Paso 5.2: Iniciar Bases de Datos

```bash
# Cargar variables de entorno
export $(cat .env.databases | xargs)

# Crear red Docker
docker network create voyager_network || true

# Iniciar contenedores de bases de datos
docker compose -f docker-compose.databases.yml up -d

# Ver progreso
docker compose -f docker-compose.databases.yml logs -f
```

**Espera 1-2 minutos** hasta que veas:
```
postgres_master    | database system is ready to accept connections
mysql_master       | ready for connections
mongodb_master     | Waiting for connections
sqlserver_master   | SQL Server is now ready for client connections
```

Presiona `Ctrl+C` para salir de los logs.

### Paso 5.3: Verificar Salud de Bases de Datos

```bash
# Ver estado
docker ps

# Debería mostrar 4 contenedores corriendo:
# postgres_master
# mysql_master
# mongodb_master
# sqlserver_master

# Ver uso de memoria
docker stats --no-stream
```

### Paso 5.4: Construir Imagen del Backend

```bash
cd ~/Voyager-cloudDB-Back

# Construir imagen
docker build -t crudclouddb-api:latest .

# Esto tomará 3-5 minutos
```

### Paso 5.5: Iniciar Backend Manualmente (Primera Vez)

```bash
# Ejecutar contenedor backend con todas las variables
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
  -e DB_HOST=localhost \
  -e DB_PORT=5432 \
  -e DB_NAME=voyager_main \
  -e DB_USER=admin_pg \
  -e DB_PASSWORD=$(grep POSTGRES_PASSWORD .env.databases | cut -d '=' -f2) \
  -e JWT_SECRET=REEMPLAZAR_CON_TU_JWT_SECRET \
  -e JWT_ISSUER=CrudCloudDb.API \
  -e JWT_AUDIENCE=CrudCloudDb.Frontend \
  crudclouddb-api:latest

# Ver logs
docker logs -f crudclouddb_backend
```

**Deberías ver:**
```
Now listening on: http://[::]:5191
Application started. Press Ctrl+C to shut down.
```

### Paso 5.6: Probar Backend

```bash
# Test health endpoint
curl http://localhost:5191/health

# Debería retornar: Healthy
```

### Paso 5.7: Iniciar NGINX

```bash
cd ~/Voyager-cloudDB-Back

# Iniciar NGINX
docker compose up -d nginx

# Ver logs
docker logs voyager-backend-nginx
```

### Paso 5.8: Verificación Final

```bash
# Ver todos los contenedores
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Probar acceso vía NGINX
curl http://localhost/health
curl https://localhost/health -k  # -k para ignorar SSL self-signed si aplica
```

---

## 🤖 FASE 6: Activar Despliegue Automático con GitHub Actions

### Paso 6.1: Hacer un Cambio de Prueba

En tu máquina local:

```powershell
# Hacer un cambio menor
echo "# Test deployment" >> README.md

git add README.md
git commit -m "test: trigger automatic deployment"
git push origin deployment/docker-nginx
```

### Paso 6.2: Monitorear el Despliegue

1. Ve a GitHub → Tu Repositorio → **Actions**
2. Deberías ver un workflow en ejecución: "🚀 Deploy Backend to Production"
3. Click en el workflow para ver los logs en tiempo real

### Paso 6.3: Ver Logs desde el Servidor

Mientras se ejecuta, puedes ver qué pasa en el servidor:

```bash
# Conectarse al servidor
ssh -i "$HOME\.ssh\voyager-deploy\id_ed25519" github-deployer@NUEVA_IP_DEL_SERVIDOR

# Ver cambios en tiempo real
watch -n 2 'docker ps --format "table {{.Names}}\t{{.Status}}"'

# O ver uso de recursos
watch -n 2 'docker stats --no-stream'
```

---

## 🎉 FASE 7: Verificación Post-Despliegue

### Paso 7.1: Tests de Conectividad

```bash
# Test local
curl http://localhost:5191/health

# Test vía NGINX
curl http://localhost/health

# Test público
curl https://service.voyager.andrescortes.dev/health
```

### Paso 7.2: Verificar Logs

```bash
# Logs del backend
docker logs --tail 50 crudclouddb_backend

# Logs de NGINX
docker logs --tail 50 voyager-backend-nginx

# Logs de bases de datos
docker logs --tail 50 postgres_master
docker logs --tail 50 mysql_master
```

### Paso 7.3: Verificar Uso de Recursos

```bash
# Ver uso de memoria
free -h

# Ver uso de disco
df -h

# Ver uso de contenedores
docker stats --no-stream

# Ver procesos
top -o %MEM
```

---

## 📊 Métricas Esperadas

### Uso de Memoria (Aproximado)

```
Sistema Operativo:  512 MB
NGINX:              50 MB
Backend API:        300-400 MB
PostgreSQL:         400-500 MB
MySQL:              400-500 MB
MongoDB:            250-350 MB
SQL Server:         700-800 MB
─────────────────────────────
TOTAL:             ~3.5-3.8 GB de 4 GB
```

### Tiempos de Respuesta

- Backend health check: < 100ms
- API endpoints: < 500ms
- Despliegue completo: 5-8 minutos

---

## 🆘 Troubleshooting

### Problema: OOM (Out of Memory)

```bash
# Ver si hay errores OOM
dmesg | grep -i "out of memory"

# Verificar swap
free -h

# Reiniciar contenedor con problemas
docker restart NOMBRE_CONTENEDOR
```

### Problema: Contenedor no inicia

```bash
# Ver logs completos
docker logs NOMBRE_CONTENEDOR

# Inspeccionar configuración
docker inspect NOMBRE_CONTENEDOR

# Verificar red
docker network inspect voyager_network
```

### Problema: No se conecta a la base de datos

```bash
# Probar conectividad desde el backend
docker exec crudclouddb_backend ping postgres_master

# Verificar que las DBs están en la misma red
docker network inspect voyager_network | grep -A 20 "Containers"
```

### Problema: GitHub Actions falla

1. Verifica que todos los secrets estén configurados
2. Verifica la conexión SSH desde GitHub:
   ```bash
   ssh -i ~/.ssh/voyager-deploy/id_ed25519 github-deployer@SERVER_HOST "echo OK"
   ```
3. Revisa logs en GitHub Actions → Workflow → Ver detalles del paso fallido

---

## ✅ Checklist Final

- [ ] Swap de 2GB configurado y activo
- [ ] Usuario `github-deployer` creado
- [ ] Claves SSH funcionando
- [ ] Firewall configurado (puertos 22, 80, 443)
- [ ] Docker y Docker Compose instalados
- [ ] 20 secrets configurados en GitHub
- [ ] Certificados SSL en `/ssl/`
- [ ] Bases de datos iniciadas y saludables
- [ ] Backend respondiendo en puerto 5191
- [ ] NGINX funcionando y sirviendo tráfico HTTPS
- [ ] GitHub Actions ejecutándose correctamente
- [ ] Uso de memoria < 3.8 GB

---

## 📚 Comandos Útiles

### Mantenimiento Regular

```bash
# Limpiar imágenes antiguas
docker image prune -a -f

# Limpiar volúmenes no usados
docker volume prune -f

# Ver espacio usado por Docker
docker system df

# Backup de bases de datos
docker exec postgres_master pg_dump -U admin_pg voyager_main > backup_$(date +%Y%m%d).sql

# Reiniciar todos los contenedores
docker restart $(docker ps -q)
```

### Monitoreo

```bash
# Ver logs en tiempo real de todos los contenedores
docker compose -f docker-compose.databases.yml logs -f

# Ver uso de recursos en tiempo real
htop

# Ver conexiones de red
sudo netstat -tulpn | grep -E ':(80|443|5191|5432|3306|27017|1433)'
```

---

**🎊 ¡Felicitaciones! Tu backend está desplegado de forma optimizada y segura.**

**📧 Soporte**: Guarda este documento para futuras referencias.

**📅 Última actualización**: 2025-11-16


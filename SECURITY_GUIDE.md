# 🔐 Guía de Seguridad y Mejores Prácticas

## 📋 Índice
1. [Seguridad de Acceso SSH](#1-seguridad-de-acceso-ssh)
2. [Gestión de Secrets y Variables de Entorno](#2-gestión-de-secrets-y-variables-de-entorno)
3. [Seguridad de Contenedores Docker](#3-seguridad-de-contenedores-docker)
4. [Hardening de Bases de Datos](#4-hardening-de-bases-de-datos)
5. [Configuración de NGINX](#5-configuración-de-nginx)
6. [Backups y Recuperación](#6-backups-y-recuperación)
7. [Monitoreo y Alertas](#7-monitoreo-y-alertas)
8. [Checklist de Auditoría](#8-checklist-de-auditoría)

---

## 1. Seguridad de Acceso SSH

### 🔑 Gestión de Claves SSH

#### ✅ Buenas Prácticas

1. **Usar ED25519 en lugar de RSA**
   ```bash
   # ED25519 es más seguro y rápido
   ssh-keygen -t ed25519 -C "tu-comentario"
   
   # Si necesitas RSA, usa 4096 bits mínimo
   ssh-keygen -t rsa -b 4096 -C "tu-comentario"
   ```

2. **Una clave por propósito**
   - Clave para GitHub Actions (dedicada)
   - Clave para administración manual (separada)
   - Nunca reutilizar claves entre proyectos

3. **Rotar claves periódicamente**
   ```bash
   # Cada 6-12 meses, generar nuevas claves
   # Actualizar en GitHub Secrets
   # Eliminar claves antiguas del servidor
   ```

4. **Proteger claves privadas**
   ```bash
   # Permisos correctos (600 para privada, 644 para pública)
   chmod 600 ~/.ssh/id_ed25519
   chmod 644 ~/.ssh/id_ed25519.pub
   
   # Agregar passphrase para mayor seguridad
   ssh-keygen -p -f ~/.ssh/id_ed25519
   ```

#### ⚠️ Malas Prácticas a Evitar

- ❌ Usar la misma clave SSH en múltiples servidores
- ❌ Subir claves privadas a Git
- ❌ Compartir claves SSH entre usuarios
- ❌ Usar claves sin passphrase en entornos críticos

### 🛡️ Configuración SSH del Servidor

#### Archivo: `/etc/ssh/sshd_config`

```bash
# Configuración mínima recomendada
Port 22                          # Considera cambiar a puerto no estándar
Protocol 2                       # Solo protocolo 2
PermitRootLogin no               # NUNCA permitir root
PasswordAuthentication no        # Solo claves SSH
PubkeyAuthentication yes         # Activar autenticación por clave
ChallengeResponseAuthentication no
UsePAM yes

# Restringir usuarios
AllowUsers github-deployer tu_usuario_admin

# Timeouts y límites
LoginGraceTime 30
MaxAuthTries 3
MaxSessions 5
ClientAliveInterval 300
ClientAliveCountMax 2

# Desactivar características inseguras
PermitEmptyPasswords no
X11Forwarding no
PermitUserEnvironment no
AllowAgentForwarding no
AllowTcpForwarding no
```

#### Aplicar cambios:
```bash
# Validar configuración
sudo sshd -t

# Reiniciar SSH
sudo systemctl restart sshd
```

### 🔥 Configuración de Firewall

```bash
# Instalar UFW
sudo apt install ufw -y

# Denegar todo por defecto
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Permitir solo puertos necesarios
sudo ufw allow 22/tcp comment 'SSH'
sudo ufw allow 80/tcp comment 'HTTP'
sudo ufw allow 443/tcp comment 'HTTPS'

# Limitar intentos de conexión SSH (brute force protection)
sudo ufw limit 22/tcp

# Activar firewall
sudo ufw enable

# Verificar reglas
sudo ufw status numbered
```

### 🚫 Fail2Ban (Protección contra Brute Force)

```bash
# Instalar Fail2Ban
sudo apt install fail2ban -y

# Crear configuración personalizada
sudo nano /etc/fail2ban/jail.local
```

**Contenido de `/etc/fail2ban/jail.local`:**
```ini
[DEFAULT]
bantime = 3600       # Ban por 1 hora
findtime = 600       # Ventana de 10 minutos
maxretry = 3         # 3 intentos fallidos

[sshd]
enabled = true
port = 22
logpath = /var/log/auth.log
```

```bash
# Reiniciar Fail2Ban
sudo systemctl restart fail2ban

# Ver bans activos
sudo fail2ban-client status sshd
```

---

## 2. Gestión de Secrets y Variables de Entorno

### 🔐 Mejores Prácticas

1. **Nunca hardcodear secrets en el código**
   ```csharp
   // ❌ MAL
   var connectionString = "Server=localhost;Password=password123;";
   
   // ✅ BIEN
   var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
   ```

2. **Usar GitHub Secrets correctamente**
   - Uno por variable sensible
   - Nombres descriptivos en mayúsculas (DB_PASSWORD, JWT_SECRET)
   - Actualizar cuando rotes credenciales

3. **Generar secrets fuertes**
   ```bash
   # JWT Secret (64 bytes)
   openssl rand -base64 64
   
   # Passwords de bases de datos (32 bytes)
   openssl rand -base64 32
   
   # API Keys (hex 32 bytes)
   openssl rand -hex 32
   ```

4. **Archivo .env en el servidor**
   ```bash
   # Crear con permisos restrictivos
   touch .env.databases
   chmod 600 .env.databases
   
   # Solo el propietario puede leer/escribir
   ls -la .env.databases
   # -rw------- 1 github-deployer github-deployer
   ```

5. **Git ignore para archivos sensibles**
   ```gitignore
   # .gitignore
   .env
   .env.local
   .env.databases
   .env.production
   *.env
   
   # Claves y certificados
   *.key
   *.pem
   *.crt
   *.pfx
   
   # Archivos de configuración con secrets
   appsettings.Development.json
   appsettings.Production.json
   ```

### 🔄 Rotación de Secrets

#### Cada 3-6 meses, rotar:

1. **JWT Secret**
   ```bash
   # Generar nuevo
   NEW_JWT_SECRET=$(openssl rand -base64 64)
   
   # Actualizar en GitHub Secrets
   # Actualizar en servidor
   # Reiniciar backend
   ```

2. **Passwords de Bases de Datos**
   ```sql
   -- PostgreSQL
   ALTER USER admin_pg PASSWORD 'nuevo_password_seguro';
   
   -- MySQL
   ALTER USER 'admin_mysql'@'%' IDENTIFIED BY 'nuevo_password_seguro';
   ```

3. **API Keys (MercadoPago, etc.)**
   - Generar nuevo en panel del proveedor
   - Actualizar en GitHub Secrets
   - Verificar que funciona
   - Revocar el antiguo

---

## 3. Seguridad de Contenedores Docker

### 🐳 Mejores Prácticas

#### 1. Límites de Recursos (Prevenir DoS)

```yaml
services:
  backend:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
```

#### 2. Ejecutar como Usuario No-Root

**En Dockerfile:**
```dockerfile
# Crear usuario no-root
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Cambiar propietario de archivos
RUN chown -R appuser:appgroup /app

# Cambiar a usuario no-root
USER appuser
```

#### 3. Imágenes Actualizadas

```bash
# Actualizar regularmente
docker compose pull
docker compose up -d

# Verificar vulnerabilidades con Docker Scout
docker scout cves crudclouddb-api:latest
```

#### 4. No Exponer Docker Socket Innecesariamente

```yaml
# ⚠️ Solo si es absolutamente necesario
volumes:
  - /var/run/docker.sock:/var/run/docker.sock:ro  # Read-only
```

#### 5. Health Checks en Todos los Contenedores

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:5191/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

#### 6. Redes Aisladas

```bash
# Crear red personalizada
docker network create --driver bridge voyager_network

# Conectar solo contenedores que necesitan comunicarse
docker run --network voyager_network ...
```

### 🔒 Escaneo de Vulnerabilidades

```bash
# Instalar Trivy
sudo apt install wget apt-transport-https gnupg lsb-release -y
wget -qO - https://aquasecurity.github.io/trivy-repo/deb/public.key | sudo apt-key add -
echo "deb https://aquasecurity.github.io/trivy-repo/deb $(lsb_release -sc) main" | sudo tee /etc/apt/sources.list.d/trivy.list
sudo apt update && sudo apt install trivy -y

# Escanear imagen
trivy image crudclouddb-api:latest

# Escanear solo vulnerabilidades críticas y altas
trivy image --severity HIGH,CRITICAL crudclouddb-api:latest
```

---

## 4. Hardening de Bases de Datos

### PostgreSQL

```bash
# Conectarse a la base de datos
docker exec -it postgres_master psql -U admin_pg -d voyager_main
```

```sql
-- 1. Cambiar password por defecto
ALTER USER admin_pg PASSWORD 'nuevo_password_super_seguro';

-- 2. Revocar permisos públicos
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT ALL ON SCHEMA public TO admin_pg;

-- 3. Crear usuarios con permisos limitados (para apps)
CREATE USER app_readonly WITH PASSWORD 'password_readonly';
GRANT CONNECT ON DATABASE voyager_main TO app_readonly;
GRANT USAGE ON SCHEMA public TO app_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO app_readonly;

-- 4. Activar auditoría
ALTER DATABASE voyager_main SET log_statement = 'all';
ALTER DATABASE voyager_main SET log_connections = 'on';
ALTER DATABASE voyager_main SET log_disconnections = 'on';
```

### MySQL

```sql
-- Conectarse
docker exec -it mysql_master mysql -u root -p

-- 1. Eliminar usuarios anónimos
DELETE FROM mysql.user WHERE User='';

-- 2. Eliminar base de datos de prueba
DROP DATABASE IF EXISTS test;

-- 3. Limitar acceso por host
CREATE USER 'admin_mysql'@'172.20.%' IDENTIFIED BY 'password_seguro';
GRANT ALL PRIVILEGES ON voyager_main.* TO 'admin_mysql'@'172.20.%';

-- 4. Activar logs de consultas lentas (ya configurado en my.cnf)
SET GLOBAL slow_query_log = 'ON';
SET GLOBAL long_query_time = 2;

FLUSH PRIVILEGES;
```

### MongoDB

```javascript
// Conectarse
docker exec -it mongodb_master mongosh -u admin_mongo -p

// 1. Crear usuario con permisos limitados
use voyager_main
db.createUser({
  user: "app_user",
  pwd: "password_seguro",
  roles: [
    { role: "readWrite", db: "voyager_main" }
  ]
})

// 2. Activar auditoría
db.adminCommand({
  setParameter: 1,
  auditAuthorizationSuccess: true
})
```

### SQL Server

```sql
-- Conectarse
docker exec -it sqlserver_master /opt/mssql-tools/bin/sqlcmd -S localhost -U sa

-- 1. Cambiar password de SA
ALTER LOGIN sa WITH PASSWORD = 'NuevoPasswordComplejo123!';
GO

-- 2. Crear login con permisos limitados
CREATE LOGIN app_user WITH PASSWORD = 'PasswordSeguro123!';
GO
USE voyager_main;
CREATE USER app_user FOR LOGIN app_user;
ALTER ROLE db_datareader ADD MEMBER app_user;
ALTER ROLE db_datawriter ADD MEMBER app_user;
GO
```

---

## 5. Configuración de NGINX

### 🔐 Seguridad en nginx.conf

```nginx
http {
    # Ocultar versión de NGINX
    server_tokens off;
    
    # Tamaño máximo de request body
    client_max_body_size 10M;
    
    # Timeouts
    client_body_timeout 12;
    client_header_timeout 12;
    send_timeout 10;
    
    # Buffers
    client_body_buffer_size 1K;
    client_header_buffer_size 1k;
    large_client_header_buffers 2 1k;
    
    # Rate limiting (prevenir DDoS)
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    limit_conn_zone $binary_remote_addr zone=addr:10m;
    
    server {
        listen 443 ssl http2;
        server_name service.voyager.andrescortes.dev;
        
        # SSL/TLS
        ssl_certificate /etc/nginx/ssl/fullchain.pem;
        ssl_certificate_key /etc/nginx/ssl/privkey.pem;
        
        # Protocolos y ciphers seguros
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers 'ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384';
        ssl_prefer_server_ciphers on;
        
        # OCSP Stapling
        ssl_stapling on;
        ssl_stapling_verify on;
        
        # Sesiones SSL
        ssl_session_timeout 1d;
        ssl_session_cache shared:SSL:50m;
        ssl_session_tickets off;
        
        # Security headers
        add_header X-Frame-Options "SAMEORIGIN" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header X-XSS-Protection "1; mode=block" always;
        add_header Referrer-Policy "no-referrer-when-downgrade" always;
        add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;
        add_header Content-Security-Policy "default-src 'self' https:; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'" always;
        
        # Rate limiting
        limit_req zone=api_limit burst=20 nodelay;
        limit_conn addr 10;
        
        location / {
            proxy_pass http://backend_api;
            
            # Security headers para proxy
            proxy_hide_header X-Powered-By;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_set_header Host $host;
        }
    }
}
```

---

## 6. Backups y Recuperación

### 📦 Script de Backup Automático

**Crear archivo: `backup.sh`**

```bash
#!/bin/bash

BACKUP_DIR="/home/github-deployer/backups"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Backup PostgreSQL
docker exec postgres_master pg_dump -U admin_pg voyager_main | gzip > $BACKUP_DIR/postgres_$DATE.sql.gz

# Backup MySQL
docker exec mysql_master mysqldump -u root -p$MYSQL_ROOT_PASSWORD voyager_main | gzip > $BACKUP_DIR/mysql_$DATE.sql.gz

# Backup MongoDB
docker exec mongodb_master mongodump --username admin_mongo --password $MONGO_PASSWORD --authenticationDatabase admin --out /tmp/mongo_backup
docker cp mongodb_master:/tmp/mongo_backup $BACKUP_DIR/mongodb_$DATE
tar -czf $BACKUP_DIR/mongodb_$DATE.tar.gz -C $BACKUP_DIR mongodb_$DATE
rm -rf $BACKUP_DIR/mongodb_$DATE

# Mantener solo últimos 7 días
find $BACKUP_DIR -name "*.gz" -mtime +7 -delete

echo "Backup completado: $DATE"
```

```bash
# Hacer ejecutable
chmod +x backup.sh

# Agregar a crontab (diario a las 3 AM)
crontab -e
# Agregar:
0 3 * * * /home/github-deployer/Voyager-cloudDB-Back/backup.sh >> /home/github-deployer/backups/backup.log 2>&1
```

---

## 7. Monitoreo y Alertas

### 📊 Métricas a Monitorear

1. **Uso de Recursos**
   - CPU > 80% por más de 5 minutos
   - RAM > 90%
   - Disco > 85%
   - Swap > 50%

2. **Salud de Contenedores**
   - Contenedores reiniciándose
   - Health checks fallando
   - Contenedores detenidos

3. **Logs de Aplicación**
   - Errores 500
   - Fallos de autenticación
   - Consultas lentas (> 2 segundos)

### 🔔 Configurar Alertas con Discord Webhook

```bash
# Script de alerta: alert.sh
#!/bin/bash

WEBHOOK_URL="TU_DISCORD_WEBHOOK_URL"
MESSAGE="$1"

curl -H "Content-Type: application/json" -X POST -d "{\"content\": \"⚠️ ALERTA: $MESSAGE\"}" $WEBHOOK_URL
```

```bash
# Ejemplo de uso en monitoreo
if [ "$MEM_PERCENT" -gt 90 ]; then
    ./alert.sh "Memoria crítica: ${MEM_PERCENT}%"
fi
```

---

## 8. Checklist de Auditoría

### 🔍 Auditoría Mensual

```
[ ] Actualizar sistema operativo: sudo apt update && sudo apt upgrade
[ ] Actualizar imágenes Docker: docker compose pull
[ ] Revisar logs de SSH: journalctl -u sshd | grep Failed
[ ] Revisar logs de Fail2Ban: sudo fail2ban-client status
[ ] Verificar backups: ls -lh ~/backups
[ ] Revisar uso de disco: df -h
[ ] Escanear vulnerabilidades: trivy image crudclouddb-api:latest
[ ] Revisar certificados SSL: openssl x509 -in ssl/fullchain.pem -noout -dates
[ ] Verificar health checks: curl https://service.voyager.andrescortes.dev/health
[ ] Revisar logs de errores: docker logs crudclouddb_backend | grep ERROR
```

---

**✅ Implementar estas medidas reducirá significativamente el riesgo de seguridad de tu aplicación.**

**📅 Última actualización**: 2025-11-16


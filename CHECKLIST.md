# ✅ Checklist Interactivo - Migración Paso a Paso

## 📌 Instrucciones
- Copia este archivo y marca cada checkbox con [x] al completar
- Guarda tus notas y contraseñas en un lugar seguro
- No saltees pasos críticos marcados con 🔴

---

## 🎯 FASE 1: Preparación Local (10 minutos)

### Generar Claves SSH
- [ ] Crear directorio: `mkdir ~/.ssh/voyager-deploy`
- [ ] Generar clave: `ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/voyager-deploy/id_ed25519`
- [ ] Copiar clave pública: `cat ~/.ssh/voyager-deploy/id_ed25519.pub`
- [ ] 📝 Clave pública guardada en: ________________

### Preparar Contraseñas
- [ ] Generar JWT Secret (64 bytes): `openssl rand -base64 64`
- [ ] 📝 JWT Secret: ________________
- [ ] Generar password PostgreSQL: `openssl rand -base64 32`
- [ ] 📝 PG Password: ________________
- [ ] Generar password MySQL: `openssl rand -base64 32`
- [ ] 📝 MySQL Password: ________________
- [ ] Generar password MongoDB: `openssl rand -base64 32`
- [ ] 📝 Mongo Password: ________________
- [ ] Generar password SQL Server (min 8 chars, complejo)
- [ ] 📝 SQL Server Password: ________________

---

## 🖥️ FASE 2: Configuración del Servidor (20 minutos)

### Conexión Inicial
- [ ] 🔴 Conectar como root: `ssh root@IP_SERVIDOR`
- [ ] 📝 IP del servidor: ________________
- [ ] 📝 Usuario inicial: ________________

### Actualizar Sistema
- [ ] `sudo apt update`
- [ ] `sudo apt upgrade -y`

### Instalar Docker
- [ ] `curl -fsSL https://get.docker.com -o get-docker.sh`
- [ ] `sudo sh get-docker.sh`
- [ ] `sudo apt install docker-compose-plugin -y`
- [ ] Verificar: `docker --version`
- [ ] Verificar: `docker compose version`

### Configurar Swap 🔴 CRÍTICO
- [ ] Verificar actual: `free -h`
- [ ] Crear swap: `sudo fallocate -l 2G /swapfile`
- [ ] Permisos: `sudo chmod 600 /swapfile`
- [ ] Formatear: `sudo mkswap /swapfile`
- [ ] Activar: `sudo swapon /swapfile`
- [ ] Permanente: `echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab`
- [ ] Optimizar: `echo 'vm.swappiness=10' | sudo tee -a /etc/sysctl.conf`
- [ ] Aplicar: `sudo sysctl -p`
- [ ] Verificar: `free -h` (debe mostrar 2GB swap)

### Crear Usuario Deployer 🔴 CRÍTICO
- [ ] `sudo adduser --disabled-password --gecos "" github-deployer`
- [ ] `sudo usermod -aG docker github-deployer`
- [ ] Verificar: `groups github-deployer`

### Configurar SSH
- [ ] `sudo su - github-deployer`
- [ ] `mkdir -p ~/.ssh && chmod 700 ~/.ssh`
- [ ] `nano ~/.ssh/authorized_keys` (pegar clave pública)
- [ ] `chmod 600 ~/.ssh/authorized_keys`
- [ ] `exit`

### Probar Conexión SSH 🔴 CRÍTICO
- [ ] Desde local: `ssh -i ~/.ssh/voyager-deploy/id_ed25519 github-deployer@IP_SERVIDOR`
- [ ] ✅ Conexión exitosa sin password

### Configurar Firewall
- [ ] `sudo ufw allow 22/tcp`
- [ ] `sudo ufw allow 80/tcp`
- [ ] `sudo ufw allow 443/tcp`
- [ ] `sudo ufw --force enable`
- [ ] Verificar: `sudo ufw status`

### Hardening SSH (Opcional pero Recomendado)
- [ ] Backup: `sudo cp /etc/ssh/sshd_config /etc/ssh/sshd_config.backup`
- [ ] Editar: `sudo nano /etc/ssh/sshd_config`
  - [ ] `PermitRootLogin no`
  - [ ] `PasswordAuthentication no`
  - [ ] `AllowUsers github-deployer`
- [ ] Validar: `sudo sshd -t`
- [ ] Reiniciar: `sudo systemctl restart sshd`

---

## 📁 FASE 3: Estructura en Servidor (10 minutos)

### Crear Directorios
- [ ] `cd ~`
- [ ] `mkdir -p Voyager-cloudDB-Back/ssl`
- [ ] `mkdir -p Voyager-cloudDB-Back/data/{postgres,mysql,mongodb,sqlserver}`
- [ ] `mkdir -p Voyager-cloudDB-Back/logs`
- [ ] `cd Voyager-cloudDB-Back`

### Configurar Variables de Entorno
- [ ] `nano .env.databases`
- [ ] Pegar contenido del template
- [ ] Reemplazar passwords con los generados
- [ ] Guardar y salir
- [ ] `chmod 600 .env.databases`
- [ ] Verificar: `ls -la .env.databases` (debe ser -rw-------)

### Contenido de .env.databases:
```env
POSTGRES_USER=admin_pg
POSTGRES_PASSWORD=[TU_PASSWORD_PG]
POSTGRES_DB=voyager_main

MYSQL_ROOT_PASSWORD=[TU_PASSWORD_MYSQL_ROOT]
MYSQL_DATABASE=voyager_main
MYSQL_USER=admin_mysql
MYSQL_PASSWORD=[TU_PASSWORD_MYSQL]

MONGO_INITDB_ROOT_USERNAME=admin_mongo
MONGO_INITDB_ROOT_PASSWORD=[TU_PASSWORD_MONGO]
MONGO_INITDB_DATABASE=voyager_main

SA_PASSWORD=[TU_PASSWORD_SQLSERVER]
ACCEPT_EULA=Y
```

### Copiar Certificados SSL (si los tienes)
- [ ] `cd ssl`
- [ ] Copiar `fullchain.pem` desde local
- [ ] Copiar `privkey.pem` desde local
- [ ] Verificar: `ls -la`

---

## 🔐 FASE 4: GitHub Secrets (15 minutos)

### Acceder a GitHub
- [ ] Ir a repositorio en GitHub
- [ ] Settings → Secrets and variables → Actions
- [ ] Click "New repository secret"

### Secrets de Servidor (3)
- [ ] `SERVER_HOST` = [IP_DEL_SERVIDOR]
- [ ] `SERVER_USER` = github-deployer
- [ ] `SSH_PRIVATE_KEY` = [contenido COMPLETO de id_ed25519]

### Secrets de Base de Datos Principal (5)
- [ ] `DB_HOST` = localhost
- [ ] `DB_PORT` = 5432
- [ ] `DB_NAME` = voyager_main
- [ ] `DB_USER` = admin_pg
- [ ] `DB_PASSWORD` = [password PostgreSQL]

### Secrets de Hosts de Bases de Datos (4)
- [ ] `DB_HOST_POSTGRESQL` = postgres_master
- [ ] `DB_HOST_MYSQL` = mysql_master
- [ ] `DB_HOST_MONGODB` = mongodb_master
- [ ] `DB_HOST_SQLSERVER` = sqlserver_master

### Secrets de JWT (1)
- [ ] `JWT_SECRET` = [JWT Secret de 64 bytes]

### Secrets de Email (3)
- [ ] `SMTP_SENDER_EMAIL` = [tu-email@gmail.com]
- [ ] `SMTP_USERNAME` = [tu-email@gmail.com]
- [ ] `SMTP_PASSWORD` = [App Password de Gmail]

### Secrets de MercadoPago (4)
- [ ] `MERCADOPAGO_ACCESS_TOKEN` = [tu token]
- [ ] `MERCADOPAGO_PUBLIC_KEY` = [tu key]
- [ ] `MERCADOPAGO_NOTIFICATION_URL` = https://service.voyager.andrescortes.dev/api/webhooks/mercadopago
- [ ] `MERCADOPAGO_WEBHOOK_SECRET` = [openssl rand -base64 32]

### Opcional (1)
- [ ] `DISCORD_WEBHOOK_URL` = [tu webhook Discord]

### Verificar Total
- [ ] ✅ Total de secrets: 20 (o 21 con Discord)

---

## 🚀 FASE 5: Primer Deploy Manual (20 minutos)

### Clonar Repositorio
- [ ] `cd ~/Voyager-cloudDB-Back`
- [ ] `git config --global user.name "GitHub Deployer"`
- [ ] `git config --global user.email "deployer@voyager.local"`
- [ ] `git clone -b deployment/docker-nginx https://github.com/TU_USUARIO/TU_REPO.git tmp`
- [ ] 📝 URL del repo: ________________
- [ ] `cp -r tmp/* .`
- [ ] `cp -r tmp/.github .`
- [ ] `rm -rf tmp`
- [ ] Verificar: `ls -la` (debe mostrar docker-compose.yml, Dockerfile, etc.)

### Crear Red Docker
- [ ] `docker network create voyager_network`
- [ ] Verificar: `docker network ls | grep voyager`

### Iniciar Bases de Datos 🔴 CRÍTICO
- [ ] `export $(cat .env.databases | xargs)`
- [ ] `docker compose -f docker-compose.databases.yml up -d`
- [ ] Esperar 2 minutos: `sleep 120`
- [ ] Ver logs: `docker compose -f docker-compose.databases.yml logs -f`
- [ ] Presionar Ctrl+C cuando veas "ready for connections"

### Verificar Bases de Datos
- [ ] `docker ps` (debe mostrar 4 contenedores)
- [ ] postgres_master - RUNNING
- [ ] mysql_master - RUNNING
- [ ] mongodb_master - RUNNING
- [ ] sqlserver_master - RUNNING

### Build Backend
- [ ] `docker build -t crudclouddb-api:latest .`
- [ ] Esperar 3-5 minutos
- [ ] Verificar: `docker images | grep crudclouddb-api`

### Iniciar Backend
- [ ] Ejecutar comando docker run con todas las variables
- [ ] (Ver QUICK_START.md paso 5 para comando completo)
- [ ] Esperar 30 segundos
- [ ] Ver logs: `docker logs -f crudclouddb_backend`
- [ ] Buscar: "Application started"

### Verificar Backend 🔴 CRÍTICO
- [ ] `curl http://localhost:5191/health`
- [ ] ✅ Debe retornar: "Healthy"

### Iniciar NGINX
- [ ] `docker compose up -d nginx`
- [ ] Esperar 5 segundos
- [ ] Ver logs: `docker logs voyager-backend-nginx`

### Verificar NGINX
- [ ] `curl http://localhost/health`
- [ ] `curl -k https://localhost/health` (si tienes SSL)

---

## ✅ FASE 6: Verificación Final (10 minutos)

### Ejecutar Script de Verificación
- [ ] `chmod +x verify-deployment.sh`
- [ ] `./verify-deployment.sh`
- [ ] Porcentaje de éxito: _____ %
- [ ] ✅ Debe ser > 90%

### Verificaciones Manuales
- [ ] `docker ps` - 6 contenedores corriendo
- [ ] `docker stats --no-stream` - Memoria < 4GB
- [ ] `free -h` - Swap disponible
- [ ] `df -h` - Espacio en disco > 2GB

### Tests de Conectividad
- [ ] Backend: `curl http://localhost:5191/health`
- [ ] NGINX HTTP: `curl http://localhost/health`
- [ ] NGINX HTTPS: `curl -k https://localhost/health`
- [ ] Público: `curl https://service.voyager.andrescortes.dev/health`

---

## 🔄 FASE 7: Activar CI/CD (5 minutos)

### Hacer Push para Activar GitHub Actions
- [ ] En tu máquina local
- [ ] `git add .`
- [ ] `git commit -m "feat: setup optimized deployment configuration"`
- [ ] `git push origin deployment/docker-nginx`

### Monitorear Deploy
- [ ] Ir a GitHub → Actions
- [ ] Ver workflow "🚀 Deploy Backend to Production"
- [ ] ✅ Debe completarse en 5-8 minutos

### Verificar en Servidor
- [ ] `docker ps` - Contenedores actualizados
- [ ] `docker logs --tail 50 crudclouddb_backend`
- [ ] Sin errores críticos

---

## 🎉 FASE 8: Post-Deploy (10 minutos)

### Configurar Backups
- [ ] `chmod +x backup.sh`
- [ ] Probar: `./backup.sh`
- [ ] Verificar backups: `ls -lh ~/backups/`
- [ ] Configurar cron: `crontab -e`
- [ ] Agregar: `0 3 * * * /home/github-deployer/Voyager-cloudDB-Back/backup.sh`

### Configurar Monitoreo
- [ ] `chmod +x monitor.sh`
- [ ] Probar: `./monitor.sh full`
- [ ] Crear alias: `echo "alias mon='~/Voyager-cloudDB-Back/monitor.sh'" >> ~/.bashrc`
- [ ] `source ~/.bashrc`

### Documentar
- [ ] Guardar passwords en gestor seguro (LastPass, 1Password, etc.)
- [ ] Documentar IP del servidor
- [ ] Documentar usuarios y permisos
- [ ] Guardar backup de claves SSH

---

## 🔐 FASE 9: Seguridad Adicional (Opcional, 15 minutos)

### Instalar Fail2Ban
- [ ] `sudo apt install fail2ban -y`
- [ ] `sudo systemctl enable fail2ban`
- [ ] `sudo systemctl start fail2ban`
- [ ] Verificar: `sudo fail2ban-client status sshd`

### Configurar Alertas Discord
- [ ] Crear webhook en Discord
- [ ] Agregar `DISCORD_WEBHOOK_URL` a GitHub Secrets
- [ ] Probar: `curl -X POST -H "Content-Type: application/json" -d '{"content":"Test"}' TU_WEBHOOK_URL`

### Auditoría Final
- [ ] `sudo ufw status` - Firewall activo
- [ ] `sudo systemctl status fail2ban` - Activo
- [ ] `grep PermitRootLogin /etc/ssh/sshd_config` - debe ser "no"
- [ ] `ls -la .env.databases` - permisos 600

---

## 📊 Resumen de Progreso

```
Total de tareas: ~120
Completadas: _____ / 120
Porcentaje: _____ %

Tiempo estimado: 40-45 minutos
Tiempo real: _____ minutos
```

---

## 🆘 Si Algo Sale Mal

### Backend no responde
```bash
docker logs crudclouddb_backend
docker restart crudclouddb_backend
curl http://localhost:5191/health
```

### Base de datos no conecta
```bash
docker logs postgres_master
docker exec -it postgres_master psql -U admin_pg -d voyager_main
```

### Sin memoria
```bash
free -h
docker stats --no-stream
# Si es necesario, reiniciar contenedor más pesado
docker restart sqlserver_master
```

### GitHub Actions falla
```
1. Verificar todos los secrets están configurados
2. Revisar logs en GitHub Actions
3. Verificar SSH: ssh -i ~/.ssh/voyager-deploy/id_ed25519 github-deployer@IP
```

---

## ✅ Checklist de Finalización

- [ ] Todos los contenedores corriendo (6)
- [ ] verify-deployment.sh > 90%
- [ ] Backend responde en todos los endpoints
- [ ] GitHub Actions ejecutándose correctamente
- [ ] Backups configurados (cron)
- [ ] Passwords guardados de forma segura
- [ ] Documentación leída y entendida
- [ ] Rollback probado manualmente
- [ ] Monitoreo activo

---

**🎊 ¡Felicitaciones! Migración completada exitosamente.**

**Próximos pasos:**
1. Monitorear durante 24 horas
2. Revisar logs diariamente
3. Verificar backups semanalmente
4. Actualizar sistema mensualmente

**📅 Fecha de migración**: ________________  
**✅ Estado**: ________________  
**📝 Notas**: ________________

---


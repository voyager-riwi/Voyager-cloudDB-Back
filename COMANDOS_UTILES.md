# 🛠️ COMANDOS ÚTILES - VOYAGER CLOUDDB

Cheatsheet de comandos para el día a día en el servidor.

---

## 🚀 Deploy y Mantenimiento

### Despliegue Completo
```bash
cd ~/Voyager-cloudDB-Back
./deploy-production.sh
```

### Actualizar Código sin Redesplegar
```bash
cd ~/Voyager-cloudDB-Back
git fetch origin deployment/docker-nginx
git reset --hard origin/deployment/docker-nginx
```

### Redesplegar Solo Backend (sin tocar bases de datos)
```bash
cd ~/Voyager-cloudDB-Back

# Build nueva imagen
docker build -t crudclouddb-api:latest .

# Detener backend actual
docker stop crudclouddb_backend
docker rm crudclouddb_backend

# Iniciar nuevo backend
docker run -d \
  --name crudclouddb_backend \
  --restart unless-stopped \
  --network voyager_network \
  --memory="768m" \
  --cpus="1.0" \
  -p 5191:5191 \
  --env-file .env \
  -e DB_HOST=postgres_master \
  -e DB_HOST_POSTGRESQL=postgres_master \
  -e DB_HOST_MYSQL=mysql_master \
  -e DB_HOST_MONGODB=mongodb_master \
  -e DB_HOST_SQLSERVER=sqlserver_master \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://0.0.0.0:5191 \
  -v /root/Voyager-cloudDB-Back/logs:/app/logs \
  crudclouddb-api:latest
```

---

## 📊 Monitoreo

### Ver Estado de Todos los Contenedores
```bash
docker ps
```

### Ver Uso de Recursos
```bash
docker stats --no-stream
```

### Ver Uso de Memoria del Sistema
```bash
free -h
```

### Ver Espacio en Disco
```bash
df -h
```

### Ver Procesos del Sistema
```bash
top
# O mejor aún:
htop
```

---

## 📋 Logs

### Logs del Backend (últimas 100 líneas)
```bash
docker logs crudclouddb_backend --tail 100
```

### Logs del Backend (en tiempo real)
```bash
docker logs crudclouddb_backend -f
```

### Logs de PostgreSQL
```bash
docker logs postgres_master --tail 100
docker logs postgres_master -f
```

### Logs de MySQL
```bash
docker logs mysql_master --tail 100
docker logs mysql_master -f
```

### Logs de MongoDB
```bash
docker logs mongodb_master --tail 100
docker logs mongodb_master -f
```

### Logs de SQL Server
```bash
docker logs sqlserver_master --tail 100
docker logs sqlserver_master -f
```

### Logs de Nginx
```bash
docker logs voyager-backend-nginx --tail 100
docker logs voyager-backend-nginx -f
```

### Buscar Errores en Logs
```bash
# Backend
docker logs crudclouddb_backend 2>&1 | grep -i error

# Cualquier base de datos
docker logs postgres_master 2>&1 | grep -i error
docker logs mysql_master 2>&1 | grep -i error
```

---

## 🔍 Health Checks

### Backend API Health
```bash
curl http://localhost:5191/health
```

### Backend API Health (con formato JSON)
```bash
curl -s http://localhost:5191/health | jq .
```

### Backend API Health (externo HTTPS)
```bash
curl https://service.voyager.andrescortes.dev/health
```

### Verificar Nginx
```bash
docker exec voyager-backend-nginx nginx -t
```

### Verificar que los Puertos Estén Escuchando
```bash
ss -tlnp | grep -E ':(5191|80|443|5432|3306|27017|1433)'
```

---

## 🗄️ Gestión de Bases de Datos

### Reiniciar Todas las Bases de Datos
```bash
cd ~/Voyager-cloudDB-Back
docker-compose -f docker-compose.databases.yml restart
```

### Reiniciar Solo PostgreSQL
```bash
docker restart postgres_master
```

### Reiniciar Solo MySQL
```bash
docker restart mysql_master
```

### Reiniciar Solo MongoDB
```bash
docker restart mongodb_master
```

### Reiniciar Solo SQL Server
```bash
docker restart sqlserver_master
```

### Detener Todas las Bases de Datos
```bash
cd ~/Voyager-cloudDB-Back
docker-compose -f docker-compose.databases.yml down
```

### Levantar Todas las Bases de Datos
```bash
cd ~/Voyager-cloudDB-Back
docker-compose -f docker-compose.databases.yml up -d
```

### Ver Salud de Bases de Datos
```bash
docker ps --filter "name=master" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

---

## 🔧 Gestión del Backend

### Reiniciar Backend
```bash
docker restart crudclouddb_backend
```

### Detener Backend
```bash
docker stop crudclouddb_backend
```

### Iniciar Backend (si está detenido)
```bash
docker start crudclouddb_backend
```

### Ver Variables de Entorno del Backend
```bash
docker inspect crudclouddb_backend | grep -A 30 "Env"
```

### Entrar al Contenedor del Backend (Shell)
```bash
docker exec -it crudclouddb_backend bash
```

---

## 🧹 Limpieza

### Limpiar Contenedores Detenidos
```bash
docker container prune -f
```

### Limpiar Imágenes No Usadas
```bash
docker image prune -a -f
```

### Limpiar Volúmenes No Usados
```bash
docker volume prune -f
```

### Limpieza Completa (CUIDADO: borra todo lo no usado)
```bash
docker system prune -a --volumes -f
```

### Ver Espacio Usado por Docker
```bash
docker system df
```

---

## 📦 Gestión de Volúmenes

### Listar Volúmenes
```bash
docker volume ls
```

### Ver Detalles de un Volumen
```bash
docker volume inspect voyager_postgres_data
docker volume inspect voyager_mysql_data
docker volume inspect voyager_mongodb_data
docker volume inspect voyager_sqlserver_data
```

### Eliminar Volumen Específico (CUIDADO: borra datos)
```bash
# PostgreSQL
docker volume rm voyager_postgres_data

# MySQL
docker volume rm voyager_mysql_data

# MongoDB
docker volume rm voyager_mongodb_data

# SQL Server
docker volume rm voyager_sqlserver_data
```

---

## 🌐 Gestión de Red

### Listar Redes Docker
```bash
docker network ls
```

### Ver Detalles de la Red de Voyager
```bash
docker network inspect voyager_network
```

### Ver IPs de Contenedores en la Red
```bash
docker network inspect voyager_network | grep -A 5 "Containers"
```

---

## 🔐 Seguridad y Certificados SSL

### Ver Certificados SSL
```bash
cd ~/Voyager-cloudDB-Back
ls -la ssl/
```

### Verificar Validez del Certificado
```bash
openssl x509 -in ssl/fullchain.pem -text -noout
```

### Verificar Fecha de Expiración del Certificado
```bash
openssl x509 -in ssl/fullchain.pem -noout -dates
```

---

## 🔄 Git y Código

### Ver Estado del Repositorio
```bash
cd ~/Voyager-cloudDB-Back
git status
```

### Ver Commits Recientes
```bash
git log --oneline -10
```

### Ver Diferencias con el Remoto
```bash
git fetch origin
git diff HEAD origin/deployment/docker-nginx
```

### Descartar Cambios Locales
```bash
git reset --hard HEAD
git clean -fd
```

---

## 🐛 Debugging Avanzado

### Ver Procesos Dentro del Contenedor Backend
```bash
docker top crudclouddb_backend
```

### Ver Eventos de Docker
```bash
docker events --since 1h
```

### Ver Logs del Sistema (systemd)
```bash
journalctl -xe
```

### Ver Conexiones de Red Activas
```bash
netstat -tuln | grep -E ':(5191|80|443|5432|3306|27017|1433)'
```

### Test de Conectividad a Bases de Datos

#### PostgreSQL
```bash
docker exec -it postgres_master psql -U admin_pg -d voyager_main -c "SELECT version();"
```

#### MySQL
```bash
docker exec -it mysql_master mysql -u root -p${MYSQL_ROOT_PASSWORD} -e "SELECT VERSION();"
```

#### MongoDB
```bash
docker exec -it mongodb_master mongosh -u admin_mongo -p${MONGO_INITDB_ROOT_PASSWORD} --eval "db.version()"
```

#### SQL Server
```bash
docker exec -it sqlserver_master /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P ${SA_PASSWORD} -Q "SELECT @@VERSION"
```

---

## 🔥 Comandos de Emergencia

### Reiniciar Todo (Backend + Bases de Datos)
```bash
cd ~/Voyager-cloudDB-Back

# Detener todo
docker stop crudclouddb_backend
docker-compose -f docker-compose.databases.yml down

# Esperar 10 segundos
sleep 10

# Levantar bases de datos
docker-compose -f docker-compose.databases.yml up -d

# Esperar 30 segundos
sleep 30

# Levantar backend
docker start crudclouddb_backend
```

### Liberar Memoria (si el servidor está lento)
```bash
# Limpiar cache del sistema
sync && echo 3 > /proc/sys/vm/drop_caches

# Reiniciar Docker (último recurso)
systemctl restart docker
```

### Rollback Completo del Backend
```bash
# Detener backend actual
docker stop crudclouddb_backend
docker rm crudclouddb_backend

# Usar imagen de backup
docker tag crudclouddb-api:previous crudclouddb-api:latest

# Redesplegar
./deploy-production.sh
```

---

## 📊 Comandos de Monitoreo Continuo

### Monitoreo en Tiempo Real (Auto-refresh cada 5s)
```bash
watch -n 5 "docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
```

### Monitoreo de Recursos (Auto-refresh cada 5s)
```bash
watch -n 5 "docker stats --no-stream --format 'table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}'"
```

### Monitoreo de Health Check (Auto-refresh cada 10s)
```bash
watch -n 10 "curl -s http://localhost:5191/health | jq ."
```

---

## 💾 Backups

### Crear Backup de PostgreSQL
```bash
docker exec postgres_master pg_dump -U admin_pg voyager_main > backup_postgres_$(date +%Y%m%d_%H%M%S).sql
```

### Crear Backup de MySQL
```bash
docker exec mysql_master mysqldump -u root -p${MYSQL_ROOT_PASSWORD} voyager_main > backup_mysql_$(date +%Y%m%d_%H%M%S).sql
```

### Crear Backup de MongoDB
```bash
docker exec mongodb_master mongodump --username admin_mongo --password ${MONGO_INITDB_ROOT_PASSWORD} --authenticationDatabase admin --db voyager_main --out /tmp/backup
docker cp mongodb_master:/tmp/backup ./backup_mongodb_$(date +%Y%m%d_%H%M%S)
```

---

## 🎯 Atajos Útiles

Añade estos alias a tu `~/.bashrc` para comandos más rápidos:

```bash
# Editar ~/.bashrc
nano ~/.bashrc

# Añadir al final:
alias dps='docker ps'
alias dlogs='docker logs'
alias dstats='docker stats --no-stream'
alias voyager='cd ~/Voyager-cloudDB-Back'
alias backend-logs='docker logs crudclouddb_backend -f'
alias backend-health='curl -s http://localhost:5191/health | jq .'
alias deploy='cd ~/Voyager-cloudDB-Back && ./deploy-production.sh'

# Guardar y recargar
source ~/.bashrc
```

Ahora puedes usar:
```bash
dps                # Ver contenedores
backend-logs       # Ver logs del backend
backend-health     # Ver health check
deploy             # Redesplegar todo
```

---

**💡 Tip**: Guarda este archivo en favoritos para consulta rápida.

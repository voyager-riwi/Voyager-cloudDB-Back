# 🚀 GUÍA DE DESPLIEGUE MANUAL - VOYAGER CLOUDDB

Esta guía te llevará paso a paso para desplegar el backend en el servidor de producción.

---

## ✅ Pre-requisitos

- Acceso SSH al servidor (`root@91.98.42.248`)
- Docker y Docker Compose instalados
- Git configurado
- Puertos abiertos: 80, 443, 5191, 5432, 3306, 27017, 1433

---

## 📋 PASOS DE DESPLIEGUE

### **PASO 1: Conectar al Servidor**

```bash
ssh root@91.98.42.248
cd ~/Voyager-cloudDB-Back
```

---

### **PASO 2: Hacer Pull de los Cambios Más Recientes**

```bash
# Descargar los cambios del repositorio
git fetch origin deployment/docker-nginx

# Aplicar los cambios (esto sobrescribirá archivos locales)
git reset --hard origin/deployment/docker-nginx

# Limpiar archivos no rastreados
git clean -fd

# Verificar que tienes los archivos actualizados
ls -la

# Debes ver:
# - deploy-production.sh (NUEVO)
# - docker-compose.databases.yml (ACTUALIZADO)
# - .env.databases.example (NUEVO)
```

---

### **PASO 3: Crear Archivo `.env.databases`**

Este archivo contiene las credenciales de las bases de datos master.

```bash
# Crear el archivo con contraseñas seguras
cat > .env.databases << 'EOF'
# PostgreSQL Master
POSTGRES_USER=admin_pg
POSTGRES_PASSWORD=VoyagerPostgres2024!Secure
POSTGRES_DB=voyager_main

# MySQL Master
MYSQL_ROOT_PASSWORD=VoyagerMySQL2024!SecureRoot
MYSQL_DATABASE=voyager_main
MYSQL_USER=admin_mysql
MYSQL_PASSWORD=VoyagerMySQL2024!Secure

# MongoDB Master
MONGO_INITDB_ROOT_USERNAME=admin_mongo
MONGO_INITDB_ROOT_PASSWORD=VoyagerMongo2024!Secure
MONGO_INITDB_DATABASE=voyager_main

# SQL Server Master (mínimo 8 caracteres, mayúsculas, minúsculas, números y símbolos)
SA_PASSWORD=VoyagerSQL2024!SecurePass
EOF

# Proteger el archivo
chmod 600 .env.databases

# Verificar que se creó correctamente
cat .env.databases
```

⚠️ **IMPORTANTE**: Estas contraseñas son de ejemplo. **Cámbialas por contraseñas seguras únicas**.

---

### **PASO 4: Dar Permisos de Ejecución al Script de Despliegue**

```bash
chmod +x deploy-production.sh
```

---

### **PASO 5: Ejecutar el Script de Despliegue**

```bash
# Ejecutar el script de despliegue
./deploy-production.sh
```

El script hará automáticamente:
1. ✅ Verificar pre-requisitos (espacio en disco, memoria)
2. 💾 Crear backup del backend actual
3. ⬇️ Descargar código más reciente (ya hecho en paso 2)
4. 🔧 Arreglar MySQL (si está fallando)
5. 🗄️ Reiniciar bases de datos master con configuración optimizada
6. 🔨 Construir nueva imagen Docker del backend
7. ▶️ Desplegar nuevo contenedor backend
8. 🏥 Ejecutar health checks

---

### **PASO 6: Verificar que Todo Funciona**

```bash
# Ver estado de todos los contenedores
docker ps

# Debes ver TODOS estos contenedores corriendo:
# - crudclouddb_backend (healthy)
# - voyager-backend-nginx
# - postgres_master (healthy)
# - mysql_master (healthy)
# - mongodb_master (healthy)
# - sqlserver_master (healthy o starting)

# Ver logs del backend
docker logs crudclouddb_backend --tail 50

# Probar el health check local
curl http://localhost:5191/health

# Probar el health check externo (desde tu PC)
curl https://service.voyager.andrescortes.dev/health
```

---

### **PASO 7: Verificar Recursos del Servidor**

```bash
# Ver uso de memoria y CPU
docker stats --no-stream

# Ver memoria del sistema
free -h

# Ver espacio en disco
df -h
```

**Consumo esperado:**
- PostgreSQL: ~19MB RAM
- MySQL: ~50-100MB RAM
- MongoDB: ~15MB RAM
- SQL Server: ~57MB RAM
- Backend: ~7MB RAM
- Nginx: <1MB RAM
- **Total**: ~150-200MB RAM (de 3.7GB disponibles)

---

## 🔧 SOLUCIÓN DE PROBLEMAS

### **MySQL no inicia (Restarting)**

```bash
# Ver logs de MySQL
docker logs mysql_master --tail 100

# Si dice "query-cache-type deprecated":
docker stop mysql_master
docker rm mysql_master
docker volume rm voyager_mysql_data
docker-compose -f docker-compose.databases.yml up -d mysql_master
```

### **SQL Server unhealthy**

```bash
# Ver logs de SQL Server
docker logs sqlserver_master --tail 100

# Si dice "Password did not match":
# Verificar que la contraseña en .env.databases cumple requisitos:
# - Mínimo 8 caracteres
# - Al menos 1 mayúscula
# - Al menos 1 minúscula
# - Al menos 1 número
# - Al menos 1 símbolo especial

# Reiniciar SQL Server
docker stop sqlserver_master
docker rm sqlserver_master
docker volume rm voyager_sqlserver_data
docker-compose -f docker-compose.databases.yml up -d sqlserver_master
```

### **Backend no responde**

```bash
# Ver logs del backend
docker logs crudclouddb_backend

# Reiniciar backend
docker restart crudclouddb_backend

# Si sigue sin funcionar, verificar variables de entorno
docker inspect crudclouddb_backend | grep -A 30 "Env"
```

### **Nginx no redirige correctamente**

```bash
# Ver logs de Nginx
docker logs voyager-backend-nginx

# Probar configuración de Nginx
docker exec voyager-backend-nginx nginx -t

# Reiniciar Nginx
docker restart voyager-backend-nginx
```

---

## 🔄 ROLLBACK (Si algo sale mal)

Si el despliegue falla y necesitas volver a la versión anterior:

```bash
# Detener y eliminar backend actual
docker stop crudclouddb_backend
docker rm crudclouddb_backend

# Restaurar imagen anterior
docker tag crudclouddb-api:previous crudclouddb-api:latest

# Reiniciar backend con imagen anterior
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

## 📊 MONITOREO CONTINUO

### Ver logs en tiempo real

```bash
# Backend
docker logs crudclouddb_backend -f

# Todas las bases de datos
docker logs postgres_master -f
docker logs mysql_master -f
docker logs mongodb_master -f
docker logs sqlserver_master -f
```

### Verificar salud periódicamente

```bash
# Health check del backend
watch -n 10 curl -s http://localhost:5191/health

# Estado de contenedores
watch -n 5 docker ps
```

---

## ✅ CHECKLIST POST-DESPLIEGUE

- [ ] Todos los contenedores están corriendo (`docker ps`)
- [ ] Backend responde al health check local (`curl http://localhost:5191/health`)
- [ ] Backend responde al health check externo (`curl https://service.voyager.andrescortes.dev/health`)
- [ ] PostgreSQL está healthy
- [ ] MySQL está healthy
- [ ] MongoDB está healthy
- [ ] SQL Server está healthy o iniciando
- [ ] Nginx está corriendo
- [ ] Memoria del servidor < 80% (`free -h`)
- [ ] No hay errores críticos en logs del backend

---

## 🎯 PRÓXIMOS PASOS

Una vez que el backend esté funcionando correctamente:

1. **Configurar GitHub Actions** (opcional, para deploy automático)
2. **Desplegar Frontend** (Vue.js)
3. **Configurar monitoreo** (opcional, Prometheus/Grafana)
4. **Configurar backups automáticos** de bases de datos

---

## 📞 SOPORTE

Si tienes problemas, comparte:
1. Salida del comando `docker ps`
2. Logs del backend: `docker logs crudclouddb_backend --tail 100`
3. Logs de la base de datos problemática
4. Uso de recursos: `docker stats --no-stream`

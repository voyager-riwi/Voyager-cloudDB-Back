# ⚡ PASOS INMEDIATOS - EJECUTAR AHORA

## 🎯 Objetivo
Desplegar el backend y arreglar las bases de datos en 15 minutos.

---

## ✅ CHECKLIST DE EJECUCIÓN

### **PASO 1: Commit y Push desde tu PC** 💻

```bash
# En tu máquina local (donde tienes el código)
cd c:\Users\maria\RiderProjects\CrudCloudDb-voyager

# Ver archivos modificados
git status

# Añadir archivos nuevos/modificados
git add docker-compose.databases.yml
git add deploy-production.sh
git add GUIA_DESPLIEGUE_MANUAL.md
git add RESUMEN_DESPLIEGUE.md
git add .env.databases.example
git add DIAGNOSTICO_SERVIDOR.md
git add DEPLOY_MANUAL_VS_ACTIONS.md
git add COMANDOS_UTILES.md
git add PASOS_INMEDIATOS.md
git add COMMIT_MESSAGE.md
git add README.md

# Commit
git commit -m "feat: Optimizar despliegue y arreglar bases de datos master

- Reducir límites de memoria de DBs (~840MB ahorro)
- Eliminar parámetros deprecated de MySQL
- Script automatizado de despliegue
- Documentación completa

Ver RESUMEN_DESPLIEGUE.md para detalles"

# Push a tu rama actual
git push origin feature/Miguel

# IMPORTANTE: También push a la rama de deployment
git push origin feature/Miguel:deployment/docker-nginx
```

⏱️ **Tiempo**: 2 minutos

---

### **PASO 2: Conectar al Servidor** 🔐

```bash
# Desde tu PC, abrir terminal y conectar via SSH
ssh root@91.98.42.248

# Verificar que estás en el servidor
hostname
# Debe mostrar: tren-voyager
```

⏱️ **Tiempo**: 30 segundos

---

### **PASO 3: Actualizar Código en el Servidor** ⬇️

```bash
# En el servidor
cd ~/Voyager-cloudDB-Back

# Descargar los cambios
git fetch origin deployment/docker-nginx
git reset --hard origin/deployment/docker-nginx
git clean -fd

# Verificar que los archivos nuevos están ahí
ls -la deploy-production.sh
ls -la GUIA_DESPLIEGUE_MANUAL.md

# Dar permisos de ejecución al script
chmod +x deploy-production.sh
```

⏱️ **Tiempo**: 1 minuto

---

### **PASO 4: Crear Archivo .env.databases** 🔐

```bash
# Crear el archivo con credenciales
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

# SQL Server Master
SA_PASSWORD=VoyagerSQL2024!SecurePass
EOF

# Proteger el archivo
chmod 600 .env.databases

# Verificar que se creó correctamente
cat .env.databases
```

⚠️ **IMPORTANTE**: Estas contraseñas son de ejemplo. Idealmente cámbialas por contraseñas únicas.

⏱️ **Tiempo**: 1 minuto

---

### **PASO 5: Ejecutar Deploy** 🚀

```bash
# Ejecutar el script de despliegue
./deploy-production.sh
```

El script hará automáticamente:
- ✅ Verificar pre-requisitos
- 💾 Crear backup del backend actual
- 🔧 Arreglar MySQL (si está fallando)
- 🗄️ Reiniciar bases de datos con configuración optimizada
- 🔨 Construir nueva imagen Docker
- ▶️ Desplegar nuevo backend
- 🏥 Ejecutar health checks

⏱️ **Tiempo**: 8-10 minutos

**Durante el proceso verás**:
```
=========================================
🚀 VOYAGER CLOUDDB - DESPLIEGUE MANUAL
=========================================
📅 Fecha: ...

🔍 [1/8] Verificando pre-requisitos...
✅ Espacio en disco: XXG disponibles
...
✅ DESPLIEGUE COMPLETADO EXITOSAMENTE
=========================================
```

---

### **PASO 6: Verificar que Todo Funciona** ✅

```bash
# Ver estado de todos los contenedores
docker ps

# Debes ver TODOS estos contenedores con "Up" o "healthy":
# - crudclouddb_backend
# - voyager-backend-nginx
# - postgres_master
# - mysql_master
# - mongodb_master
# - sqlserver_master

# Probar health check local
curl http://localhost:5191/health

# Debe responder:
# {"status":"healthy","timestamp":"...","environment":"Production","version":"1.0.0"}

# Ver uso de recursos
docker stats --no-stream
```

⏱️ **Tiempo**: 2 minutos

---

### **PASO 7: Probar desde el Exterior** 🌐

```bash
# DESDE TU PC (no el servidor), abrir otra terminal

# Probar health check externo HTTPS
curl https://service.voyager.andrescortes.dev/health

# Debe responder igual que el local
```

⏱️ **Tiempo**: 30 segundos

---

## 🎉 ¡LISTO!

Si todos los pasos funcionaron correctamente:

✅ Backend desplegado y funcionando  
✅ Todas las bases de datos corriendo  
✅ Memoria optimizada (~70-80% vs 100% anterior)  
✅ MySQL arreglado (sin reinicio continuo)  
✅ SQL Server con contraseña correcta  

---

## 🐛 Si Algo Falla

### MySQL sigue reiniciándose
```bash
docker logs mysql_master --tail 50
# Ver el error específico y compartirlo
```

### SQL Server unhealthy
```bash
docker logs sqlserver_master --tail 50
# Verificar que la contraseña cumpla requisitos
```

### Backend no responde
```bash
docker logs crudclouddb_backend --tail 100
# Ver errores en logs
```

### Memoria sigue alta
```bash
docker stats --no-stream
free -h
# Compartir los valores
```

---

## 📞 Soporte

Si encuentras problemas, comparte:

1. **Salida de**: `docker ps`
2. **Logs del componente problemático**: `docker logs [nombre_contenedor] --tail 100`
3. **Uso de recursos**: `docker stats --no-stream`

---

## ⏱️ RESUMEN DE TIEMPOS

| Paso | Tiempo | Acumulado |
|------|--------|-----------|
| 1. Commit y Push | 2 min | 2 min |
| 2. Conectar SSH | 30 seg | 2.5 min |
| 3. Actualizar código | 1 min | 3.5 min |
| 4. Crear .env.databases | 1 min | 4.5 min |
| 5. Ejecutar deploy | 8-10 min | 12-14.5 min |
| 6. Verificar | 2 min | 14-16.5 min |
| 7. Test externo | 30 seg | 14.5-17 min |

**TOTAL**: ~15-17 minutos ⚡

---

## 🎯 DESPUÉS DEL DEPLOY

### Monitoreo (primeras 24 horas)

```bash
# Ver logs en tiempo real (mantén esta terminal abierta)
docker logs crudclouddb_backend -f

# En otra terminal, ver recursos cada 5 minutos
watch -n 300 "docker stats --no-stream && echo '---' && free -h"
```

### Si todo va bien durante 1-2 días

✅ Considerar migrar a GitHub Actions (ver `DEPLOY_MANUAL_VS_ACTIONS.md`)  
✅ Configurar backups automáticos  
✅ Desplegar frontend (Vue.js)  

---

**¡Éxito con el despliegue! 🚀**

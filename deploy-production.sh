#!/bin/bash

# ==========================================
# 🚀 SCRIPT DE DESPLIEGUE MANUAL - VOYAGER CLOUDDB
# ==========================================
# Despliega el backend y las bases de datos en producción
# Autor: Voyager Team
# ==========================================

set -e  # Exit on error

echo "========================================="
echo "🚀 VOYAGER CLOUDDB - DESPLIEGUE MANUAL"
echo "========================================="
echo "📅 Fecha: $(date)"
echo ""

# ==========================================
# PASO 1: Verificar Pre-requisitos
# ==========================================
echo "🔍 [1/8] Verificando pre-requisitos..."

# Verificar espacio en disco (mínimo 2GB)
AVAILABLE_SPACE=$(df -BG . | tail -1 | awk '{print $4}' | sed 's/G//')
if [ "$AVAILABLE_SPACE" -lt 2 ]; then
  echo "❌ ERROR: Espacio en disco insuficiente (${AVAILABLE_SPACE}GB disponibles)"
  exit 1
fi
echo "✅ Espacio en disco: ${AVAILABLE_SPACE}GB disponibles"

# Verificar memoria disponible
FREE_MEM=$(free -m | awk 'NR==2 {print $7}')
echo "ℹ️  Memoria disponible: ${FREE_MEM}MB"

# Verificar que existe .env.databases
if [ ! -f ".env.databases" ]; then
  echo "❌ ERROR: Archivo .env.databases no existe"
  echo "   Créalo ejecutando los comandos de creación del archivo"
  exit 1
fi
echo "✅ Archivo .env.databases encontrado"

# ==========================================
# PASO 2: Backup del Backend Actual
# ==========================================
echo ""
echo "💾 [2/8] Creando backup del backend actual..."
if docker ps -a | grep -q crudclouddb_backend; then
  docker commit crudclouddb_backend crudclouddb-api:backup-$(date +%Y%m%d-%H%M%S) || true
  echo "✅ Backup creado"
else
  echo "ℹ️  No hay contenedor backend para respaldar"
fi

# ==========================================
# PASO 3: Pull del Código más Reciente
# ==========================================
echo ""
echo "⬇️  [3/8] Descargando código más reciente..."
git fetch origin deployment/docker-nginx
git reset --hard origin/deployment/docker-nginx
git clean -fd
echo "✅ Código actualizado"

# ==========================================
# PASO 4: Arreglar MySQL (si está fallando)
# ==========================================
echo ""
echo "🔧 [4/8] Verificando y arreglando MySQL..."

if docker ps -a | grep mysql_master | grep -q "Restarting"; then
  echo "⚠️  MySQL está reiniciándose, procediendo a arreglarlo..."
  
  # Detener y eliminar MySQL
  docker stop mysql_master || true
  docker rm mysql_master || true
  
  # Eliminar volumen corrupto
  echo "⚠️  Eliminando volumen MySQL corrupto (esto borrará datos de MySQL)"
  docker volume rm voyager_mysql_data || true
  
  echo "✅ MySQL limpiado"
else
  echo "✅ MySQL no requiere reparación"
fi

# ==========================================
# PASO 5: Reiniciar Bases de Datos Master
# ==========================================
echo ""
echo "🗄️  [5/8] Reiniciando bases de datos master..."

# Cargar variables de entorno de bases de datos
export $(cat .env.databases | grep -v '^#' | xargs)

# Detener y eliminar contenedores de DBs
echo "Deteniendo contenedores de bases de datos..."
docker-compose -f docker-compose.databases.yml down

# Levantar bases de datos con nueva configuración
echo "Levantando bases de datos con configuración optimizada..."
docker-compose -f docker-compose.databases.yml up -d

# Esperar a que las bases de datos estén listas
echo "⏳ Esperando a que las bases de datos estén listas..."
sleep 30

# Verificar health de las bases de datos
echo "🏥 Verificando salud de las bases de datos..."
docker ps --filter "name=postgres_master" --format "table {{.Names}}\t{{.Status}}"
docker ps --filter "name=mysql_master" --format "table {{.Names}}\t{{.Status}}"
docker ps --filter "name=mongodb_master" --format "table {{.Names}}\t{{.Status}}"
docker ps --filter "name=sqlserver_master" --format "table {{.Names}}\t{{.Status}}"

echo "✅ Bases de datos reiniciadas"

# ==========================================
# PASO 6: Construir Imagen del Backend
# ==========================================
echo ""
echo "🔨 [6/8] Construyendo imagen Docker del backend..."

# Etiquetar imagen anterior como 'previous' para rollback
if docker images | grep -q "crudclouddb-api.*latest"; then
  docker tag crudclouddb-api:latest crudclouddb-api:previous || true
fi

# Build con cache
docker build \
  --tag crudclouddb-api:latest \
  --build-arg BUILDKIT_INLINE_CACHE=1 \
  --file Dockerfile \
  . || {
    echo "❌ ERROR: Falló la construcción de la imagen"
    exit 1
  }

echo "✅ Imagen construida exitosamente"

# ==========================================
# PASO 7: Desplegar Backend
# ==========================================
echo ""
echo "▶️  [7/8] Desplegando backend..."

# Detener contenedor actual (Graceful Shutdown)
if docker ps | grep -q crudclouddb_backend; then
  echo "Deteniendo contenedor backend actual..."
  docker stop crudclouddb_backend --time 30 || true
  docker rm crudclouddb_backend || true
  echo "✅ Contenedor anterior detenido"
fi

# Cargar variables de entorno del backend
export $(cat .env | grep -v '^#' | xargs)

# Iniciar nuevo contenedor backend
echo "Iniciando nuevo contenedor backend..."
docker run -d \
  --name crudclouddb_backend \
  --restart unless-stopped \
  --network voyager_network \
  --memory="768m" \
  --cpus="1.0" \
  -p 5191:5191 \
  -e DB_HOST=postgres_master \
  -e DB_PORT=5432 \
  -e DB_NAME=${DB_NAME} \
  -e DB_USER=${DB_USER} \
  -e DB_PASSWORD=${DB_PASSWORD} \
  -e JWT_SECRET=${JWT_SECRET} \
  -e JWT_ISSUER=${JWT_ISSUER} \
  -e JWT_AUDIENCE=${JWT_AUDIENCE} \
  -e JWT_EXPIRY_MINUTES=${JWT_EXPIRY_MINUTES} \
  -e SMTP_SERVER=${SMTP_SERVER} \
  -e SMTP_PORT=${SMTP_PORT} \
  -e SMTP_SENDER_EMAIL=${SMTP_SENDER_EMAIL} \
  -e SMTP_SENDER_NAME="${SMTP_SENDER_NAME}" \
  -e SMTP_USERNAME=${SMTP_USERNAME} \
  -e SMTP_PASSWORD=${SMTP_PASSWORD} \
  -e SMTP_ENABLE_SSL=${SMTP_ENABLE_SSL} \
  -e DB_HOST_POSTGRESQL=postgres_master \
  -e DB_HOST_MYSQL=mysql_master \
  -e DB_HOST_MONGODB=mongodb_master \
  -e DB_HOST_SQLSERVER=sqlserver_master \
  -e TIMEZONE_ID="${TIMEZONE_ID}" \
  -e DISCORD_WEBHOOK_URL=${DISCORD_WEBHOOK_URL} \
  -e MERCADOPAGO_ACCESS_TOKEN=${MERCADOPAGO_ACCESS_TOKEN} \
  -e MERCADOPAGO_PUBLIC_KEY=${MERCADOPAGO_PUBLIC_KEY} \
  -e MERCADOPAGO_NOTIFICATION_URL=${MERCADOPAGO_NOTIFICATION_URL} \
  -e MERCADOPAGO_WEBHOOK_SECRET=${MERCADOPAGO_WEBHOOK_SECRET} \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://0.0.0.0:5191 \
  -v /root/Voyager-cloudDB-Back/logs:/app/logs \
  crudclouddb-api:latest || {
    echo "❌ ERROR: Falló el inicio del contenedor backend"
    exit 1
  }

echo "✅ Backend desplegado"

# ==========================================
# PASO 8: Health Checks
# ==========================================
echo ""
echo "🏥 [8/8] Ejecutando health checks..."

# Esperar 30 segundos para que el backend inicie
echo "⏳ Esperando 30s para que el backend esté listo..."
sleep 30

# Intentar health check 5 veces con intervalo de 10s
HEALTH_CHECK_ATTEMPTS=5
HEALTH_CHECK_PASSED=false

for i in $(seq 1 $HEALTH_CHECK_ATTEMPTS); do
  echo "Intento $i/$HEALTH_CHECK_ATTEMPTS..."
  
  if curl -f -s http://localhost:5191/health > /dev/null 2>&1; then
    HEALTH_CHECK_PASSED=true
    echo "✅ Backend respondiendo correctamente"
    
    # Mostrar respuesta del health check
    echo ""
    echo "📊 Respuesta del health check:"
    curl -s http://localhost:5191/health | jq . || curl -s http://localhost:5191/health
    break
  else
    echo "⚠️  Backend aún no responde..."
    if [ $i -lt $HEALTH_CHECK_ATTEMPTS ]; then
      sleep 10
    fi
  fi
done

if [ "$HEALTH_CHECK_PASSED" = false ]; then
  echo ""
  echo "❌ ERROR: Backend no pasa health check después de $HEALTH_CHECK_ATTEMPTS intentos"
  echo ""
  echo "📋 Logs del backend:"
  docker logs crudclouddb_backend --tail 50
  exit 1
fi

# ==========================================
# RESUMEN FINAL
# ==========================================
echo ""
echo "========================================="
echo "✅ DESPLIEGUE COMPLETADO EXITOSAMENTE"
echo "========================================="
echo ""
echo "📊 Estado de Contenedores:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""
echo "💾 Uso de Recursos:"
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}"
echo ""
echo "🌐 URLs de Acceso:"
echo "   • Backend API: http://localhost:5191"
echo "   • Backend HTTPS: https://service.voyager.andrescortes.dev"
echo "   • Health Check: http://localhost:5191/health"
echo ""
echo "📋 Para ver logs del backend:"
echo "   docker logs crudclouddb_backend -f"
echo ""
echo "🔄 Para rollback (si algo falla):"
echo "   docker stop crudclouddb_backend"
echo "   docker rm crudclouddb_backend"
echo "   docker tag crudclouddb-api:previous crudclouddb-api:latest"
echo "   ./deploy-production.sh"
echo ""
echo "========================================="

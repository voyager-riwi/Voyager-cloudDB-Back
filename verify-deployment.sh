#!/bin/bash

# =======================================================
# Script de Verificación Post-Migración
# Uso: ./verify-deployment.sh
# =======================================================

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

PASSED=0
FAILED=0
WARNINGS=0

# Función para imprimir resultados
print_result() {
    local status=$1
    local message=$2
    
    if [ "$status" == "PASS" ]; then
        echo -e "${GREEN}✅ PASS${NC} - $message"
        ((PASSED++))
    elif [ "$status" == "FAIL" ]; then
        echo -e "${RED}❌ FAIL${NC} - $message"
        ((FAILED++))
    elif [ "$status" == "WARN" ]; then
        echo -e "${YELLOW}⚠️  WARN${NC} - $message"
        ((WARNINGS++))
    else
        echo -e "${BLUE}ℹ️  INFO${NC} - $message"
    fi
}

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}🔍 Verificación Post-Migración${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# =======================================================
# 1. Verificación del Sistema
# =======================================================
echo -e "${BLUE}1️⃣  Verificación del Sistema${NC}"
echo "-----------------------------------"

# Swap
if swapon --show | grep -q swapfile; then
    SWAP_SIZE=$(swapon --show --bytes | grep swapfile | awk '{print $3/1024/1024/1024}')
    if (( $(echo "$SWAP_SIZE >= 2" | bc -l) )); then
        print_result "PASS" "Swap configurado (${SWAP_SIZE}GB)"
    else
        print_result "WARN" "Swap pequeño (${SWAP_SIZE}GB, recomendado: 2GB)"
    fi
else
    print_result "FAIL" "Swap no configurado"
fi

# Memoria disponible
FREE_MEM=$(free -m | awk 'NR==2 {print $7}')
if [ "$FREE_MEM" -gt 500 ]; then
    print_result "PASS" "Memoria disponible: ${FREE_MEM}MB"
elif [ "$FREE_MEM" -gt 200 ]; then
    print_result "WARN" "Memoria baja: ${FREE_MEM}MB"
else
    print_result "FAIL" "Memoria crítica: ${FREE_MEM}MB"
fi

# Espacio en disco
DISK_AVAIL=$(df -BG / | tail -1 | awk '{print $4}' | sed 's/G//')
if [ "$DISK_AVAIL" -gt 5 ]; then
    print_result "PASS" "Espacio en disco: ${DISK_AVAIL}GB"
elif [ "$DISK_AVAIL" -gt 2 ]; then
    print_result "WARN" "Espacio bajo: ${DISK_AVAIL}GB"
else
    print_result "FAIL" "Espacio crítico: ${DISK_AVAIL}GB"
fi

echo ""

# =======================================================
# 2. Verificación de Docker
# =======================================================
echo -e "${BLUE}2️⃣  Verificación de Docker${NC}"
echo "-----------------------------------"

# Docker instalado
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version | awk '{print $3}' | sed 's/,//')
    print_result "PASS" "Docker instalado (v$DOCKER_VERSION)"
else
    print_result "FAIL" "Docker no instalado"
fi

# Docker Compose instalado
if docker compose version &> /dev/null; then
    COMPOSE_VERSION=$(docker compose version --short)
    print_result "PASS" "Docker Compose instalado (v$COMPOSE_VERSION)"
else
    print_result "FAIL" "Docker Compose no instalado"
fi

# Red Docker
if docker network ls | grep -q voyager_network; then
    print_result "PASS" "Red voyager_network existe"
else
    print_result "FAIL" "Red voyager_network no existe"
fi

echo ""

# =======================================================
# 3. Verificación de Contenedores
# =======================================================
echo -e "${BLUE}3️⃣  Verificación de Contenedores${NC}"
echo "-----------------------------------"

CONTAINERS=("crudclouddb_backend" "postgres_master" "mysql_master" "mongodb_master" "sqlserver_master" "voyager-backend-nginx")

for container in "${CONTAINERS[@]}"; do
    if docker ps --format '{{.Names}}' | grep -q "^${container}$"; then
        STATUS=$(docker inspect --format='{{.State.Status}}' $container)
        if [ "$STATUS" == "running" ]; then
            print_result "PASS" "$container está corriendo"
        else
            print_result "FAIL" "$container estado: $STATUS"
        fi
    else
        print_result "FAIL" "$container no encontrado"
    fi
done

echo ""

# =======================================================
# 4. Verificación de Health Checks
# =======================================================
echo -e "${BLUE}4️⃣  Verificación de Health Checks${NC}"
echo "-----------------------------------"

# Backend directo (puerto 5191)
if curl -sf http://localhost:5191/health > /dev/null 2>&1; then
    print_result "PASS" "Backend (5191) responde"
else
    print_result "FAIL" "Backend (5191) no responde"
fi

# NGINX HTTP (puerto 80)
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost/health 2>/dev/null)
if [ "$HTTP_CODE" == "200" ] || [ "$HTTP_CODE" == "301" ] || [ "$HTTP_CODE" == "302" ]; then
    print_result "PASS" "NGINX HTTP (80) responde (código: $HTTP_CODE)"
else
    print_result "WARN" "NGINX HTTP respuesta: $HTTP_CODE (puede ser redirect SSL)"
fi

# NGINX HTTPS (puerto 443)
HTTPS_CODE=$(curl -sk -o /dev/null -w "%{http_code}" https://localhost/health 2>/dev/null)
if [ "$HTTPS_CODE" == "200" ]; then
    print_result "PASS" "NGINX HTTPS (443) responde"
elif [ "$HTTPS_CODE" == "000" ]; then
    print_result "WARN" "NGINX HTTPS no configurado o certificados faltantes"
else
    print_result "WARN" "NGINX HTTPS respuesta: $HTTPS_CODE"
fi

echo ""

# =======================================================
# 5. Verificación de Conectividad de Bases de Datos
# =======================================================
echo -e "${BLUE}5️⃣  Verificación de Bases de Datos${NC}"
echo "-----------------------------------"

# PostgreSQL
if docker exec postgres_master pg_isready -U admin_pg > /dev/null 2>&1; then
    print_result "PASS" "PostgreSQL acepta conexiones"
else
    print_result "FAIL" "PostgreSQL no acepta conexiones"
fi

# MySQL
if docker exec mysql_master mysqladmin ping -h localhost > /dev/null 2>&1; then
    print_result "PASS" "MySQL acepta conexiones"
else
    print_result "FAIL" "MySQL no acepta conexiones"
fi

# MongoDB
if docker exec mongodb_master mongosh --eval "db.adminCommand('ping')" > /dev/null 2>&1; then
    print_result "PASS" "MongoDB acepta conexiones"
else
    print_result "FAIL" "MongoDB no acepta conexiones"
fi

# SQL Server
if docker exec sqlserver_master /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -Q "SELECT 1" > /dev/null 2>&1; then
    print_result "PASS" "SQL Server acepta conexiones"
else
    print_result "WARN" "SQL Server no responde (puede estar iniciando)"
fi

echo ""

# =======================================================
# 6. Verificación de Uso de Recursos
# =======================================================
echo -e "${BLUE}6️⃣  Verificación de Uso de Recursos${NC}"
echo "-----------------------------------"

# Memoria total usada por contenedores
TOTAL_MEM_MB=$(docker stats --no-stream --format "{{.MemUsage}}" | awk -F'/' '{sum += $1} END {print sum}')
TOTAL_RAM_MB=$(free -m | awk 'NR==2{print $2}')

if [ ! -z "$TOTAL_MEM_MB" ]; then
    MEM_PERCENT=$((TOTAL_MEM_MB * 100 / TOTAL_RAM_MB))
    
    if [ "$MEM_PERCENT" -lt 80 ]; then
        print_result "PASS" "Uso de memoria: ${MEM_PERCENT}% (${TOTAL_MEM_MB}MB de ${TOTAL_RAM_MB}MB)"
    elif [ "$MEM_PERCENT" -lt 90 ]; then
        print_result "WARN" "Uso de memoria alto: ${MEM_PERCENT}%"
    else
        print_result "FAIL" "Uso de memoria crítico: ${MEM_PERCENT}%"
    fi
fi

echo ""

# =======================================================
# 7. Verificación de Archivos de Configuración
# =======================================================
echo -e "${BLUE}7️⃣  Verificación de Archivos${NC}"
echo "-----------------------------------"

# .env.databases
if [ -f ".env.databases" ]; then
    PERMS=$(stat -c "%a" .env.databases 2>/dev/null || stat -f "%Lp" .env.databases 2>/dev/null)
    if [ "$PERMS" == "600" ]; then
        print_result "PASS" ".env.databases existe con permisos correctos"
    else
        print_result "WARN" ".env.databases permisos incorrectos (actual: $PERMS, esperado: 600)"
    fi
else
    print_result "WARN" ".env.databases no encontrado"
fi

# docker-compose.databases.yml
if [ -f "docker-compose.databases.yml" ]; then
    print_result "PASS" "docker-compose.databases.yml existe"
else
    print_result "FAIL" "docker-compose.databases.yml no encontrado"
fi

# Certificados SSL
if [ -f "ssl/fullchain.pem" ] && [ -f "ssl/privkey.pem" ]; then
    print_result "PASS" "Certificados SSL presentes"
else
    print_result "WARN" "Certificados SSL faltantes (HTTPS no funcionará)"
fi

echo ""

# =======================================================
# 8. Verificación de Seguridad
# =======================================================
echo -e "${BLUE}8️⃣  Verificación de Seguridad${NC}"
echo "-----------------------------------"

# UFW
if command -v ufw &> /dev/null; then
    if ufw status | grep -q "Status: active"; then
        print_result "PASS" "Firewall UFW activo"
    else
        print_result "WARN" "Firewall UFW inactivo"
    fi
else
    print_result "WARN" "UFW no instalado"
fi

# Fail2Ban
if command -v fail2ban-client &> /dev/null; then
    if systemctl is-active --quiet fail2ban; then
        print_result "PASS" "Fail2Ban activo"
    else
        print_result "WARN" "Fail2Ban instalado pero inactivo"
    fi
else
    print_result "WARN" "Fail2Ban no instalado"
fi

# SSH configuración
if grep -q "PermitRootLogin no" /etc/ssh/sshd_config 2>/dev/null; then
    print_result "PASS" "SSH: Root login deshabilitado"
else
    print_result "WARN" "SSH: Root login habilitado (inseguro)"
fi

if grep -q "PasswordAuthentication no" /etc/ssh/sshd_config 2>/dev/null; then
    print_result "PASS" "SSH: Autenticación por password deshabilitada"
else
    print_result "WARN" "SSH: Autenticación por password habilitada"
fi

echo ""

# =======================================================
# 9. Verificación de Logs
# =======================================================
echo -e "${BLUE}9️⃣  Verificación de Logs${NC}"
echo "-----------------------------------"

# Errores recientes en backend
BACKEND_ERRORS=$(docker logs --tail 100 crudclouddb_backend 2>&1 | grep -i "error\|exception\|fail" | wc -l)
if [ "$BACKEND_ERRORS" -eq 0 ]; then
    print_result "PASS" "Sin errores recientes en backend"
elif [ "$BACKEND_ERRORS" -lt 5 ]; then
    print_result "WARN" "$BACKEND_ERRORS errores en logs del backend"
else
    print_result "FAIL" "$BACKEND_ERRORS errores en logs del backend"
fi

# Errores OOM
OOM_COUNT=$(dmesg | grep -i "out of memory" | wc -l 2>/dev/null)
if [ "$OOM_COUNT" -eq 0 ]; then
    print_result "PASS" "Sin errores OOM detectados"
else
    print_result "FAIL" "$OOM_COUNT errores OOM detectados"
fi

echo ""

# =======================================================
# Resumen Final
# =======================================================
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}📊 Resumen de Verificación${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

TOTAL=$((PASSED + FAILED + WARNINGS))

echo -e "${GREEN}✅ Pasadas:     $PASSED${NC}"
echo -e "${YELLOW}⚠️  Advertencias: $WARNINGS${NC}"
echo -e "${RED}❌ Fallidas:    $FAILED${NC}"
echo "-----------------------------------"
echo -e "   Total:       $TOTAL"
echo ""

# Calcular porcentaje de éxito
if [ "$TOTAL" -gt 0 ]; then
    SUCCESS_PERCENT=$((PASSED * 100 / TOTAL))
    
    if [ "$SUCCESS_PERCENT" -ge 90 ]; then
        echo -e "${GREEN}🎉 Excelente: $SUCCESS_PERCENT% de verificaciones pasadas${NC}"
        echo -e "${GREEN}✅ El sistema está listo para producción${NC}"
    elif [ "$SUCCESS_PERCENT" -ge 75 ]; then
        echo -e "${YELLOW}⚠️  Aceptable: $SUCCESS_PERCENT% de verificaciones pasadas${NC}"
        echo -e "${YELLOW}Revisa las advertencias antes de ir a producción${NC}"
    else
        echo -e "${RED}❌ Insuficiente: $SUCCESS_PERCENT% de verificaciones pasadas${NC}"
        echo -e "${RED}Corrige los errores antes de continuar${NC}"
    fi
fi

echo ""
echo -e "${CYAN}========================================${NC}"

# Recomendaciones
if [ "$FAILED" -gt 0 ]; then
    echo ""
    echo -e "${YELLOW}💡 Recomendaciones:${NC}"
    echo "  1. Revisa los errores marcados con ❌"
    echo "  2. Ejecuta: docker logs crudclouddb_backend"
    echo "  3. Verifica conectividad: ./monitor.sh network"
    echo "  4. Revisa el tutorial: DEPLOYMENT_TUTORIAL.md"
fi

# Exit code basado en resultados
if [ "$FAILED" -gt 0 ]; then
    exit 1
else
    exit 0
fi


#!/bin/bash

# =======================================================
# Script de Monitoreo - Voyager Backend
# Uso: ./monitor.sh [status|health|resources|logs|full]
# =======================================================

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Función para imprimir encabezado
print_header() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

# Función para verificar estado de contenedores
check_container_status() {
    print_header "📋 Estado de Contenedores"
    
    CONTAINERS=("crudclouddb_backend" "voyager-backend-nginx" "postgres_master" "mysql_master" "mongodb_master" "sqlserver_master")
    
    for container in "${CONTAINERS[@]}"; do
        if docker ps --format '{{.Names}}' | grep -q "^${container}$"; then
            STATUS=$(docker inspect --format='{{.State.Status}}' $container)
            HEALTH=$(docker inspect --format='{{.State.Health.Status}}' $container 2>/dev/null || echo "no healthcheck")
            
            if [ "$STATUS" = "running" ]; then
                echo -e "${GREEN}✅ $container${NC} - Running"
                if [ "$HEALTH" != "no healthcheck" ]; then
                    if [ "$HEALTH" = "healthy" ]; then
                        echo -e "   ${GREEN}💚 Health: $HEALTH${NC}"
                    else
                        echo -e "   ${YELLOW}⚠️  Health: $HEALTH${NC}"
                    fi
                fi
            else
                echo -e "${RED}❌ $container${NC} - $STATUS"
            fi
        else
            echo -e "${RED}❌ $container${NC} - Not found"
        fi
    done
    echo ""
}

# Función para verificar salud de endpoints
check_health_endpoints() {
    print_header "🏥 Health Checks"
    
    # Backend directo
    if curl -sf http://localhost:5191/health > /dev/null; then
        echo -e "${GREEN}✅ Backend (5191)${NC} - Healthy"
    else
        echo -e "${RED}❌ Backend (5191)${NC} - Unhealthy"
    fi
    
    # NGINX
    if curl -sf http://localhost/health > /dev/null; then
        echo -e "${GREEN}✅ NGINX (80)${NC} - Healthy"
    else
        echo -e "${YELLOW}⚠️  NGINX (80)${NC} - Check failed (may redirect to HTTPS)"
    fi
    
    # HTTPS (si está configurado)
    if curl -sfk https://localhost/health > /dev/null; then
        echo -e "${GREEN}✅ NGINX (443)${NC} - Healthy"
    else
        echo -e "${YELLOW}⚠️  NGINX (443)${NC} - Check failed"
    fi
    
    echo ""
}

# Función para mostrar uso de recursos
check_resources() {
    print_header "💾 Uso de Recursos del Sistema"
    
    # Memoria
    echo -e "${BLUE}Memoria:${NC}"
    free -h | awk 'NR==1 || NR==2 {print "  "$0}'
    
    echo ""
    echo -e "${BLUE}Swap:${NC}"
    free -h | awk 'NR==1 || NR==3 {print "  "$0}'
    
    echo ""
    echo -e "${BLUE}Disco:${NC}"
    df -h / | awk 'NR==1 || NR==2 {print "  "$0}'
    
    echo ""
    print_header "🐳 Uso de Recursos de Contenedores"
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}" | grep -E "NAME|crudclouddb|voyager|postgres|mysql|mongo|sqlserver"
    echo ""
}

# Función para mostrar logs recientes
check_logs() {
    print_header "📋 Logs Recientes del Backend"
    docker logs --tail 30 crudclouddb_backend 2>/dev/null || echo "No se pudo acceder a los logs del backend"
    echo ""
    
    print_header "📋 Logs Recientes de NGINX"
    docker logs --tail 20 voyager-backend-nginx 2>/dev/null || echo "No se pudo acceder a los logs de NGINX"
    echo ""
}

# Función para verificar puertos
check_ports() {
    print_header "🔌 Puertos en Uso"
    echo -e "${BLUE}Puertos críticos:${NC}"
    netstat -tulpn 2>/dev/null | grep -E ':(22|80|443|5191|5432|3306|27017|1433)' | awk '{print "  "$4" - "$7}' || \
    ss -tulpn | grep -E ':(22|80|443|5191|5432|3306|27017|1433)' | awk '{print "  "$5}'
    echo ""
}

# Función para verificar conectividad de red
check_network() {
    print_header "🌐 Conectividad de Red"
    
    # Red Docker
    if docker network inspect voyager_network > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Red voyager_network${NC} - Existe"
        CONTAINERS_IN_NETWORK=$(docker network inspect voyager_network --format='{{range .Containers}}{{.Name}} {{end}}')
        echo -e "   Contenedores: $CONTAINERS_IN_NETWORK"
    else
        echo -e "${RED}❌ Red voyager_network${NC} - No encontrada"
    fi
    
    echo ""
    
    # Pruebas de ping entre contenedores
    if docker ps --format '{{.Names}}' | grep -q "^crudclouddb_backend$"; then
        echo -e "${BLUE}Conectividad desde Backend:${NC}"
        
        for db in postgres_master mysql_master mongodb_master sqlserver_master; do
            if docker exec crudclouddb_backend ping -c 1 -W 2 $db > /dev/null 2>&1; then
                echo -e "  ${GREEN}✅ $db${NC} - Alcanzable"
            else
                echo -e "  ${RED}❌ $db${NC} - No alcanzable"
            fi
        done
    fi
    echo ""
}

# Función para ver procesos que consumen más recursos
check_top_processes() {
    print_header "🔝 Procesos que Consumen Más Recursos"
    echo -e "${BLUE}Top 10 por Memoria:${NC}"
    ps aux --sort=-%mem | head -11
    echo ""
}

# Función para verificar errores en logs del sistema
check_system_errors() {
    print_header "⚠️  Errores Recientes del Sistema"
    
    # Errores OOM
    OOM_COUNT=$(dmesg | grep -i "out of memory" | wc -l)
    if [ "$OOM_COUNT" -gt 0 ]; then
        echo -e "${RED}❌ Se detectaron $OOM_COUNT errores OOM (Out of Memory)${NC}"
        dmesg | grep -i "out of memory" | tail -3
    else
        echo -e "${GREEN}✅ No se detectaron errores OOM${NC}"
    fi
    
    echo ""
    
    # Errores de Docker
    if journalctl -u docker --since "1 hour ago" | grep -i error > /dev/null 2>&1; then
        echo -e "${YELLOW}⚠️  Se encontraron errores en Docker (última hora):${NC}"
        journalctl -u docker --since "1 hour ago" | grep -i error | tail -5
    else
        echo -e "${GREEN}✅ No hay errores recientes en Docker${NC}"
    fi
    
    echo ""
}

# Función de reporte completo
full_report() {
    echo ""
    echo -e "${GREEN}╔════════════════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║   VOYAGER BACKEND - REPORTE COMPLETO DE SISTEMA  ║${NC}"
    echo -e "${GREEN}╚════════════════════════════════════════════════════╝${NC}"
    echo ""
    echo -e "${BLUE}Fecha:${NC} $(date)"
    echo -e "${BLUE}Hostname:${NC} $(hostname)"
    echo -e "${BLUE}Uptime:${NC} $(uptime -p)"
    echo ""
    
    check_container_status
    check_health_endpoints
    check_resources
    check_ports
    check_network
    check_system_errors
    
    print_header "📊 Resumen"
    RUNNING_CONTAINERS=$(docker ps --format '{{.Names}}' | grep -E 'crudclouddb|voyager|postgres|mysql|mongo|sqlserver' | wc -l)
    echo -e "${BLUE}Contenedores activos:${NC} $RUNNING_CONTAINERS/6"
    
    TOTAL_MEM=$(free -m | awk 'NR==2{print $2}')
    USED_MEM=$(free -m | awk 'NR==2{print $3}')
    MEM_PERCENT=$((USED_MEM * 100 / TOTAL_MEM))
    
    if [ "$MEM_PERCENT" -gt 90 ]; then
        echo -e "${RED}⚠️  Uso de memoria: ${MEM_PERCENT}% (CRÍTICO)${NC}"
    elif [ "$MEM_PERCENT" -gt 75 ]; then
        echo -e "${YELLOW}⚠️  Uso de memoria: ${MEM_PERCENT}% (ALTO)${NC}"
    else
        echo -e "${GREEN}✅ Uso de memoria: ${MEM_PERCENT}% (NORMAL)${NC}"
    fi
    
    echo ""
}

# Menú principal
case "$1" in
    status)
        check_container_status
        ;;
    health)
        check_health_endpoints
        ;;
    resources)
        check_resources
        ;;
    logs)
        check_logs
        ;;
    ports)
        check_ports
        ;;
    network)
        check_network
        ;;
    top)
        check_top_processes
        ;;
    errors)
        check_system_errors
        ;;
    full)
        full_report
        ;;
    *)
        echo "Uso: $0 [status|health|resources|logs|ports|network|top|errors|full]"
        echo ""
        echo "Comandos disponibles:"
        echo "  status     - Estado de contenedores"
        echo "  health     - Health checks de endpoints"
        echo "  resources  - Uso de CPU, memoria y disco"
        echo "  logs       - Logs recientes"
        echo "  ports      - Puertos en uso"
        echo "  network    - Conectividad de red"
        echo "  top        - Procesos que consumen más recursos"
        echo "  errors     - Errores recientes del sistema"
        echo "  full       - Reporte completo"
        echo ""
        exit 1
        ;;
esac


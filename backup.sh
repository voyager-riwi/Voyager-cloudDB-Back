#!/bin/bash

# =======================================================
# Script de Backup Automático - Voyager Backend
# Uso: ./backup.sh
# Cron: 0 3 * * * /home/github-deployer/Voyager-cloudDB-Back/backup.sh
# =======================================================

# Configuración
BACKUP_DIR="/home/github-deployer/backups"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=7

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Crear directorio de backups si no existe
mkdir -p $BACKUP_DIR

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}🔄 Iniciando Backup - $DATE${NC}"
echo -e "${BLUE}========================================${NC}"

# =======================================================
# Función para verificar si el contenedor está corriendo
# =======================================================
check_container() {
    if docker ps --format '{{.Names}}' | grep -q "^$1$"; then
        return 0
    else
        return 1
    fi
}

# =======================================================
# Backup PostgreSQL
# =======================================================
echo -e "\n${YELLOW}📦 Backup PostgreSQL...${NC}"
if check_container "postgres_master"; then
    docker exec postgres_master pg_dump -U admin_pg voyager_main | gzip > $BACKUP_DIR/postgres_$DATE.sql.gz
    
    if [ $? -eq 0 ]; then
        SIZE=$(du -h $BACKUP_DIR/postgres_$DATE.sql.gz | cut -f1)
        echo -e "${GREEN}✅ PostgreSQL backup completado ($SIZE)${NC}"
    else
        echo -e "${RED}❌ PostgreSQL backup falló${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Contenedor postgres_master no está corriendo${NC}"
fi

# =======================================================
# Backup MySQL
# =======================================================
echo -e "\n${YELLOW}📦 Backup MySQL...${NC}"
if check_container "mysql_master"; then
    # Obtener password del archivo .env.databases si existe
    if [ -f ".env.databases" ]; then
        MYSQL_PASSWORD=$(grep MYSQL_ROOT_PASSWORD .env.databases | cut -d '=' -f2)
        docker exec mysql_master mysqldump -u root -p"$MYSQL_PASSWORD" voyager_main 2>/dev/null | gzip > $BACKUP_DIR/mysql_$DATE.sql.gz
        
        if [ $? -eq 0 ]; then
            SIZE=$(du -h $BACKUP_DIR/mysql_$DATE.sql.gz | cut -f1)
            echo -e "${GREEN}✅ MySQL backup completado ($SIZE)${NC}"
        else
            echo -e "${RED}❌ MySQL backup falló${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  Archivo .env.databases no encontrado, saltando MySQL backup${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Contenedor mysql_master no está corriendo${NC}"
fi

# =======================================================
# Backup MongoDB
# =======================================================
echo -e "\n${YELLOW}📦 Backup MongoDB...${NC}"
if check_container "mongodb_master"; then
    if [ -f ".env.databases" ]; then
        MONGO_PASSWORD=$(grep MONGO_INITDB_ROOT_PASSWORD .env.databases | cut -d '=' -f2)
        MONGO_USER=$(grep MONGO_INITDB_ROOT_USERNAME .env.databases | cut -d '=' -f2)
        
        # Crear backup dentro del contenedor
        docker exec mongodb_master mongodump \
            --username "$MONGO_USER" \
            --password "$MONGO_PASSWORD" \
            --authenticationDatabase admin \
            --out /tmp/mongo_backup \
            > /dev/null 2>&1
        
        # Copiar backup al host
        docker cp mongodb_master:/tmp/mongo_backup $BACKUP_DIR/mongodb_$DATE
        
        # Comprimir
        tar -czf $BACKUP_DIR/mongodb_$DATE.tar.gz -C $BACKUP_DIR mongodb_$DATE > /dev/null 2>&1
        
        # Limpiar directorio temporal
        rm -rf $BACKUP_DIR/mongodb_$DATE
        docker exec mongodb_master rm -rf /tmp/mongo_backup
        
        if [ -f "$BACKUP_DIR/mongodb_$DATE.tar.gz" ]; then
            SIZE=$(du -h $BACKUP_DIR/mongodb_$DATE.tar.gz | cut -f1)
            echo -e "${GREEN}✅ MongoDB backup completado ($SIZE)${NC}"
        else
            echo -e "${RED}❌ MongoDB backup falló${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  Archivo .env.databases no encontrado, saltando MongoDB backup${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Contenedor mongodb_master no está corriendo${NC}"
fi

# =======================================================
# Backup SQL Server
# =======================================================
echo -e "\n${YELLOW}📦 Backup SQL Server...${NC}"
if check_container "sqlserver_master"; then
    if [ -f ".env.databases" ]; then
        SA_PASSWORD=$(grep SA_PASSWORD .env.databases | cut -d '=' -f2)
        
        # Crear backup dentro del contenedor
        docker exec sqlserver_master /opt/mssql-tools/bin/sqlcmd \
            -S localhost \
            -U sa \
            -P "$SA_PASSWORD" \
            -Q "BACKUP DATABASE voyager_main TO DISK='/var/opt/mssql/backup_$DATE.bak' WITH FORMAT" \
            > /dev/null 2>&1
        
        # Copiar backup al host
        docker cp sqlserver_master:/var/opt/mssql/backup_$DATE.bak $BACKUP_DIR/sqlserver_$DATE.bak
        
        # Comprimir
        gzip $BACKUP_DIR/sqlserver_$DATE.bak
        
        # Limpiar backup del contenedor
        docker exec sqlserver_master rm -f /var/opt/mssql/backup_$DATE.bak
        
        if [ -f "$BACKUP_DIR/sqlserver_$DATE.bak.gz" ]; then
            SIZE=$(du -h $BACKUP_DIR/sqlserver_$DATE.bak.gz | cut -f1)
            echo -e "${GREEN}✅ SQL Server backup completado ($SIZE)${NC}"
        else
            echo -e "${RED}❌ SQL Server backup falló${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  Archivo .env.databases no encontrado, saltando SQL Server backup${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Contenedor sqlserver_master no está corriendo${NC}"
fi

# =======================================================
# Backup de Imágenes Docker (opcional)
# =======================================================
echo -e "\n${YELLOW}📦 Backup de imagen Docker del backend...${NC}"
if docker images | grep -q "crudclouddb-api.*latest"; then
    docker save crudclouddb-api:latest | gzip > $BACKUP_DIR/backend_image_$DATE.tar.gz
    
    if [ $? -eq 0 ]; then
        SIZE=$(du -h $BACKUP_DIR/backend_image_$DATE.tar.gz | cut -f1)
        echo -e "${GREEN}✅ Imagen Docker backup completado ($SIZE)${NC}"
    else
        echo -e "${RED}❌ Imagen Docker backup falló${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Imagen crudclouddb-api:latest no encontrada${NC}"
fi

# =======================================================
# Limpieza de backups antiguos
# =======================================================
echo -e "\n${YELLOW}🧹 Limpiando backups antiguos (> $RETENTION_DAYS días)...${NC}"
DELETED_COUNT=$(find $BACKUP_DIR -name "*.gz" -o -name "*.bak" -mtime +$RETENTION_DAYS | wc -l)
find $BACKUP_DIR -name "*.gz" -o -name "*.bak" -mtime +$RETENTION_DAYS -delete

if [ $DELETED_COUNT -gt 0 ]; then
    echo -e "${GREEN}✅ Se eliminaron $DELETED_COUNT backups antiguos${NC}"
else
    echo -e "${GREEN}✅ No hay backups antiguos para eliminar${NC}"
fi

# =======================================================
# Resumen
# =======================================================
echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}📊 Resumen de Backups${NC}"
echo -e "${BLUE}========================================${NC}"

TOTAL_SIZE=$(du -sh $BACKUP_DIR | cut -f1)
BACKUP_COUNT=$(find $BACKUP_DIR -name "*$DATE*" | wc -l)

echo -e "${GREEN}✅ Backups completados: $BACKUP_COUNT${NC}"
echo -e "${BLUE}📦 Tamaño total de backups: $TOTAL_SIZE${NC}"
echo -e "${BLUE}📁 Ubicación: $BACKUP_DIR${NC}"

# Listar backups creados hoy
echo -e "\n${YELLOW}Archivos creados:${NC}"
ls -lh $BACKUP_DIR/*$DATE* 2>/dev/null | awk '{print "  "$9" - "$5}'

echo -e "\n${GREEN}✅ Backup completado exitosamente${NC}"
echo -e "${BLUE}========================================${NC}"

# =======================================================
# Opcional: Enviar notificación a Discord
# =======================================================
if [ ! -z "$DISCORD_WEBHOOK_URL" ]; then
    curl -H "Content-Type: application/json" \
         -X POST \
         -d "{\"content\": \"✅ Backup completado - $DATE\\nArchivos: $BACKUP_COUNT\\nTamaño total: $TOTAL_SIZE\"}" \
         "$DISCORD_WEBHOOK_URL" \
         > /dev/null 2>&1
fi

exit 0


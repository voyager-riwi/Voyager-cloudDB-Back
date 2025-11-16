# 📚 Documentación Completa - Migración y Despliegue Voyager Backend

## 🎯 Propósito

Este conjunto de documentos proporciona una guía completa para migrar y desplegar el backend de Voyager en un servidor con **4 GB de RAM** de forma segura y optimizada.

---

## 📖 Documentos Incluidos

### 1. 📋 [MIGRATION_PLAN.md](./MIGRATION_PLAN.md)
**Plan estratégico de migración y arquitectura**

- Análisis de distribución de recursos (4GB RAM)
- Arquitectura de contenedores optimizada
- Estrategias de optimización (Multi-stage builds, límites de recursos)
- Plan de rollback
- Checklist pre-migración

**📌 Léelo primero para entender la estrategia global**

---

### 2. 🚀 [DEPLOYMENT_TUTORIAL.md](./DEPLOYMENT_TUTORIAL.md)
**Tutorial paso a paso de implementación**

Dividido en 7 fases completas:

1. **Preparación de máquina local** (generación de claves SSH)
2. **Configuración del servidor** (Docker, swap, usuario deployer)
3. **Configuración de GitHub Actions** (20 secrets)
4. **Preparar repositorio local**
5. **Primer despliegue manual** (bases de datos, backend, NGINX)
6. **Activar CD automático**
7. **Verificación post-despliegue**

**📌 Sigue este documento paso a paso durante la migración**

---

### 3. 🔐 [SECURITY_GUIDE.md](./SECURITY_GUIDE.md)
**Guía completa de seguridad y mejores prácticas**

- Hardening SSH (Fail2Ban, UFW)
- Gestión de secrets y rotación
- Seguridad de contenedores Docker
- Hardening de bases de datos (PostgreSQL, MySQL, MongoDB, SQL Server)
- Configuración segura de NGINX
- Backups automatizados
- Monitoreo y alertas

**📌 Implementa estas medidas para asegurar tu servidor**

---

## 📁 Archivos de Configuración

### Archivos Principales

| Archivo | Descripción |
|---------|-------------|
| `docker-compose.databases.yml` | Configuración optimizada de bases de datos con límites de recursos |
| `docker-compose.yml` | Configuración de NGINX |
| `Dockerfile` | Imagen optimizada del backend (.NET 8) |
| `nginx.conf` | Configuración de proxy reverso con seguridad y SSL |
| `.github/workflows/deploy.yml` | Pipeline CI/CD con GitHub Actions |

### Archivos de Configuración Adicionales

| Archivo | Descripción |
|---------|-------------|
| `mysql-config/my.cnf` | Configuración optimizada de MySQL (768MB RAM) |
| `.env.databases.example` | Plantilla de variables de entorno para bases de datos |
| `monitor.sh` | Script de monitoreo del sistema y contenedores |

---

## 🚀 Inicio Rápido

### Prerrequisitos
- Servidor con **Ubuntu 20.04+** o **Debian 11+**
- Mínimo **4 GB RAM**
- **20 GB** de espacio en disco libre
- Acceso SSH como root o sudo
- Cuenta GitHub con permisos de admin

### Flujo de Trabajo

```
1. Lee MIGRATION_PLAN.md (15 minutos)
   ↓
2. Sigue DEPLOYMENT_TUTORIAL.md (30-45 minutos)
   ↓
3. Implementa medidas de SECURITY_GUIDE.md (20 minutos)
   ↓
4. Ejecuta monitor.sh para verificar
   ↓
5. ¡Listo! Backend desplegado y seguro
```

---

## 🛠️ Comandos Útiles

### Verificar Estado del Sistema

```bash
# Ejecutar script de monitoreo
./monitor.sh full

# Ver estado rápido
./monitor.sh status

# Ver salud de endpoints
./monitor.sh health

# Ver uso de recursos
./monitor.sh resources
```

### Gestión de Contenedores

```bash
# Ver todos los contenedores
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Ver logs del backend
docker logs -f crudclouddb_backend

# Reiniciar backend
docker restart crudclouddb_backend

# Ver uso de recursos
docker stats --no-stream
```

### Bases de Datos

```bash
# Iniciar todas las bases de datos
docker compose -f docker-compose.databases.yml up -d

# Ver logs de bases de datos
docker compose -f docker-compose.databases.yml logs -f

# Detener bases de datos
docker compose -f docker-compose.databases.yml down
```

---

## 📊 Distribución de Recursos

```
┌─────────────────────────────────────────────┐
│ Sistema Operativo      : 512 MB   (12.5%)  │
│ NGINX                  : 128 MB   (3.1%)   │
│ Backend API            : 512 MB   (12.5%)  │
│ PostgreSQL             : 768 MB   (18.8%)  │
│ MySQL                  : 768 MB   (18.8%)  │
│ MongoDB                : 512 MB   (12.5%)  │
│ SQL Server             : 896 MB   (21.8%)  │
├─────────────────────────────────────────────┤
│ TOTAL                  : 4096 MB  (100%)   │
└─────────────────────────────────────────────┘
```

---

## 🔧 Troubleshooting

### Problema: Memoria Insuficiente

```bash
# Verificar uso de memoria
free -h

# Ver contenedor que consume más
docker stats --no-stream --format "table {{.Name}}\t{{.MemUsage}}"

# Verificar swap
swapon --show

# Si no hay swap, crearlo (2GB)
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
```

### Problema: Contenedor no Inicia

```bash
# Ver logs completos
docker logs NOMBRE_CONTENEDOR

# Inspeccionar configuración
docker inspect NOMBRE_CONTENEDOR

# Verificar red
docker network inspect voyager_network
```

### Problema: Health Check Falla

```bash
# Probar endpoint directamente
curl http://localhost:5191/health

# Ver logs recientes
docker logs --tail 50 crudclouddb_backend

# Reiniciar contenedor
docker restart crudclouddb_backend
```

---

## 🔐 Seguridad - Checklist

```
✅ Swap de 2GB configurado
✅ Firewall UFW activado (puertos 22, 80, 443)
✅ SSH hardened (solo claves, sin root)
✅ Fail2Ban instalado y activo
✅ Usuario github-deployer creado
✅ Claves SSH dedicadas generadas
✅ 20 GitHub Secrets configurados
✅ Passwords de DB aleatorios (32+ caracteres)
✅ JWT Secret seguro (64+ caracteres)
✅ Certificados SSL válidos
✅ Backups automatizados (cron diario)
✅ Límites de recursos en contenedores
✅ Health checks configurados
✅ Logs centralizados
✅ Monitoreo activo
```

---

## 📈 Métricas Esperadas

### Tiempos de Respuesta

- Backend health check: **< 100ms**
- API endpoints (GET): **< 500ms**
- API endpoints (POST): **< 1000ms**
- Despliegue completo: **5-8 minutos**

### Uso de Recursos en Estado Normal

- CPU: **10-30%**
- RAM: **3.2-3.8 GB** de 4 GB
- Disco: **< 10 GB** usado
- Swap: **< 500 MB** usado

---

## 🆘 Soporte y Contacto

### Recursos Adicionales

- [Documentación Docker](https://docs.docker.com/)
- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [NGINX Documentation](https://nginx.org/en/docs/)
- [PostgreSQL Tuning](https://wiki.postgresql.org/wiki/Tuning_Your_PostgreSQL_Server)

### Logs de Referencia

```bash
# Logs del sistema
journalctl -u docker
journalctl -u sshd

# Logs de aplicación
docker logs crudclouddb_backend
docker logs voyager-backend-nginx

# Logs de bases de datos
docker logs postgres_master
docker logs mysql_master
docker logs mongodb_master
docker logs sqlserver_master
```

---

## 🔄 Actualizaciones y Mantenimiento

### Actualización del Sistema (Mensual)

```bash
# Actualizar paquetes del SO
sudo apt update && sudo apt upgrade -y

# Actualizar imágenes Docker
cd ~/Voyager-cloudDB-Back
docker compose -f docker-compose.databases.yml pull
docker compose pull

# Reiniciar servicios
docker compose -f docker-compose.databases.yml up -d
docker compose up -d
```

### Rotación de Secrets (Cada 6 meses)

1. Generar nuevos secrets
2. Actualizar en GitHub Secrets
3. Actualizar en servidor (.env.databases)
4. Reiniciar servicios
5. Verificar funcionamiento
6. Revocar secrets antiguos

---

## 📝 Notas Importantes

### ⚠️ Advertencias

- **No ejecutar** `docker system prune -a` sin backup (elimina imágenes)
- **Siempre** verificar health checks después de cambios
- **Mantener** al menos 2 GB de espacio libre en disco
- **Monitorear** uso de memoria constantemente (4GB es el límite)

### ✅ Recomendaciones

- Ejecutar `monitor.sh full` diariamente
- Revisar logs semanalmente
- Hacer backups antes de cambios mayores
- Probar rollback en entorno de prueba
- Documentar cambios personalizados

---

## 📅 Registro de Cambios

### 2025-11-16 - v1.0
- ✅ Plan de migración completo
- ✅ Tutorial paso a paso
- ✅ Guía de seguridad
- ✅ Configuraciones optimizadas para 4GB RAM
- ✅ Script de monitoreo
- ✅ Pipeline CI/CD con GitHub Actions

---

## 🎓 Aprende Más

### Conceptos Clave

- **Multi-stage builds**: Reduce tamaño de imágenes Docker
- **Health checks**: Verifica estado de contenedores automáticamente
- **Graceful shutdown**: Detiene servicios sin perder datos
- **Resource limits**: Previene que un contenedor consuma toda la RAM
- **Swap**: Memoria virtual que previene crashes por OOM

### Tecnologías Utilizadas

- Docker & Docker Compose
- NGINX (Reverse Proxy)
- GitHub Actions (CI/CD)
- SSH (Acceso seguro)
- UFW & Fail2Ban (Seguridad)
- .NET 8 (Backend)
- PostgreSQL, MySQL, MongoDB, SQL Server (Bases de datos)

---

**✨ ¡Éxito en tu migración!**

Si sigues esta documentación paso a paso, tendrás un backend desplegado de forma segura y optimizada en tu servidor de 4GB RAM.

---

**📧 Feedback**: Si encuentras errores o mejoras, documenta y actualiza estos archivos.

**📅 Última actualización**: 2025-11-16


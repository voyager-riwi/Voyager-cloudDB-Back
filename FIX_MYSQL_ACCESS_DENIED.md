# 🔧 FIX: MySQL Access Denied Error

## ❌ Problema Identificado

```
Access denied for user 'ycimp3tz5l41yj7f'@'%' to database 'db_zzyehhwutlci1aio'
```

**Causa raíz**: El orden de los comandos `GRANT` y `REVOKE` estaba incorrecto.

### **Secuencia Incorrecta (Antes)**:

```sql
1. CREATE USER
2. GRANT permisos en su DB ✅
3. REVOKE ALL PRIVILEGES ON *.* ❌ (elimina los permisos del paso 2)
4. REVOKE en bases del sistema
5. REVOKE en otras bases de usuarios
6. FLUSH PRIVILEGES
```

**Problema**: El `REVOKE ALL PRIVILEGES ON *.*` en el paso 3 **eliminaba** los permisos que acabábamos de otorgar en el paso 2.

---

## ✅ Solución Implementada

### **Secuencia Correcta (Ahora)**:

```sql
1. CREATE DATABASE mi_db
2. CREATE USER 'username'@'%' IDENTIFIED BY 'password'
3. REVOKE en bases del sistema (mysql, information_schema, etc.)
4. REVOKE en otras bases de usuarios existentes
5. GRANT permisos SOLO en su DB ✅ (al final, después de los REVOKEs)
6. FLUSH PRIVILEGES
```

**Solución**: Los `REVOKE` se ejecutan **ANTES** del `GRANT`, asegurando que los permisos finales sean solo los necesarios.

---

## 🔧 Cambios Específicos

### **Eliminado**:
```sql
-- ❌ REMOVIDO - Estaba eliminando permisos de la DB del usuario
REVOKE ALL PRIVILEGES ON *.* FROM 'username'@'%';
```

### **Mejorado**:
- ✅ Backticks en nombres de DBs: `` `{dbName}` `` (mejor compatibilidad)
- ✅ Orden correcto: `REVOKE` → `GRANT`
- ✅ Solo revoca DBs específicas, no `*.*` global

---

## 🧪 Cómo Verificar

### **Test 1: Crear nueva base de datos MySQL**

```bash
POST https://service.voyager.andrescortes.dev/api/Databases
{
  "engine": "MySQL"
}
```

**Respuesta esperada**:
```json
{
  "success": true,
  "data": {
    "connectionString": "Server=91.98.42.248;Port=3306;Database=db_xxx;Uid=user_yyy;Pwd=zzz"
  }
}
```

### **Test 2: Conectarse con las credenciales en DBeaver/MySQL Workbench**

```
Host: 91.98.42.248
Port: 3306
Database: db_xxx
User: user_yyy
Password: zzz
```

**Resultado esperado**:
- ✅ Conexión exitosa
- ✅ Puedes crear tablas
- ✅ Puedes insertar/actualizar/eliminar datos
- ✅ NO ves otras bases de datos

### **Test 3: Verificar aislamiento**

Intenta conectarte a otra base de datos:

```sql
USE otra_base_de_datos;
-- Error esperado: Access denied
```

```sql
SELECT * FROM otra_base.tabla;
-- Error esperado: Access denied
```

---

## 📊 Permisos Otorgados

El usuario tiene estos permisos **SOLO en su base de datos**:

| Permiso | Descripción |
|---------|-------------|
| `SELECT` | Leer datos |
| `INSERT` | Insertar datos |
| `UPDATE` | Actualizar datos |
| `DELETE` | Eliminar datos |
| `CREATE` | Crear tablas/índices |
| `DROP` | Eliminar tablas/índices |
| `INDEX` | Crear/eliminar índices |
| `ALTER` | Modificar estructura de tablas |
| `CREATE TEMPORARY TABLES` | Crear tablas temporales |
| `LOCK TABLES` | Bloquear tablas |
| `EXECUTE` | Ejecutar stored procedures |
| `CREATE VIEW` | Crear vistas |
| `SHOW VIEW` | Ver definición de vistas |
| `CREATE ROUTINE` | Crear stored procedures/functions |
| `ALTER ROUTINE` | Modificar stored procedures/functions |
| `TRIGGER` | Crear triggers |
| `REFERENCES` | Crear foreign keys |

**NO tiene**:
- ❌ Permisos globales (`*.*`)
- ❌ Acceso a bases de datos del sistema
- ❌ Acceso a otras bases de datos de usuarios
- ❌ Permisos de `SUPER`, `CREATE USER`, `GRANT OPTION`

---

## 🚀 Deploy

### **Mensaje de Commit**:

```bash
git add CrudCloudDb.Infrastructure/Services/DockerService.cs
git commit -m "fix: Correct MySQL GRANT/REVOKE order to prevent access denied errors

Problem: Users were getting 'Access denied' errors when connecting to their MySQL databases
Cause: REVOKE ALL PRIVILEGES ON *.* was executed AFTER GRANT, removing the granted permissions

Solution:
- Reordered SQL commands: REVOKE operations before GRANT
- Removed global REVOKE ALL PRIVILEGES ON *.* (was too aggressive)
- REVOKE now targets specific system databases and other user databases
- GRANT is executed last to ensure final permissions are correct
- Added backticks to database names for better compatibility

Result: Users can now connect successfully and have full access to their own database"
git push origin deployment/docker-nginx
```

---

## ✅ Resumen

**Problema**: `Access denied` al conectarse a MySQL
**Causa**: Orden incorrecto de `GRANT` y `REVOKE`
**Solución**: Ejecutar `REVOKE` antes de `GRANT`
**Resultado**: Usuario puede conectarse y trabajar normalmente

---

## 🎯 Próximos Pasos

1. **Haz el commit** con el mensaje de arriba
2. **Espera 1-2 minutos** para el deploy
3. **Crea una nueva base de datos MySQL** desde Swagger
4. **Conéctate con las credenciales** en DBeaver
5. ✅ Debería funcionar correctamente

**Nota**: Las bases de datos MySQL creadas **ANTES** de este fix pueden seguir con el problema. Para solucionarlas, tendrías que:
- Eliminarlas y recrearlas, O
- Ejecutar manualmente el `GRANT` desde el contenedor maestro

¿Quieres que te proporcione el script para reparar las bases de datos existentes?


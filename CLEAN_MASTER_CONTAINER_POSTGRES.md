# 🗑️ ELIMINAR BASES DE DATOS DE PRUEBA DEL CONTENEDOR MAESTRO

## 📋 **Bases de Datos a CONSERVAR:**
- ✅ `postgres` (sistema)
- ✅ `crud_cloud_db` (backend)
- ✅ `System_EPS_Voyager` (tu proyecto)
- ✅ `crudpak_jjd` (tu proyecto)
- ✅ `template0` (sistema)
- ✅ `template1` (sistema)

## 🗑️ **Bases de Datos a ELIMINAR:**
- ❌ Todas las que empiecen con `db_` (bases de prueba generadas automáticamente)

---

## 🚀 **PASOS PARA LIMPIAR EL CONTENEDOR**

### **Paso 1: Conectarte al servidor PostgreSQL**

Desde tu terminal local (cmd de Windows):

```bash
# Conectar a PostgreSQL en el servidor remoto
psql -h 91.98.42.248 -p 5432 -U postgres -d postgres
```

**Te pedirá la password del usuario `postgres`** (la password de superusuario).

---

### **Paso 2: Listar todas las bases de datos**

Una vez conectado, ejecuta:

```sql
-- Ver TODAS las bases de datos
\l

-- O con SQL:
SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname;
```

Verás algo como:
```
postgres
crud_cloud_db
System_EPS_Voyager
crudpak_jjd
db_abc123
db_xyz789
db_test001
...
```

---

### **Paso 3: Eliminar solo las bases de prueba (db_*)**

**Opción A: Eliminar UNA base a la vez**

```sql
-- Desconectar usuarios conectados a la DB
SELECT pg_terminate_backend(pid) 
FROM pg_stat_activity 
WHERE datname = 'db_abc123';

-- Eliminar la base de datos
DROP DATABASE db_abc123;
```

Repite para cada `db_*` que veas.

---

**Opción B: Script para eliminar TODAS las que empiezan con db_**

```sql
-- Ver cuántas bases de datos db_* existen
SELECT datname FROM pg_database WHERE datname LIKE 'db_%';

-- Para eliminarlas todas, ejecuta esto:
DO $$
DECLARE
    db_name TEXT;
BEGIN
    FOR db_name IN 
        SELECT datname FROM pg_database 
        WHERE datname LIKE 'db_%'
    LOOP
        -- Desconectar sesiones activas
        EXECUTE format('SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = %L', db_name);
        
        -- Eliminar la base de datos
        EXECUTE format('DROP DATABASE IF EXISTS %I', db_name);
        
        RAISE NOTICE 'Eliminada: %', db_name;
    END LOOP;
END $$;
```

Este script:
1. Busca todas las bases de datos que empiezan con `db_`
2. Desconecta usuarios conectados
3. Elimina cada base de datos
4. Muestra un mensaje por cada una eliminada

---

### **Paso 4: Verificar que se eliminaron**

```sql
-- Ver las bases de datos restantes
\l

-- Deberías ver SOLO:
postgres
crud_cloud_db
System_EPS_Voyager
crudpak_jjd
template0
template1
```

---

### **Paso 5: Limpiar la tabla database_instances en la BD del backend**

Ahora que eliminaste las bases de datos físicas del contenedor, necesitas **limpiar los registros** de la tabla `database_instances` en la BD del backend:

```bash
# Salir de postgres
\q

# Conectar a la BD del backend
psql -h 91.98.42.248 -p 5432 -U postgres -d crud_cloud_db
```

Luego ejecuta:

```sql
-- Eliminar TODOS los registros de database_instances de tu usuario
DELETE FROM database_instances
WHERE user_id = '3ecfe914-4d7c-4860-a2d9-c920650f5579';

-- Verificar (debe dar 0)
SELECT COUNT(*) FROM database_instances
WHERE user_id = '3ecfe914-4d7c-4860-a2d9-c920650f5579';
```

---

## 🎯 **ALTERNATIVA: Usar DBeaver**

Si prefieres usar interfaz gráfica:

### **Paso 1: Conectar a PostgreSQL en DBeaver**

```
Host: 91.98.42.248
Port: 5432
Database: postgres
Username: postgres
Password: (tu password de superusuario)
```

### **Paso 2: Ver las bases de datos**

En el árbol de navegación verás todas las bases de datos.

### **Paso 3: Eliminar las db_* manualmente**

1. Click derecho en cada `db_xxx`
2. "Delete" o "Drop Database"
3. Confirmar

**Repite para todas las que empiezan con `db_`**

### **Paso 4: Limpiar database_instances**

Ejecuta el DELETE en DBeaver conectado a `crud_cloud_db`.

---

## ⚠️ **IMPORTANTE: Eliminar también los USUARIOS**

Cada base de datos `db_*` tiene un usuario asociado. Deberías eliminarlos también:

```sql
-- Ver todos los usuarios (roles)
\du

-- Ver solo los usuarios de bases de datos de prueba
SELECT usename FROM pg_user WHERE usename LIKE '%db_%';

-- Eliminar usuarios uno por uno
DROP USER IF EXISTS usuario_db_abc123;

-- O con script:
DO $$
DECLARE
    user_name TEXT;
BEGIN
    FOR user_name IN 
        SELECT usename FROM pg_user 
        WHERE usename LIKE '%db_%'
    LOOP
        EXECUTE format('DROP USER IF EXISTS %I', user_name);
        RAISE NOTICE 'Usuario eliminado: %', user_name;
    END LOOP;
END $$;
```

---

## 📝 **RESUMEN COMPLETO**

### **Desde psql (terminal):**

```bash
# 1. Conectar
psql -h 91.98.42.248 -p 5432 -U postgres -d postgres

# 2. Ejecutar el script de eliminación de bases de datos (Opción B arriba)

# 3. Eliminar usuarios
# (Ejecutar el script de eliminación de usuarios)

# 4. Salir
\q

# 5. Conectar a crud_cloud_db
psql -h 91.98.42.248 -p 5432 -U postgres -d crud_cloud_db

# 6. Limpiar database_instances
DELETE FROM database_instances WHERE user_id = '3ecfe914-4d7c-4060-a2d9-c920650f5579';

# 7. Salir
\q
```

---

## ✅ **Después de Limpiar**

1. **Reinicia la API** (opcional, pero recomendado)
2. **Abre Swagger**
3. **Crea una DB PostgreSQL nueva**
4. ✅ Debería crear exitosamente (1/2)

---

## 🎯 **Script Todo-en-Uno**

He creado un archivo `CLEAN_MASTER_CONTAINER.sql` que puedes ejecutar directamente desde psql conectado al contenedor maestro.

---

**¿Prefieres usar `psql` desde terminal o DBeaver?** Te guío paso a paso. 🚀


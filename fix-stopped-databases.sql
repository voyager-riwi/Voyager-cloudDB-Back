-- ======================================================
-- Script para corregir bases de datos marcadas como Stopped
-- cuando deberían estar Running
-- ======================================================

-- Ver el estado actual de todas las bases de datos
SELECT 
    Id,
    Name,
    Engine,
    Status,
    DeletedAt,
    CreatedAt
FROM DatabaseInstances
ORDER BY CreatedAt DESC;

-- Actualizar todas las bases de datos que NO están eliminadas
-- y que tienen Status = 2 (Stopped) a Status = 1 (Running)
UPDATE DatabaseInstances
SET Status = 1  -- 1 = Running
WHERE Status = 2  -- 2 = Stopped
  AND DeletedAt IS NULL;  -- Solo las que NO están eliminadas

-- Verificar los cambios
SELECT 
    Id,
    Name,
    Engine,
    Status,
    DeletedAt,
    CreatedAt
FROM DatabaseInstances
ORDER BY CreatedAt DESC;

-- Información de los valores del enum:
-- 0 = Creating
-- 1 = Running
-- 2 = Stopped
-- 3 = Error
-- 4 = Deleted

#!/bin/bash

echo "🔍 DIAGNÓSTICO COMPLETO DE MERCADOPAGO"
echo "========================================"
echo

echo "📋 1. Verificando credenciales en uso:"
docker logs crudclouddb_backend | grep -A 3 "MercadoPago configured" | tail -4
echo

echo "📋 2. Verificando que las variables de entorno se cargaron:"
docker exec crudclouddb_backend env | grep MERCADOPAGO
echo

echo "📋 3. Verificando últimas preferencias creadas:"
docker logs crudclouddb_backend | grep -A 5 "Respuesta de MercadoPago recibida" | tail -10
echo

echo "📋 4. Verificando webhooks recibidos (últimos 5):"
docker logs crudclouddb_backend | grep "WEBHOOK RECIBIDO DE MERCADOPAGO" | tail -5
echo

echo "📋 5. Verificando acceso al endpoint webhook:"
echo "Testeando GET..."
curl -s https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago | head -1
echo

echo "📋 6. Verificando estado del contenedor:"
docker ps | grep crudclouddb_backend
echo

echo "📋 7. Verificando logs de errores recientes:"
docker logs crudclouddb_backend --tail 50 | grep -i error | tail -3
echo

echo "🎯 ACCIÓN RECOMENDADA:"
echo "Si ves credenciales diferentes entre entornos, ese es el problema."
echo "Si no ves logs de 'MercadoPago configured', las variables no se están cargando."

# ============================================
# 🔍 DIAGNÓSTICO COMPLETO DE MERCADOPAGO
# ============================================

Write-Host "🔍 DIAGNÓSTICO COMPLETO DE MERCADOPAGO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "⚠️  NOTA: Con GitHub Actions, NO hacer docker-compose down manual" -ForegroundColor Yellow
Write-Host

Write-Host "📋 1. Verificando credenciales en uso:" -ForegroundColor Yellow
docker logs crudclouddb_backend 2>$null | Select-String "MercadoPago configured" -Context 0,3 | Select-Object -Last 1
Write-Host

Write-Host "📋 2. Verificando variables de entorno:" -ForegroundColor Yellow
docker exec crudclouddb_backend env 2>$null | Select-String "MERCADOPAGO"
Write-Host

Write-Host "📋 3. Verificando últimas preferencias:" -ForegroundColor Yellow
docker logs crudclouddb_backend 2>$null | Select-String "Respuesta de MercadoPago recibida" -Context 0,5 | Select-Object -Last 1
Write-Host

Write-Host "📋 4. Verificando webhooks recibidos:" -ForegroundColor Yellow
docker logs crudclouddb_backend 2>$null | Select-String "WEBHOOK RECIBIDO DE MERCADOPAGO" | Select-Object -Last 5
Write-Host

Write-Host "📋 5. Verificando acceso al endpoint:" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago" -Method GET -TimeoutSec 10
    Write-Host "✅ Endpoint accesible: $($response.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ Error accediendo al endpoint: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host

Write-Host "📋 6. Estado del contenedor:" -ForegroundColor Yellow
docker ps | Select-String "crudclouddb_backend"
Write-Host

Write-Host "📋 7. Verificando si hay archivo .env en el servidor:" -ForegroundColor Yellow
if (Test-Path ".env") {
    Write-Host "✅ Archivo .env encontrado" -ForegroundColor Green
    Write-Host "Credenciales en .env:"
    Get-Content ".env" | Select-String "MERCADOPAGO"
} else {
    Write-Host "❌ Archivo .env NO encontrado" -ForegroundColor Red
}
Write-Host

Write-Host "🎯 RESULTADO DEL DIAGNÓSTICO:" -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta

$logs = docker logs crudclouddb_backend 2>$null | Select-String "MercadoPago configured" -Context 0,3 | Select-Object -Last 1
if ($logs) {
    Write-Host "✅ Credenciales configuradas" -ForegroundColor Green
    Write-Host $logs.Line
} else {
    Write-Host "❌ NO se encontraron credenciales configuradas" -ForegroundColor Red
    Write-Host "Esto significa que el contenedor no está usando las credenciales correctas" -ForegroundColor Red
}

Write-Host
Write-Host "💡 CON GITHUB ACTIONS:" -ForegroundColor Yellow
Write-Host "  - El deploy es automático después de push" -ForegroundColor White
Write-Host "  - Las variables pueden estar en GitHub Secrets" -ForegroundColor White
Write-Host "  - El .env local puede NO estar en el servidor" -ForegroundColor White
Write-Host
Write-Host "🔧 PASOS PARA GitHub Actions:" -ForegroundColor Green
Write-Host "  1. Verificar qué credenciales muestra el diagnóstico" -ForegroundColor White
Write-Host "  2. Si no hay, las variables están mal configuradas en GitHub" -ForegroundColor White
Write-Host "  3. Si hay diferentes, decidir cuál cuenta usar" -ForegroundColor White
Write-Host "  4. Configurar GitHub Secrets con las credenciales correctas" -ForegroundColor White

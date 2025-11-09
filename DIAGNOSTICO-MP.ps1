# ============================================
# 🔍 DIAGNÓSTICO AVANZADO DE MERCADOPAGO
# ============================================
# Para cuando las credenciales están correctas vía GitHub Secrets
# ============================================

Write-Host "🔍 DIAGNÓSTICO AVANZADO DE MERCADOPAGO" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "✅ Credenciales verificadas como correctas vía GitHub Secrets" -ForegroundColor Green
Write-Host

Write-Host "📋 1. Verificando credenciales activas en el contenedor:" -ForegroundColor Yellow
$mpConfig = docker logs crudclouddb_backend 2>$null | Select-String "MercadoPago configured" -Context 0,3 | Select-Object -Last 1
if ($mpConfig) {
    Write-Host $mpConfig.Line -ForegroundColor Green
} else {
    Write-Host "❌ NO se encontraron credenciales en logs de inicio" -ForegroundColor Red
}
Write-Host

Write-Host "📋 2. Testeando preferencia recién creada:" -ForegroundColor Yellow
$preference = docker logs crudclouddb_backend 2>$null | Select-String "Respuesta de MercadoPago recibida" -Context 0,5 | Select-Object -Last 1
if ($preference) {
    Write-Host "✅ Última preferencia creada:" -ForegroundColor Green
    $preference.Context.PostContext | ForEach-Object { Write-Host "   $_" }
} else {
    Write-Host "⚠️ No hay preferencias recientes en logs" -ForegroundColor Yellow
}
Write-Host

Write-Host "📋 3. Verificando webhooks recibidos desde MercadoPago:" -ForegroundColor Yellow
$webhooks = docker logs crudclouddb_backend 2>$null | Select-String "WEBHOOK RECIBIDO DE MERCADOPAGO" | Select-Object -Last 5
if ($webhooks) {
    Write-Host "✅ Webhooks recibidos:" -ForegroundColor Green
    $webhooks | ForEach-Object { Write-Host "   $($_.Line)" }
} else {
    Write-Host "❌ NO se han recibido webhooks de MercadoPago" -ForegroundColor Red
    Write-Host "   Esto es el PROBLEMA PRINCIPAL" -ForegroundColor Red
}
Write-Host

Write-Host "📋 4. Verificando configuración del webhook endpoint:" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago" -Method GET -TimeoutSec 10
    Write-Host "✅ Endpoint accesible: $($response.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ Error accediendo al endpoint: $($_.Exception.Message)" -ForegroundColor Red
}

# Test manual del webhook
Write-Host "📋 5. Testeando webhook manualmente:" -ForegroundColor Yellow
try {
    $testPayload = @{
        topic = "merchant_order"
        resource = "https://api.mercadolibre.com/merchant_orders/12345678"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago" -Method POST -Body $testPayload -ContentType "application/json" -TimeoutSec 10
    Write-Host "✅ Webhook responde correctamente" -ForegroundColor Green
    
    # Verificar en logs
    Start-Sleep -Seconds 2
    $testLogs = docker logs crudclouddb_backend 2>$null | Select-String "WEBHOOK RECIBIDO DE MERCADOPAGO" | Select-Object -Last 1
    if ($testLogs) {
        Write-Host "✅ Test apareció en logs: $($testLogs.Line)" -ForegroundColor Green
    } else {
        Write-Host "❌ Test NO apareció en logs - problema en el procesamiento" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error testeando webhook: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host

Write-Host "📋 6. Verificando logs de errores recientes:" -ForegroundColor Yellow
$errors = docker logs crudclouddb_backend --tail 100 2>$null | Select-String -Pattern "error|exception|fail" -SimpleMatch | Select-Object -Last 5
if ($errors) {
    Write-Host "⚠️ Errores encontrados:" -ForegroundColor Yellow
    $errors | ForEach-Object { Write-Host "   $($_.Line)" -ForegroundColor Red }
} else {
    Write-Host "✅ No hay errores recientes" -ForegroundColor Green
}
Write-Host

Write-Host "🎯 DIAGNÓSTICO ESPECÍFICO:" -ForegroundColor Magenta
Write-Host "=================================" -ForegroundColor Magenta

if (-not $webhooks) {
    Write-Host "🚨 PROBLEMA IDENTIFICADO: NO LLEGAN WEBHOOKS" -ForegroundColor Red
    Write-Host
    Write-Host "Posibles causas:" -ForegroundColor Yellow
    Write-Host "1. 🔧 NotificationUrl no está configurada en la preferencia" -ForegroundColor White
    Write-Host "2. 🌐 MercadoPago no puede alcanzar tu servidor" -ForegroundColor White
    Write-Host "3. 🔐 Cuenta de MercadoPago tiene restricciones" -ForegroundColor White
    Write-Host "4. ⏱️ MercadoPago está enviando webhooks a otra URL" -ForegroundColor White
    Write-Host
    Write-Host "🔍 VERIFICACIONES NECESARIAS:" -ForegroundColor Cyan
    Write-Host "A. Ve a https://www.mercadopago.com.co/developers/panel/notifications/webhooks" -ForegroundColor White
    Write-Host "B. Verifica si hay una URL de webhook configurada" -ForegroundColor White
    Write-Host "C. Si no hay, configura: https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago" -ForegroundColor White
    Write-Host "D. Si hay otra URL, ese es el problema" -ForegroundColor White
    Write-Host
    Write-Host "📧 También verifica en tu email si MercadoPago envía notificaciones de pagos" -ForegroundColor White
} else {
    Write-Host "✅ Los webhooks SÍ llegan - problema en el procesamiento interno" -ForegroundColor Green
}
Write-Host

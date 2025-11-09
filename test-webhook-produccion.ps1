# ============================================
# TEST COMPLETO DE WEBHOOK EN PRODUCCIÓN
# Dominio: service.voyager.andrescortes.dev
# ============================================

Write-Host "`n🔍 TESTING WEBHOOK DE PRODUCCIÓN`n" -ForegroundColor Cyan

$domain = "service.voyager.andrescortes.dev"
$webhookUrl = "https://$domain/api/webhooks/mercadopago"

# ============================================
# Test 1: Health Check
# ============================================
Write-Host "1️⃣ Testing Health endpoint..." -ForegroundColor Yellow
Write-Host "   URL: https://$domain/api/Health" -ForegroundColor Gray

try {
    $health = Invoke-WebRequest -Uri "https://$domain/api/Health" -TimeoutSec 10 -ErrorAction Stop
    Write-Host "   ✅ Status: $($health.StatusCode) - API está funcionando" -ForegroundColor Green
    Write-Host "   Response: $($health.Content.Substring(0, [Math]::Min(100, $health.Content.Length)))" -ForegroundColor Gray
}
catch {
    Write-Host "   ❌ ERROR: API NO responde" -ForegroundColor Red
    Write-Host "   Detalles: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host "`n⚠️  El servidor puede no estar desplegado o hay un problema de red`n" -ForegroundColor Yellow
}

Write-Host ""

# ============================================
# Test 2: Plans Endpoint
# ============================================
Write-Host "2️⃣ Testing Plans endpoint..." -ForegroundColor Yellow
Write-Host "   URL: https://$domain/api/Plans" -ForegroundColor Gray

try {
    $plansResponse = Invoke-WebRequest -Uri "https://$domain/api/Plans" -TimeoutSec 10 -ErrorAction Stop
    $plans = $plansResponse.Content | ConvertFrom-Json
    
    if ($plans) {
        Write-Host "   ✅ Status: $($plansResponse.StatusCode) - Encontrados $($plans.Count) planes" -ForegroundColor Green
        
        foreach ($plan in $plans) {
            Write-Host "      - $($plan.name): $($plan.price) COP ($($plan.databaseLimitPerEngine) DBs)" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "   ❌ ERROR: No se pudieron obtener los planes" -ForegroundColor Red
    Write-Host "   Detalles: $($_.Exception.Message)" -ForegroundColor Gray
}

Write-Host ""

# ============================================
# Test 3: Webhook Endpoint
# ============================================
Write-Host "3️⃣ Testing Webhook endpoint..." -ForegroundColor Yellow
Write-Host "   URL: $webhookUrl" -ForegroundColor Gray

try {
    $webhookBody = @{
        resource = "/merchant_orders/123456789"
        topic = "merchant_order"
    } | ConvertTo-Json

    Write-Host "   Enviando petición de prueba..." -ForegroundColor Gray

    $webhook = Invoke-WebRequest `
        -Uri $webhookUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body $webhookBody `
        -TimeoutSec 10 `
        -ErrorAction Stop

    Write-Host "   ✅ Status: $($webhook.StatusCode) - Webhook responde correctamente" -ForegroundColor Green
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 200) {
        Write-Host "   ✅ Status: 200 - Webhook funciona correctamente" -ForegroundColor Green
    }
    elseif ($statusCode -eq 400) {
        Write-Host "   ⚠️  Status: 400 - Endpoint existe pero rechazó datos de prueba (esperado)" -ForegroundColor Yellow
    }
    elseif ($statusCode -eq 404) {
        Write-Host "   ❌ Status: 404 - Endpoint NO encontrado" -ForegroundColor Red
        Write-Host "   Verifica que la ruta sea: /api/webhooks/mercadopago" -ForegroundColor Yellow
    }
    else {
        Write-Host "   ❌ Status: $statusCode - Error inesperado" -ForegroundColor Red
        Write-Host "   Detalles: $($_.Exception.Message)" -ForegroundColor Gray
    }
}

Write-Host ""

# ============================================
# Resumen y URL para MercadoPago
# ============================================
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  📋 URL PARA CONFIGURAR EN MERCADOPAGO:" -ForegroundColor White
Write-Host "" -ForegroundColor Cyan
Write-Host "  $webhookUrl" -ForegroundColor Green
Write-Host "" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Copiar al portapapeles
try {
    Set-Clipboard -Value $webhookUrl
    Write-Host "✅ URL copiada al portapapeles automáticamente`n" -ForegroundColor Green
}
catch {
    Write-Host "⚠️  No se pudo copiar al portapapeles, copia manualmente:  $webhookUrl`n" -ForegroundColor Yellow
}

# Instrucciones finales
Write-Host "📝 PASOS SIGUIENTES:" -ForegroundColor Yellow
Write-Host "1. Ve a: https://www.mercadopago.com.co/developers/panel/app/2955353636/webhooks" -ForegroundColor White
Write-Host "2. Click en 'Crear webhook'" -ForegroundColor White
Write-Host "3. Pega la URL de arriba (Ctrl+V)" -ForegroundColor White
Write-Host "4. Selecciona evento: merchant_order" -ForegroundColor White
Write-Host "5. Modo: Producción" -ForegroundColor White
Write-Host "6. Guardar`n" -ForegroundColor White

# Preguntar si abrir el navegador
$openBrowser = Read-Host "¿Abrir el panel de MercadoPago en el navegador? (s/n)"

if ($openBrowser -eq "s" -or $openBrowser -eq "S") {
    Start-Process "https://www.mercadopago.com.co/developers/panel/app/2955353636/webhooks"
    Write-Host "`n✅ Navegador abierto. Configura el webhook con la URL que está en tu portapapeles.`n" -ForegroundColor Green
}
else {
    Write-Host "`n👍 Puedes abrir el panel manualmente cuando estés listo.`n" -ForegroundColor Cyan
}

Write-Host "🎉 ¡Testing completado!`n" -ForegroundColor Green


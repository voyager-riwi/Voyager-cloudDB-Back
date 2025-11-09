# ============================================
# OBTENER URL PARA WEBHOOK DE MERCADOPAGO
# ============================================

Write-Host "`n🔍 OBTENIENDO URL PARA WEBHOOK DE MERCADOPAGO`n" -ForegroundColor Cyan

# Verificar si ngrok está corriendo
try {
    $ngrokApi = Invoke-RestMethod -Uri "http://127.0.0.1:4040/api/tunnels" -Method GET -ErrorAction Stop
    $publicUrl = $ngrokApi.tunnels[0].public_url
    
    if ($publicUrl) {
        $webhookUrl = "$publicUrl/api/webhooks/mercadopago"
        
        Write-Host "✅ ngrok está corriendo correctamente`n" -ForegroundColor Green
        
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Yellow
        Write-Host "  COPIA ESTA URL PARA MERCADOPAGO:" -ForegroundColor White
        Write-Host "" -ForegroundColor Yellow
        Write-Host "  $webhookUrl" -ForegroundColor Cyan
        Write-Host "" -ForegroundColor Yellow
        Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Yellow
        
        # Copiar al portapapeles
        Set-Clipboard -Value $webhookUrl
        Write-Host "📋 URL copiada al portapapeles automáticamente`n" -ForegroundColor Green
        
        Write-Host "📝 PASOS SIGUIENTES:" -ForegroundColor Yellow
        Write-Host "1. Ve a: https://www.mercadopago.com.co/developers/panel/app/2955353636/webhooks" -ForegroundColor White
        Write-Host "2. Click en 'Crear webhook' o 'Nuevo'" -ForegroundColor White
        Write-Host "3. Pega la URL (ya está en tu portapapeles: Ctrl+V)" -ForegroundColor White
        Write-Host "4. Selecciona evento: merchant_order" -ForegroundColor White
        Write-Host "5. Modo: Producción" -ForegroundColor White
        Write-Host "6. Click en 'Guardar'`n" -ForegroundColor White
        
        # Abrir el navegador automáticamente
        $openBrowser = Read-Host "¿Abrir el panel de MercadoPago en el navegador? (s/n)"
        
        if ($openBrowser -eq "s" -or $openBrowser -eq "S") {
            Start-Process "https://www.mercadopago.com.co/developers/panel/app/2955353636/webhooks"
            Write-Host "`n✅ Navegador abierto. Pega la URL que está en tu portapapeles.`n" -ForegroundColor Green
        }
        
        Write-Host "🧪 Para probar que funciona:" -ForegroundColor Yellow
        Write-Host "1. Crea una preferencia de pago" -ForegroundColor White
        Write-Host "2. Haz un pago" -ForegroundColor White
        Write-Host "3. Verifica los logs de tu app`n" -ForegroundColor White
        
    }
    else {
        throw "No se encontró URL pública de ngrok"
    }
}
catch {
    Write-Host "❌ ERROR: ngrok NO está corriendo`n" -ForegroundColor Red
    Write-Host "Para que el webhook funcione, necesitas ngrok:" -ForegroundColor Yellow
    Write-Host "1. Abre otra terminal" -ForegroundColor White
    Write-Host "2. Ejecuta: ngrok http 5191" -ForegroundColor White
    Write-Host "3. Vuelve a ejecutar este script`n" -ForegroundColor White
    Write-Host "Descarga ngrok: https://ngrok.com/download" -ForegroundColor Gray
    Write-Host ""
}


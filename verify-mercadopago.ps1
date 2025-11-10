# Script para verificar la configuración de Mercado Pago
# Ejecutar: .\verify-mercadopago.ps1

Write-Host "🔍 Verificando configuración de Mercado Pago..." -ForegroundColor Cyan
Write-Host ""

# Verificar si existe el archivo .env
$envFile = ".env"
if (Test-Path $envFile) {
    Write-Host "✅ Archivo .env encontrado" -ForegroundColor Green
    
    # Leer el archivo .env
    $envContent = Get-Content $envFile -Raw
    
    # Verificar MERCADOPAGO_ACCESS_TOKEN
    if ($envContent -match "MERCADOPAGO_ACCESS_TOKEN=APP_USR-") {
        Write-Host "OK MERCADOPAGO_ACCESS_TOKEN configurado" -ForegroundColor Green
    } elseif ($envContent -match "MERCADOPAGO_ACCESS_TOKEN=") {
        Write-Host "ADVERTENCIA MERCADOPAGO_ACCESS_TOKEN encontrado pero verifica el valor" -ForegroundColor Yellow
    } else {
        Write-Host "ERROR MERCADOPAGO_ACCESS_TOKEN no encontrado en .env" -ForegroundColor Red
    }
    
    # Verificar MERCADOPAGO_PUBLIC_KEY
    if ($envContent -match "MERCADOPAGO_PUBLIC_KEY=APP_USR-") {
        Write-Host "OK MERCADOPAGO_PUBLIC_KEY configurado" -ForegroundColor Green
    } elseif ($envContent -match "MERCADOPAGO_PUBLIC_KEY=") {
        Write-Host "ADVERTENCIA MERCADOPAGO_PUBLIC_KEY encontrado pero verifica el valor" -ForegroundColor Yellow
    } else {
        Write-Host "ERROR MERCADOPAGO_PUBLIC_KEY no encontrado en .env" -ForegroundColor Red
    }
    
    # Verificar MERCADOPAGO_WEBHOOK_SECRET
    if ($envContent -match "MERCADOPAGO_WEBHOOK_SECRET=\w+") {
        Write-Host "OK MERCADOPAGO_WEBHOOK_SECRET configurado" -ForegroundColor Green
    } elseif ($envContent -match "MERCADOPAGO_WEBHOOK_SECRET=") {
        Write-Host "ADVERTENCIA MERCADOPAGO_WEBHOOK_SECRET vacio" -ForegroundColor Yellow
    } else {
        Write-Host "ADVERTENCIA MERCADOPAGO_WEBHOOK_SECRET no encontrado (opcional)" -ForegroundColor Yellow
    }
    
} else {
    Write-Host "❌ Archivo .env no encontrado" -ForegroundColor Red
    Write-Host "   Ejecuta: cp .env.example .env" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "📋 Verificando archivos de configuración..." -ForegroundColor Cyan

# Verificar MercadoPagoSettings.cs
$settingsFile = "CrudCloudDb.API\Configuration\MercadoPagoSettings.cs"
if (Test-Path $settingsFile) {
    Write-Host "✅ MercadoPagoSettings.cs existe" -ForegroundColor Green
    
    $settingsContent = Get-Content $settingsFile -Raw
    if ($settingsContent -match "WebhookSecret") {
        Write-Host "✅ WebhookSecret definido en MercadoPagoSettings" -ForegroundColor Green
    } else {
        Write-Host "❌ WebhookSecret no definido en MercadoPagoSettings" -ForegroundColor Red
    }
} else {
    Write-Host "❌ MercadoPagoSettings.cs no encontrado" -ForegroundColor Red
}

# Verificar PaymentsController.cs
$paymentsController = "CrudCloudDb.API\Controllers\PaymentsController.cs"
if (Test-Path $paymentsController) {
    Write-Host "✅ PaymentsController.cs existe" -ForegroundColor Green
    
    $controllerContent = Get-Content $paymentsController -Raw
    if ($controllerContent -match "GetPublicKey") {
        Write-Host "✅ Endpoint GetPublicKey configurado" -ForegroundColor Green
    } else {
        Write-Host "❌ Endpoint GetPublicKey no encontrado" -ForegroundColor Red
    }
} else {
    Write-Host "❌ PaymentsController.cs no encontrado" -ForegroundColor Red
}

# Verificar WebhooksController.cs
$webhooksController = "CrudCloudDb.API\Controllers\WebhooksController.cs"
if (Test-Path $webhooksController) {
    Write-Host "✅ WebhooksController.cs existe" -ForegroundColor Green
} else {
    Write-Host "❌ WebhooksController.cs no encontrado" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎯 Endpoints disponibles:" -ForegroundColor Cyan
Write-Host "   GET  /api/payments/public-key      - Obtener Public Key" -ForegroundColor Gray
Write-Host "   POST /api/payments/create-preference - Crear preferencia de pago" -ForegroundColor Gray
Write-Host "   POST /api/webhooks/mercadopago     - Recibir notificaciones" -ForegroundColor Gray

Write-Host ""
Write-Host "Para mas informacion, consulta: MERCADOPAGO_SETUP.md" -ForegroundColor Cyan
Write-Host ""

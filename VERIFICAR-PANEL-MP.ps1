# ============================================
# 🔧 VERIFICACIÓN PANEL MERCADOPAGO
# ============================================
# Script para verificar configuración en el panel web
# ============================================

Write-Host "🔧 VERIFICACIÓN MANUAL EN PANEL MERCADOPAGO" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host

Write-Host "📋 PASO 1: Verificar cuenta activa" -ForegroundColor Yellow
Write-Host "Ve a: https://www.mercadopago.com.co/home" -ForegroundColor Cyan
Write-Host "Verifica:"
Write-Host "  ✅ Cuenta verificada (sin alertas rojas)"
Write-Host "  ✅ Puede recibir pagos"
Write-Host "  ✅ No hay restricciones"
Write-Host

Write-Host "📋 PASO 2: Verificar webhooks configurados" -ForegroundColor Yellow
Write-Host "Ve a: https://www.mercadopago.com.co/developers/panel/notifications/webhooks" -ForegroundColor Cyan
Write-Host "Verifica:"
Write-Host "  ✅ Hay webhooks configurados"
Write-Host "  ✅ URL: https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago"
Write-Host "  ✅ Events: payment, merchant_order"
Write-Host "  ✅ Status: Active"
Write-Host

Write-Host "📋 PASO 3: Verificar actividad de pagos" -ForegroundColor Yellow
Write-Host "Ve a: https://www.mercadopago.com.co/activities" -ForegroundColor Cyan
Write-Host "Busca:"
Write-Host "  🔍 Intentos de pago de las últimas horas"
Write-Host "  🔍 Estado de esos pagos (aprobados/rechazados)"
Write-Host "  🔍 Si hay merchant orders creadas"
Write-Host

Write-Host "📋 PASO 4: Si NO hay webhooks configurados" -ForegroundColor Red
Write-Host "Entonces ESE es el problema. Debes configurar:" -ForegroundColor White
Write-Host "  URL: https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago"
Write-Host "  Events: payment, merchant_order"
Write-Host

Write-Host "📋 PASO 5: Si SÍ hay webhooks configurados" -ForegroundColor Green
Write-Host "Verifica los intentos:"
Write-Host "  🔍 Click en 'View details' de algún webhook"
Write-Host "  🔍 Verifica si MercadoPago está enviando requests"
Write-Host "  🔍 Verifica si tu servidor responde 200 OK"
Write-Host "  🔍 Si responde 4xx/5xx, ese es el problema"
Write-Host

Write-Host "🎯 PROBLEMAS COMUNES:" -ForegroundColor Magenta
Write-Host "========================" -ForegroundColor Magenta
Write-Host "1. 🚫 NO hay webhooks configurados en el panel" -ForegroundColor Red
Write-Host "   → MercadoPago no sabe dónde enviar notificaciones"
Write-Host
Write-Host "2. 🔗 URL incorrecta en el webhook" -ForegroundColor Yellow
Write-Host "   → Verifica que sea exactamente: https://service.voyager.andrescortes.dev/api/Webhooks/mercadopago"
Write-Host
Write-Host "3. ⚙️ Events incorrectos configurados" -ForegroundColor Yellow
Write-Host "   → Debe incluir 'payment' y 'merchant_order'"
Write-Host
Write-Host "4. 🔴 Webhook desactivado" -ForegroundColor Red
Write-Host "   → Verifica que el status sea 'Active'"
Write-Host
Write-Host "5. 🌐 Problema de conectividad" -ForegroundColor Yellow
Write-Host "   → MercadoPago no puede alcanzar tu servidor"
Write-Host
Write-Host "6. 🏦 Cuenta con restricciones" -ForegroundColor Red
Write-Host "   → Cuenta nueva o no verificada completamente"
Write-Host

Write-Host "💡 DESPUÉS DE REVISAR EL PANEL:" -ForegroundColor Cyan
Write-Host "1. Si NO hay webhooks → Configúralos"
Write-Host "2. Si SÍ hay webhooks → Ejecuta DIAGNOSTICO-MP.ps1"
Write-Host "3. Comparte screenshots del panel si es necesario"
Write-Host

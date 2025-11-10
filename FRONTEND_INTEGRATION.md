# 🎨 Integración Frontend - Mercado Pago Checkout Pro

Este documento muestra cómo integrar Mercado Pago Checkout Pro en tu frontend.

## 📦 Instalación

### Opción 1: CDN (Recomendado para empezar)

```html
<script src="https://sdk.mercadopago.com/js/v2"></script>
```

### Opción 2: NPM

```bash
npm install @mercadopago/sdk-js
```

## 🔧 Configuración Básica

### 1. Obtener la Public Key del Backend

```javascript
// Función para obtener la Public Key
async function getMercadoPagoPublicKey() {
  try {
    const response = await fetch('https://api.voyager.com/api/payments/public-key');
    const data = await response.json();
    return data.publicKey;
  } catch (error) {
    console.error('Error obteniendo Public Key:', error);
    throw error;
  }
}
```

### 2. Inicializar Mercado Pago

```javascript
let mp = null;

async function initMercadoPago() {
  const publicKey = await getMercadoPagoPublicKey();
  mp = new MercadoPago(publicKey, {
    locale: 'es-AR' // o 'es-CO' para Colombia
  });
  console.log('✅ Mercado Pago inicializado');
}

// Inicializar al cargar la página
initMercadoPago();
```

## 💳 Crear y Procesar Pago

### Ejemplo Completo - React/Next.js

```jsx
import { useState, useEffect } from 'react';

export default function PlanUpgrade() {
  const [loading, setLoading] = useState(false);
  const [mp, setMp] = useState(null);

  // Inicializar Mercado Pago al montar el componente
  useEffect(() => {
    async function init() {
      try {
        const response = await fetch('/api/payments/public-key');
        const { publicKey } = await response.json();
        
        const mercadopago = new window.MercadoPago(publicKey, {
          locale: 'es-AR'
        });
        
        setMp(mercadopago);
      } catch (error) {
        console.error('Error inicializando Mercado Pago:', error);
      }
    }
    
    init();
  }, []);

  // Función para crear preferencia y abrir checkout
  async function handleUpgradeToPremium(planId) {
    setLoading(true);
    
    try {
      // 1. Obtener token de autenticación
      const token = localStorage.getItem('authToken');
      
      // 2. Crear preferencia de pago en el backend
      const response = await fetch('https://api.voyager.com/api/payments/create-preference', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ planId })
      });
      
      const result = await response.json();
      
      if (!result.succeeded) {
        throw new Error(result.message || 'Error creando preferencia');
      }
      
      // 3. Abrir Checkout de Mercado Pago
      if (mp) {
        mp.checkout({
          preference: {
            id: result.data.preferenceId
          },
          autoOpen: true
        });
      } else {
        // Fallback: redirigir directamente
        window.location.href = result.data.initPoint;
      }
      
    } catch (error) {
      console.error('Error procesando pago:', error);
      alert('Error al procesar el pago. Por favor intenta nuevamente.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="plan-upgrade">
      <h2>Actualizar a Premium</h2>
      <p>Obtén acceso a más bases de datos y funciones avanzadas</p>
      
      <button 
        onClick={() => handleUpgradeToPremium('plan-premium-id')}
        disabled={loading || !mp}
      >
        {loading ? 'Procesando...' : 'Actualizar a Premium - $9.99/mes'}
      </button>
    </div>
  );
}
```

### Ejemplo Vanilla JavaScript

```html
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="UTF-8">
  <title>Actualizar Plan</title>
  <script src="https://sdk.mercadopago.com/js/v2"></script>
</head>
<body>
  <button id="upgrade-btn">Actualizar a Premium - $9.99/mes</button>

  <script>
    let mp = null;

    // Inicializar Mercado Pago
    async function initMercadoPago() {
      try {
        const response = await fetch('https://api.voyager.com/api/payments/public-key');
        const { publicKey } = await response.json();
        
        mp = new MercadoPago(publicKey, { locale: 'es-AR' });
        console.log('✅ Mercado Pago listo');
      } catch (error) {
        console.error('Error:', error);
      }
    }

    // Crear preferencia y abrir checkout
    async function upgradeToPremium() {
      const token = localStorage.getItem('authToken');
      const planId = 'your-premium-plan-id';

      try {
        // Crear preferencia
        const response = await fetch('https://api.voyager.com/api/payments/create-preference', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify({ planId })
        });

        const result = await response.json();

        if (result.succeeded) {
          // Abrir checkout
          mp.checkout({
            preference: {
              id: result.data.preferenceId
            },
            autoOpen: true
          });
        } else {
          alert('Error: ' + result.message);
        }
      } catch (error) {
        console.error('Error:', error);
        alert('Error al procesar el pago');
      }
    }

    // Event listeners
    document.getElementById('upgrade-btn').addEventListener('click', upgradeToPremium);
    
    // Inicializar al cargar
    initMercadoPago();
  </script>
</body>
</html>
```

## 🎯 Páginas de Resultado

Crea estas páginas para manejar las redirecciones después del pago:

### 1. Payment Success (`/payment-success`)

```jsx
export default function PaymentSuccess() {
  useEffect(() => {
    // Opcional: Verificar el estado del pago con el backend
    async function verifyPayment() {
      const params = new URLSearchParams(window.location.search);
      const paymentId = params.get('payment_id');
      const status = params.get('status');
      
      console.log('Pago exitoso:', { paymentId, status });
      
      // Actualizar estado del usuario en el frontend
      // Redirigir al dashboard después de unos segundos
      setTimeout(() => {
        window.location.href = '/dashboard';
      }, 3000);
    }
    
    verifyPayment();
  }, []);

  return (
    <div className="payment-success">
      <h1>¡Pago Exitoso! ✅</h1>
      <p>Tu plan ha sido actualizado a Premium.</p>
      <p>Redirigiendo al dashboard...</p>
    </div>
  );
}
```

### 2. Payment Failure (`/payment-failure`)

```jsx
export default function PaymentFailure() {
  return (
    <div className="payment-failure">
      <h1>Pago Rechazado ❌</h1>
      <p>No se pudo procesar tu pago.</p>
      <button onClick={() => window.location.href = '/plans'}>
        Intentar Nuevamente
      </button>
    </div>
  );
}
```

### 3. Payment Pending (`/payment-pending`)

```jsx
export default function PaymentPending() {
  return (
    <div className="payment-pending">
      <h1>Pago Pendiente ⏳</h1>
      <p>Tu pago está siendo procesado.</p>
      <p>Te notificaremos cuando se complete.</p>
    </div>
  );
}
```

## 🔄 Verificar Estado del Plan

Después de un pago exitoso, actualiza el estado del usuario:

```javascript
async function refreshUserPlan() {
  const token = localStorage.getItem('authToken');
  
  try {
    const response = await fetch('https://api.voyager.com/api/users/me', {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
    
    const user = await response.json();
    
    // Actualizar estado en tu aplicación
    console.log('Plan actual:', user.currentPlan);
    
    // Actualizar UI
    updatePlanBadge(user.currentPlan);
    
  } catch (error) {
    console.error('Error obteniendo usuario:', error);
  }
}
```

## 🎨 Ejemplo de UI Completa

```jsx
import { useState, useEffect } from 'react';

export default function PlansPage() {
  const [plans, setPlans] = useState([]);
  const [currentPlan, setCurrentPlan] = useState(null);
  const [mp, setMp] = useState(null);

  useEffect(() => {
    // Inicializar Mercado Pago
    async function init() {
      const response = await fetch('/api/payments/public-key');
      const { publicKey } = await response.json();
      setMp(new window.MercadoPago(publicKey));
    }
    
    // Cargar planes
    async function loadPlans() {
      const response = await fetch('https://api.voyager.com/api/plans');
      const data = await response.json();
      setPlans(data);
    }
    
    // Cargar plan actual del usuario
    async function loadCurrentPlan() {
      const token = localStorage.getItem('authToken');
      const response = await fetch('https://api.voyager.com/api/users/me', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      const user = await response.json();
      setCurrentPlan(user.currentPlan);
    }
    
    init();
    loadPlans();
    loadCurrentPlan();
  }, []);

  async function handleUpgrade(planId) {
    const token = localStorage.getItem('authToken');
    
    try {
      const response = await fetch('https://api.voyager.com/api/payments/create-preference', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ planId })
      });
      
      const result = await response.json();
      
      if (result.succeeded) {
        mp.checkout({
          preference: { id: result.data.preferenceId },
          autoOpen: true
        });
      }
    } catch (error) {
      console.error('Error:', error);
    }
  }

  return (
    <div className="plans-page">
      <h1>Elige tu Plan</h1>
      
      <div className="plans-grid">
        {plans.map(plan => (
          <div key={plan.id} className="plan-card">
            <h2>{plan.name}</h2>
            <p className="price">${plan.price}/mes</p>
            <p>{plan.databaseLimitPerEngine} bases de datos por motor</p>
            
            {currentPlan?.id === plan.id ? (
              <button disabled>Plan Actual</button>
            ) : (
              <button onClick={() => handleUpgrade(plan.id)}>
                {plan.price === 0 ? 'Gratis' : 'Actualizar'}
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
```

## 🧪 Testing

### Tarjetas de Prueba

Para probar la integración, usa estas tarjetas:

**Aprobada:**
- Número: `5031 7557 3453 0604`
- CVV: `123`
- Fecha: Cualquier fecha futura

**Rechazada:**
- Número: `5031 4332 1540 6351`
- CVV: `123`
- Fecha: Cualquier fecha futura

**Más tarjetas:** https://www.mercadopago.com.ar/developers/es/docs/checkout-pro/additional-content/test-cards

## 🔒 Seguridad

### Buenas Prácticas

1. **Nunca expongas el Access Token en el frontend**
   - Solo usa la Public Key
   - El Access Token debe estar solo en el backend

2. **Valida siempre en el backend**
   - No confíes solo en las redirecciones
   - Usa webhooks para confirmar pagos

3. **Usa HTTPS**
   - Mercado Pago requiere HTTPS en producción

4. **Maneja errores apropiadamente**
   - Muestra mensajes claros al usuario
   - Registra errores en el backend

## 📱 Responsive Design

```css
.plans-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 2rem;
  padding: 2rem;
}

.plan-card {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 2rem;
  text-align: center;
}

.plan-card button {
  width: 100%;
  padding: 1rem;
  font-size: 1rem;
  border: none;
  border-radius: 4px;
  background: #009ee3;
  color: white;
  cursor: pointer;
}

.plan-card button:disabled {
  background: #ccc;
  cursor: not-allowed;
}
```

## ✅ Checklist de Integración

- [ ] Script de Mercado Pago cargado
- [ ] Public Key obtenida del backend
- [ ] Mercado Pago inicializado correctamente
- [ ] Botón de pago funcional
- [ ] Páginas de resultado creadas (success/failure/pending)
- [ ] Manejo de errores implementado
- [ ] UI responsive
- [ ] Pruebas con tarjetas de prueba realizadas

## 🎉 ¡Listo!

Tu frontend está listo para procesar pagos con Mercado Pago. El backend se encarga de todo el procesamiento y actualización de planes automáticamente.

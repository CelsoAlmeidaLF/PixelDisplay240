/**
 * PixelDisplay240 - Account Settings Page
 * System settings, cache management, and developer tools
 */

// ============================================
// Environment Info
// ============================================

function setEnvironmentInfo() {
    const envValue = document.getElementById('envValue');
    const apiUrl = document.getElementById('apiUrl');
    
    if (envValue) {
        envValue.textContent = window.location.hostname.includes('localhost') ? 'Desenvolvimento' : 'Produção';
    }
    
    if (apiUrl) {
        apiUrl.textContent = window.location.origin;
    }
}

// ============================================
// Storage Management
// ============================================

function clearLocalStorage() {
    if (confirm('Isso removerá todos os dados locais, incluindo seu login. Continuar?')) {
        localStorage.clear();
        alert('LocalStorage limpo. Você será redirecionado para o login.');
        window.location.href = '/Account/Login';
    }
}

function clearSessionStorage() {
    sessionStorage.clear();
    alert('SessionStorage limpo.');
}

function clearCaches() {
    if ('caches' in window) {
        caches.keys().then(names => {
            names.forEach(name => caches.delete(name));
        });
        alert('Caches limpos. Recarregue a página.');
    } else {
        alert('API de Cache não suportada neste navegador.');
    }
}

// ============================================
// Developer Tools
// ============================================

function copyToken() {
    const token = localStorage.getItem('pd240_token') || sessionStorage.getItem('pd240_token');
    if (token) {
        navigator.clipboard.writeText(token).then(() => {
            alert('Token copiado para a área de transferência.');
        }).catch(() => {
            // Fallback for older browsers
            const textarea = document.createElement('textarea');
            textarea.value = token;
            document.body.appendChild(textarea);
            textarea.select();
            document.execCommand('copy');
            document.body.removeChild(textarea);
            alert('Token copiado para a área de transferência.');
        });
    } else {
        alert('Nenhum token encontrado.');
    }
}

function toggleDebug() {
    const debug = localStorage.getItem('pd240_debug') === 'true';
    localStorage.setItem('pd240_debug', !debug);
    alert(`Debug ${!debug ? 'ativado' : 'desativado'}. Recarregue a página.`);
}

// ============================================
// Initialization
// ============================================

function initializeSettingsPage() {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
    
    // Set environment info
    setEnvironmentInfo();
}

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initializeSettingsPage);

// Export functions for inline event handlers
window.clearLocalStorage = clearLocalStorage;
window.clearSessionStorage = clearSessionStorage;
window.clearCaches = clearCaches;
window.copyToken = copyToken;
window.toggleDebug = toggleDebug;

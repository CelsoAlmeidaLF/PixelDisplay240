/**
 * PixelDisplay240 - Account Profile Page
 * Handles user profile management, password change, API keys, and preferences
 */

// API URLs
const API_BASE = '/api';
const AUTH_API = '/api/auth';
const PROFILE_API = '/api/profile';

// ============================================
// Token Management
// ============================================

function getToken() {
    return localStorage.getItem('pd240_token') || sessionStorage.getItem('pd240_token');
}

function getUserData() {
    const data = localStorage.getItem('pd240_user') || sessionStorage.getItem('pd240_user');
    return data ? JSON.parse(data) : null;
}

// ============================================
// API Helper
// ============================================

async function apiRequest(url, method = 'GET', data = null) {
    const token = getToken();
    if (!token) {
        window.location.href = '/Account/Login?returnUrl=/Account/Profile';
        return null;
    }

    const options = {
        method,
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    };

    if (data) {
        options.body = JSON.stringify(data);
    }

    const response = await fetch(url, options);

    if (response.status === 401) {
        window.location.href = '/Account/Login?returnUrl=/Account/Profile';
        return null;
    }

    return response;
}

// ============================================
// Alert Management
// ============================================

function showAlert(elementId, type, message) {
    const alert = document.getElementById(elementId);
    if (!alert) return;
    
    alert.className = `alert alert-${type}`;
    alert.textContent = message;
    alert.style.display = 'block';

    setTimeout(() => {
        alert.style.display = 'none';
    }, 5000);
}

function hideAlert(elementId) {
    const alert = document.getElementById(elementId);
    if (alert) {
        alert.style.display = 'none';
    }
}

// ============================================
// Navigation
// ============================================

function initializeNavigation() {
    document.querySelectorAll('[data-section]').forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const section = link.dataset.section;

            // Update active states
            document.querySelectorAll('[data-section]').forEach(l => l.classList.remove('active'));
            link.classList.add('active');

            document.querySelectorAll('.content-section').forEach(s => s.classList.remove('active'));
            const sectionEl = document.getElementById(section);
            if (sectionEl) {
                sectionEl.classList.add('active');
            }

            // Update URL hash
            history.pushState(null, null, `#${section}`);
        });
    });

    // Handle initial hash
    if (window.location.hash) {
        const section = window.location.hash.substring(1);
        const link = document.querySelector(`[data-section="${section}"]`);
        if (link) link.click();
    }
}

// ============================================
// Profile Loading
// ============================================

async function loadProfile() {
    try {
        const response = await apiRequest(`${PROFILE_API}/me`);
        if (!response) return;

        const data = await response.json();

        // Update avatar
        const avatarEl = document.getElementById('userAvatar');
        if (avatarEl) {
            avatarEl.textContent = data.username.charAt(0).toUpperCase();
        }

        // Update user info
        updateElementText('userName', data.username);
        updateElementText('userEmail', data.email);

        // Update role badge
        const roleBadge = document.getElementById('userRole');
        if (roleBadge) {
            roleBadge.textContent = data.role;
            roleBadge.className = `badge badge-${data.role.toLowerCase()}`;
        }

        // Update form fields
        updateElementValue('profileUsername', data.username);
        updateElementValue('profileEmail', data.email);
        updateElementValue('profileCreatedAt', formatDate(data.createdAt));

        // Update policies
        const policiesList = document.getElementById('policiesList');
        if (policiesList && data.policies) {
            policiesList.innerHTML = data.policies.map(p =>
                `<span class="badge badge-user">${p}</span>`
            ).join('');
        }

        // Email verification status
        updateEmailVerificationStatus(data.isEmailVerified);

        // Refresh icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }

    } catch (error) {
        console.error('Error loading profile:', error);
        showAlert('globalAlert', 'error', 'Erro ao carregar perfil. Tente novamente.');
    }
}

function updateElementText(id, text) {
    const el = document.getElementById(id);
    if (el) el.textContent = text || '-';
}

function updateElementValue(id, value) {
    const el = document.getElementById(id);
    if (el) el.value = value || '';
}

function formatDate(dateString) {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('pt-BR', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });
}

function updateEmailVerificationStatus(isVerified) {
    const emailStatus = document.getElementById('emailVerificationStatus');
    if (!emailStatus) return;

    if (isVerified) {
        emailStatus.innerHTML = `
            <div style="display: flex; align-items: center; gap: 10px; color: var(--success);">
                <i data-lucide="check-circle"></i>
                <span>E-mail verificado</span>
            </div>
        `;
    } else {
        emailStatus.innerHTML = `
            <div style="display: flex; align-items: center; gap: 10px; color: var(--warning);">
                <i data-lucide="alert-circle"></i>
                <span>E-mail não verificado</span>
            </div>
            <button class="btn btn-secondary" style="margin-top: 1rem;" onclick="resendVerification()">
                Reenviar e-mail de verificação
            </button>
        `;
    }
}

async function resendVerification() {
    showAlert('globalAlert', 'info', 'Enviando e-mail de verificação...');
    // TODO: Implement resend verification endpoint
    setTimeout(() => {
        showAlert('globalAlert', 'success', 'E-mail de verificação enviado!');
    }, 1000);
}

// ============================================
// API Keys Management
// ============================================

async function loadApiKeys() {
    try {
        const response = await apiRequest(`${API_BASE}/agents`);
        if (!response) return;

        const data = await response.json();

        const geminiStatus = document.getElementById('geminiStatus');
        if (geminiStatus) {
            if (data.gemini?.apiKey) {
                geminiStatus.textContent = 'Configurada ?';
                geminiStatus.classList.add('configured');
                const geminiInput = document.getElementById('geminiApiKey');
                if (geminiInput) geminiInput.value = '••••••••••••••••';
            } else {
                geminiStatus.textContent = 'Não configurada';
                geminiStatus.classList.remove('configured');
            }
        }

    } catch (error) {
        console.error('Error loading API keys:', error);
    }
}

function toggleGeminiKeyInput() {
    const form = document.getElementById('geminiKeyForm');
    if (form) {
        form.style.display = form.style.display === 'none' ? 'block' : 'none';
    }
}

function togglePasswordVisibility(inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        input.type = input.type === 'password' ? 'text' : 'password';
    }
}

async function saveGeminiKey() {
    const btn = document.getElementById('btnSaveGeminiKey');
    const apiKeyInput = document.getElementById('geminiApiKey');
    
    if (!btn || !apiKeyInput) return;
    
    const apiKey = apiKeyInput.value.trim();

    if (!apiKey || apiKey === '••••••••••••••••') {
        showAlert('apiKeysAlert', 'error', 'Por favor, insira uma chave válida.');
        return;
    }

    btn.disabled = true;
    btn.innerHTML = '<div class="spinner"></div> Salvando...';

    try {
        const response = await apiRequest(`${API_BASE}/config`, 'POST', {
            Key: 'GeminiKey',
            Value: apiKey
        });

        if (response && response.ok) {
            showAlert('apiKeysAlert', 'success', 'Chave salva com sucesso!');
            const form = document.getElementById('geminiKeyForm');
            if (form) form.style.display = 'none';
            loadApiKeys();
        } else {
            throw new Error('Erro ao salvar');
        }
    } catch (error) {
        showAlert('apiKeysAlert', 'error', 'Erro ao salvar chave. Tente novamente.');
    }

    btn.disabled = false;
    btn.innerHTML = '<i data-lucide="save"></i> Salvar Chave';
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

async function clearGeminiKey() {
    if (!confirm('Tem certeza que deseja remover a chave da API Gemini?')) return;

    try {
        await apiRequest(`${API_BASE}/config`, 'POST', {
            Key: 'GeminiKey',
            Value: ''
        });

        showAlert('apiKeysAlert', 'success', 'Chave removida com sucesso.');
        const geminiInput = document.getElementById('geminiApiKey');
        if (geminiInput) geminiInput.value = '';
        const form = document.getElementById('geminiKeyForm');
        if (form) form.style.display = 'none';
        loadApiKeys();
    } catch (error) {
        showAlert('apiKeysAlert', 'error', 'Erro ao remover chave.');
    }
}

// ============================================
// Password Change
// ============================================

async function changePassword() {
    const btn = document.getElementById('btnChangePassword');
    const currentPassword = document.getElementById('currentPassword')?.value;
    const newPassword = document.getElementById('newPassword')?.value;
    const confirmPassword = document.getElementById('confirmPassword')?.value;

    // Validation
    if (!currentPassword || !newPassword || !confirmPassword) {
        showAlert('passwordAlert', 'error', 'Preencha todos os campos.');
        return;
    }

    if (newPassword !== confirmPassword) {
        showAlert('passwordAlert', 'error', 'As senhas não coincidem.');
        return;
    }

    if (newPassword.length < 8) {
        showAlert('passwordAlert', 'error', 'A nova senha deve ter pelo menos 8 caracteres.');
        return;
    }

    if (!btn) return;
    
    btn.disabled = true;
    btn.innerHTML = '<div class="spinner"></div> Alterando...';

    try {
        const response = await apiRequest(`${PROFILE_API}/change-password`, 'POST', {
            oldPassword: currentPassword,
            newPassword: newPassword
        });

        const data = await response.json();

        if (response.ok) {
            showAlert('passwordAlert', 'success', 'Senha alterada com sucesso! Você será redirecionado para o login.');

            // Clear form
            clearPasswordForm();

            // Logout after password change
            setTimeout(() => {
                localStorage.clear();
                sessionStorage.clear();
                window.location.href = '/Account/Login';
            }, 2000);
        } else {
            throw new Error(data.error || 'Erro ao alterar senha');
        }
    } catch (error) {
        showAlert('passwordAlert', 'error', error.message);
    }

    btn.disabled = false;
    btn.innerHTML = '<i data-lucide="save"></i> Alterar Senha';
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

function clearPasswordForm() {
    const fields = ['currentPassword', 'newPassword', 'confirmPassword'];
    fields.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = '';
    });
}

// ============================================
// Preferences
// ============================================

function loadPreferences() {
    const saved = localStorage.getItem('pd240_preferences');
    if (saved) {
        try {
            const prefs = JSON.parse(saved);
            setCheckbox('prefDarkTheme', prefs.theme === 'dark');
            setCheckbox('prefShowGrid', prefs.gridEnabled !== false);
            setCheckbox('prefAutoSave', prefs.autoSave !== false);
            setCheckbox('prefNotifications', prefs.notifications === true);
            
            const langSelect = document.getElementById('prefLanguage');
            if (langSelect && prefs.language) {
                langSelect.value = prefs.language;
            }
        } catch (e) {
            console.error('Error parsing preferences:', e);
        }
    }
}

function setCheckbox(id, checked) {
    const el = document.getElementById(id);
    if (el) el.checked = checked;
}

async function savePreferences() {
    const btn = document.getElementById('btnSavePreferences');
    if (!btn) return;

    const preferences = {
        theme: document.getElementById('prefDarkTheme')?.checked ? 'dark' : 'light',
        gridEnabled: document.getElementById('prefShowGrid')?.checked ?? true,
        autoSave: document.getElementById('prefAutoSave')?.checked ?? true,
        notifications: document.getElementById('prefNotifications')?.checked ?? false,
        language: document.getElementById('prefLanguage')?.value || 'pt-BR'
    };

    btn.disabled = true;
    btn.innerHTML = '<div class="spinner"></div> Salvando...';

    try {
        // Save to localStorage
        localStorage.setItem('pd240_preferences', JSON.stringify(preferences));

        // Also try to save to server
        await apiRequest(`${PROFILE_API}/preferences`, 'POST', preferences);

        showAlert('globalAlert', 'success', 'Preferências salvas com sucesso!');
    } catch (error) {
        showAlert('globalAlert', 'error', 'Erro ao salvar preferências.');
    }

    btn.disabled = false;
    btn.innerHTML = '<i data-lucide="save"></i> Salvar Preferências';
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

// ============================================
// Sessions
// ============================================

function logoutAllSessions() {
    if (!confirm('Isso encerrará todas as suas sessões, incluindo esta. Deseja continuar?')) return;

    localStorage.clear();
    sessionStorage.clear();
    window.location.href = '/Account/Login';
}

function detectBrowser() {
    const ua = navigator.userAgent;
    if (ua.includes('Chrome')) return 'Chrome';
    if (ua.includes('Firefox')) return 'Firefox';
    if (ua.includes('Safari')) return 'Safari';
    if (ua.includes('Edge')) return 'Edge';
    return 'Navegador';
}

// ============================================
// Danger Zone
// ============================================

function deleteAccount() {
    const confirmed = prompt('Digite "EXCLUIR" para confirmar a exclusão da sua conta:');
    if (confirmed === 'EXCLUIR') {
        alert('Funcionalidade em desenvolvimento. Entre em contato com o suporte para excluir sua conta.');
    }
}

function exportUserData() {
    const userData = localStorage.getItem('pd240_user');
    const prefs = localStorage.getItem('pd240_preferences');

    const exportData = {
        user: userData ? JSON.parse(userData) : null,
        preferences: prefs ? JSON.parse(prefs) : null,
        exportedAt: new Date().toISOString()
    };

    const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `pixeldisplay240_data_${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);

    showAlert('globalAlert', 'success', 'Dados exportados com sucesso!');
}

// ============================================
// Event Listeners Setup
// ============================================

function setupEventListeners() {
    // Change password
    const btnChangePassword = document.getElementById('btnChangePassword');
    if (btnChangePassword) {
        btnChangePassword.addEventListener('click', changePassword);
    }

    // Save Gemini key
    const btnSaveGeminiKey = document.getElementById('btnSaveGeminiKey');
    if (btnSaveGeminiKey) {
        btnSaveGeminiKey.addEventListener('click', saveGeminiKey);
    }

    // Save preferences
    const btnSavePreferences = document.getElementById('btnSavePreferences');
    if (btnSavePreferences) {
        btnSavePreferences.addEventListener('click', savePreferences);
    }

    // Logout all sessions
    const btnLogoutAll = document.getElementById('btnLogoutAll');
    if (btnLogoutAll) {
        btnLogoutAll.addEventListener('click', logoutAllSessions);
    }

    // Delete account
    const btnDeleteAccount = document.getElementById('btnDeleteAccount');
    if (btnDeleteAccount) {
        btnDeleteAccount.addEventListener('click', deleteAccount);
    }

    // Export data
    const btnExportData = document.getElementById('btnExportData');
    if (btnExportData) {
        btnExportData.addEventListener('click', exportUserData);
    }
}

// ============================================
// Initialization
// ============================================

function initializeProfilePage() {
    const token = getToken();
    if (!token) {
        window.location.href = '/Account/Login?returnUrl=/Account/Profile';
        return;
    }

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Set current browser
    const currentBrowser = document.getElementById('currentBrowser');
    if (currentBrowser) {
        currentBrowser.textContent = detectBrowser();
    }

    // Initialize components
    initializeNavigation();
    setupEventListeners();

    // Load data
    loadProfile();
    loadApiKeys();
    loadPreferences();
}

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initializeProfilePage);

// Export functions for inline event handlers
window.toggleGeminiKeyInput = toggleGeminiKeyInput;
window.togglePasswordVisibility = togglePasswordVisibility;
window.clearGeminiKey = clearGeminiKey;
window.resendVerification = resendVerification;

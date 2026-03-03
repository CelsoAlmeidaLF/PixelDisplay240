const $ = id => document.getElementById(id);

// ========== UTILITÁRIOS ==========
const showMessage = (containerId, message, type = 'error') => {
    const container = $(containerId);
    if (!container) return;
    container.style.display = 'block';
    container.className = type === 'success' ? 'success-msg' : type === 'info' ? 'info-msg' : 'error-msg';
    container.textContent = message;
};

const hideMessage = (containerId) => {
    const container = $(containerId);
    if (container) container.style.display = 'none';
};

const hideAllModals = () => {
    $('modalRegister').style.display = 'none';
    $('modalForgotPassword').style.display = 'none';
    $('modalResetPassword').style.display = 'none';
};

// ========== LOGIN ==========
$('btnLogin').addEventListener('click', async () => {
    const username = $('username').value.trim();
    const password = $('password').value;

    hideMessage('out');

    if (!username || !password) {
        showMessage('out', 'Por favor, preencha todos os campos.');
        return;
    }

    try {
        const res = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const data = await res.json().catch(() => ({}));

        if (res.ok) {
            showMessage('out', data.message || 'Login realizado com sucesso!', 'success');

            localStorage.setItem('api_token', data.token);
            localStorage.setItem('user_name', data.username);
            localStorage.setItem('user_role', data.role);
            if (data.refreshToken) {
                localStorage.setItem('refresh_token', data.refreshToken);
            }

            setTimeout(() => {
                window.location.href = data.redirectUrl || '/admin.html';
            }, 500);
        } else {
            showMessage('out', data.error || 'Credenciais inválidas.');
        }
    } catch (e) {
        showMessage('out', 'Falha de comunicação com o servidor.');
    }
});

// Enter para submeter login
$('password')?.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') $('btnLogin').click();
});

// ========== MODAL DE REGISTRO ==========
$('linkRegister')?.addEventListener('click', (e) => {
    e.preventDefault();
    hideAllModals();
    $('modalRegister').style.display = 'flex';
    $('reg-username').focus();
});

$('linkBackLogin')?.addEventListener('click', (e) => {
    e.preventDefault();
    hideAllModals();
    $('username').focus();
});

$('btnDoRegister')?.addEventListener('click', async () => {
    const username = $('reg-username').value.trim();
    const email = $('reg-email').value.trim();
    const password = $('reg-password').value;

    hideMessage('out-register');

    if (!username || !email || !password) {
        showMessage('out-register', 'Por favor, preencha todos os campos.');
        return;
    }

    if (password.length < 8) {
        showMessage('out-register', 'A senha deve ter pelo menos 8 caracteres.');
        return;
    }

    if (!email.includes('@')) {
        showMessage('out-register', 'Por favor, informe um e-mail válido.');
        return;
    }

    try {
        const res = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password })
        });

        const data = await res.json().catch(() => ({}));

        if (res.ok) {
            showMessage('out-register', data.message || 'Cadastro realizado! Aguarde aprovação.', 'success');
            setTimeout(() => {
                hideAllModals();
                $('username').value = username;
                $('username').focus();
            }, 2000);
        } else {
            showMessage('out-register', data.error || 'Erro ao realizar cadastro.');
        }
    } catch (e) {
        showMessage('out-register', 'Erro de conexão com o servidor.');
    }
});

// ========== MODAL DE RECUPERAÇÃO DE SENHA ==========
$('linkForgotPassword')?.addEventListener('click', (e) => {
    e.preventDefault();
    hideAllModals();
    $('modalForgotPassword').style.display = 'flex';
    $('forgot-email').focus();
});

$('linkBackLoginFromForgot')?.addEventListener('click', (e) => {
    e.preventDefault();
    hideAllModals();
    $('username').focus();
});

$('btnDoForgot')?.addEventListener('click', async () => {
    const email = $('forgot-email').value.trim();

    hideMessage('out-forgot');

    if (!email) {
        showMessage('out-forgot', 'Por favor, informe seu e-mail.');
        return;
    }

    if (!email.includes('@')) {
        showMessage('out-forgot', 'Por favor, informe um e-mail válido.');
        return;
    }

    try {
        const res = await fetch('/api/auth/forgot-password', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email })
        });

        const data = await res.json().catch(() => ({}));

        // Sempre mostra sucesso por segurança (não revelar se email existe)
        showMessage('out-forgot', data.message || 'Se o e-mail estiver cadastrado, você receberá um link de recuperação.', 'success');

    } catch (e) {
        showMessage('out-forgot', 'Erro de conexão com o servidor.');
    }
});

// ========== MODAL DE RESET DE SENHA ==========
$('linkBackLoginFromReset')?.addEventListener('click', (e) => {
    e.preventDefault();
    hideAllModals();
    // Remove token da URL
    window.history.replaceState({}, document.title, window.location.pathname);
    $('username').focus();
});

$('btnDoReset')?.addEventListener('click', async () => {
    const token = $('reset-token').value;
    const newPassword = $('reset-password').value;
    const confirmPassword = $('reset-password-confirm').value;

    hideMessage('out-reset');

    if (!newPassword || !confirmPassword) {
        showMessage('out-reset', 'Por favor, preencha todos os campos.');
        return;
    }

    if (newPassword.length < 8) {
        showMessage('out-reset', 'A senha deve ter pelo menos 8 caracteres.');
        return;
    }

    if (newPassword !== confirmPassword) {
        showMessage('out-reset', 'As senhas não conferem.');
        return;
    }

    try {
        const res = await fetch('/api/auth/reset-password', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ token, newPassword })
        });

        const data = await res.json().catch(() => ({}));

        if (res.ok) {
            showMessage('out-reset', data.message || 'Senha alterada com sucesso!', 'success');
            setTimeout(() => {
                hideAllModals();
                window.history.replaceState({}, document.title, window.location.pathname);
                $('username').focus();
            }, 2000);
        } else {
            showMessage('out-reset', data.error || 'Token inválido ou expirado.');
        }
    } catch (e) {
        showMessage('out-reset', 'Erro de conexão com o servidor.');
    }
});

// ========== VERIFICAR URL PARAMS ==========
window.addEventListener('load', () => {
    const urlParams = new URLSearchParams(window.location.search);
    
    // Verificar se veio com token de reset
    const resetToken = urlParams.get('token');
    if (resetToken) {
        $('reset-token').value = resetToken;
        $('modalResetPassword').style.display = 'flex';
        $('reset-password').focus();
        return;
    }

    // Verificar se é verificação de email
    const verified = urlParams.get('verified');
    if (verified === 'true') {
        showMessage('out', 'E-mail verificado com sucesso! Você já pode fazer login.', 'success');
    }

    // Focus inicial
    $('username')?.focus();
});

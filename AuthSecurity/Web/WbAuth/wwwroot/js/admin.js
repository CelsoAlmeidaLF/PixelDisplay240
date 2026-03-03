const $ = id => document.getElementById(id);

// Feedback UI
const showMessage = (id, msg, type) => {
    const el = $(id);
    el.style.display = 'block';
    el.style.background = type === 'success' ? 'rgba(74, 222, 128, 0.1)' : 'rgba(239, 68, 68, 0.1)';
    el.style.border = `1px solid ${type === 'success' ? 'var(--success)' : 'var(--error)'}`;
    el.style.color = type === 'success' ? '#86efac' : '#fca5a5';
    el.textContent = msg;
};

// Alterar Senha
$('btnChangePassword').addEventListener('click', async () => {
    const oldPassword = $('old-password').value;
    const newPassword = $('new-password').value;
    const confirmPassword = $('confirm-password').value;
    const token = localStorage.getItem('api_token');

    // A validação agora acontece inteiramente no Backend (Minimal API)
    if (newPassword !== confirmPassword) {
        showMessage('pass-msg', 'As novas senhas não coincidem.', 'error');
        return;
    }

    try {
        const res = await fetch('/api/auth/change-password', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ oldPassword, newPassword })
        });

        if (res.ok) {
            showMessage('pass-msg', 'Senha atualizada com sucesso!', 'success');
            $('old-password').value = '';
            $('new-password').value = '';
            $('confirm-password').value = '';
        } else {
            const data = await res.json().catch(() => ({}));
            showMessage('pass-msg', data.error || 'Erro ao alterar senha. Verifique a senha atual.', 'error');
        }
    } catch (e) {
        showMessage('pass-msg', 'Erro de conexão com o servidor.', 'error');
    }
});

// Logout
$('btnLogout').addEventListener('click', () => {
    localStorage.clear();
    window.location.href = '/login.html';
});

// Inicialização
window.addEventListener('load', async () => {
    const token = localStorage.getItem('api_token');

    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    try {
        // Validar token no servidor antes de mostrar qualquer coisa
        const res = await fetch('/api/auth/validate', {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (!res.ok) {
            throw new Error('Token inválido ou expirado');
        }

        // Se o servidor validou, mostramos o conteúdo
        document.body.style.display = 'grid';

        const userName = localStorage.getItem('user_name');
        const userRole = localStorage.getItem('user_role');

        // Atualizar UI com dados do usuário
        if ($('token')) $('token').value = token;

        if (userName) {
            const userInfoName = document.querySelector('#user-info div:first-child');
            if (userInfoName) userInfoName.textContent = userName;
            if ($('client-name')) $('client-name').textContent = userName;
        }

        if (userRole) {
            const userInfoRole = document.querySelector('#user-info div:last-child');
            if (userInfoRole) userInfoRole.textContent = userRole;
            if ($('user-role-label')) $('user-role-label').textContent = userRole;
        }

        // Decodificar token para mostrar expiração (simples)
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const expiry = new Date(payload.exp * 1000);
            if ($('token-expiry')) $('token-expiry').textContent = expiry.toLocaleTimeString();
        } catch (tokenErr) {
            console.warn('Erro ao decodificar token:', tokenErr);
        }

        // Atualizar hora do último acesso (simulado para o portal do cliente)
        if ($('last-login-time')) {
            $('last-login-time').textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        }

    } catch (e) {
        console.error('Falha na autenticação:', e);
        localStorage.clear();
        window.location.href = '/login.html';
    }
});

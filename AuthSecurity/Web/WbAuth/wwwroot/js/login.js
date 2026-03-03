const $ = id => document.getElementById(id);

// Configuração Global (Ação orientada pela API)
$('btnLogin').addEventListener('click', async () => {
    const username = $('username').value;
    const password = $('password').value;

    try {
        const res = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const data = await res.json().catch(() => ({}));

        if (res.ok) {
            // Sucesso: Segue instruções da API
            if (data.message) alert(data.message);

            localStorage.setItem('api_token', data.token);
            localStorage.setItem('user_name', data.username);
            localStorage.setItem('user_role', data.role);

            window.location.href = data.redirectUrl;
        } else {
            // Erro: Mostra o que a API mandou
            alert(data.error || 'Erro inesperado.');
        }
    } catch (e) {
        alert('Falha de comunicação com o servidor.');
    }
});

// Modal de Registro (UI pura)
$('linkRegister')?.addEventListener('click', (e) => {
    e.preventDefault();
    $('modalRegister').style.display = 'flex';
});

$('linkBackLogin')?.addEventListener('click', (e) => {
    e.preventDefault();
    $('modalRegister').style.display = 'none';
});

// Registro orientado pela API
$('btnDoRegister')?.addEventListener('click', async () => {
    const username = $('reg-username').value;
    const email = $('reg-email').value;
    const password = $('reg-password').value;

    try {
        const res = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password })
        });

        const data = await res.json().catch(() => ({}));

        if (res.ok) {
            alert(data.message);
            $('modalRegister').style.display = 'none';
            $('username').value = username;
        } else {
            alert(data.error || 'Erro ao realizar cadastro.');
        }
    } catch (e) {
        alert('Erro de conexão com o servidor.');
    }
});

window.addEventListener('load', () => {
    $('username')?.focus();
});

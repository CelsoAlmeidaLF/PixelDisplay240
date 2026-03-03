const $ = id => document.getElementById(id);

$('btnLogin').addEventListener('click', async () => {
    const username = $('username').value;
    const password = $('password').value;
    const out = $('out');

    try {
        const res = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const data = await res.json().catch(() => ({}));

        if (res.ok) {
            if (data.message) alert(data.message);

            localStorage.setItem('api_token', data.token);
            window.location.href = data.redirectUrl;
        } else {
            // Em vez de lógica local, o JS apenas exibe o erro da API
            const errorMsg = data.error || 'Acesso negado.';
            if (out) {
                out.style.display = 'block';
                out.textContent = errorMsg;
            } else {
                alert(errorMsg);
            }
        }
    } catch (e) {
        alert('Erro de conexão com o servidor.');
    }
});

window.addEventListener('load', () => {
    $('username')?.focus();
});

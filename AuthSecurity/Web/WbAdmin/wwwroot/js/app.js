const $ = id => document.getElementById(id);

const showOut = (id, text) => {
    const el = $(id);
    el.style.display = 'block';
    el.textContent = typeof text === 'string' ? text : JSON.stringify(text, null, 2);
};

$('btnHello')?.addEventListener('click', async () => {
    try {
        const res = await fetch('/api/hello');
        const data = await res.text();
        showOut('outHello', data);
    } catch (e) {
        showOut('outHello', 'Erro: ' + e.message);
    }
});

$('btnTime')?.addEventListener('click', async () => {
    try {
        const res = await fetch('/api/time');
        const data = await res.json();
        showOut('outTime', data);
    } catch (e) {
        showOut('outTime', 'Erro: ' + e.message);
    }
});

$('btnEcho')?.addEventListener('click', async () => {
    const msg = $('txtEcho').value;
    if (!msg) return alert('Digite uma mensagem');

    try {
        const res = await fetch('/api/echo?msg=' + encodeURIComponent(msg));
        const data = await res.json();
        showOut('outEcho', data);
    } catch (e) {
        showOut('outEcho', 'Erro: ' + e.message);
    }
});

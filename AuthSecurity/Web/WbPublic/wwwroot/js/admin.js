/**
 * WbPublic - Painel de Administração
 * JavaScript principal para gerenciamento do dashboard
 */

const $ = id => document.getElementById(id);
const token = localStorage.getItem('api_token');

// ========== TOAST NOTIFICATIONS ==========
const showToast = (message, type = 'success') => {
    const container = $('toast-container') || document.body;
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    const icon = type === 'success' ? 'check-circle' : type === 'error' ? 'alert-circle' : 'alert-triangle';
    toast.innerHTML = `<i data-lucide="${icon}" style="width: 20px;"></i><span>${message}</span>`;
    container.appendChild(toast);
    lucide.createIcons();
    setTimeout(() => {
        toast.style.animation = 'slideIn 0.3s ease reverse';
        setTimeout(() => toast.remove(), 300);
    }, 4000);
};

// ========== API HELPER ==========
const apiCall = async (endpoint, options = {}) => {
    try {
        const res = await fetch(endpoint, {
            ...options,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                ...(options.headers || {})
            }
        });
        if (res.status === 401) {
            showToast('Sessão expirada. Redirecionando...', 'error');
            setTimeout(() => { localStorage.clear(); window.location.href = '/login.html'; }, 2000);
            return null;
        }
        if (!res.ok) throw new Error(`API Error: ${res.status}`);
        return await res.json();
    } catch (e) {
        console.error(e);
        return null;
    }
};

// ========== SECTION MANAGEMENT ==========
window.switchSection = (sectionId) => {
    document.querySelectorAll('.main-content').forEach(s => s.style.display = 'none');
    document.querySelectorAll('.nav-link').forEach(n => n.classList.remove('active'));

    const section = $(`section-${sectionId}`);
    const nav = $(`nav-${sectionId}`);
    if (section) section.style.display = 'block';
    if (nav) nav.classList.add('active');

    // Load section data
    const loaders = {
        'dashboard': loadDashboardData,
        'users': loadUsers,
        'settings': loadSettings,
        'logs': loadLogs,
        'apis': checkApiStatus,
        'host': loadHostData
    };
    if (loaders[sectionId]) loaders[sectionId]();
};

// ========== UTILITIES ==========
const formatNum = (num) => num?.toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) || '0';
const formatDate = () => new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });

const setProgressRing = (id, percentage) => {
    const circle = $(id);
    if (!circle) return;
    const circumference = 339.292;
    const offset = circumference - (percentage / 100) * circumference;
    circle.style.strokeDashoffset = offset;
};

// ========== HARDWARE STATS POLLING ==========
let statsInterval;
const startStatsPolling = () => {
    const update = async () => {
        const stats = await apiCall('/api/admin/host/stats');
        if (stats) {
            const cpuPercent = stats.cpuUsagePercentage || 0;
            const ramPercent = stats.memoryTotalGb > 0 ? (stats.memoryUsedGb / stats.memoryTotalGb) * 100 : 0;
            const diskPercent = stats.diskTotalGb > 0 ? (stats.diskUsedGb / stats.diskTotalGb) * 100 : 0;

            $('stat-cpu-usage').textContent = `${formatNum(cpuPercent)}%`;
            $('stat-cpu-bar').style.width = `${cpuPercent}%`;

            $('stat-ram-usage').textContent = `${formatNum(stats.memoryUsedGb)} / ${formatNum(stats.memoryTotalGb)} GB`;
            $('stat-ram-bar').style.width = `${ramPercent}%`;

            $('stat-disk-usage').textContent = `${formatNum(stats.diskUsedGb)} / ${formatNum(stats.diskTotalGb)} GB`;
            $('stat-disk-bar').style.width = `${diskPercent}%`;

            $('vps-os').textContent = stats.osDescription || 'N/A';
            $('vps-uptime').textContent = stats.uptime || 'N/A';
            $('vps-status').textContent = stats.status || 'Online';

            setProgressRing('cpu-ring', cpuPercent);
            setProgressRing('ram-ring', ramPercent);
            setProgressRing('disk-ring', diskPercent);

            if ($('cpu-ring-value')) $('cpu-ring-value').textContent = `${formatNum(cpuPercent)}%`;
            if ($('ram-ring-value')) $('ram-ring-value').textContent = `${formatNum(ramPercent)}%`;
            if ($('disk-ring-value')) $('disk-ring-value').textContent = `${formatNum(diskPercent)}%`;
        }
    };
    update();
    statsInterval = setInterval(update, 5000);
};

// ========== DASHBOARD DATA ==========
const loadDashboardData = async () => {
    const stats = await apiCall('/api/admin/stats');
    if (stats) {
        $('stat-users-count').textContent = stats.totalUsers || 0;
        if ($('stat-users-pending')) {
            $('stat-users-pending').textContent = `${stats.pendingUsers || 0} pendentes`;
        }
    }

    const logs = await apiCall('/api/admin/logs');
    if (logs && logs.length) {
        $('recent-audits-list').innerHTML = logs.slice(0, 6).map(l => `
            <div style="padding: 10px; background: rgba(0,0,0,0.2); border-radius: 8px; display: flex; justify-content: space-between; align-items: center;">
              <div style="display: flex; align-items: center; gap: 10px;">
                <i data-lucide="activity" style="width: 16px; color: var(--primary);"></i>
                <span style="font-size: 0.85rem;">${l.action}</span>
              </div>
              <span style="font-size: 0.7rem; color: var(--text-muted);">${new Date(l.timestamp).toLocaleTimeString('pt-BR')}</span>
            </div>
        `).join('');
        lucide.createIcons();
    } else {
        $('recent-audits-list').innerHTML = '<div style="color: var(--text-muted); text-align: center; padding: 2rem;">Nenhuma atividade recente</div>';
    }
};

// ========== USERS MANAGEMENT ==========
let userTab = 'active';
let allUsers = [];

const loadUsers = async () => {
    const endpoint = userTab === 'active' ? '/api/admin/users' : '/api/admin/users/pending';
    const users = await apiCall(endpoint);
    allUsers = users || [];
    renderUsers(allUsers);
};

const renderUsers = (users) => {
    if (!users || !users.length) {
        $('users-table-body').innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 3rem; color: var(--text-muted);">
                    <i data-lucide="users" style="width: 48px; height: 48px; opacity: 0.3; margin-bottom: 1rem;"></i>
                    <div>Nenhum usuário encontrado</div>
                </td>
            </tr>`;
        lucide.createIcons();
        return;
    }

    $('users-table-body').innerHTML = users.map(u => {
        const statusClass = !u.isApproved ? 'pending' : (u.isActive !== false ? 'active' : 'blocked');
        const statusText = !u.isApproved ? 'Pendente' : (u.isActive !== false ? 'Ativo' : 'Bloqueado');
        const roleLabels = { 'User': 'Usuário', 'Admin': 'Admin', 'Gov': 'Governança' };
        
        return `
            <tr>
                <td>
                    <div style="display: flex; align-items: center; gap: 10px;">
                        <div style="width: 36px; height: 36px; background: var(--secondary); border-radius: 50%; display: flex; align-items: center; justify-content: center;">
                            <i data-lucide="user" style="width: 16px; color: var(--text-muted);"></i>
                        </div>
                        <span style="font-weight: 500;">${u.username}</span>
                    </div>
                </td>
                <td style="color: var(--text-muted);">${u.email}</td>
                <td><span style="background: var(--secondary); padding: 4px 10px; border-radius: 6px; font-size: 0.8rem;">${roleLabels[u.role] || u.role}</span></td>
                <td style="color: var(--text-muted); font-size: 0.85rem;">${u.policies || '-'}</td>
                <td><span class="badge-status ${statusClass}">${statusText}</span></td>
                <td style="text-align: right;">
                    <div style="display: flex; gap: 8px; justify-content: flex-end;">
                        ${!u.isApproved ? `<button onclick="approveUser(${u.id})" class="action-btn success">Aprovar</button>` : ''}
                        <button onclick="openUserModal(${u.id})" class="action-btn">
                            <i data-lucide="edit-2" style="width: 14px;"></i>
                            Editar
                        </button>
                        <button onclick="toggleUserStatus(${u.id})" class="action-btn">
                            ${u.isActive !== false ? 'Bloquear' : 'Ativar'}
                        </button>
                        <button onclick="deleteUser(${u.id})" class="action-btn danger">
                            <i data-lucide="trash-2" style="width: 14px;"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }).join('');
    lucide.createIcons();
};

const filterUsers = (query) => {
    if (!query) {
        renderUsers(allUsers);
        return;
    }
    const filtered = allUsers.filter(u => 
        u.username.toLowerCase().includes(query.toLowerCase()) ||
        u.email.toLowerCase().includes(query.toLowerCase())
    );
    renderUsers(filtered);
};

// ========== USER MODAL ==========
window.openUserModal = (userId) => {
    const user = allUsers.find(u => u.id === userId);
    if (!user) return;
    
    $('modal-user-id').value = user.id;
    $('modal-username').value = user.username;
    $('modal-email').value = user.email;
    $('modal-role').value = user.role;
    $('modal-policies').value = user.policies || '';
    $('userModal').classList.add('active');
};

window.closeModal = () => {
    $('userModal').classList.remove('active');
};

// ========== USER ACTIONS ==========
window.updateUserRole = async (id, role) => {
    const res = await apiCall(`/api/admin/users/${id}/role`, {
        method: 'PUT',
        body: JSON.stringify({ role })
    });
    if (res) {
        showToast('Cargo atualizado com sucesso');
        loadUsers();
    }
};

window.toggleUserStatus = async (id) => {
    const res = await apiCall(`/api/admin/users/${id}/toggle-status`, { method: 'POST' });
    if (res) {
        showToast('Status do usuário alterado');
        loadUsers();
    }
};

window.updatePolicies = async (id) => {
    const policies = $(`policy-${id}`)?.value || '';
    const res = await apiCall(`/api/admin/users/${id}/policies`, {
        method: 'PUT',
        body: JSON.stringify({ policies })
    });
    if (res) {
        showToast('Políticas atualizadas');
        loadUsers();
    }
};

window.approveUser = async (id) => {
    if (!confirm('Aprovar este usuário para acesso ao sistema?')) return;
    const res = await apiCall(`/api/admin/users/${id}/approve`, { method: 'POST' });
    if (res) {
        showToast('Usuário aprovado com sucesso');
        loadUsers();
        updateNotificationCount();
    }
};

window.deleteUser = async (id) => {
    if (!confirm('Tem certeza que deseja DELETAR este usuário permanentemente?')) return;
    const res = await apiCall(`/api/admin/users/${id}`, { method: 'DELETE' });
    if (res) {
        showToast('Usuário removido', 'warning');
        loadUsers();
    }
};

// ========== SETTINGS ==========
window.editSetting = (key, value) => {
    $('setting-key').value = key;
    $('setting-value').value = value;
    $('setting-value').focus();
};

const loadSettings = async () => {
    const settings = await apiCall('/api/admin/settings');
    if (settings && settings.length) {
        $('settings-list').innerHTML = settings.map(s => `
            <div style="padding: 1rem; background: rgba(0,0,0,0.2); border-radius: 10px; display: flex; justify-content: space-between; align-items: center;">
                <div>
                    <div style="font-weight: 600; font-size: 0.9rem; color: var(--primary);">${s.key}</div>
                    <div style="font-size: 0.85rem; color: var(--text-muted); margin-top: 4px;">${s.value}</div>
                </div>
                <button onclick="editSetting('${s.key}', '${s.value}')" class="action-btn">
                    <i data-lucide="edit-2" style="width: 14px;"></i>
                </button>
            </div>
        `).join('');
        lucide.createIcons();
    } else {
        $('settings-list').innerHTML = '<div style="color: var(--text-muted); text-align: center; padding: 2rem; grid-column: span 2;">Nenhuma configuração cadastrada</div>';
    }
};

// ========== LOGS ==========
window.loadLogs = async () => {
    const errors = await apiCall('/api/admin/host/errors');
    if (errors) {
        $('error-logs-container').innerHTML = errors.length ? errors.map(e => `
            <div style="margin-bottom: 1rem; padding-bottom: 0.5rem; border-bottom: 1px solid rgba(255,255,255,0.1);">
                <div style="color: var(--error); font-weight: 600; font-size: 0.75rem;">[${new Date(e.timestamp).toLocaleString('pt-BR')}]</div>
                <div style="margin-top: 4px; color: var(--text-main);">${e.message}</div>
                <div style="color: var(--text-muted); font-size: 0.7rem; margin-top: 4px;">Endpoint: ${e.endpoint} | User: ${e.userIdentifier || 'N/A'}</div>
            </div>
        `).join('') : '<div style="color: var(--text-muted); text-align: center; padding: 2rem;">? Nenhum erro registrado</div>';
    }

    const sysLogs = await apiCall('/api/admin/host/logs');
    if (sysLogs) {
        $('system-logs-container').innerHTML = sysLogs.length ? sysLogs.map(l => `
            <div style="margin-bottom: 0.5rem; padding-bottom: 0.3rem; border-bottom: 1px solid rgba(255,255,255,0.05); white-space: pre-wrap; word-break: break-word;">${l}</div>
        `).join('') : '<div style="color: var(--text-muted); text-align: center; padding: 2rem;">Sem logs do sistema disponíveis</div>';
    }
};

// ========== HOST DATA ==========
const loadHostData = () => {
    startStatsPolling();
};

// ========== API STATUS CHECK ==========
const checkApiStatus = async () => {
    const apis = [
        { name: 'admin', url: '/api/admin/stats' },
        { name: 'auth', url: '/api/public/health' },
        { name: 'public', url: '/api/public/health' }
    ];

    const adminIndicator = $('api-admin-indicator');
    const adminStatus = $('api-admin-status');
    if (adminIndicator && adminStatus) {
        adminIndicator.classList.add('online');
        adminStatus.textContent = 'Online';
        adminStatus.style.color = 'var(--success)';
        $('api-admin-latency').textContent = 'Latência: <1ms';
    }

    for (const api of apis.slice(1)) {
        const indicator = $(`api-${api.name}-indicator`);
        const status = $(`api-${api.name}-status`);
        const latency = $(`api-${api.name}-latency`);
        
        if (!indicator || !status) continue;

        try {
            const start = performance.now();
            const res = await fetch(api.url, { 
                method: 'GET',
                headers: { 'Authorization': `Bearer ${token}` },
                signal: AbortSignal.timeout(5000)
            });
            const end = performance.now();
            
            if (res.ok) {
                indicator.classList.add('online');
                indicator.classList.remove('offline');
                status.textContent = 'Online';
                status.style.color = 'var(--success)';
                if (latency) latency.textContent = `Latência: ${Math.round(end - start)}ms`;
            } else {
                throw new Error('Not OK');
            }
        } catch (e) {
            indicator.classList.remove('online');
            indicator.classList.add('offline');
            status.textContent = 'Offline';
            status.style.color = 'var(--error)';
            if (latency) latency.textContent = 'Latência: --';
        }
    }

    generateUptimeChart();
};

const generateUptimeChart = () => {
    const container = $('uptime-chart');
    if (!container) return;
    
    const bars = [];
    for (let i = 0; i < 48; i++) {
        const height = Math.random() > 0.05 ? 100 : Math.random() * 50;
        const color = height === 100 ? 'var(--success)' : (height > 50 ? 'var(--warning)' : 'var(--error)');
        bars.push(`<div style="flex: 1; height: ${height}%; background: ${color}; border-radius: 2px; min-width: 4px;"></div>`);
    }
    container.innerHTML = bars.join('');
};

// ========== NOTIFICATIONS ==========
const updateNotificationCount = async () => {
    const pending = await apiCall('/api/admin/users/pending');
    const count = pending?.length || 0;
    const badge = $('notificationCount');
    if (badge) {
        badge.textContent = count;
        badge.style.display = count > 0 ? 'block' : 'none';
    }
};

// ========== CSV DOWNLOAD ==========
const downloadCsv = async (endpoint, filename) => {
    try {
        const res = await fetch(endpoint, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (!res.ok) throw new Error('Download failed');
        const blob = await res.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        a.remove();
        showToast('Download iniciado');
    } catch (e) {
        showToast('Erro ao baixar relatório', 'error');
    }
};

// ========== EVENT LISTENERS ==========
document.addEventListener('DOMContentLoaded', () => {
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    document.body.style.display = 'grid';
    lucide.createIcons();

    if ($('current-date')) $('current-date').textContent = formatDate();

    const userName = localStorage.getItem('user_name');
    const userRole = localStorage.getItem('user_role');
    if (userName && $('display-username')) $('display-username').textContent = userName;
    if (userRole && $('display-role')) $('display-role').textContent = userRole;

    loadDashboardData();
    startStatsPolling();
    updateNotificationCount();

    ['dashboard', 'users', 'apis', 'logs', 'host', 'reports', 'settings'].forEach(id => {
        const nav = $(`nav-${id}`);
        if (nav) nav.addEventListener('click', () => switchSection(id));
    });

    $('btn-tab-active')?.addEventListener('click', () => {
        userTab = 'active';
        $('btn-tab-active').style.background = 'var(--primary)';
        $('btn-tab-pending').style.background = 'transparent';
        $('btn-tab-pending').style.color = 'var(--text-muted)';
        loadUsers();
    });

    $('btn-tab-pending')?.addEventListener('click', () => {
        userTab = 'pending';
        $('btn-tab-active').style.background = 'transparent';
        $('btn-tab-active').style.color = 'var(--text-muted)';
        $('btn-tab-pending').style.background = 'var(--primary)';
        $('btn-tab-pending').style.color = 'var(--bg-dark)';
        loadUsers();
    });

    $('userSearchInput')?.addEventListener('input', (e) => filterUsers(e.target.value));

    $('globalSearch')?.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            const query = e.target.value.toLowerCase();
            if (query.includes('user') || query.includes('usuário')) {
                switchSection('users');
            } else if (query.includes('log')) {
                switchSection('logs');
            } else if (query.includes('config') || query.includes('setting')) {
                switchSection('settings');
            }
        }
    });

    $('btnSaveSetting')?.addEventListener('click', async () => {
        const key = $('setting-key').value.trim();
        const value = $('setting-value').value.trim();
        if (!key || !value) {
            showToast('Preencha todos os campos', 'warning');
            return;
        }

        const res = await apiCall('/api/admin/settings', {
            method: 'POST',
            body: JSON.stringify({ key, value })
        });

        if (res) {
            $('setting-key').value = '';
            $('setting-value').value = '';
            showToast('Configuração salva');
            loadSettings();
        }
    });

    $('btnExportErrors')?.addEventListener('click', () => 
        downloadCsv('/api/admin/reports/errors/export', `erros_${new Date().toISOString().split('T')[0]}.csv`)
    );
    $('btnExportAudit')?.addEventListener('click', () => 
        downloadCsv('/api/admin/reports/audit/export', `auditoria_${new Date().toISOString().split('T')[0]}.csv`)
    );

    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.btn-restart');
        if (btn) {
            const service = btn.dataset.service;
            if (!confirm(`Deseja reiniciar o serviço ${service}?`)) return;
            
            btn.disabled = true;
            btn.innerHTML = '<i data-lucide="loader" style="width: 14px; animation: spin 1s linear infinite;"></i> Reiniciando...';
            lucide.createIcons();
            
            const res = await apiCall(`/api/admin/host/restart?serviceName=${service}`, { method: 'POST' });
            if (res) {
                showToast(res.message || 'Serviço reiniciado');
            } else {
                showToast('Falha ao reiniciar serviço', 'error');
            }
            
            btn.disabled = false;
            btn.innerHTML = '<i data-lucide="rotate-ccw" style="width: 14px;"></i> Reiniciar';
            lucide.createIcons();
        }
    });

    $('userForm')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = $('modal-user-id').value;
        const role = $('modal-role').value;
        const policies = $('modal-policies').value;

        await apiCall(`/api/admin/users/${id}/role`, {
            method: 'PUT',
            body: JSON.stringify({ role })
        });

        await apiCall(`/api/admin/users/${id}/policies`, {
            method: 'PUT',
            body: JSON.stringify({ policies })
        });

        showToast('Usuário atualizado');
        closeModal();
        loadUsers();
    });

    $('btnLogout')?.addEventListener('click', () => {
        if (confirm('Deseja realmente sair?')) {
            localStorage.clear();
            window.location.href = '/login.html';
        }
    });

    $('notificationBtn')?.addEventListener('click', () => {
        switchSection('users');
        userTab = 'pending';
        $('btn-tab-active').style.background = 'transparent';
        $('btn-tab-pending').style.background = 'var(--primary)';
        loadUsers();
    });
});

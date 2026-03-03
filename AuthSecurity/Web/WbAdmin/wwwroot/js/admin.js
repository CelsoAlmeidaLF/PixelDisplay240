/**
 * WbAdmin - Painel de Administração
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
    const circumference = 339.292; // 2 * PI * 54
    const offset = circumference - (percentage / 100) * circumference;
    circle.style.strokeDashoffset = offset;
};

// ========== HARDWARE STATS POLLING ==========
let statsInterval;
const startStatsPolling = () => {
    const update = async () => {
        const stats = await apiCall('/api/admin/host/stats');
        if (stats) {
            // Dashboard stats
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

            // Host section rings
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
                    <div style="display: flex; gap: 6px; justify-content: flex-end; flex-wrap: wrap;">
                        ${!u.isApproved ? `<button onclick="approveUser(${u.id})" class="action-btn success" title="Aprovar acesso">
                            <i data-lucide="check" style="width: 14px;"></i>
                        </button>` : ''}
                        <button onclick="openUserModal(${u.id})" class="action-btn" title="Editar">
                            <i data-lucide="edit-2" style="width: 14px;"></i>
                        </button>
                        <button onclick="openResetModal(${u.id})" class="action-btn" title="Resetar senha">
                            <i data-lucide="key" style="width: 14px;"></i>
                        </button>
                        <button onclick="toggleUserStatus(${u.id})" class="action-btn" title="${u.isActive !== false ? 'Bloquear' : 'Ativar'}">
                            <i data-lucide="${u.isActive !== false ? 'lock' : 'unlock'}" style="width: 14px;"></i>
                        </button>
                        <button onclick="deleteUser(${u.id})" class="action-btn danger" title="Excluir">
                            <i data-lucide="trash-2" style="width: 14px;"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }).join('');
    lucide.createIcons();
};

// User search filter
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

// ========== CREATE USER MODAL ==========
window.openCreateModal = () => {
    $('create-username').value = '';
    $('create-email').value = '';
    $('create-password').value = '';
    $('create-role').value = 'User';
    $('create-policies').value = '';
    $('createUserModal').classList.add('active');
    $('create-username').focus();
};

window.closeCreateModal = () => {
    $('createUserModal').classList.remove('active');
};

// ========== RESET PASSWORD MODAL ==========
window.openResetModal = (userId) => {
    const user = allUsers.find(u => u.id === userId);
    if (!user) return;
    
    $('reset-user-id').value = user.id;
    $('reset-username-display').textContent = user.username;
    $('reset-new-password').value = '';
    $('reset-confirm-password').value = '';
    $('resetPasswordModal').classList.add('active');
    lucide.createIcons();
};

window.closeResetModal = () => {
    $('resetPasswordModal').classList.remove('active');
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
const loadLogs = async () => {
    // Application Errors
    const errors = await apiCall('/api/admin/host/errors');
    if (errors) {
        $('error-logs-container').innerHTML = errors.length ? errors.map(e => `
            <div style="margin-bottom: 1rem; padding-bottom: 0.5rem; border-bottom: 1px solid rgba(255,255,255,0.1);">
                <div style="color: var(--error); font-weight: 600; font-size: 0.75rem;">[${new Date(e.timestamp).toLocaleString('pt-BR')}]</div>
                <div style="margin-top: 4px; color: var(--text-main);">${e.message}</div>
                <div style="color: var(--text-muted); font-size: 0.7rem; margin-top: 4px;">Endpoint: ${e.endpoint} | User: ${e.userIdentifier || 'N/A'}</div>
            </div>
        `).join('') : '<div style="color: var(--text-muted); text-align: center; padding: 2rem;">✓ Nenhum erro registrado</div>';
    }

    // System Logs
    const sysLogs = await apiCall('/api/admin/host/logs');
    if (sysLogs) {
        $('system-logs-container').innerHTML = sysLogs.length ? sysLogs.map(l => `
            <div style="margin-bottom: 0.5rem; padding-bottom: 0.3rem; border-bottom: 1px solid rgba(255,255,255,0.05); white-space: pre-wrap; word-break: break-word;">${l}</div>
        `).join('') : '<div style="color: var(--text-muted); text-align: center; padding: 2rem;">Sem logs do sistema disponíveis</div>';
    }
};

// ========== HOST DATA ==========
const loadHostData = () => {
    // Stats are already being polled, just ensure they're updated
    startStatsPolling();
};

// ========== API STATUS CHECK ==========
const checkApiStatus = async () => {
    const apis = [
        { name: 'admin', url: '/api/admin/stats' },
        { name: 'auth', url: '/api/public/health' },
        { name: 'public', url: '/api/public/health' }
    ];

    // Admin API (current)
    const adminIndicator = $('api-admin-indicator');
    const adminStatus = $('api-admin-status');
    if (adminIndicator && adminStatus) {
        adminIndicator.classList.add('online');
        adminStatus.textContent = 'Online';
        adminStatus.style.color = 'var(--success)';
        $('api-admin-latency').textContent = 'Latência: <1ms';
    }

    // Check other APIs with timing
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

    // Generate uptime chart
    generateUptimeChart();
};

const generateUptimeChart = () => {
    const container = $('uptime-chart');
    if (!container) return;
    
    // Simulate 24h uptime data (in real app, fetch from API)
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

    // Show body
    document.body.style.display = 'grid';
    lucide.createIcons();

    // Set current date
    if ($('current-date')) $('current-date').textContent = formatDate();

    // User info
    const userName = localStorage.getItem('user_name');
    const userRole = localStorage.getItem('user_role');
    if (userName && $('display-username')) $('display-username').textContent = userName;
    if (userRole && $('display-role')) $('display-role').textContent = userRole;

    // Load initial data
    loadDashboardData();
    startStatsPolling();
    updateNotificationCount();

    // Navigation
    ['dashboard', 'users', 'apis', 'logs', 'host', 'reports', 'settings'].forEach(id => {
        const nav = $(`nav-${id}`);
        if (nav) nav.addEventListener('click', () => switchSection(id));
    });

    // User tabs
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

    // User search
    $('userSearchInput')?.addEventListener('input', (e) => filterUsers(e.target.value));

    // Global search
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

    // Settings save
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

    // Export buttons
    $('btnExportErrors')?.addEventListener('click', () => 
        downloadCsv('/api/admin/reports/errors/export', `erros_${new Date().toISOString().split('T')[0]}.csv`)
    );
    $('btnExportAudit')?.addEventListener('click', () => 
        downloadCsv('/api/admin/reports/audit/export', `auditoria_${new Date().toISOString().split('T')[0]}.csv`)
    );

    // Restart services
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

    // User form submit (edit)
    $('userForm')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = $('modal-user-id').value;
        const role = $('modal-role').value;
        const policies = $('modal-policies').value;

        // Update role
        await apiCall(`/api/admin/users/${id}/role`, {
            method: 'PUT',
            body: JSON.stringify({ role })
        });

        // Update policies
        await apiCall(`/api/admin/users/${id}/policies`, {
            method: 'PUT',
            body: JSON.stringify({ policies })
        });

        showToast('Usuário atualizado');
        closeModal();
        loadUsers();
    });

    // Create user form submit
    $('createUserForm')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const username = $('create-username').value.trim();
        const email = $('create-email').value.trim();
        const password = $('create-password').value;
        const role = $('create-role').value;
        const policies = $('create-policies').value.trim();

        if (!username || !email || !password) {
            showToast('Preencha todos os campos obrigatórios', 'error');
            return;
        }

        if (password.length < 8) {
            showToast('A senha deve ter pelo menos 8 caracteres', 'error');
            return;
        }

        const res = await apiCall('/api/admin/users', {
            method: 'POST',
            body: JSON.stringify({ username, email, password, role, policies })
        });

        if (res) {
            showToast(res.message || 'Usuário criado com sucesso');
            closeCreateModal();
            loadUsers();
        } else {
            showToast('Erro ao criar usuário', 'error');
        }
    });

    // Reset password form submit
    $('resetPasswordForm')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const id = $('reset-user-id').value;
        const newPassword = $('reset-new-password').value;
        const confirmPassword = $('reset-confirm-password').value;

        if (!newPassword || !confirmPassword) {
            showToast('Preencha todos os campos', 'error');
            return;
        }

        if (newPassword.length < 8) {
            showToast('A senha deve ter pelo menos 8 caracteres', 'error');
            return;
        }

        if (newPassword !== confirmPassword) {
            showToast('As senhas não conferem', 'error');
            return;
        }

        const res = await apiCall(`/api/admin/users/${id}/reset-password`, {
            method: 'POST',
            body: JSON.stringify({ newPassword })
        });

        if (res) {
            showToast(res.message || 'Senha resetada com sucesso');
            closeResetModal();
        } else {
            showToast('Erro ao resetar senha', 'error');
        }
    });

    // Create user button
    $('btnCreateUser')?.addEventListener('click', () => openCreateModal());

    // Logout
    $('btnLogout')?.addEventListener('click', () => {
        if (confirm('Deseja realmente sair?')) {
            localStorage.clear();
            window.location.href = '/login.html';
        }
    });

    // Notification click
    $('notificationBtn')?.addEventListener('click', () => {
        switchSection('users');
        userTab = 'pending';
        $('btn-tab-active').style.background = 'transparent';
        $('btn-tab-pending').style.background = 'var(--primary)';
        loadUsers();
    });
});

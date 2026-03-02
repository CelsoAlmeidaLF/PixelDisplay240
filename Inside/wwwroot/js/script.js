/**
 * PixelDisplay240 - IDE Runner / Scripting Entry Point
 * Orchestrates Theme, Managers and Prototype MVC
 */

// ========== Theme Manager ==========
class ThemeManager {
    constructor() {
        this.presets = {
            gameboy: ['#0f380f', '#306230', '#8bac0f', '#9bbc0f'],
            pico8: ['#000000', '#1D2B53', '#7E2553', '#008751', '#AB5236', '#5F574F', '#C2C3C7', '#FFF1E8', '#FF004D', '#FFA300', '#FFEC27', '#00E436', '#29ADFF', '#83769C', '#FF77A8', '#FFCCAA'],
            nes: ['#7C7C7C', '#0000FC', '#0000BC', '#4428BC', '#940084', '#A80020', '#A81000', '#881400', '#503000', '#007800', '#006800', '#0058F8', '#004058', '#000000']
        };
        this.themes = {
            default: { bg: '#0f172a', card: '#1e293b', primary: '#38bdf8', border: '#334155' },
            dracula: { bg: '#282a36', card: '#44475a', primary: '#bd93f9', border: '#6272a4' },
            nord: { bg: '#2e3440', card: '#3b4252', primary: '#88c0d0', border: '#4c566a' },
            monokai: { bg: '#272822', card: '#3e3d32', primary: '#f92672', border: '#75715e' }
        };
    }

    setTheme(id) {
        const t = this.themes[id];
        if (!t) return;
        const root = document.documentElement.style;
        root.setProperty('--bg-dark', t.bg);
        root.setProperty('--card-bg', t.card);
        root.setProperty('--primary', t.primary);
        root.setProperty('--border', t.border);
    }
}

// ========== Navigation Manager ==========
class NavigationManager {
    constructor(app) {
        this.app = app;
        this.panels = {
            design: document.getElementById('design-view'),
            prototype: document.getElementById('prototype-view')
        };
        this.btns = {
            design: document.getElementById('nav-design'),
            prototype: document.getElementById('nav-prototype')
        };
        this._setup();
    }

    _setup() {
        Object.keys(this.btns).forEach(id => {
            if (this.btns[id]) {
                this.btns[id].onclick = () => this.switch(id);
            }
        });
    }

    switch(viewId) {
        Object.keys(this.panels).forEach(k => {
            if (this.panels[k]) this.panels[k].classList.toggle('active', k === viewId);
            if (this.btns[k]) this.btns[k].classList.toggle('active', k === viewId);
        });

        if (viewId === 'prototype' && this.app.prototype) {
            this.app.prototype.refresh();
            if (this.app.prototype.view && this.app.prototype.view.editor) {
                setTimeout(() => this.app.prototype.view.editor.layout(), 10);
            }
        }

        if (window.lucide) window.lucide.createIcons();
    }
}

// ========== API Client ==========
class ApiClient {
    constructor() {
        this.tokenKey = 'pixeldisplay240_jwt';
        this.token = null;
    }

    async init() {
        this.token = localStorage.getItem(this.tokenKey) || '';
        if (!this.token || !this._isTokenValid(this.token)) {
            await this._fetchToken();
        }
    }

    _isTokenValid(token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1] || ''));
            const exp = payload.exp ? payload.exp * 1000 : 0;
            return exp > Date.now() + 60000;
        } catch {
            return false;
        }
    }

    async _fetchToken() {
        try {
            const resp = await fetch('/api/auth/token', { method: 'GET' });
            if (resp.ok) {
                const data = await resp.json();
                if (data?.token) {
                    this.token = data.token;
                    localStorage.setItem(this.tokenKey, data.token);
                }
            }
        } catch (e) {
            console.warn('Token fetch failed:', e);
        }
    }

    async request(url, options = {}) {
        if (!this.token || !this._isTokenValid(this.token)) {
            await this._fetchToken();
        }
        const headers = new Headers(options.headers || {});
        if (this.token) {
            headers.set('Authorization', `Bearer ${this.token}`);
        }
        return fetch(url, { ...options, headers });
    }
}

// ========== Status Manager ==========
class StatusManager {
    constructor() {
        this.statusEl = document.getElementById('status-view');
        this.timeEl = document.getElementById('status-time');
    }

    refreshMetrics() {
        if (this.timeEl) {
            this.timeEl.textContent = new Date().toLocaleTimeString();
        }
    }

    update(viewName) {
        if (this.statusEl) this.statusEl.textContent = viewName;
        this.refreshMetrics();
    }
}

// ========== Main App ==========
class PixelDisplay240App {
    constructor() {
        // Core Managers
        this.theme = new ThemeManager();
        this.nav = new NavigationManager(this);
        this.status = new StatusManager();
        this.currentTool = 'brush'; // Default tool

        // Prototype MVC
        this.model = new PrototypeModel();
        this.view = new PrototypeView(null);
        this.prototype = new PrototypeController(this.model, this.view);

        // Expose to view
        this.view.app = this;

        // Designer Modules (Image Editor)
        if (typeof LayerManager !== 'undefined') {
            this.layers = new LayerManager(this);
            this.drawing = new DrawingManager(this);
            this.layout = new LayoutManager(this);
            this.ai = new AIPixelArtGenerator(this);
        }

        // Toolbars
        if (typeof TftCommandToolbar !== 'undefined') {
            this.tftToolbar = new TftCommandToolbar('proto-tft-toolbar');
        }

        this.init();
    }

    async init() {
        console.log("PixelDisplay240 IDE Initializing...");

        // 1. Initialize API and Auth
        this.api = new ApiClient();
        await this.api.init();

        // Link API to view now that it's ready
        this.view.setApiClient(this.api);

        // 2. Load Modules
        await this.prototype.init(this);
        
        // Initialize Designer Default Layer if empty
        if (this.layers && this.layers.layers.length === 0) {
            this.layers.add('Fundo', null, true);
        }

        if (this.tftToolbar) {
            this.tftToolbar.init(this.view.editor || this.view.dom.codePreview);
        }

        // 3. UI Setup
        this.setupEventListeners();

        // 4. Finalize
        if (window.lucide) window.lucide.createIcons();
        this.hideLoading();
    }

    setupEventListeners() {
        // Global Keybindings
        window.addEventListener('keydown', (e) => {
            if (e.ctrlKey && e.key === 's') {
                e.preventDefault();
                this.prototype.view.onProjectSave?.();
            }
            if (e.ctrlKey && e.key === 'z') {
                e.preventDefault();
                this.prototype.undo?.();
            }
            if (e.ctrlKey && e.key === 'y') {
                e.preventDefault();
                this.prototype.redo?.();
            }
        });

        // Tool Switching Logic
        document.querySelectorAll('.tool-btn').forEach(btn => {
            if (btn.id.startsWith('tool-')) {
                btn.onclick = (e) => {
                    const id = btn.id;
                    const toolName = id.replace('tool-', '');

                    // Actions that are not tools
                    if (['ai-gen', 'clear', 'font-gen'].includes(toolName)) {
                         this.handleToolbarAction(toolName);
                         return;
                    }

                    // Set Active Tool
                    this.currentTool = toolName;
                    
                    // Update UI
                    document.querySelectorAll('.tool-btn').forEach(b => b.classList.remove('active'));
                    btn.classList.add('active');
                };
            }
        });

        // Tab Switching
        const tabs = document.querySelectorAll('.nav-tab');
        tabs.forEach(tab => {
            tab.onclick = () => {
                const target = tab.dataset.target;
                this.nav.switch(target);
                if (target === 'prototype') this.prototype.refresh();
            };
        });
    }

    handleToolbarAction(action) {
        if (action === 'clear') {
            if (confirm('Limpar camada atual?')) {
                const layer = this.layers.active;
                if (layer) {
                    layer.ctx.clearRect(0, 0, layer.canvas.width, layer.canvas.height);
                    this.refresh();
                }
            }
        } else if (action === 'ai-gen') {
            const modal = document.getElementById('ai-modal');
            if (modal) modal.style.display = 'flex';
        }
    }

    refresh() {
        // Trigger a redraw of the active layer stack
        if (this.layers) {
            const tempCtx = this.drawing?.tempCtx; 
            const canvasWrapper = document.getElementById('canvas-wrapper');
            // Normally the layers are just canvases stacked. 
            // We might need to force update if we are using a composite canvas, 
            // but here we use DOM stacking (absolute positioning).
            // So just clearing one canvas reflects immediately.
            // But we might want to update preview.
        }
    }
}

// Ensure the app starts after DOM is ready
const startApp = () => {
    // Only start if not already started
    if (window.app) return;
    
    // Ensure dependencies from other files are available
    if (typeof PrototypeModel === 'undefined' || typeof PrototypeView === 'undefined') {
        console.warn('Waiting for dependencies...');
        setTimeout(startApp, 100);
        return;
    }

    try {
        window.app = new PixelDisplay240App();
    } catch (e) {
        console.error("Critical: Failed to start PixelDisplay240App", e);
        document.getElementById('loading-status').innerHTML = '<span style="color:red">Falha na inicialização do App.</span>';
    }
};

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', startApp);
} else {
    startApp();
}

if (window.PixelDisplay240System) window.PixelDisplay240System.register('main', '5.1.5');

/**
 * PixelDisplay240 System Runtime & Module Orchestrator v5.1.1
 * Centralizes the loading, versioning and integrity of the IDE modules.
 */

(function () {
    // Detect base URL from this script's location
    const scriptTag = document.currentScript;
    const runtimeUrl = scriptTag ? scriptTag.src : '';
    const autoBaseUrl = runtimeUrl.substring(0, runtimeUrl.lastIndexOf('/') + 1) || '/js/';

    window.PixelDisplay240System = {
        version: '5.1.0',
        config: {
            baseUrl: autoBaseUrl,
            timeout: 12000,
            cacheBust: true
        },

        // Manifest of active modules in the system
        // IMPORTANT: The order here defines the execution order if loaded as async=false
        manifest: [
            { id: 'model', file: 'model.js', description: 'Núcleo de Dados' },
            { id: 'tft', file: 'tft-commands.js', description: 'Catálogo de Comandos' },
            { id: 'view', file: 'view.js', description: 'Motor de Renderização' },
            { id: 'controller', file: 'controller.js', description: 'Lógica de Interface' },
            { id: 'designer', file: 'designer.js', description: 'Ferramentas de Design' },
            { id: 'main', file: 'script.js', description: 'Orquestrador Principal' }
        ],

        modules: {},
        loadedCount: 0,
        startTime: Date.now(),
        isBooted: false,

        /**
         * Entry point: Initializes the loading process
         */
        boot: function () {
            if (this.isBooted) return;
            this.isBooted = true;

            console.log(`%c[PixelDisplay240] Runtime Booting from: ${this.config.baseUrl}`, 'color: #38bdf8; font-weight: bold;');

            this.updateStatus('Iniciando orquestrador...');

            // Load modules sequentially to avoid any race condition with async=false interpretation
            this.loadNext(0);

            // Fail-safe timeout
            setTimeout(() => this.checkTimeout(), this.config.timeout);
        },

        loadNext: function (index) {
            if (index >= this.manifest.length) return;

            const mod = this.manifest[index];
            const script = document.createElement('script');
            let url = this.config.baseUrl + mod.file;

            if (this.config.cacheBust) {
                url += `?v=${this.version}_${this.startTime}`;
            }

            script.src = url;
            script.async = false;

            script.onload = () => {
                // We don't call loadNext here because the scripts themselves 
                // must call register() which will be enough.
                // But if they don't call register(), the system hangs.
                // So we load them all "in order" using async=false.
                if (index < this.manifest.length - 1) {
                    // Try to load next one immediately to let browser parallelize the FETCH
                    // while maintaining execution order via async=false
                }
            };

            script.onerror = () => {
                this.reportError(mod.file, 'Falha no carregamento. Verifique se o arquivo existe em ' + url);
            };

            document.body.appendChild(script);

            // Trigger next immediately for parallel download (async=false handles execution order)
            this.loadNext(index + 1);
        },

        /**
         * Called by individual modules to register themselves
         */
        register: function (id, version) {
            console.log(`[System] Registered: ${id} v${version}`);
            this.modules[id] = version;
            this.loadedCount++;

            this.updateStatus(`Carregando módulos... (${this.loadedCount}/${this.manifest.length})`);

            if (this.loadedCount === this.manifest.length) {
                this.finalize();
            }
        },

        updateStatus: function (msg) {
            const el = document.getElementById('loading-status');
            if (el) el.textContent = msg;
            console.log(`[Status] ${msg}`);
        },

        reportError: function (file, msg) {
            console.error(`[System Error] ${file}: ${msg}`);
            const log = document.getElementById('version-check-log');
            if (log) {
                log.innerHTML += `<div style="color:#ef4444; margin-bottom:4px; font-size:0.7rem;"><b>${file}:</b> ${msg}</div>`;
            }
            const status = document.getElementById('loading-status');
            if (status) {
                status.textContent = "Erro no core JS.";
                status.style.color = "#ef4444";
            }
        },

        finalize: function () {
            // Final Integrity Check
            const missing = this.manifest.filter(m => !this.modules[m.id]);

            if (missing.length > 0) {
                this.reportError('Integridade', `Módulos ausentes: ${missing.map(m => m.id).join(', ')}`);
                return;
            }

            this.updateStatus('Interface Pronta.');

            const overlay = document.getElementById('app-loading-overlay');
            const container = document.querySelector('.app-container');

            if (overlay) {
                setTimeout(() => {
                    overlay.style.transition = 'opacity 0.5s ease';
                    overlay.style.opacity = '0';
                    overlay.style.pointerEvents = 'none';
                    if (container) {
                        container.style.transition = 'opacity 0.5s ease';
                        container.style.opacity = '1';
                    }
                    setTimeout(() => overlay.remove(), 500);
                }, 1000);
            }
        },

        checkTimeout: function () {
            if (this.loadedCount < this.manifest.length) {
                const missing = this.manifest.filter(m => !this.modules[m.id]);
                this.reportError('Timeout', `Carregamento incompleto. Travado em: ${missing.map(m => m.id).join(', ')}`);
            }
        }
    };

    // Auto-boot sequence
    if (document.readyState === 'loading') {
        window.addEventListener('DOMContentLoaded', () => window.PixelDisplay240System.boot());
    } else {
        window.PixelDisplay240System.boot();
    }

    // Safety check for script.js (main) - it might have run already if mistakenly included
    // or if the browser cached it differently.
})();

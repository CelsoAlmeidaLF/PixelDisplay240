/**
 * Prototype Controller - Orchestrates Model, View, and C# Backend
 */
class PrototypeController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
        this.app = null; // Will be set by script.js

        this.draggingElement = null;
        this.dragOffset = { x: 0, y: 0 };

        this.resizingElement = null;

        this.setupViewCallbacks();
    }

    // Called after app is set
    async init(app) {
        this.app = app;
        if (app.api) this.view.app = app; // Give view access to app/model
        await this.sync();
    }

    async sync() {
        if (!this.app || !this.app.api) return;
        this.view.setSyncStatus('saving');
        try {
            // Fetch project structure
            const resp = await this.app.api.request('/api/prototype');
            const data = await resp.json();
            this.model.updateFromData(data);

            this.view.render(this.model);
            this.view.setSyncStatus('synced');
        } catch (err) {
            console.error('Sync failed:', err);
            this.view.setSyncStatus('local'); // Trabalhando com cache local
        }
    }

    async callApi(path, method = 'POST', body = null) {
        // Obsoleto: Substituído por queueSave() para salvamento integral.
        // Mantido apenas para compatibilidade temporária se necessário.
        return this.queueSave();
    }

    /**
     * Sincroniza o estado total do projeto com o servidor.
     * Debounced para evitar excesso de requisições.
     */
    queueSave() {
        this.model.saveToLocal();
        this.view.render(this.model);
        this.view.setSyncStatus('saving');

        if (this._saveTimer) clearTimeout(this._saveTimer);
        this._saveTimer = setTimeout(async () => {
            if (!this.app || !this.app.api) return;
            try {
                const resp = await this.app.api.request('/api/prototype/save', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(this.model.getRawData())
                });
                if (resp.ok) {
                    const data = await resp.json();
                    // Atualiza IDs gerados pelo server ou sequenciadores
                    this.model.updateFromData(data);
                    this.view.render(this.model);
                    this.view.setSyncStatus('synced');
                } else {
                    throw new Error('Server error on save');
                }
            } catch (err) {
                console.error('Bulk save failed:', err);
                this.view.setSyncStatus('error');
            }
        }, 1000);
    }

    setupViewCallbacks() {
        // TELAS
        this.view.onAddScreen = (template) => {
            const id = `screen_${Date.now()}`;
            const newScreen = {
                id,
                name: `Tela_${this.model.screens.length + 1}`,
                elements: []
            };
            this.model.screens.push(newScreen);
            this.model.activeScreenId = id;
            this.queueSave();
        };

        this.view.onSelectScreen = (id) => {
            this.model.activeScreenId = id;
            this.model.selectedElementId = null;
            this.queueSave();
        };

        this.view.onDeleteScreen = (id) => {
            if (this.model.screens.length <= 1) return;
            this.model.screens = this.model.screens.filter(s => s.id !== id);
            if (this.model.activeScreenId === id) {
                this.model.activeScreenId = this.model.screens[0].id;
            }
            this.queueSave();
        };

        this.view.onReorderScreen = (id, newIndex) => {
            const idx = this.model.screens.findIndex(s => s.id === id);
            if (idx === -1) return;
            const screen = this.model.screens.splice(idx, 1)[0];
            const target = Math.max(0, Math.min(newIndex, this.model.screens.length));
            this.model.screens.splice(target, 0, screen);
            this.view._codeEditedManually = false;
            this.queueSave();
        };

        // ELEMENTOS
        this.view.onAddElement = (type, asset = null) => {
            const screen = this.model.activeScreen;
            if (!screen) return;
            const elId = `el_${Date.now()}`;
            const isCircle = type === 'circle';
            const el = {
                id: elId,
                type,
                name: `${type}_${screen.elements.length + 1}`,
                x: 10 + (screen.elements.length * 5),
                y: 10 + (screen.elements.length * 5),
                w: isCircle ? 60 : 80,
                h: isCircle ? 60 : 40,
                color: "#38bdf8",
                asset
            };
            screen.elements.push(el);
            this.model.selectedElementId = elId;
            this.queueSave();
        };

        this.view.onSelectElement = (id) => {
            this.model.selectedElementId = id;
            this.queueSave();
        };

        this.view.onElementDelete = (id) => {
            const screen = this.model.activeScreen;
            if (!screen) return;
            screen.elements = screen.elements.filter(e => e.id !== id);
            if (this.model.selectedElementId === id) this.model.selectedElementId = null;
            this.queueSave();
        };

        this.view.onReorderElement = (id, newIndex) => {
            const screen = this.model.activeScreen;
            if (!screen || this._isReordering) return;
            this._isReordering = true;

            const idx = screen.elements.findIndex(e => e.id === id);
            if (idx !== -1) {
                const el = screen.elements.splice(idx, 1)[0];
                const target = Math.max(0, Math.min(newIndex, screen.elements.length));
                screen.elements.splice(target, 0, el);
                this.view._codeEditedManually = false;
                this.queueSave();
            }

            setTimeout(() => { this._isReordering = false; }, 500);
        };

        this.view.onPropertyChange = (elId, prop, val) => {
            const screen = this.model.activeScreen;
            if (!screen) return;

            if (prop === 'background') {
                screen.backgroundAsset = val;
                screen.background = this.model.assets.find(a => a.name === val)?.dataUrl;
            } else {
                const el = screen.elements.find(e => e.id === elId);
                if (el) el[prop] = val;
            }
            this.queueSave();
        };

        this.view.onAssetUpload = (asset) => {
            this.model.assets.push(asset);
            this.queueSave();
        };

        this.view.onDeleteAsset = (name) => {
            this.model.assets = this.model.assets.filter(a => a.name !== name);
            this.model.screens.forEach(s => {
                if (s.backgroundAsset === name) { s.backgroundAsset = null; s.background = null; }
                s.elements.forEach(el => { if (el.asset === name) el.asset = null; });
            });
            this.queueSave();
        };

        this.view.onScreenPropertyChange = async (screenId, prop, val, oldVal) => {
            const screen = this.model.screens.find(s => s.id === screenId);
            if (!screen) return;

            const oldName = oldVal || screen.name;

            // 1. Update local model
            screen[prop] = val;

            // 2. Persist to backend
            if (prop === 'backgroundAsset') {
                const asset = this.model.assets.find(a => a.name === val);
                await this.callApi('/screen/background', 'POST', {
                    screenId: screenId,
                    assetName: val,
                    dataUrl: asset ? asset.dataUrl : null
                });
            } else {
                await this.callApi('/screen/update', 'POST', { screenId, [prop]: val });
            }

            // 3. Sync code
            if (this.view._codeEditedManually && this.view.dom.codePreview) {
                let code = this.view.dom.codePreview.value;
                if (prop === 'name') {
                    const oldClean = this.view._cleanName(oldName);
                    const newClean = this.view._cleanName(val);
                    // Replaces both definition "void draw_OldName(" and calls "draw_OldName("
                    const fnRegex = new RegExp(`draw_${oldClean}\\s*\\(`, 'g');
                    code = code.replace(fnRegex, `draw_${newClean}(`);
                } else if (prop === 'backgroundColor') {
                    const cleanName = this.view._cleanName(screen.name);
                    const fnName = `draw_${cleanName}`;
                    const fillRegex = new RegExp(`(void\\s+${fnName}\\s*\\([^)]*\\)\\s*\\{[\\s\\S]*?tft\\.fillScreen\\s*\\()([^;]*?)(\\))`, 'm');
                    const new565 = this.view.hexTo565(val);
                    code = code.replace(fillRegex, `$1${new565}$3`);
                }
                this.view.dom.codePreview.value = code;
                this.view.parseAndRenderCode(code);
            } else {
                this.view.renderCodePreview(this.model);
            }
        };

        // Import background from Designer or from disk
        this.view.onImportBackground = async (dataUrl, sourceName) => {
            const activeId = this.model.activeScreenId;
            if (!activeId) return;
            await this.callApi('/screen/background', 'POST', {
                screenId: activeId,
                assetName: null,
                dataUrl: dataUrl
            });
        };

        this.view.onCanvasMouseDown = (e) => this.handleMouseDown(e);
        this.view.onCanvasMouseMove = (e) => this.handleMouseMove(e);
        this.view.onCanvasMouseUp = (e) => this.handleMouseUp(e);

        // Code editor → model sync (fires 800ms after user stops typing)
        this.view.onCodeChanged = (code) => this._syncCodeToModel(code);
    }

    /**
     * Bidirectional sync: parse C++ code → update model elements.
     * Rewritten to be non-destructive. It tries to match elements by type and order.
     * New elements are created, existing ones updated.
     * UNMATCHED elements in the code are assumed to be new and added.
     * UNMATCHED elements in the model are NOT deleted to avoid accidental loss of unparseable items.
     */
    _syncCodeToModel(code) {
        // Debounce actual sync to avoid rapid API calls while typing
        if (this._syncTimer) clearTimeout(this._syncTimer);
        this._syncTimer = setTimeout(async () => {
            if (this._isSyncingOrder || this._isReordering) return;
            await this._doSyncCodeToModel(code);
        }, 800);
    }

    async _doSyncCodeToModel(code) {
        // 1. Sync Screen Order
        const parsedScreenOrder = this.view._parseScreenOrderFromCode(code);
        let orderChanged = false;

        if (parsedScreenOrder.length > 0) {
            for (let i = 0; i < parsedScreenOrder.length; i++) {
                const cleanName = parsedScreenOrder[i];
                const modelScreen = this.model.screens[i];
                if (!modelScreen || this.view._cleanName(modelScreen.name) !== cleanName) {
                    // Find where this screen is now
                    const actualScreen = this.model.screens.find(s => this.view._cleanName(s.name) === cleanName);
                    if (actualScreen) {
                        this._isSyncingOrder = true;
                        console.log(`[Controller] Sincronizando ordem da tela localmente: ${actualScreen.name} para o índice ${i}`);

                        // Reorder locally
                        const idx = this.model.screens.findIndex(s => s.id === actualScreen.id);
                        this.model.screens.splice(idx, 1);
                        this.model.screens.splice(i, 0, actualScreen);

                        this.queueSave();
                        orderChanged = true;
                        this._isSyncingOrder = false;
                        break;
                    }
                }
            }
        }
        if (orderChanged) return;

        const screen = this.model.activeScreen;
        if (!screen) return;

        const fnName = (screen.name || 'Screen')
            .replace(/\s+/g, '_')
            .replace(/[^a-zA-Z0-9_]/g, '');

        const parsedElements = this.view._parseElementsFromCode(code, fnName);
        if (!parsedElements) return;

        // Group parsed elements to match
        const modelElements = [...screen.elements];
        const matchedIds = new Set();
        const finalOrderIds = [];

        // 2. Map parsed elements to model elements
        for (const p of parsedElements) {
            let match = modelElements.find(el =>
                !matchedIds.has(el.id) &&
                el.type === p.type &&
                (p.name ? el.name === p.name : true)
            );

            if (!match) {
                match = modelElements.find(el => !matchedIds.has(el.id) && el.type === p.type);
            }

            if (match) {
                matchedIds.add(match.id);
                finalOrderIds.push(match.id);

                const c1 = this.view.hexTo565(match.color || '#000000');
                const c2 = this.view.hexTo565(p.color || '#000000');

                if (match.x !== p.x || match.y !== p.y || match.w !== p.w || match.h !== p.h ||
                    c1 !== c2 || (p.name && match.name !== p.name) || match.asset !== p.asset) {

                    match.x = p.x; match.y = p.y; match.w = p.w; match.h = p.h;
                    match.color = p.color;
                    match.name = p.name;
                    match.asset = p.asset;
                    this.queueSave();
                }
            } else {
                const elId = `el_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`;
                const newEl = {
                    id: elId,
                    type: p.type,
                    name: p.name || `${p.type}_${screen.elements.length + 1}`,
                    x: p.x, y: p.y, w: p.w, h: p.h,
                    color: p.color,
                    asset: p.asset
                };
                screen.elements.push(newEl);
                finalOrderIds.push(elId);
                this.queueSave();
            }
        }

        // 3. Handle deletions local
        const oldLen = screen.elements.length;
        screen.elements = screen.elements.filter(el => matchedIds.has(el.id));
        if (screen.elements.length !== oldLen) this.queueSave();

        // 4. FIX ORDER local
        this._isSyncingOrder = true;
        try {
            let localOrderChanged = false;
            for (let i = 0; i < finalOrderIds.length; i++) {
                const desiredId = finalOrderIds[i];
                const currentIndex = screen.elements.findIndex(el => el.id === desiredId);
                if (currentIndex !== -1 && currentIndex !== i) {
                    const el = screen.elements.splice(currentIndex, 1)[0];
                    screen.elements.splice(i, 0, el);
                    localOrderChanged = true;
                }
            }
            if (localOrderChanged) this.queueSave();
        } finally {
            this._isSyncingOrder = false;
        }
    }

    handleMouseDown(e) {
        // Calculate scale factor between visual canvas (likely scaled by CSS) and internal resolution (240x240)
        const rect = e.target.getBoundingClientRect();
        const scaleX = 240 / rect.width;
        const scaleY = 240 / rect.height;

        const x = (e.clientX - rect.left) * scaleX;
        const y = (e.clientY - rect.top) * scaleY;

        const screen = this.model.activeScreen;
        if (!screen) return;

        // Check for resize handle on selected element first
        if (this.model.selectedElementId) {
            const el = screen.elements.find(e => e.id === this.model.selectedElementId);
            if (el) {
                // Bottom-right corner handle area (10x10)
                if (x >= el.x + el.w - 10 && x <= el.x + el.w + 5 &&
                    y >= el.y + el.h - 10 && y <= el.y + el.h + 5) {
                    this.resizingElement = el;
                    this.draggingElement = null; // Ensure we don't drag
                    return;
                }
            }
        }

        this.draggingElement = null;
        this.resizingElement = null;

        // Find element under mouse for dragging
        for (let i = screen.elements.length - 1; i >= 0; i--) {
            const el = screen.elements[i];
            if (x >= el.x && x <= el.x + el.w && y >= el.y && y <= el.y + el.h) {
                this.draggingElement = el;
                this.dragOffset = { x: x - el.x, y: y - el.y };
                this.view.onSelectElement(el.id);
                break;
            }
        }

        if (!this.draggingElement && !this.resizingElement) {
            this.view.onSelectElement(null);
        }
    }

    handleMouseMove(e) {
        if (!this.draggingElement && !this.resizingElement) return;

        const rect = e.target.getBoundingClientRect();
        // Calculate scale factor once here too
        const scaleX = 240 / rect.width;
        const scaleY = 240 / rect.height;

        const currentX = (e.clientX - rect.left) * scaleX;
        const currentY = (e.clientY - rect.top) * scaleY;

        if (this.resizingElement) {
            // Calculate new width/height, minimum 5px
            const newW = Math.max(5, currentX - this.resizingElement.x);
            const newH = Math.max(5, currentY - this.resizingElement.y);

            this.resizingElement.w = Math.round(newW);
            this.resizingElement.h = Math.round(newH);
            this.view.render(this.model);
            return;
        }

        if (this.draggingElement) {
            const x = Math.round(currentX - this.dragOffset.x);
            const y = Math.round(currentY - this.dragOffset.y);

            this.draggingElement.x = x;
            this.draggingElement.y = y;
            this.view.render(this.model);
        }
    }

    async handleMouseUp(e) {
        if (this.resizingElement || this.draggingElement) {
            this.queueSave();
            this.resizingElement = null;
            this.draggingElement = null;
        }
    }

    async importFromCollection(dataUrl) {
        this.model.assets.push({
            name: `Design_${Date.now()}`,
            dataUrl: dataUrl,
            kind: 'image'
        });
        this.queueSave();
    }

    refresh() { this.sync(); }
}

if (window.PixelDisplay240System) window.PixelDisplay240System.register('controller', '5.1.0');

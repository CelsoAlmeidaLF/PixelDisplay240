/**
 * Prototype Model - Single Source of Truth for Prototype State
 * Manages screens, elements, and assets with localStorage persistence.
 */
class PrototypeModel {
    constructor() {
        this.screens = [];
        this.assets = [];
        this.activeScreenId = null;
        this.selectedElementId = null;
        this.screenSeq = 1;
        this.elementSeq = 1;

        // Attempt to load from localStorage on construct
        this.loadFromLocal();

        // If still empty, create a default screen
        if (this.screens.length === 0) {
            const defaultId = `screen_${Date.now()}`;
            this.screens.push({
                id: defaultId,
                name: 'Main',
                elements: [],
                background: null,
                backgroundAsset: null,
                backgroundColor: '#111111'
            });
            this.activeScreenId = defaultId;
        }
    }

    /**
     * Get the currently active screen object.
     */
    get activeScreen() {
        return this.screens.find(s => s.id === this.activeScreenId) || this.screens[0];
    }

    /**
     * Update model from API/Server data.
     * @param {Object} data - Raw data from server
     */
    updateFromData(data) {
        if (!data) return;

        this.screens = Array.isArray(data.screens) ? data.screens : [];
        this.assets = Array.isArray(data.assets) ? data.assets : [];
        this.activeScreenId = data.activeScreenId || (this.screens[0]?.id ?? null);
        this.selectedElementId = data.selectedElementId || null;
        this.screenSeq = data.screenSeq || this.screens.length + 1;
        this.elementSeq = data.elementSeq || 1;

        // Ensure at least one screen exists
        if (this.screens.length === 0) {
            const defaultId = `screen_${Date.now()}`;
            this.screens.push({
                id: defaultId,
                name: 'Main',
                elements: [],
                background: null,
                backgroundAsset: null,
                backgroundColor: '#111111'
            });
            this.activeScreenId = defaultId;
        }

        this.saveToLocal();
    }

    /**
     * Get raw data object for serialization/API calls.
     */
    getRawData() {
        return {
            screens: this.screens,
            assets: this.assets,
            activeScreenId: this.activeScreenId,
            selectedElementId: this.selectedElementId,
            screenSeq: this.screenSeq,
            elementSeq: this.elementSeq
        };
    }

    /**
     * Save current state to localStorage.
     */
    saveToLocal() {
        try {
            localStorage.setItem('pixeldisplay240_prototype', JSON.stringify(this.getRawData()));
        } catch (e) {
            console.warn('[PrototypeModel] Failed to save to localStorage:', e);
        }
    }

    /**
     * Load state from localStorage.
     */
    loadFromLocal() {
        try {
            const stored = localStorage.getItem('pixeldisplay240_prototype');
            if (stored) {
                const data = JSON.parse(stored);
                this.screens = data.screens || [];
                this.assets = data.assets || [];
                this.activeScreenId = data.activeScreenId || null;
                this.selectedElementId = data.selectedElementId || null;
                this.screenSeq = data.screenSeq || 1;
                this.elementSeq = data.elementSeq || 1;
            }
        } catch (e) {
            console.warn('[PrototypeModel] Failed to load from localStorage:', e);
        }
    }

    /**
     * Add a new screen.
     * @param {string} name - Screen name
     * @param {string} template - Optional template type
     * @returns {Object} The new screen
     */
    addScreen(name, template = null) {
        const id = `screen_${Date.now()}_${this.screenSeq++}`;
        const screen = {
            id,
            name: name || `Screen_${this.screenSeq}`,
            elements: [],
            background: null,
            backgroundAsset: null,
            backgroundColor: '#111111'
        };

        // Apply template if specified
        if (template) {
            screen.elements = this._getTemplateElements(template);
        }

        this.screens.push(screen);
        this.activeScreenId = id;
        this.saveToLocal();
        return screen;
    }

    /**
     * Remove a screen by ID.
     * @param {string} id - Screen ID to remove
     */
    removeScreen(id) {
        if (this.screens.length <= 1) return false;
        this.screens = this.screens.filter(s => s.id !== id);
        if (this.activeScreenId === id) {
            this.activeScreenId = this.screens[0]?.id || null;
        }
        this.saveToLocal();
        return true;
    }

    /**
     * Add an element to the active screen.
     * @param {string} type - Element type
     * @param {Object} props - Additional properties
     * @returns {Object} The new element
     */
    addElement(type, props = {}) {
        const screen = this.activeScreen;
        if (!screen) return null;

        const id = `el_${Date.now()}_${this.elementSeq++}`;
        const element = {
            id,
            type,
            name: props.name || `${type}_${screen.elements.length + 1}`,
            x: props.x ?? 10 + (screen.elements.length * 5),
            y: props.y ?? 10 + (screen.elements.length * 5),
            w: props.w ?? (type.includes('Circle') ? 60 : 80),
            h: props.h ?? (type.includes('Circle') ? 60 : 40),
            color: props.color || '#38bdf8',
            asset: props.asset || null,
            ...props
        };

        screen.elements.push(element);
        this.selectedElementId = id;
        this.saveToLocal();
        return element;
    }

    /**
     * Remove an element by ID from the active screen.
     * @param {string} id - Element ID to remove
     */
    removeElement(id) {
        const screen = this.activeScreen;
        if (!screen) return false;

        screen.elements = screen.elements.filter(e => e.id !== id);
        if (this.selectedElementId === id) {
            this.selectedElementId = null;
        }
        this.saveToLocal();
        return true;
    }

    /**
     * Update an element's property.
     * @param {string} id - Element ID
     * @param {string} prop - Property name
     * @param {*} value - New value
     */
    updateElement(id, prop, value) {
        const screen = this.activeScreen;
        if (!screen) return false;

        const element = screen.elements.find(e => e.id === id);
        if (element) {
            element[prop] = value;
            this.saveToLocal();
            return true;
        }
        return false;
    }

    /**
     * Add an asset (image/sprite).
     * @param {Object} asset - Asset object with name, dataUrl, kind
     */
    addAsset(asset) {
        if (!asset || !asset.name) return;
        // Remove existing with same name
        this.assets = this.assets.filter(a => a.name !== asset.name);
        this.assets.push(asset);
        this.saveToLocal();
    }

    /**
     * Remove an asset by name.
     * @param {string} name - Asset name
     */
    removeAsset(name) {
        this.assets = this.assets.filter(a => a.name !== name);
        // Clear references in screens
        this.screens.forEach(screen => {
            if (screen.backgroundAsset === name) {
                screen.backgroundAsset = null;
                screen.background = null;
            }
            screen.elements.forEach(el => {
                if (el.asset === name) {
                    el.asset = null;
                }
            });
        });
        this.saveToLocal();
    }

    /**
     * Get template elements based on template type.
     * @param {string} template - Template name
     * @returns {Array} Array of element objects
     */
    _getTemplateElements(template) {
        const ts = Date.now();
        switch (template) {
            case 'dashboard':
                return [
                    { id: `el_${ts}_1`, type: 'fillRect', name: 'Header', x: 0, y: 0, w: 240, h: 30, color: '#1e293b' },
                    { id: `el_${ts}_2`, type: 'drawCentreString', name: 'CPU: 45%', x: 120, y: 8, w: 0, h: 16, color: '#38bdf8' },
                    { id: `el_${ts}_3`, type: 'fillCircle', name: 'Status_OK', x: 190, y: 15, w: 20, h: 20, color: '#4ade80' },
                    { id: `el_${ts}_4`, type: 'fillRect', name: 'ChartArea', x: 20, y: 60, w: 200, h: 120, color: '#0f172a' }
                ];
            case 'menu':
                return [
                    { id: `el_${ts}_1`, type: 'drawCentreString', name: 'MENU PRINCIPAL', x: 120, y: 20, w: 0, h: 16, color: '#f8fafc' },
                    { id: `el_${ts}_2`, type: 'fillRoundRect', name: 'Opt_1', x: 40, y: 60, w: 160, h: 35, color: '#334155' },
                    { id: `el_${ts}_3`, type: 'drawString', name: 'Configurações', x: 60, y: 70, w: 0, h: 16, color: '#ffffff' },
                    { id: `el_${ts}_4`, type: 'fillRoundRect', name: 'Opt_2', x: 40, y: 105, w: 160, h: 35, color: '#334155' },
                    { id: `el_${ts}_5`, type: 'drawString', name: 'Sensores', x: 60, y: 115, w: 0, h: 16, color: '#ffffff' }
                ];
            case 'loading':
                return [
                    { id: `el_${ts}_1`, type: 'drawCentreString', name: 'LOADING...', x: 120, y: 100, w: 0, h: 16, color: '#38bdf8' },
                    { id: `el_${ts}_2`, type: 'drawRect', name: 'ProgressBorder', x: 40, y: 130, w: 160, h: 10, color: '#475569' },
                    { id: `el_${ts}_3`, type: 'fillRect', name: 'ProgressBar', x: 42, y: 132, w: 80, h: 6, color: '#38bdf8' }
                ];
            case 'clock':
                return [
                    { id: `el_${ts}_1`, type: 'drawCircle', name: 'ClockFace', x: 20, y: 20, w: 200, h: 200, color: '#1e293b' },
                    { id: `el_${ts}_2`, type: 'drawCentreString', name: '12:45', x: 120, y: 90, w: 0, h: 32, color: '#ffffff' },
                    { id: `el_${ts}_3`, type: 'drawCentreString', name: 'Quarta, 25 Fev', x: 120, y: 140, w: 0, h: 12, color: '#475569' }
                ];
            default:
                return [];
        }
    }
}

// Register with the system orchestrator
if (window.PixelDisplay240System) {
    window.PixelDisplay240System.register('model', '5.1.0');
}

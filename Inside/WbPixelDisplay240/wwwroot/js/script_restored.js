// --- PixelDisplay240 Studio - Main Module (script_restored.js) ---
// Orchestrates the initialization of all sub-modules

if (typeof EditorModule === 'undefined') {
    window.EditorModule = class EditorModule {
        constructor() {
            this.dom = (id) => {
                const el = document.getElementById(id);
                return el;
            };
        }
    };
}

class ThemeManager extends EditorModule {
    constructor() {
        super();
        this._init();
    }
    _init() {
        const paletteSel = this.dom('palette-presets');
        if (paletteSel) paletteSel.onchange = () => this.loadPalette(paletteSel.value);
    }
    loadPalette(id) {
        // Simplified loader - actual logic in designer.js or here
    }
}

class PixelDisplay240App extends EditorModule {
    constructor() {
        super();
        console.log("[App] Initializing System...");

        // Modules
        this.theme = new ThemeManager();
        this.wm = (typeof WindowManager !== 'undefined') ? new WindowManager(this) : null;
        this.hardware = (typeof HardwareManager !== 'undefined') ? new HardwareManager(this) : null;

        // Designer & Prototype
        this.layers = (typeof LayerManager !== 'undefined') ? new LayerManager(this) : null;
        this.layout = (typeof LayoutManager !== 'undefined') ? new LayoutManager(this) : null;
        this.drawing = (typeof DrawingManager !== 'undefined') ? new DrawingManager(this) : null;

        // MVC Prototype
        if (typeof PrototypeController !== 'undefined') {
            const pModel = new PrototypeModel();
            const pView = new PrototypeView();
            this.prototype = new PrototypeController(pModel, pView);
        }

        this.currentTool = 'brush';
        this.init();
    }

    init() {
        console.log("[App] Wiring UI Events...");
        this._setupTools();
        this._wireUI();

        if (this.prototype) {
            this.prototype.init(this);
        }

        if (this.layers && this.layers.layers && this.layers.layers.length === 0) {
            this.layers.add("Fundo", null, true);
        }
    }

    _setupTools() {
        document.querySelectorAll('.tool-btn').forEach(btn => {
            btn.onclick = () => {
                document.querySelectorAll('.tool-btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                this.currentTool = btn.id.replace('tool-', '');
            };
        });
    }

    _wireUI() {
        const navDesign = this.dom('nav-design');
        const navProto = this.dom('nav-prototype');
        const navHard = this.dom('nav-hardware');

        const views = {
            design: this.dom('design-view'),
            prototype: this.dom('prototype-view'),
            hardware: this.dom('hardware-view')
        };

        const switchView = (id) => {
            Object.keys(views).forEach(k => {
                if (views[k]) views[k].classList.toggle('active', k === id);
            });
            [navDesign, navProto, navHard].forEach(b => {
                if (b) b.classList.toggle('active', b.id === `nav-${id}`);
            });
        };

        if (navDesign) navDesign.onclick = () => switchView('design');
        if (navProto) navProto.onclick = () => switchView('prototype');
        if (navHard) navHard.onclick = () => switchView('hardware');
    }

    refresh() {
        if (this.layout) {
            this.layout.renderGrid();
            this.layout.updateRulers();
        }
        // Update preview canvas if available
        const previewCanvas = document.getElementById('preview-canvas');
        if (previewCanvas && this.layers && this.layers.active) {
            const ctx = previewCanvas.getContext('2d');
            ctx.clearRect(0, 0, previewCanvas.width, previewCanvas.height);
            // Composite all visible layers
            this.layers.layers.forEach(layer => {
                if (layer.visible) {
                    ctx.drawImage(layer.canvas, 0, 0);
                }
            });
        }
    }

    saveHistory() { }
}

// Global entry point for System Runtime
if (window.PixelDisplay240System) {
    window.PixelDisplay240System.register('main', '5.1.0');
    window.app = new PixelDisplay240App();
}

// --- PixelDisplay240 Studio - Designer Module ---
// Contains Logic specific to the Image Editor / Canvas Design View

// Ensure EditorModule base class exists if script.js hasn't run yet
if (typeof EditorModule === 'undefined') {
    window.EditorModule = class EditorModule {
        constructor() {
            this.dom = (id) => {
                const el = document.getElementById(id);
                if (!el && !['status-memory', 'status-time'].includes(id)) {
                    // Silently return null for missing elements instead of throwing
                }
                return el;
            };
        }
    };
}

class Layer {
    constructor(appRef, name, width, height, dataURL = null, backgroundColor = null) {
        this.app = appRef;
        this.name = name;
        this.visible = true;
        this.canvas = document.createElement('canvas');
        this.canvas.width = width;
        this.canvas.height = height;
        this.canvas.className = 'canvas-layer';
        this.ctx = this.canvas.getContext('2d', { willReadFrequently: true });
        if (dataURL) {
            const img = new Image();
            img.onload = () => {
                this.ctx.drawImage(img, 0, 0, width, height);
                this.app.refresh();
            };
            img.src = dataURL;
        } else if (backgroundColor) {
            this.ctx.fillStyle = backgroundColor;
            this.ctx.fillRect(0, 0, width, height);
        }
    }
}

class LayerManager extends EditorModule {
    constructor(app) {
        super();
        this.app = app;
        this.layers = [];
        this.activeIndex = 0;
        this.container = this.dom('layers-container');
        this.listEl = this.dom('layers-list');
    }

    add(name = "Camada", dataURL = null, silent = false) {
        const w = parseInt(this.dom('canvas-width')?.value) || 240;
        const h = parseInt(this.dom('canvas-height')?.value) || 240;
        const bg = (this.layers.length === 0) ? (this.dom('bg-color-picker')?.value || '#000000') : null;
        const newLayer = new Layer(this.app, name, w, h, dataURL, bg);
        this.layers.push(newLayer);
        this.activeIndex = this.layers.length - 1;
        if (this.container) this.container.insertBefore(newLayer.canvas, this.dom('temp-canvas'));
        this.updateZIndex();
        this.renderUI();
        if (!silent) this.app.saveHistory();
    }

    select(index) {
        if (!this.layers[index]) return;
        this.activeIndex = index;
        this.renderUI();
    }

    get active() { return this.layers[this.activeIndex]; }

    delete(index) {
        if (this.layers.length <= 1) return;
        this.layers[index].canvas.remove();
        this.layers.splice(index, 1);
        this.activeIndex = Math.min(this.activeIndex, this.layers.length - 1);
        this.app.refresh();
        this.app.saveHistory();
    }

    toggleVisibility(index) {
        const layer = this.layers[index];
        layer.visible = !layer.visible;
        layer.canvas.style.display = layer.visible ? 'block' : 'none';
        this.renderUI();
        this.app.refresh();
    }

    async rename(index) {
        const newName = await this.app.dialog.prompt('Renomear Camada', 'Digite o novo nome:', this.layers[index].name);
        if (newName && newName.trim()) {
            this.layers[index].name = newName.trim();
            this.renderUI();
            this.app.saveHistory();
        }
    }

    updateZIndex() { this.layers.forEach((l, i) => l.canvas.style.zIndex = i); }

    renderUI() {
        if (!this.listEl) return;
        this.listEl.innerHTML = '';
        [...this.layers].reverse().forEach((l, revIdx) => {
            const i = this.layers.length - 1 - revIdx;
            const item = document.createElement('div');
            item.className = `layer-item ${i === this.activeIndex ? 'active' : ''}`;
            item.innerHTML = `
                <span style="cursor:pointer; display:flex; align-items:center;" onclick="event.stopPropagation(); app.layers.toggleVisibility(${i})">
                    <i data-lucide="${l.visible ? 'eye' : 'eye-off'}" style="width:14px; height:14px;"></i>
                </span>
                <span style="flex:1; margin-left:8px;" ondblclick="event.stopPropagation(); app.layers.rename(${i})">${l.name}</span>
                <button class="text-btn" onclick="event.stopPropagation(); app.layers.delete(${i})" title="Excluir">
                    <i data-lucide="trash-2" style="width:14px; height:14px;"></i>
                </button>
            `;
            item.onclick = () => this.select(i);
            this.listEl.appendChild(item);
        });
        if (typeof lucide !== 'undefined') lucide.createIcons();
    }
}

class DrawingManager extends EditorModule {
    constructor(app) {
        super();
        this.app = app;
        this.isDrawing = false;
        this.startX = 0; this.startY = 0;
        this.lastX = 0; this.lastY = 0;
        this.symX = false;
        this.symY = false;
        this.tempCanvas = this.dom('temp-canvas');
        this.tempCtx = this.tempCanvas ? this.tempCanvas.getContext('2d') : null;
        this._setupEvents();
    }

    _setupEvents() {
        const wrapper = this.dom('canvas-wrapper');
        if (wrapper) {
            wrapper.addEventListener('mousedown', (e) => this._onMouseDown(e));
            wrapper.addEventListener('mousemove', (e) => this._onMouseMove(e));
            window.addEventListener('mouseup', () => this._onMouseUp());
        }
        const btnX = this.dom('btn-sym-x'), btnY = this.dom('btn-sym-y');
        if (btnX) btnX.onclick = () => { this.symX = !this.symX; btnX.classList.toggle('active', this.symX); };
        if (btnY) btnY.onclick = () => { this.symY = !this.symY; btnY.classList.toggle('active', this.symY); };
    }

    _getMousePos(e) {
        const active = this.app.layers.active;
        if (!active) return [0, 0];
        const r = this.dom('canvas-wrapper').getBoundingClientRect();
        return [(e.clientX - r.left) * (active.canvas.width / r.width), (e.clientY - r.top) * (active.canvas.height / r.height)];
    }

    _onMouseDown(e) {
        const active = this.app.layers.active;
        if (!active || !active.visible) return;
        this.isDrawing = true;
        [this.startX, this.startY] = this._getMousePos(e);
        [this.lastX, this.lastY] = [this.startX, this.startY];

        // Fix: Explicitly draw dot on click
        if (this.app.currentTool === 'brush' || this.app.currentTool === 'eraser') {
            const ctx = active.ctx;
            const color = this.dom('color-picker')?.value || '#fff';
            const size = parseInt(this.dom('brush-size')?.value) || 2;

            ctx.save();
            if (this.app.currentTool === 'eraser') ctx.globalCompositeOperation = 'destination-out';

            ctx.lineCap = 'round';
            ctx.strokeStyle = color;
            ctx.lineWidth = size;

            this._drawSymmetricStroke(ctx, this.startX, this.startY, this.startX, this.startY, size, color);
            ctx.restore();
            this.app.refresh();
        }

        if (this.app.currentTool === 'picker') {
            this._pickColor(this.startX, this.startY);
            this.isDrawing = false;
        } else if (this.app.currentTool === 'fill') {
            this._floodFill(active.ctx, Math.floor(this.startX), Math.floor(this.startY));
            this.isDrawing = false;
            this.app.refresh();
            this.app.saveHistory();
        } else if (this.app.currentTool === 'text') {
            this._drawText(this.startX, this.startY);
            this.isDrawing = false;
        }
    }

    _onMouseMove(e) {
        if (!this.isDrawing || !this.app.layers.active) return;
        let [x, y] = this._getMousePos(e);
        const tool = this.app.currentTool;
        const ctx = this.app.layers.active.ctx;
        const color = this.dom('color-picker')?.value || '#fff';
        const size = parseInt(this.dom('brush-size')?.value) || 2;

        // Smart Guides & Snapping (Center 120 + User Guides)
        const snapThreshold = 5;
        const centerX = 120;
        const centerY = 120;

        if (this.app.layout) this.app.layout.clearSmartGuides();

        // Check snapping for X
        const userGuidesV = this.app.layout ? this.app.layout.getGuidePositions('v') : [];
        const allGuidesX = [centerX, ...userGuidesV];
        for (const gx of allGuidesX) {
            if (Math.abs(x - gx) < snapThreshold) {
                x = gx;
                if (this.app.layout) this.app.layout.showSmartGuide('v', gx);
                break;
            }
        }

        // Check snapping for Y
        const userGuidesH = this.app.layout ? this.app.layout.getGuidePositions('h') : [];
        const allGuidesY = [centerY, ...userGuidesH];
        for (const gy of allGuidesY) {
            if (Math.abs(y - gy) < snapThreshold) {
                y = gy;
                if (this.app.layout) this.app.layout.showSmartGuide('h', gy);
                break;
            }
        }

        if (tool === 'brush' || tool === 'eraser') {
            ctx.save();
            if (tool === 'eraser') ctx.globalCompositeOperation = 'destination-out';

            const pattern = this.dom('brush-pattern')?.value || 'solid';
            if (pattern !== 'solid') {
                const p = this._createPattern(ctx, pattern, color);
                if (p) ctx.strokeStyle = p; else ctx.strokeStyle = color;
            } else {
                ctx.strokeStyle = color;
            }

            ctx.lineWidth = size;
            ctx.lineCap = 'round';
            this._drawSymmetricStroke(ctx, this.lastX, this.lastY, x, y, size, color);
            ctx.restore();
            [this.lastX, this.lastY] = [x, y];
            this.app.refresh();
        } else if (['line', 'rect', 'circle'].includes(tool)) {
            const pattern = this.dom('brush-pattern')?.value || 'solid';
            this._drawPreview(tool, this.startX, this.startY, x, y, color, size);
        }
    }

    _onMouseUp() {
        if (!this.isDrawing) return;
        this.isDrawing = false;
        if (this.app.layout) this.app.layout.clearSmartGuides();
        const tool = this.app.currentTool;
        const active = this.app.layers.active;
        if (!active) return;

        if (['line', 'rect', 'circle'].includes(tool)) {
            const [x, y] = [this.lastX, this.lastY]; // Should be current mouse pos, but let's use a stored pos if needed
            // Finalize drawing on main canvas
            const color = this.dom('color-picker')?.value || '#fff';
            const size = parseInt(this.dom('brush-size')?.value) || 2;
            this._drawShape(active.ctx, tool, this.startX, this.startY, this.lastX, this.lastY, color, size, true);
            if (this.tempCtx) this.tempCtx.clearRect(0, 0, this.tempCanvas.width, this.tempCanvas.height);
        }

        this.app.refresh();
        this.app.saveHistory();
    }

    _drawSymmetricStroke(ctx, x1, y1, x2, y2, size, color) {
        const w = this.app.layers.active.canvas.width;
        const h = this.app.layers.active.canvas.height;
        this._line(ctx, x1, y1, x2, y2);
        if (this.symX) this._line(ctx, w - x1, y1, w - x2, y2);
        if (this.symY) this._line(ctx, x1, h - y1, x2, h - y2);
        if (this.symX && this.symY) this._line(ctx, w - x1, h - y1, w - x2, h - y2);
    }

    _line(ctx, x1, y1, x2, y2) { ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2); ctx.stroke(); }

    _createPattern(ctx, type, color) {
        const pCanvas = document.createElement('canvas');
        pCanvas.width = 4; pCanvas.height = 4;
        const pCtx = pCanvas.getContext('2d');
        pCtx.fillStyle = color;
        if (type === 'checker') {
            pCtx.fillRect(0, 0, 2, 2); pCtx.fillRect(2, 2, 2, 2);
        } else if (type === 'dots') {
            pCtx.fillRect(0, 0, 1, 1);
        } else if (type === 'lines') {
            pCtx.fillRect(0, 0, 4, 1);
        }
        return ctx.createPattern(pCanvas, 'repeat');
    }

    _drawPreview(tool, x1, y1, x2, y2, color, size) {
        if (!this.tempCtx) return;
        this.tempCtx.clearRect(0, 0, this.tempCanvas.width, this.tempCanvas.height);
        this._drawShape(this.tempCtx, tool, x1, y1, x2, y2, color, size, false);
        this.lastX = x2; this.lastY = y2;
    }

    _drawShape(ctx, tool, x1, y1, x2, y2, color, size, symmetric) {
        ctx.strokeStyle = color;
        ctx.lineWidth = size;
        ctx.lineCap = 'round';

        const draw = (ctx, x1, y1, x2, y2) => {
            ctx.beginPath();
            if (tool === 'line') {
                ctx.moveTo(x1, y1); ctx.lineTo(x2, y2);
            } else if (tool === 'rect') {
                ctx.strokeRect(x1, y1, x2 - x1, y2 - y1);
            } else if (tool === 'circle') {
                const r = Math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2);
                ctx.arc(x1, y1, r, 0, Math.PI * 2);
            }
            ctx.stroke();
        };

        draw(ctx, x1, y1, x2, y2);
        if (symmetric && this.app.layers.active) {
            const w = this.app.layers.active.canvas.width, h = this.app.layers.active.canvas.height;
            if (this.symX) draw(ctx, w - x1, y1, w - x2, y2);
            if (this.symY) draw(ctx, x1, h - y1, x2, h - y2);
            if (this.symX && this.symY) draw(ctx, w - x1, h - y1, w - x2, h - y2);
        }
    }

    _pickColor(x, y) {
        const active = this.app.layers.active;
        if (!active) return;
        const data = active.ctx.getImageData(x, y, 1, 1).data;
        const hex = "#" + ((1 << 24) + (data[0] << 16) + (data[1] << 8) + data[2]).toString(16).slice(1);
        if (this.dom('color-picker')) this.dom('color-picker').value = hex;
    }

    _floodFill(ctx, x, y) {
        const color = this.dom('color-picker').value;
        const r = parseInt(color.slice(1, 3), 16);
        const g = parseInt(color.slice(3, 5), 16);
        const b = parseInt(color.slice(5, 7), 16);

        const imageData = ctx.getImageData(0, 0, ctx.canvas.width, ctx.canvas.height);
        const data = imageData.data;
        const targetColor = this._getPixel(data, x, y, ctx.canvas.width);

        if (this._colorMatch(targetColor, [r, g, b, 255])) return;

        const stack = [[x, y]];
        while (stack.length > 0) {
            const [cx, cy] = stack.pop();
            if (cx < 0 || cy < 0 || cx >= ctx.canvas.width || cy >= ctx.canvas.height) continue;

            const current = this._getPixel(data, cx, cy, ctx.canvas.width);
            if (this._colorMatch(current, targetColor)) {
                this._setPixel(data, cx, cy, ctx.canvas.width, [r, g, b, 255]);
                stack.push([cx + 1, cy], [cx - 1, cy], [cx, cy + 1], [cx, cy - 1]);
            }
        }
        ctx.putImageData(imageData, 0, 0);
    }

    _getPixel(data, x, y, w) {
        const i = (y * w + x) * 4;
        return [data[i], data[i + 1], data[i + 2], data[i + 3]];
    }

    _setPixel(data, x, y, w, color) {
        const i = (y * w + x) * 4;
        data[i] = color[0]; data[i + 1] = color[1]; data[i + 2] = color[2]; data[i + 3] = color[3];
    }

    _colorMatch(c1, c2) {
        return c1[0] === c2[0] && c1[1] === c2[1] && c1[2] === c2[2] && c1[3] === c2[3];
    }

    async _drawText(x, y) {
        const text = await this.app.dialog.prompt('Ferramenta de Texto', 'O que voc� deseja escrever?', '');
        if (text && text.trim()) {
            const active = this.app.layers.active;
            const ctx = active.ctx;
            const color = this.dom('color-picker')?.value || '#fff';
            const size = parseInt(this.dom('brush-size')?.value) || 20;

            ctx.fillStyle = color;
            ctx.font = `${size}px Inter, sans-serif`;
            ctx.textBaseline = 'top';
            ctx.fillText(text, x, y);

            this.app.refresh();
            this.app.saveHistory();
        }
    }
}

class LayoutManager extends EditorModule {
    constructor(app) {
        super();
        this.app = app;
        this.gridCanvas = this.dom('grid-canvas');
        this.gridCtx = this.gridCanvas ? this.gridCanvas.getContext('2d') : null;
        this.rulerH = this.dom('ruler-h');
        this.rulerV = this.dom('ruler-v');
        this.guides = this.dom('guides-container');
        this.draggingGuide = null;
        this.guideSnapThreshold = 5;

        this._setupEvents();
    }

    _setupEvents() {
        const toggleGrid = this.dom('toggle-grid');
        if (toggleGrid) toggleGrid.onchange = () => this.renderGrid();

        const toggleRuler = this.dom('toggle-ruler');
        if (toggleRuler) toggleRuler.onchange = () => this.updateRulers();

        const toggleGuides = this.dom('toggle-guides');
        if (toggleGuides) toggleGuides.onchange = () => {
            if (this.guides) this.guides.style.display = toggleGuides.checked ? 'block' : 'none';
        };

        const zoomInput = this.dom('work-zoom');
        if (zoomInput) zoomInput.oninput = () => {
            const z = zoomInput.value;
            if (this.dom('work-zoom-val')) this.dom('work-zoom-val').textContent = z + '%';
            const container = document.querySelector('.canvas-container');
            if (container) container.style.transform = `scale(${z / 100})`;
        };

        if (this.rulerH) this.rulerH.onclick = (e) => this._addGuide('v', e, this.rulerH);
        if (this.rulerV) this.rulerV.onclick = (e) => this._addGuide('h', e, this.rulerV);

        const btnClear = this.dom('btn-clear-guides');
        if (btnClear) btnClear.onclick = () => { if (this.guides) this.guides.innerHTML = ''; };

        // Global Guide Dragging
        window.addEventListener('mousemove', (e) => this._handleGuideDrag(e));
        window.addEventListener('mouseup', () => this._stopGuideDrag());
    }

    renderGrid() {
        const active = this.app.layers.active;
        if (!this.gridCtx || !active) return;
        this.gridCtx.clearRect(0, 0, this.gridCanvas.width, this.gridCanvas.height);
        if (!this.dom('toggle-grid')?.checked) return;

        this.gridCtx.strokeStyle = 'rgba(255,255,255,0.1)';
        this.gridCtx.lineWidth = 0.5;
        const step = 10;
        for (let i = 0; i <= this.gridCanvas.width; i += step) {
            this.gridCtx.beginPath(); this.gridCtx.moveTo(i, 0); this.gridCtx.lineTo(i, this.gridCanvas.height); this.gridCtx.stroke();
        }
        for (let j = 0; j <= this.gridCanvas.height; j += step) {
            this.gridCtx.beginPath(); this.gridCtx.moveTo(0, j); this.gridCtx.lineTo(this.gridCanvas.width, j); this.gridCtx.stroke();
        }
    }

    updateRulers() {
        const active = this.app.layers.active;
        if (!this.rulerH || !this.rulerV || !active) return;
        this.rulerH.innerHTML = ''; this.rulerV.innerHTML = '';

        if (!this.dom('toggle-ruler')?.checked) {
            this.rulerH.style.visibility = 'hidden'; this.rulerV.style.visibility = 'hidden';
            return;
        }
        this.rulerH.style.visibility = 'visible'; this.rulerV.style.visibility = 'visible';

        const w = active.canvas.width, h = active.canvas.height;
        const step = 20, major = 100;

        for (let i = 0; i <= w; i += step) {
            const isCenter = (i === 120);
            const isMajor = (i % major === 0) || isCenter;
            const m = document.createElement('div');
            m.className = 'ruler-mark' + (isCenter ? ' center-mark' : '');
            m.style.left = i + 'px';
            m.style.height = isMajor ? '100%' : '40%';

            if (isMajor) {
                const l = document.createElement('span');
                l.className = 'ruler-label' + (isCenter ? ' center-label' : '');
                l.textContent = i;
                l.style.left = (i + 2) + 'px';
                l.id = `ruler-h-label-${i}`;
                this.rulerH.appendChild(l);
            }
            this.rulerH.appendChild(m);
        }

        for (let i = 0; i <= h; i += step) {
            const isCenter = (i === 120);
            const isMajor = (i % major === 0) || isCenter;
            const m = document.createElement('div');
            m.className = 'ruler-mark' + (isCenter ? ' center-mark' : '');
            m.style.top = i + 'px';
            m.style.width = isMajor ? '100%' : '40%';

            if (isMajor) {
                const l = document.createElement('span');
                l.className = 'ruler-label' + (isCenter ? ' center-label' : '');
                l.textContent = i;
                l.style.top = (i + 2) + 'px';
                l.id = `ruler-v-label-${i}`;
                this.rulerV.appendChild(l);
            }
            this.rulerV.appendChild(m);
        }
    }

    _addGuide(type, e, ruler) {
        const active = this.app.layers.active;
        if (!active) return;
        const rect = ruler.getBoundingClientRect();
        let pos = type === 'v'
            ? (e.clientX - rect.left) * (active.canvas.width / rect.width)
            : (e.clientY - rect.top) * (active.canvas.height / rect.height);

        // Snap guide on creation
        pos = this._snapPos(pos);

        const g = document.createElement('div');
        g.className = `guide guide-${type}`;
        g.dataset.type = type;
        if (type === 'v') g.style.left = pos + 'px'; else g.style.top = pos + 'px';

        g.onmousedown = (ev) => {
            ev.stopPropagation();
            this.draggingGuide = g;
            g.classList.add('dragging');
        };

        if (this.guides) this.guides.appendChild(g);
        this.showSmartGuide(type, pos);
        setTimeout(() => this.clearSmartGuides(), 500);
    }

    _snapPos(pos) {
        const centerX = 120;
        if (Math.abs(pos - centerX) < this.guideSnapThreshold) return centerX;
        // Also snap to nearest 10 for "smart" feeling
        if (Math.abs(pos - Math.round(pos / 10) * 10) < 3) return Math.round(pos / 10) * 10;
        return Math.round(pos);
    }

    _handleGuideDrag(e) {
        if (!this.draggingGuide) return;
        const type = this.draggingGuide.dataset.type;
        const active = this.app.layers.active;
        if (!active) return;

        const wrapper = this.dom('canvas-wrapper');
        const rect = wrapper.getBoundingClientRect();

        let pos = type === 'v'
            ? (e.clientX - rect.left) * (active.canvas.width / rect.width)
            : (e.clientY - rect.top) * (active.canvas.height / rect.height);

        // Snap while dragging
        pos = this._snapPos(pos);

        if (type === 'v') this.draggingGuide.style.left = pos + 'px';
        else this.draggingGuide.style.top = pos + 'px';

        this.showSmartGuide(type, pos);

        // If dragged way out, prepare for deletion
        if (pos < -20 || pos > 260) {
            this.draggingGuide.style.opacity = '0.3';
        } else {
            this.draggingGuide.style.opacity = '1';
        }
    }

    _stopGuideDrag() {
        if (!this.draggingGuide) return;

        const type = this.draggingGuide.dataset.type;
        let pos = parseInt(type === 'v' ? this.draggingGuide.style.left : this.draggingGuide.style.top);

        // Delete if dragged out
        if (pos < -20 || pos > 260) {
            this.draggingGuide.remove();
        }

        this.draggingGuide.classList.remove('dragging');
        this.draggingGuide = null;
        this.clearSmartGuides();
    }

    showSmartGuide(type, pos) {
        if (!this.guides) return;
        this.clearSmartGuides(); // Only one smart guide per axis
        const g = document.createElement('div');
        g.className = `guide guide-${type} smart-guide`;
        g.style.borderColor = 'var(--primary)';
        g.style.borderStyle = 'dashed';
        g.style.opacity = '0.8';
        if (type === 'v') g.style.left = pos + 'px'; else g.style.top = pos + 'px';
        this.guides.appendChild(g);

        // Highlight ruler label
        const rulerId = type === 'v' ? 'ruler-h' : 'ruler-v';
        const label = document.getElementById(`${rulerId}-label-${Math.round(pos)}`);
        if (label) label.classList.add('active-guide-label');
    }

    clearSmartGuides() {
        if (!this.guides) return;
        this.guides.querySelectorAll('.smart-guide').forEach(g => g.remove());
        document.querySelectorAll('.active-guide-label').forEach(l => l.classList.remove('active-guide-label'));
    }

    getGuidePositions(type) {
        if (!this.guides) return [];
        const selector = type === 'v' ? '.guide-v:not(.smart-guide)' : '.guide-h:not(.smart-guide)';
        const guides = Array.from(this.guides.querySelectorAll(selector));
        return guides.map(g => parseInt(type === 'v' ? g.style.left : g.style.top));
    }
}

class AIPixelArtGenerator extends EditorModule {
    constructor(app) {
        super();
        this.app = app;
        this.palettes = {
            2: [[15, 56, 15], [155, 188, 15]],
            4: [[15, 56, 15], [48, 98, 48], [139, 172, 15], [155, 188, 15]],
            8: [[0, 0, 0], [255, 0, 0], [0, 255, 0], [0, 0, 255], [255, 255, 0], [255, 0, 255], [0, 255, 255], [255, 255, 255]],
            16: [[0, 0, 0], [128, 0, 0], [0, 128, 0], [128, 128, 0], [0, 0, 128], [128, 0, 128], [0, 128, 128], [192, 192, 192], [128, 128, 128], [255, 0, 0], [0, 255, 0], [255, 255, 0], [0, 0, 255], [255, 0, 255], [0, 255, 255], [255, 255, 255]]
        };
    }

    async generate() {
        const promptInput = this.dom('ai-prompt');
        const prompt = promptInput ? promptInput.value.trim() : '';
        if (!prompt) return this.app.toast.show('warning', 'Prompt vazio', 'Descreva o que deseja gerar.');

        this._setLoading(true);
        const seed = Math.floor(Math.random() * 1e6);

        try {
            // Chama a API unificada do Servidor C#
            const params = new URLSearchParams({ prompt, seed });
            const resp = await this.app.api.request(`/api/ai/image?${params.toString()}`);

            if (!resp.ok) {
                const errData = await resp.json().catch(() => ({}));
                throw new Error(errData.body || `HTTP ${resp.status}`);
            }

            const blob = await resp.blob();
            const img = await this._loadBlobImage(blob);
            this._displayPreview(img);
        } catch (e) {
            this.app.logger.error('AIGenerator', 'Falha na gera��o via Servidor', { prompt, seed, error: e.message });
            const msg = e.message.includes('Quota exceeded')
                ? 'Quota de imagens excedida. Verifique seu plano/billing.'
                : 'Falha ao gerar imagem pelo servidor principal.';
            this._handleError(msg);
        }
        this._setLoading(false);
    }

    _loadBlobImage(blob) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            const objectUrl = URL.createObjectURL(blob);
            img.onload = () => { URL.revokeObjectURL(objectUrl); resolve(img); };
            img.onerror = () => { URL.revokeObjectURL(objectUrl); reject(new Error("Erro ao decodificar imagem do servidor")); };
            img.src = objectUrl;
        });
    }

    _displayPreview(img) {
        const canvas = this.dom('ai-preview-canvas');
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        ctx.imageSmoothingEnabled = false;

        const pixelSize = parseInt(this.dom('ai-pixel-size')?.value) || 1;
        if (pixelSize > 1) {
            const temp = document.createElement('canvas');
            temp.width = 240 / pixelSize; temp.height = 240 / pixelSize;
            temp.getContext('2d').drawImage(img, 0, 0, temp.width, temp.height);
            ctx.clearRect(0, 0, 240, 240);
            ctx.drawImage(temp, 0, 0, 240, 240);
        } else {
            ctx.drawImage(img, 0, 0, 240, 240);
        }

        const mode = this.dom('ai-palette')?.value;
        if (mode && mode !== 'full') {
            try {
                this._reduce(ctx, parseInt(mode) || 16);
            } catch (e) {
                this.app.toast.show('warning', 'Aviso de Seguran�a', 'A imagem foi carregada, mas o navegador bloqueou a leitura de pixels (CORS). A redu��o de paleta foi ignorada.');
            }
        }

        if (this.dom('ai-preview')) this.dom('ai-preview').style.display = 'block';
        if (this.dom('btn-ai-apply')) this.dom('btn-ai-apply').style.display = 'inline-flex';
    }

    _handleError(msg) {
        const isLocal = window.location.protocol === 'file:';
        const advice = isLocal ? "\n\nDica: Voc� est� usando o protocolo 'file:'. Use o Live Server do VS Code para evitar bloqueios do navegador." : "";
        this.app.toast.show('error', 'Erro de Gera��o', msg + advice);
    }

    _reduce(ctx, n) {
        const id = ctx.getImageData(0, 0, 240, 240);
        const d = id.data, p = this.palettes[n];
        for (let i = 0; i < d.length; i += 4) {
            let m = Infinity, b = p[0];
            for (const c of p) { const dist = (d[i] - c[0]) ** 2 + (d[i + 1] - c[1]) ** 2 + (d[i + 2] - c[2]) ** 2; if (dist < m) { m = dist; b = c; } }
            d[i] = b[0]; d[i + 1] = b[1]; d[i + 2] = b[2];
        }
        ctx.putImageData(id, 0, 0);
    }

    apply() {
        const layer = this.app.layers.active;
        if (layer) {
            try {
                layer.ctx.drawImage(this.dom('ai-preview-canvas'), 0, 0);
                this.app.refresh();
                this.app.saveHistory();
            } catch (e) {
                this.app.logger.error('AIGenerator', 'Erro ao aplicar imagem ao canvas (CORS)', { error: e.message });
                this.app.toast.show('error', 'Erro de Seguran�a', 'N�o foi poss�vel aplicar a imagem ao canvas devido a restri��es de seguran�a do navegador (CORS).');
            }
        }
        this.dom('ai-modal').style.display = 'none';
    }

    _setLoading(on) {
        if (this.dom('ai-loading')) this.dom('ai-loading').style.display = on ? 'block' : 'none';
        if (this.dom('btn-ai-generate')) this.dom('btn-ai-generate').disabled = on;
    }
}

if (window.PixelDisplay240System) window.PixelDisplay240System.register('designer', '5.1.0');

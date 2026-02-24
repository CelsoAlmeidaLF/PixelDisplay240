/**
 * TFT Command Catalog + Builder Panel
 * Each command defines its parameters and a code template.
 * When the user clicks a command button, the builder panel opens,
 * lets them fill in the parameters with a live mini-preview,
 * and then inserts the generated code into the editor.
 */

const TFT_COMMANDS = [
    // ── FORMAS ──────────────────────────────────────────────────────────────
    {
        id: 'fillRect', category: 'Formas', icon: 'square', label: 'fillRect',
        hint: 'Retângulo preenchido',
        template: (p) => `  tft.fillRect(${p.x}, ${p.y}, ${p.w}, ${p.h}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'rect' },
            { name: 'x', label: 'X', type: 'number', default: 10 },
            { name: 'y', label: 'Y', type: 'number', default: 10 },
            { name: 'w', label: 'W', type: 'number', default: 80 },
            { name: 'h', label: 'H', type: 'number', default: 40 },
            { name: 'color', label: 'Cor', type: 'color', default: '#38bdf8' },
        ],
        preview(ctx, p) {
            ctx.fillStyle = p.color;
            ctx.fillRect(+p.x, +p.y, +p.w, +p.h);
        }
    },
    {
        id: 'drawRect', category: 'Formas', icon: 'square-dashed', label: 'drawRect',
        hint: 'Retângulo contorno',
        template: (p) => `  tft.drawRect(${p.x}, ${p.y}, ${p.w}, ${p.h}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'rect_outline' },
            { name: 'x', label: 'X', type: 'number', default: 10 },
            { name: 'y', label: 'Y', type: 'number', default: 10 },
            { name: 'w', label: 'W', type: 'number', default: 80 },
            { name: 'h', label: 'H', type: 'number', default: 40 },
            { name: 'color', label: 'Cor', type: 'color', default: '#38bdf8' },
        ],
        preview(ctx, p) {
            ctx.strokeStyle = p.color; ctx.lineWidth = 1;
            ctx.strokeRect(+p.x + 0.5, +p.y + 0.5, +p.w, +p.h);
        }
    },
    {
        id: 'fillRoundRect', category: 'Formas', icon: 'square', label: 'fillRoundRect',
        hint: 'Retângulo arredondado',
        template: (p) => `  tft.fillRoundRect(${p.x}, ${p.y}, ${p.w}, ${p.h}, ${p.r}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'rounded_rect' },
            { name: 'x', label: 'X', type: 'number', default: 10 },
            { name: 'y', label: 'Y', type: 'number', default: 10 },
            { name: 'w', label: 'W', type: 'number', default: 100 },
            { name: 'h', label: 'H', type: 'number', default: 50 },
            { name: 'r', label: 'Raio', type: 'number', default: 8 },
            { name: 'color', label: 'Cor', type: 'color', default: '#38bdf8' },
        ],
        preview(ctx, p) {
            const [x, y, w, h, r] = [+p.x, +p.y, +p.w, +p.h, +p.r];
            ctx.fillStyle = p.color;
            ctx.beginPath();
            ctx.moveTo(x + r, y); ctx.lineTo(x + w - r, y);
            ctx.quadraticCurveTo(x + w, y, x + w, y + r);
            ctx.lineTo(x + w, y + h - r);
            ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
            ctx.lineTo(x + r, y + h);
            ctx.quadraticCurveTo(x, y + h, x, y + h - r);
            ctx.lineTo(x, y + r);
            ctx.quadraticCurveTo(x, y, x + r, y);
            ctx.closePath(); ctx.fill();
        }
    },
    {
        id: 'fillCircle', category: 'Formas', icon: 'circle', label: 'fillCircle',
        hint: 'Círculo preenchido',
        template: (p) => `  tft.fillCircle(${p.cx}, ${p.cy}, ${p.r}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'circle' },
            { name: 'cx', label: 'Centro X', type: 'number', default: 120 },
            { name: 'cy', label: 'Centro Y', type: 'number', default: 120 },
            { name: 'r', label: 'Raio', type: 'number', default: 40 },
            { name: 'color', label: 'Cor', type: 'color', default: '#f472b6' },
        ],
        preview(ctx, p) {
            ctx.fillStyle = p.color;
            ctx.beginPath(); ctx.arc(+p.cx, +p.cy, +p.r, 0, Math.PI * 2); ctx.fill();
        }
    },
    {
        id: 'drawCircle', category: 'Formas', icon: 'circle-dashed', label: 'drawCircle',
        hint: 'Círculo contorno',
        template: (p) => `  tft.drawCircle(${p.cx}, ${p.cy}, ${p.r}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'circle_out' },
            { name: 'cx', label: 'Centro X', type: 'number', default: 120 },
            { name: 'cy', label: 'Centro Y', type: 'number', default: 120 },
            { name: 'r', label: 'Raio', type: 'number', default: 40 },
            { name: 'color', label: 'Cor', type: 'color', default: '#f472b6' },
        ],
        preview(ctx, p) {
            ctx.strokeStyle = p.color; ctx.lineWidth = 1;
            ctx.beginPath(); ctx.arc(+p.cx, +p.cy, +p.r, 0, Math.PI * 2); ctx.stroke();
        }
    },
    {
        id: 'fillTriangle', category: 'Formas', icon: 'triangle', label: 'fillTriangle',
        hint: 'Triângulo preenchido',
        template: (p) => `  tft.fillTriangle(${p.x0},${p.y0}, ${p.x1},${p.y1}, ${p.x2},${p.y2}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'triangle' },
            { name: 'x0', label: 'X0', type: 'number', default: 120 },
            { name: 'y0', label: 'Y0', type: 'number', default: 10 },
            { name: 'x1', label: 'X1', type: 'number', default: 20 },
            { name: 'y1', label: 'Y1', type: 'number', default: 220 },
            { name: 'x2', label: 'X2', type: 'number', default: 220 },
            { name: 'y2', label: 'Y2', type: 'number', default: 220 },
            { name: 'color', label: 'Cor', type: 'color', default: '#fbbf24' },
        ],
        preview(ctx, p) {
            ctx.fillStyle = p.color;
            ctx.beginPath();
            ctx.moveTo(+p.x0, +p.y0); ctx.lineTo(+p.x1, +p.y1); ctx.lineTo(+p.x2, +p.y2);
            ctx.closePath(); ctx.fill();
        }
    },
    {
        id: 'fillEllipse', category: 'Formas', icon: 'ellipsis', label: 'fillEllipse',
        hint: 'Elipse preenchida',
        template: (p) => `  tft.fillEllipse(${p.x}, ${p.y}, ${p.rx}, ${p.ry}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'ellipse' },
            { name: 'x', label: 'Centro X', type: 'number', default: 120 },
            { name: 'y', label: 'Centro Y', type: 'number', default: 120 },
            { name: 'rx', label: 'Raio X', type: 'number', default: 60 },
            { name: 'ry', label: 'Raio Y', type: 'number', default: 30 },
            { name: 'color', label: 'Cor', type: 'color', default: '#a78bfa' },
        ],
        preview(ctx, p) {
            ctx.fillStyle = p.color;
            ctx.beginPath(); ctx.ellipse(+p.x, +p.y, +p.rx, +p.ry, 0, 0, Math.PI * 2); ctx.fill();
        }
    },
    // ── LINHAS ──────────────────────────────────────────────────────────────
    {
        id: 'drawLine', category: 'Linhas', icon: 'minus', label: 'drawLine',
        hint: 'Linha simples',
        template: (p) => `  tft.drawLine(${p.x0}, ${p.y0}, ${p.x1}, ${p.y1}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'line' },
            { name: 'x0', label: 'X0', type: 'number', default: 10 },
            { name: 'y0', label: 'Y0', type: 'number', default: 10 },
            { name: 'x1', label: 'X1', type: 'number', default: 230 },
            { name: 'y1', label: 'Y1', type: 'number', default: 230 },
            { name: 'color', label: 'Cor', type: 'color', default: '#f87171' },
        ],
        preview(ctx, p) {
            ctx.strokeStyle = p.color; ctx.lineWidth = 1;
            ctx.beginPath(); ctx.moveTo(+p.x0, +p.y0); ctx.lineTo(+p.x1, +p.y1); ctx.stroke();
        }
    },
    {
        id: 'drawFastHLine', category: 'Linhas', icon: 'move-horizontal', label: 'drawFastHLine',
        hint: 'Linha horizontal rápida',
        template: (p) => `  tft.drawFastHLine(${p.x}, ${p.y}, ${p.w}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'hline' },
            { name: 'x', label: 'X', type: 'number', default: 0 },
            { name: 'y', label: 'Y', type: 'number', default: 120 },
            { name: 'w', label: 'Larg', type: 'number', default: 240 },
            { name: 'color', label: 'Cor', type: 'color', default: '#34d399' },
        ],
        preview(ctx, p) {
            ctx.strokeStyle = p.color; ctx.lineWidth = 1;
            ctx.beginPath(); ctx.moveTo(+p.x, +p.y); ctx.lineTo(+p.x + +p.w, +p.y); ctx.stroke();
        }
    },
    {
        id: 'drawFastVLine', category: 'Linhas', icon: 'move-vertical', label: 'drawFastVLine',
        hint: 'Linha vertical rápida',
        template: (p) => `  tft.drawFastVLine(${p.x}, ${p.y}, ${p.h}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'vline' },
            { name: 'x', label: 'X', type: 'number', default: 120 },
            { name: 'y', label: 'Y', type: 'number', default: 0 },
            { name: 'h', label: 'Alt', type: 'number', default: 240 },
            { name: 'color', label: 'Cor', type: 'color', default: '#34d399' },
        ],
        preview(ctx, p) {
            ctx.strokeStyle = p.color; ctx.lineWidth = 1;
            ctx.beginPath(); ctx.moveTo(+p.x, +p.y); ctx.lineTo(+p.x, +p.y + +p.h); ctx.stroke();
        }
    },
    {
        id: 'drawPixel', category: 'Linhas', icon: 'dot', label: 'drawPixel',
        hint: 'Pixel individual',
        template: (p) => `  tft.drawPixel(${p.x}, ${p.y}, ${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'pixel' },
            { name: 'x', label: 'X', type: 'number', default: 10 },
            { name: 'y', label: 'Y', type: 'number', default: 10 },
            { name: 'color', label: 'Cor', type: 'color', default: '#ffffff' },
        ],
        preview(ctx, p) {
            ctx.fillStyle = p.color; ctx.fillRect(+p.x, +p.y, 2, 2);
        }
    },
    // ── TEXTO ───────────────────────────────────────────────────────────────
    {
        id: 'drawString', category: 'Texto', icon: 'type', label: 'drawString',
        hint: 'Texto simples',
        template: (p) => `  tft.setTextColor(${p.color});\n  tft.setTextSize(${p.size});\n  tft.drawString("${p.text}", ${p.x}, ${p.y}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'label' },
            { name: 'text', label: 'Texto', type: 'text', default: 'Hello!' },
            { name: 'x', label: 'X', type: 'number', default: 10 },
            { name: 'y', label: 'Y', type: 'number', default: 10 },
            { name: 'size', label: 'Size', type: 'number', default: 2 },
            { name: 'color', label: 'Cor', type: 'color', default: '#ffffff' },
        ],
        preview(ctx, p) {
            ctx.fillStyle = p.color;
            const fs = Math.max(6, (+p.size || 1) * 8);
            ctx.font = `${fs}px monospace`;
            ctx.fillText(p.text, +p.x, +p.y + fs);
        }
    },
    {
        id: 'drawCentreString', category: 'Texto', icon: 'align-center', label: 'drawCentreString',
        hint: 'Texto centralizado',
        template: (p) => `  tft.setTextColor(${p.color});\n  tft.setTextSize(${p.size});\n  tft.drawCentreString("${p.text}", ${p.x}, ${p.y}, ${p.font}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'title' },
            { name: 'text', label: 'Texto', type: 'text', default: 'Título' },
            { name: 'x', label: 'X ref', type: 'number', default: 120 },
            { name: 'y', label: 'Y', type: 'number', default: 10 },
            { name: 'size', label: 'Size', type: 'number', default: 2 },
            { name: 'font', label: 'Font#', type: 'number', default: 2 },
            { name: 'color', label: 'Cor', type: 'color', default: '#ffffff' },
        ],
        preview(ctx, p) {
            ctx.fillStyle = p.color;
            const fs = Math.max(6, (+p.size || 1) * 8);
            ctx.font = `${fs}px monospace`;
            ctx.textAlign = 'center';
            ctx.fillText(p.text, +p.x, +p.y + fs);
            ctx.textAlign = 'left';
        }
    },
    // ── TELA ────────────────────────────────────────────────────────────────
    {
        id: 'fillScreen', category: 'Tela', icon: 'panel-top', label: 'fillScreen',
        hint: 'Fundo da tela',
        template: (p) => `  tft.fillScreen(${p.color}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'background' },
            { name: 'color', label: 'Cor', type: 'color', default: '#000000' },
        ],
        preview(ctx, p) {
            ctx.fillStyle = p.color; ctx.fillRect(0, 0, 240, 240);
        }
    },
    {
        id: 'pushImage', category: 'Tela', icon: 'image', label: 'pushImage',
        hint: 'Imagem do array PROGMEM',
        template: (p) => `  tft.pushImage(${p.x}, ${p.y}, ${p.w}, ${p.h}, ${p.arrayName}); // ${p.label}`,
        params: [
            { name: 'label', label: 'Nome', type: 'text', default: 'image' },
            { name: 'x', label: 'X', type: 'number', default: 0 },
            { name: 'y', label: 'Y', type: 'number', default: 0 },
            { name: 'w', label: 'W', type: 'number', default: 240 },
            { name: 'h', label: 'H', type: 'number', default: 240 },
            { name: 'arrayName', label: 'Array', type: 'text', default: 'myImage' },
        ],
        preview(ctx, p) {
            ctx.strokeStyle = '#38bdf8'; ctx.setLineDash([4, 4]);
            ctx.strokeRect(+p.x + 0.5, +p.y + 0.5, +p.w, +p.h);
            ctx.setLineDash([]); ctx.fillStyle = 'rgba(56,189,248,0.08)';
            ctx.fillRect(+p.x, +p.y, +p.w, +p.h);
            ctx.fillStyle = '#38bdf8'; ctx.font = '10px monospace';
            ctx.textAlign = 'center'; ctx.fillText(`[${p.arrayName}]`, +p.x + +p.w / 2, +p.y + +p.h / 2);
            ctx.textAlign = 'left';
        }
    },
];

/**
 * TFT Command Builder Panel
 * Shows a form for the selected command with live 240x240 mini-preview.
 */
class TFTCommandBuilder {
    constructor(onInsert) {
        this.onInsert = onInsert; // callback(codeLine, commandDef, paramValues)
        this._panel = null;
        this._previewCtx = null;
        this._currentCmd = null;
        this._values = {};
        this._build();
    }

    _build() {
        // Create panel element
        this._panel = document.createElement('div');
        this._panel.id = 'tft-builder-panel';
        this._panel.style.cssText = `
            position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
            background: var(--card-bg, #1e293b);
            border: 1px solid var(--primary, #38bdf8);
            border-radius: 16px; padding: 0;
            width: 520px; max-height: 90vh; overflow: hidden;
            box-shadow: 0 25px 60px rgba(0,0,0,0.6), 0 0 0 1px rgba(56,189,248,0.2);
            display: none; z-index: 9999; flex-direction: row;
            font-family: 'Outfit', sans-serif;
        `;

        this._panel.innerHTML = `
            <div style="flex:1;display:flex;flex-direction:column;overflow:hidden;">
                <!-- Header -->
                <div style="padding:14px 16px;background:rgba(56,189,248,0.08);border-bottom:1px solid var(--border,#334155);display:flex;align-items:center;gap:10px;">
                    <i id="tft-builder-icon" data-lucide="square" style="width:20px;height:20px;color:var(--primary,#38bdf8);"></i>
                    <div>
                        <div id="tft-builder-title" style="font-size:0.9rem;font-weight:800;color:var(--primary,#38bdf8);">fillRect</div>
                        <div id="tft-builder-hint"  style="font-size:0.65rem;color:var(--text-muted,#94a3b8);">Retângulo preenchido</div>
                    </div>
                    <button id="tft-builder-close" style="margin-left:auto;background:none;border:none;cursor:pointer;color:var(--text-muted,#94a3b8);font-size:1.2rem;line-height:1;">&times;</button>
                </div>

                <!-- Params -->
                <div id="tft-builder-params" style="padding:16px;overflow-y:auto;flex:1;display:grid;grid-template-columns:1fr 1fr;gap:10px;"></div>

                <!-- Generated code -->
                <div style="padding:0 16px 10px;">
                    <div style="font-size:0.6rem;color:var(--primary,#38bdf8);font-weight:800;text-transform:uppercase;letter-spacing:1px;margin-bottom:6px;">Código gerado</div>
                    <pre id="tft-builder-code" style="
                        background:#0f172a;border:1px solid var(--border,#334155);border-radius:8px;
                        padding:8px 12px;font-size:0.7rem;font-family:'JetBrains Mono',monospace;
                        color:#4ade80;margin:0;overflow-x:auto;white-space:pre;
                    "></pre>
                </div>

                <!-- Footer -->
                <div style="padding:12px 16px;border-top:1px solid var(--border,#334155);display:flex;gap:8px;">
                    <button id="tft-builder-insert" style="
                        flex:1;padding:10px;background:linear-gradient(135deg,#3b82f6,#38bdf8);
                        border:none;border-radius:8px;color:white;font-weight:800;font-size:0.8rem;
                        cursor:pointer;display:flex;align-items:center;justify-content:center;gap:6px;
                    ">
                        <i data-lucide="plus" style="width:14px;"></i> Inserir no Editor
                    </button>
                    <button id="tft-builder-cancel" style="
                        padding:10px 16px;background:transparent;border:1px solid var(--border,#334155);
                        border-radius:8px;color:var(--text-muted,#94a3b8);font-size:0.8rem;cursor:pointer;
                    ">Cancelar</button>
                </div>
            </div>

            <!-- Mini Preview -->
            <div style="width:180px;flex-shrink:0;background:#000;border-left:1px solid var(--border,#334155);display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px;padding:16px;">
                <div style="font-size:0.6rem;color:var(--text-muted,#94a3b8);text-transform:uppercase;letter-spacing:1px;">Preview 240×240</div>
                <canvas id="tft-builder-canvas" width="240" height="240"
                    style="width:148px;height:148px;border:1px solid #334155;border-radius:4px;background:#111;image-rendering:pixelated;"></canvas>
                <div style="font-size:0.55rem;color:var(--text-muted,#94a3b8);text-align:center;">Atualiza em tempo real</div>
            </div>
        `;

        document.body.appendChild(this._panel);

        // Wire events
        document.getElementById('tft-builder-close').onclick = () => this.hide();
        document.getElementById('tft-builder-cancel').onclick = () => this.hide();
        document.getElementById('tft-builder-insert').onclick = () => this._insert();

        this._previewCtx = document.getElementById('tft-builder-canvas').getContext('2d');
    }

    show(cmdId) {
        const cmd = TFT_COMMANDS.find(c => c.id === cmdId);
        if (!cmd) return;
        this._currentCmd = cmd;
        this._values = {};
        cmd.params.forEach(p => { this._values[p.name] = p.default; });

        // Update header
        document.getElementById('tft-builder-title').textContent = cmd.label;
        document.getElementById('tft-builder-hint').textContent = cmd.hint;
        const iconEl = document.getElementById('tft-builder-icon');
        iconEl.setAttribute('data-lucide', cmd.icon);

        // Build param form
        const container = document.getElementById('tft-builder-params');
        container.innerHTML = '';
        cmd.params.forEach(p => {
            const wrap = document.createElement('div');
            // label + name span full width if it's text
            if (p.name === 'label' || p.name === 'text' || p.name === 'arrayName') {
                wrap.style.gridColumn = '1 / -1';
            }
            wrap.innerHTML = `
                <div class="input-field compact">
                    <span>${p.label}</span>
                    ${p.type === 'color'
                    ? `<input type="color" data-param="${p.name}" value="${p.default}">`
                    : `<input type="${p.type === 'number' ? 'number' : 'text'}" data-param="${p.name}" value="${p.default}">`
                }
                </div>
            `;
            container.appendChild(wrap);
        });

        // Wire param inputs
        container.querySelectorAll('input').forEach(input => {
            input.oninput = () => {
                this._values[input.dataset.param] = input.value;
                this._updatePreview();
                this._updateCode();
            };
        });

        this._panel.style.display = 'flex';
        this._updatePreview();
        this._updateCode();
        if (window.lucide) window.lucide.createIcons();
    }

    hide() {
        this._panel.style.display = 'none';
    }

    _updatePreview() {
        const ctx = this._previewCtx;
        ctx.fillStyle = '#111'; ctx.fillRect(0, 0, 240, 240);
        // Draw a faint grid
        ctx.strokeStyle = 'rgba(255,255,255,0.04)'; ctx.lineWidth = 1;
        for (let i = 0; i < 240; i += 20) {
            ctx.beginPath(); ctx.moveTo(i, 0); ctx.lineTo(i, 240); ctx.stroke();
            ctx.beginPath(); ctx.moveTo(0, i); ctx.lineTo(240, i); ctx.stroke();
        }
        // Draw the command preview
        try { this._currentCmd?.preview(ctx, this._values); } catch (e) { /* ignore bad input */ }
    }

    _colorValue(hex) {
        // Convert hex color to 0xRRGG (RGB565)
        if (!hex || !hex.startsWith('#')) return '0x0000';
        const r = parseInt(hex.slice(1, 3), 16);
        const g = parseInt(hex.slice(3, 5), 16);
        const b = parseInt(hex.slice(5, 7), 16);
        const v = ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);
        return '0x' + v.toString(16).toUpperCase().padStart(4, '0');
    }

    _resolvedParams() {
        // Make a copy with color converted to RGB565
        const out = { ...this._values };
        this._currentCmd?.params.forEach(p => {
            if (p.type === 'color') out[p.name] = this._colorValue(this._values[p.name]);
        });
        return out;
    }

    _updateCode() {
        const codeEl = document.getElementById('tft-builder-code');
        if (!this._currentCmd || !codeEl) return;
        try {
            codeEl.textContent = this._currentCmd.template(this._resolvedParams());
        } catch (e) { codeEl.textContent = '// (preencha os parâmetros)'; }
    }

    _insert() {
        if (!this._currentCmd) return;
        const codeLine = this._currentCmd.template(this._resolvedParams());
        this.onInsert?.(codeLine, this._currentCmd, this._values);
        this.hide();
    }
}

if (window.PixelDisplay240System) window.PixelDisplay240System.register('tft', '5.1.0');

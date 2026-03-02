// ========== Window & Workspace Manager ==========

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

class WindowManager extends EditorModule {
    constructor(app) {
        super();
        this.app = app;
        this.windows = new Map();
        this.activeWindow = null;
        this.resizingWindow = null;
        this.dragOffset = { x: 0, y: 0 };
        this.resizeStart = { w: 0, h: 0, x: 0, y: 0 };
        this._setupGlobalEvents();
        this._loadLayout();
    }

    _setupGlobalEvents() {
        window.addEventListener('mousemove', (e) => this._onDrag(e));
        window.addEventListener('mouseup', () => this._stopDrag());

        // Setup Sidebar Collapse Triggers per view panel
        setTimeout(() => {
            document.querySelectorAll('.view-panel').forEach(view => {
                // Safety: remove existing
                view.querySelectorAll('.collapse-trigger').forEach(t => t.remove());

                const leftTrigger = document.createElement('div');
                leftTrigger.className = 'collapse-trigger collapse-trigger-left';
                leftTrigger.innerHTML = '<i data-lucide="chevron-left"></i>';
                leftTrigger.onclick = (e) => { e.stopPropagation(); this.toggleSidebar('left'); };

                const rightTrigger = document.createElement('div');
                rightTrigger.className = 'collapse-trigger collapse-trigger-right';
                rightTrigger.innerHTML = '<i data-lucide="chevron-right"></i>';
                rightTrigger.onclick = (e) => { e.stopPropagation(); this.toggleSidebar('right'); };

                view.appendChild(leftTrigger);
                view.appendChild(rightTrigger);
            });
            if (typeof lucide !== 'undefined') lucide.createIcons();
        }, 1000);
    }

    createWindow(id, title, content, x = 100, y = 100, w = 350, h = 300) {
        if (this.windows.has(id)) {
            this.focusWindow(id);
            return;
        }

        const win = document.createElement('div');
        win.className = 'floating-window';
        win.id = `win-${id}`;
        win.style.left = x + 'px';
        win.style.top = y + 'px';
        win.style.width = w + 'px';
        win.style.height = h + 'px';

        // Store original position if content is an element
        let originalParent = null;
        let originalSibling = null;
        let borrowedElement = null;

        if (content instanceof HTMLElement) {
            borrowedElement = content;
            originalParent = content.parentElement;
            originalSibling = content.nextSibling;
        }

        win.innerHTML = `
            <div class="window-header">
                <span class="window-title">${title}</span>
                <div class="window-controls">
                    <button class="win-btn win-close"><i data-lucide="x"></i></button>
                </div>
            </div>
            <div class="window-content"></div>
            <div class="window-resizer" style="position:absolute; right:0; bottom:0; width:15px; height:15px; cursor:nwse-resize; background: linear-gradient(135deg, transparent 50%, var(--primary) 50%); opacity:0.3; border-radius:0 0 12px 0;"></div>
        `;

        const header = win.querySelector('.window-header');
        header.onmousedown = (e) => this._startDrag(id, win, e);

        const resizer = win.querySelector('.window-resizer');
        resizer.onmousedown = (e) => this._startResize(id, win, e);

        const closeBtn = win.querySelector('.win-close');
        closeBtn.onclick = (e) => { e.stopPropagation(); this.closeWindow(id); };

        win.onmousedown = () => this.focusWindow(id);

        const contentArea = win.querySelector('.window-content');
        if (typeof content === 'string') {
            contentArea.innerHTML = content;
        } else {
            contentArea.appendChild(content);
        }

        document.body.appendChild(win);
        this.windows.set(id, {
            el: win,
            borrowed: borrowedElement,
            parent: originalParent,
            sibling: originalSibling
        });

        this.focusWindow(id);
        if (typeof lucide !== 'undefined') lucide.createIcons();
        this._saveLayout();
    }

    _startDrag(id, win, e) {
        this.activeWindow = id;
        const rect = win.getBoundingClientRect();
        this.dragOffset.x = e.clientX - rect.left;
        this.dragOffset.y = e.clientY - rect.top;
        win.style.transition = 'none';
        this.focusWindow(id);
    }

    _startResize(id, win, e) {
        e.stopPropagation();
        e.preventDefault();
        this.resizingWindow = id;
        this.resizeStart = {
            w: win.offsetWidth,
            h: win.offsetHeight,
            x: e.clientX,
            y: e.clientY
        };
        this.focusWindow(id);
    }

    _onDrag(e) {
        if (this.resizingWindow) {
            const entry = this.windows.get(this.resizingWindow);
            if (!entry) return;
            const win = entry.el;
            const dw = e.clientX - this.resizeStart.x;
            const dh = e.clientY - this.resizeStart.y;
            win.style.width = Math.max(200, this.resizeStart.w + dw) + 'px';
            win.style.height = Math.max(100, this.resizeStart.h + dh) + 'px';
            return;
        }

        if (!this.activeWindow) return;
        const entry = this.windows.get(this.activeWindow);
        if (!entry) return;
        const win = entry.el;
        let x = e.clientX - this.dragOffset.x;
        let y = e.clientY - this.dragOffset.y;

        // Viewport bounds
        x = Math.max(0, Math.min(window.innerWidth - 100, x));
        y = Math.max(70, Math.min(window.innerHeight - 100, y));

        win.style.left = x + 'px';
        win.style.top = y + 'px';
    }

    _stopDrag() {
        if (this.activeWindow) {
            const entry = this.windows.get(this.activeWindow);
            if (entry) entry.el.style.transition = '';
        }
        this.activeWindow = null;
        this.resizingWindow = null;
        this._saveLayout();
    }

    focusWindow(id) {
        const entry = this.windows.get(id);
        if (!entry) return;

        this.windows.forEach(w => w.el.style.zIndex = '1000');
        entry.el.style.zIndex = '2000';
    }

    closeWindow(id) {
        const entry = this.windows.get(id);
        if (entry) {
            // Restore borrowed element
            if (entry.borrowed && entry.parent) {
                if (entry.sibling) entry.parent.insertBefore(entry.borrowed, entry.sibling);
                else entry.parent.appendChild(entry.borrowed);
            }
            entry.el.remove();
            this.windows.delete(id);
            this._saveLayout();
        }
    }

    toggleSidebar(side) {
        const selector = side === 'left' ? '.toolbar' : '.sidebar';
        const activeView = document.querySelector('.view-panel.active');
        if (!activeView) return;

        const targetPanel = activeView.querySelector(selector);
        if (!targetPanel) return;

        const shouldCollapse = !targetPanel.classList.contains('collapsed');

        // Sync ALL sidebars of this type across ALL views for consistency
        document.querySelectorAll(selector).forEach(p => {
            p.classList.toggle('collapsed', shouldCollapse);
        });

        // Update all triggers for this side across all views
        document.querySelectorAll(`.collapse-trigger-${side}`).forEach(trigger => {
            trigger.innerHTML = side === 'left'
                ? `<i data-lucide="chevron-${shouldCollapse ? 'right' : 'left'}"></i>`
                : `<i data-lucide="chevron-${shouldCollapse ? 'left' : 'right'}"></i>`;
        });

        if (typeof lucide !== 'undefined') lucide.createIcons();
        if (this.app.prototype && this.app.prototype.view && this.app.prototype.view.editor) {
            setTimeout(() => this.app.prototype.view.editor.layout(), 200);
        }
    }

    _saveLayout() {
        const layout = {};
        this.windows.forEach((entry, id) => {
            layout[id] = {
                x: entry.el.style.left,
                y: entry.el.style.top,
                w: entry.el.style.width,
                h: entry.el.style.height
            };
        });
        localStorage.setItem('pixeldisplay_layout', JSON.stringify(layout));
    }

    _loadLayout() {
        // Future: Re-open windows based on stored state
    }
}

if (window.PixelDisplay240System) window.PixelDisplay240System.register('window-manager', '5.1.0');

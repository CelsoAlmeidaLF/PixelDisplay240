/**
 * Prototype Model - Now synchronized with C# Backend
 */
class PrototypeModel {
    constructor() {
        this.screens = [];
        this.activeScreenId = null;
        this.selectedElementId = null;
        this.assets = [];

        // Tenta carregar do cache local imediatamente para velocidade
        this.loadFromLocal();
    }

    get activeScreen() {
        return this.screens.find(s => s.id === this.activeScreenId);
    }

    /**
     * Updates local state from server data
     */
    updateFromData(data) {
        if (!data) return;
        this.screens = data.screens || [];
        this.activeScreenId = data.activeScreenId;
        this.selectedElementId = data.selectedElementId;
        this.assets = data.assets || [];
        this.saveToLocal();
    }

    saveToLocal() {
        try {
            const state = {
                screens: this.screens,
                activeScreenId: this.activeScreenId,
                selectedElementId: this.selectedElementId,
                assets: this.assets
            };
            localStorage.setItem('pixeldisplay240_proto_state', JSON.stringify(state));
        } catch (e) {
            console.warn('Falha ao salvar no LocalStorage:', e);
        }
    }

    loadFromLocal() {
        try {
            const saved = localStorage.getItem('pixeldisplay240_proto_state');
            if (saved) {
                const data = JSON.parse(saved);
                this.screens = data.screens || [];
                this.activeScreenId = data.activeScreenId;
                this.selectedElementId = data.selectedElementId;
                this.assets = data.assets || [];
            }
        } catch (e) {
            console.warn('Falha ao carregar do LocalStorage:', e);
        }
    }

    getRawData() {
        return {
            screens: this.screens,
            activeScreenId: this.activeScreenId,
            selectedElementId: this.selectedElementId,
            assets: this.assets
        };
    }
}

if (window.PixelDisplay240System) window.PixelDisplay240System.register('model', '5.1.0');

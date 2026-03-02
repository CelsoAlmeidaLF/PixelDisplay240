/**
 * Prototype Model - Data structure for screens, elements and assets.
 */
class PrototypeModel {
    constructor() {
        this.screens = [];
        this.assets = [];
        this.activeScreenId = null;
        this.selectedElementId = null;
        this.screenSeq = 1;
        this.elementSeq = 1;

        this.loadFromLocal();
    }

    get activeScreen() {
        return this.screens.find(s => s.id === this.activeScreenId);
    }

    updateFromData(data) {
        if (!data) return;
        this.screens = data.screens || [];
        this.assets = data.assets || [];
        this.activeScreenId = data.activeScreenId || (this.screens[0]?.id);
        this.screenSeq = data.screenSeq || 1;
        this.elementSeq = data.elementSeq || 1;
    }

    getRawData() {
        return {
            screens: this.screens,
            assets: this.assets,
            activeScreenId: this.activeScreenId,
            screenSeq: this.screenSeq,
            elementSeq: this.elementSeq
        };
    }

    saveToLocal() {
        localStorage.setItem('proto_project_cache', JSON.stringify(this.getRawData()));
    }

    loadFromLocal() {
        const cache = localStorage.getItem('proto_project_cache');
        if (cache) {
            try { this.updateFromData(JSON.parse(cache)); } catch (e) { }
        }
    }
}

if (window.PixelDisplay240System) window.PixelDisplay240System.register('model', '5.1.0');

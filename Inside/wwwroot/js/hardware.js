/**
 * Hardware Manager - Handles device discovery, exports and serial communication
 */
class HardwareManager {
    constructor(app) {
        this.app = app;
        this.devices = [];
        this.selectedDevice = null;
    }

    async scan() {
        console.log("Scanning for devices...");
        try {
            const resp = await this.app.api.request('/api/hardware/scan');
            this.devices = await resp.json();
            return this.devices;
        } catch (err) {
            console.error("Scan failed", err);
            return [];
        }
    }

    async exportProject() {
        const project = this.app.prototype.model.getRawData();
        try {
            const resp = await this.app.api.request('/api/hardware/export', {
                method: 'POST',
                body: JSON.stringify(project)
            });
            return await resp.blob();
        } catch (err) {
            console.error("Export failed", err);
            return null;
        }
    }
}

class HardwareConfig {
    constructor() {
        this.pinSCLK = 18;
        this.pinMOSI = 23;
        this.pinMISO = 19;
        this.pinCS = 5;
        this.pinDC = 2;
        this.pinRST = 4;
    }
}

if (window.PixelDisplay240System) window.PixelDisplay240System.register('hardware', '5.1.0');

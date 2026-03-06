/**
 * TFT Commands Module - Defines available TFT_eSPI commands and their metadata
 * Used for command palette, auto-complete, and code generation.
 */

// TFT Named Color Constants (RGB565 values)
const TFT_COLORS = {
    TFT_BLACK: 0x0000,
    TFT_NAVY: 0x000F,
    TFT_DARKGREEN: 0x03E0,
    TFT_DARKCYAN: 0x03EF,
    TFT_MAROON: 0x7800,
    TFT_PURPLE: 0x780F,
    TFT_OLIVE: 0x7BE0,
    TFT_LIGHTGREY: 0xC618,
    TFT_DARKGREY: 0x7BEF,
    TFT_BLUE: 0x001F,
    TFT_GREEN: 0x07E0,
    TFT_CYAN: 0x07FF,
    TFT_RED: 0xF800,
    TFT_MAGENTA: 0xF81F,
    TFT_YELLOW: 0xFFE0,
    TFT_WHITE: 0xFFFF,
    TFT_ORANGE: 0xFDA0,
    TFT_GREENYELLOW: 0xB7E0,
    TFT_PINK: 0xFE19,
    TFT_BROWN: 0x9A60,
    TFT_GOLD: 0xFEA0,
    TFT_SILVER: 0xC618,
    TFT_SKYBLUE: 0x867D,
    TFT_VIOLET: 0x915C
};

// TFT Command Definitions
const TFT_COMMANDS = {
    // Primitive Shapes - Filled
    fillRect: {
        name: 'fillRect',
        description: 'Draw a filled rectangle',
        category: 'shapes',
        params: ['x', 'y', 'width', 'height', 'color'],
        syntax: 'tft.fillRect(x, y, w, h, color)',
        example: 'tft.fillRect(10, 10, 100, 50, TFT_BLUE);'
    },
    fillCircle: {
        name: 'fillCircle',
        description: 'Draw a filled circle',
        category: 'shapes',
        params: ['centerX', 'centerY', 'radius', 'color'],
        syntax: 'tft.fillCircle(cx, cy, r, color)',
        example: 'tft.fillCircle(120, 120, 40, TFT_RED);'
    },
    fillRoundRect: {
        name: 'fillRoundRect',
        description: 'Draw a filled rectangle with rounded corners',
        category: 'shapes',
        params: ['x', 'y', 'width', 'height', 'radius', 'color'],
        syntax: 'tft.fillRoundRect(x, y, w, h, r, color)',
        example: 'tft.fillRoundRect(20, 20, 100, 40, 10, TFT_GREEN);'
    },
    fillTriangle: {
        name: 'fillTriangle',
        description: 'Draw a filled triangle',
        category: 'shapes',
        params: ['x0', 'y0', 'x1', 'y1', 'x2', 'y2', 'color'],
        syntax: 'tft.fillTriangle(x0, y0, x1, y1, x2, y2, color)',
        example: 'tft.fillTriangle(60, 10, 10, 100, 110, 100, TFT_YELLOW);'
    },
    fillEllipse: {
        name: 'fillEllipse',
        description: 'Draw a filled ellipse',
        category: 'shapes',
        params: ['centerX', 'centerY', 'radiusX', 'radiusY', 'color'],
        syntax: 'tft.fillEllipse(cx, cy, rx, ry, color)',
        example: 'tft.fillEllipse(120, 120, 60, 30, TFT_PURPLE);'
    },
    fillScreen: {
        name: 'fillScreen',
        description: 'Fill the entire screen with a color',
        category: 'screen',
        params: ['color'],
        syntax: 'tft.fillScreen(color)',
        example: 'tft.fillScreen(TFT_BLACK);'
    },

    // Primitive Shapes - Outlined
    drawRect: {
        name: 'drawRect',
        description: 'Draw a rectangle outline',
        category: 'shapes',
        params: ['x', 'y', 'width', 'height', 'color'],
        syntax: 'tft.drawRect(x, y, w, h, color)',
        example: 'tft.drawRect(10, 10, 100, 50, TFT_WHITE);'
    },
    drawCircle: {
        name: 'drawCircle',
        description: 'Draw a circle outline',
        category: 'shapes',
        params: ['centerX', 'centerY', 'radius', 'color'],
        syntax: 'tft.drawCircle(cx, cy, r, color)',
        example: 'tft.drawCircle(120, 120, 40, TFT_CYAN);'
    },
    drawRoundRect: {
        name: 'drawRoundRect',
        description: 'Draw a rounded rectangle outline',
        category: 'shapes',
        params: ['x', 'y', 'width', 'height', 'radius', 'color'],
        syntax: 'tft.drawRoundRect(x, y, w, h, r, color)',
        example: 'tft.drawRoundRect(20, 20, 100, 40, 10, TFT_ORANGE);'
    },
    drawTriangle: {
        name: 'drawTriangle',
        description: 'Draw a triangle outline',
        category: 'shapes',
        params: ['x0', 'y0', 'x1', 'y1', 'x2', 'y2', 'color'],
        syntax: 'tft.drawTriangle(x0, y0, x1, y1, x2, y2, color)',
        example: 'tft.drawTriangle(60, 10, 10, 100, 110, 100, TFT_PINK);'
    },
    drawEllipse: {
        name: 'drawEllipse',
        description: 'Draw an ellipse outline',
        category: 'shapes',
        params: ['centerX', 'centerY', 'radiusX', 'radiusY', 'color'],
        syntax: 'tft.drawEllipse(cx, cy, rx, ry, color)',
        example: 'tft.drawEllipse(120, 120, 60, 30, TFT_GOLD);'
    },

    // Lines
    drawLine: {
        name: 'drawLine',
        description: 'Draw a line between two points',
        category: 'lines',
        params: ['x0', 'y0', 'x1', 'y1', 'color'],
        syntax: 'tft.drawLine(x0, y0, x1, y1, color)',
        example: 'tft.drawLine(0, 0, 240, 240, TFT_WHITE);'
    },
    drawFastHLine: {
        name: 'drawFastHLine',
        description: 'Draw a fast horizontal line',
        category: 'lines',
        params: ['x', 'y', 'width', 'color'],
        syntax: 'tft.drawFastHLine(x, y, w, color)',
        example: 'tft.drawFastHLine(10, 120, 220, TFT_GREEN);'
    },
    drawFastVLine: {
        name: 'drawFastVLine',
        description: 'Draw a fast vertical line',
        category: 'lines',
        params: ['x', 'y', 'height', 'color'],
        syntax: 'tft.drawFastVLine(x, y, h, color)',
        example: 'tft.drawFastVLine(120, 10, 220, TFT_BLUE);'
    },

    // Pixels
    drawPixel: {
        name: 'drawPixel',
        description: 'Draw a single pixel',
        category: 'pixels',
        params: ['x', 'y', 'color'],
        syntax: 'tft.drawPixel(x, y, color)',
        example: 'tft.drawPixel(120, 120, TFT_RED);'
    },

    // Text
    drawString: {
        name: 'drawString',
        description: 'Draw a text string at position',
        category: 'text',
        params: ['text', 'x', 'y'],
        syntax: 'tft.drawString("text", x, y)',
        example: 'tft.drawString("Hello", 10, 10);'
    },
    drawCentreString: {
        name: 'drawCentreString',
        description: 'Draw centered text at x position',
        category: 'text',
        params: ['text', 'x', 'y', 'font'],
        syntax: 'tft.drawCentreString("text", x, y, font)',
        example: 'tft.drawCentreString("Title", 120, 10, 2);'
    },
    setTextColor: {
        name: 'setTextColor',
        description: 'Set the text foreground color',
        category: 'text',
        params: ['color'],
        syntax: 'tft.setTextColor(color)',
        example: 'tft.setTextColor(TFT_WHITE);'
    },
    setTextSize: {
        name: 'setTextSize',
        description: 'Set text size multiplier (1-7)',
        category: 'text',
        params: ['size'],
        syntax: 'tft.setTextSize(size)',
        example: 'tft.setTextSize(2);'
    },

    // Images
    pushImage: {
        name: 'pushImage',
        description: 'Push an image array to the display',
        category: 'images',
        params: ['x', 'y', 'width', 'height', 'data'],
        syntax: 'tft.pushImage(x, y, w, h, imageArray)',
        example: 'tft.pushImage(0, 0, 240, 240, my_image);'
    }
};

// Command Categories for UI grouping
const TFT_CATEGORIES = {
    shapes: {
        name: 'Shapes',
        icon: 'square',
        description: 'Rectangles, circles, triangles, ellipses'
    },
    lines: {
        name: 'Lines',
        icon: 'minus',
        description: 'Lines and fast drawing primitives'
    },
    text: {
        name: 'Text',
        icon: 'type',
        description: 'Text rendering and fonts'
    },
    images: {
        name: 'Images',
        icon: 'image',
        description: 'Bitmap and sprite rendering'
    },
    pixels: {
        name: 'Pixels',
        icon: 'grid',
        description: 'Individual pixel manipulation'
    },
    screen: {
        name: 'Screen',
        icon: 'monitor',
        description: 'Screen-level operations'
    }
};

/**
 * Utility: Convert hex color to RGB565 value
 * @param {string} hex - Hex color string (#RRGGBB)
 * @returns {number} RGB565 value
 */
function hexToRGB565(hex) {
    if (!hex || hex.length < 7) return 0;
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    return ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);
}

/**
 * Utility: Convert RGB565 value to hex color
 * @param {number} rgb565 - RGB565 value
 * @returns {string} Hex color string (#RRGGBB)
 */
function rgb565ToHex(rgb565) {
    const r = Math.round(((rgb565 >> 11) & 0x1F) * 255 / 31);
    const g = Math.round(((rgb565 >> 5) & 0x3F) * 255 / 63);
    const b = Math.round((rgb565 & 0x1F) * 255 / 31);
    return '#' + [r, g, b].map(v => v.toString(16).padStart(2, '0')).join('');
}

/**
 * Get color name from RGB565 value if it matches a predefined color
 * @param {number} rgb565 - RGB565 value
 * @returns {string|null} Color name or null
 */
function getColorName(rgb565) {
    for (const [name, value] of Object.entries(TFT_COLORS)) {
        if (value === rgb565) return name;
    }
    return null;
}

/**
 * Generate code snippet for a command
 * @param {string} commandName - Command name
 * @param {Object} params - Parameter values
 * @returns {string} Code snippet
 */
function generateCommandCode(commandName, params = {}) {
    const cmd = TFT_COMMANDS[commandName];
    if (!cmd) return '';

    let code = `tft.${commandName}(`;
    const args = cmd.params.map(p => params[p] ?? 0);
    code += args.join(', ');
    code += ');';

    return code;
}

// Export for use in other modules
window.TFT_COLORS = TFT_COLORS;
window.TFT_COMMANDS = TFT_COMMANDS;
window.TFT_CATEGORIES = TFT_CATEGORIES;
window.hexToRGB565 = hexToRGB565;
window.rgb565ToHex = rgb565ToHex;
window.getColorName = getColorName;
window.generateCommandCode = generateCommandCode;

/**
 * TFT Command Toolbar - Injectable commands for the Monaco Editor
 */
class TftCommandToolbar {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        this.editor = null;
    }

    init(editor) {
        this.editor = editor;
        if (!this.container) return;

        this.container.querySelectorAll('.tft-cmd-btn').forEach(btn => {
            btn.onclick = () => {
                const cmd = btn.dataset.cmd;
                const snippet = this.getSnippet(cmd);
                if (snippet) this.injectSnippet(snippet);
            };
        });
    }

    injectSnippet(text) {
        if (!this.editor) return;

        // Monaco Editor Detection
        if (this.editor.getPosition) {
            const selection = this.editor.getSelection();
            const range = new monaco.Range(selection.startLineNumber, selection.startColumn, selection.endLineNumber, selection.endColumn);
            const id = { major: 1, minor: 1 };
            const op = { identifier: id, range: range, text: text, forceMoveMarkers: true };
            this.editor.executeEdits("my-source", [op]);
        } else {
            // Standard Textarea fallback
            const start = this.editor.selectionStart;
            const end = this.editor.selectionEnd;
            const val = this.editor.value;
            this.editor.value = val.substring(0, start) + text + val.substring(end);
            this.editor.dispatchEvent(new Event('input'));
        }
    }

    getSnippet(cmd) {
        const snippets = {
            'fillRect': 'tft.fillRect(10, 10, 50, 50, TFT_RED); // Square\n',
            'drawRect': 'tft.drawRect(10, 10, 50, 50, TFT_WHITE); // Border\n',
            'fillCircle': 'tft.fillCircle(120, 120, 30, TFT_BLUE); // Circle\n',
            'fillRoundRect': 'tft.fillRoundRect(20, 20, 60, 40, 8, TFT_GREEN); // Rounded\n',
            'drawString': 'tft.setTextColor(TFT_WHITE);\ntft.drawString("Hello!", 10, 10, 2);\n',
            'pushImage': 'tft.pushImage(0, 0, 240, 240, my_image_array);\n',
            'drawLine': 'tft.drawLine(0, 0, 240, 240, TFT_SILVER);\n'
        };
        return snippets[cmd] || `// Command ${cmd} not implemented\n`;
    }
}

// Register with the system orchestrator
if (window.PixelDisplay240System) {
    window.PixelDisplay240System.register('tft', '5.1.0');
}

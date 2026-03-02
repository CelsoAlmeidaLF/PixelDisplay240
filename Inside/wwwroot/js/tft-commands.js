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

if (window.PixelDisplay240System) window.PixelDisplay240System.register('tft', '5.1.0');

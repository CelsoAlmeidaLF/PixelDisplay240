using PixelDisplay240Api.Models;
using PixelDisplay240Api.Domain;

namespace PixelDisplay240Api.Services
{
    public class PrototypeService
    {
        private MasterPrototype _master;

        public PrototypeService()
        {
            _master = new MasterPrototype(new PrototypeProject());
        }

        public PrototypeProject GetProject() => _master.Project;

        public PrototypeScreen AddScreen(string? name = null, string? template = null)
        {
            var screen = _master.AddScreen(name);

            // Template Logic
            if (!string.IsNullOrEmpty(template))
            {
                ApplyTemplate(screen, template);
            }

            return screen;
        }

        private void ApplyTemplate(PrototypeScreen screen, string template)
        {
            switch (template.ToLower())
            {
                case "loading":
                    _master.AddElement(screen.Id, "fillScreen", null).Color = "#000000";
                    _master.AddElement(screen.Id, "drawCentreString", null).Name = "Carregando...";
                    _master.AddElement(screen.Id, "drawRect", null).Name = "ProgressBarBorder";
                    break;
                case "menu":
                    _master.AddElement(screen.Id, "fillRect", null).Name = "HeaderBg";
                    _master.AddScreen("SubMenu");
                    break;
                // ... outros templates simplificados ou mantidos conforme a lógica original
            }
        }

        public bool DeleteScreen(string id) => _master.RemoveScreen(id);

        public void SelectScreen(string id)
        {
            if (_master.GetScreen(id) != null)
            {
                _master.Project.ActiveScreenId = id;
                _master.Project.SelectedElementId = null;
            }
        }

        public void MoveScreen(string screenId, int newIndex)
        {
            var screen = _master.GetScreen(screenId);
            if (screen == null) return;

            _master.Project.Screens.Remove(screen);
            int target = Math.Max(0, Math.Min(newIndex, _master.Project.Screens.Count));
            _master.Project.Screens.Insert(target, screen);
        }

        public PrototypeElement? AddElement(string screenId, string type, string? asset = null)
        {
            return _master.AddElement(screenId, type, asset);
        }

        public bool DeleteElement(string screenId, string elId)
        {
            return _master.RemoveElement(elId);
        }

        public void PatchElement(string screenId, string elId, Dictionary<string, object?> patch)
        {
            var el = _master.FindElement(elId);
            if (el == null) return;

            foreach (var kv in patch)
            {
                var val = kv.Value?.ToString();
                switch (kv.Key.ToLower())
                {
                    case "name":   if (!string.IsNullOrEmpty(val)) el.Name = val; break;
                    case "color":  if (!string.IsNullOrEmpty(val)) el.Color = val; break;
                    case "asset":  el.Asset = val; break;
                    case "targetscreenid": el.TargetScreenId = val; break;
                    case "x": if (int.TryParse(val, out var x)) el.X = x; break;
                    case "y": if (int.TryParse(val, out var y)) el.Y = y; break;
                    case "w": if (int.TryParse(val, out var w) && w > 0) el.W = w; break;
                    case "h": if (int.TryParse(val, out var h) && h > 0) el.H = h; break;
                }
            }
        }

        public void MoveElement(string screenId, string elId, int newIndex)
        {
            var screen = _master.GetScreen(screenId);
            var el = _master.FindElement(elId);
            if (screen == null || el == null) return;

            screen.Elements.Remove(el);
            int target = Math.Max(0, Math.Min(newIndex, screen.Elements.Count));
            screen.Elements.Insert(target, el);
        }

        public void AddAsset(PrototypeAsset asset) => _master.AddAsset(asset);

        public void DeleteAsset(string name)
        {
            var asset = _master.Project.Assets.FirstOrDefault(a => a.Name == name);
            if (asset == null) return;
            
            _master.Project.Assets.Remove(asset);
            foreach(var s in _master.Project.Screens)
            {
                if (s.BackgroundAsset == name) { s.BackgroundAsset = null; s.Background = null; }
                foreach(var el in s.Elements) { if (el.Asset == name) el.Asset = null; }
            }
        }

        public void UpdateScreenBackground(string screenId, string? assetName, string? dataUrl)
        {
            var screen = _master.GetScreen(screenId);
            if (screen == null) return;
            screen.BackgroundAsset = assetName;
            screen.Background = dataUrl;
        }

        public void UpdateScreen(string screenId, Dictionary<string, object?> updates)
        {
            var screen = _master.GetScreen(screenId);
            if (screen == null) return;

            foreach (var kv in updates)
            {
                var val = kv.Value?.ToString();
                switch (kv.Key.ToLower())
                {
                    case "name": if (!string.IsNullOrEmpty(val)) screen.Name = val; break;
                    case "backgroundcolor": screen.BackgroundColor = val; break;
                }
            }
        }

        public void SaveProject(PrototypeProject newProject)
        {
            if (newProject == null) return;
            _master = new MasterPrototype(newProject);
            Console.WriteLine($"[Service] Entidade Mestre recarregada com sucesso. Telas: {newProject.Screens.Count}");
        }
    }
}

using PixelDisplay240Api.Models;
using System.Collections.Concurrent;

namespace PixelDisplay240Api.Domain
{
    /// <summary>
    /// Aggregate Root para o Projeto de Prototipagem.
    /// Centraliza todas as regras de negócio, sequenciamento e busca global.
    /// </summary>
    public class MasterPrototype
    {
        private PrototypeProject _project;
        private readonly ConcurrentDictionary<string, PrototypeElement> _elementIndex = new();
        private readonly ConcurrentDictionary<string, PrototypeScreen> _screenIndex = new();

        public MasterPrototype(PrototypeProject project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            RebuildIndexes();
        }

        public PrototypeProject Project => _project;

        /// <summary>
        /// Reconstrói os índices internos para busca rápida.
        /// </summary>
        public void RebuildIndexes()
        {
            _elementIndex.Clear();
            _screenIndex.Clear();

            foreach (var screen in _project.Screens)
            {
                _screenIndex[screen.Id] = screen;
                foreach (var el in screen.Elements)
                {
                    _elementIndex[el.Id] = el;
                }
            }
        }

        #region Screen Operations

        public PrototypeScreen AddScreen(string? name = null)
        {
            var id = $"screen_{_project.ScreenSeq++}";
            var screen = new PrototypeScreen
            {
                Id = id,
                Name = name ?? $"Tela_{_project.Screens.Count + 1}"
            };

            _project.Screens.Add(screen);
            _screenIndex[id] = screen;
            _project.ActiveScreenId = id;
            return screen;
        }

        public bool RemoveScreen(string screenId)
        {
            if (_project.Screens.Count <= 1) return false;
            
            var screen = _project.Screens.FirstOrDefault(s => s.Id == screenId);
            if (screen == null) return false;

            _project.Screens.Remove(screen);
            _screenIndex.TryRemove(screenId, out _);
            
            // Remove elementos do índice global
            foreach (var el in screen.Elements)
            {
                _elementIndex.TryRemove(el.Id, out _);
            }

            if (_project.ActiveScreenId == screenId)
            {
                _project.ActiveScreenId = _project.Screens[0].Id;
            }

            return true;
        }

        public PrototypeScreen? GetScreen(string id)
        {
            _screenIndex.TryGetValue(id, out var screen);
            return screen;
        }

        #endregion

        #region Element Operations

        public PrototypeElement? AddElement(string screenId, string type, string? asset = null)
        {
            var screen = GetScreen(screenId);
            if (screen == null) return null;

            var isCircle = type.ToLower().Contains("circle");
            var el = new PrototypeElement
            {
                Id = $"el_{_project.ElementSeq++}",
                Type = type,
                Name = $"{type}_{screen.Elements.Count + 1}",
                X = 10 + (screen.Elements.Count * 5),
                Y = 10 + (screen.Elements.Count * 5),
                W = isCircle ? 60 : 80,
                H = isCircle ? 60 : 40,
                Color = "#38bdf8",
                Asset = asset
            };

            screen.Elements.Add(el);
            _elementIndex[el.Id] = el;
            _project.SelectedElementId = el.Id;
            return el;
        }

        public bool RemoveElement(string elId)
        {
            var el = FindElement(elId);
            if (el == null) return false;

            // Encontra a tela que contém o elemento
            var screen = _project.Screens.FirstOrDefault(s => s.Elements.Any(e => e.Id == elId));
            if (screen == null) return false;

            screen.Elements.Remove(el);
            _elementIndex.TryRemove(elId, out _);
            
            if (_project.SelectedElementId == elId)
            {
                _project.SelectedElementId = null;
            }

            return true;
        }

        /// <summary>
        /// Busca global instantânea por ID de elemento (O(1))
        /// </summary>
        public PrototypeElement? FindElement(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _elementIndex.TryGetValue(id, out var el);
            return el;
        }

        public void MoveElement(string elId, string targetScreenId)
        {
            var el = FindElement(elId);
            if (el == null) return;

            var sourceScreen = _project.Screens.FirstOrDefault(s => s.Elements.Any(e => e.Id == elId));
            var targetScreen = GetScreen(targetScreenId);

            if (sourceScreen == null || targetScreen == null || sourceScreen == targetScreen) return;

            sourceScreen.Elements.Remove(el);
            targetScreen.Elements.Add(el);
        }

        #endregion

        #region Asset Operations

        public void AddAsset(PrototypeAsset asset)
        {
            // Sanitização e unicidade
            var safeName = System.Text.RegularExpressions.Regex.Replace(asset.Name, "[^a-zA-Z0-9_]+", "_");
            if (char.IsDigit(safeName[0])) safeName = "a" + safeName;
            
            var name = safeName;
            int counter = 1;
            while (_project.Assets.Any(a => a.Name == name))
            {
                name = $"{safeName}_{counter++}";
            }
            asset.Name = name;
            _project.Assets.Add(asset);
        }

        #endregion

        /// <summary>
        /// Exporta o estado atual para o formato de persistência.
        /// </summary>
        public PrototypeProject Export() => _project;
    }
}

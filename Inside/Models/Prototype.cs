using System.Text.Json.Serialization;

namespace PixelDisplay240Api.Models
{
    public class PrototypeProject
    {
        public List<PrototypeScreen> Screens { get; set; } = new();
        public string ActiveScreenId { get; set; } = "screen_1";
        public string? SelectedElementId { get; set; }
        public List<PrototypeAsset> Assets { get; set; } = new();
        public int ElementSeq { get; set; } = 1;
        public int ScreenSeq { get; set; } = 2;

        public PrototypeProject()
        {
            Screens.Add(new PrototypeScreen { Id = "screen_1", Name = "Home" });
        }
    }

    public class PrototypeScreen
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Background { get; set; }
        public string? BackgroundAsset { get; set; }
        public string? BackgroundColor { get; set; } = "#000000";
        public List<PrototypeElement> Elements { get; set; } = new();
    }

    public class PrototypeElement
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "rect";
        public string Name { get; set; } = string.Empty;
        public int X { get; set; } = 10;
        public int Y { get; set; } = 10;
        public int W { get; set; } = 50;
        public int H { get; set; } = 50;
        public string Color { get; set; } = "#38bdf8";
        public string? Asset { get; set; }
        public string? TargetScreenId { get; set; }

        // State Logic Bindings
        public string? XBind { get; set; }
        public string? YBind { get; set; }
        public string? WBind { get; set; }
        public string? HBind { get; set; }
        public string? ColorBind { get; set; }
        public string? ValueBind { get; set; }
    }

    public class PrototypeAsset
    {
        public string Name { get; set; } = string.Empty;
        public string DataUrl { get; set; } = string.Empty;
        public int Width { get; set; } = 240;
        public int Height { get; set; } = 240;
        public string Kind { get; set; } = "image";
    }
}

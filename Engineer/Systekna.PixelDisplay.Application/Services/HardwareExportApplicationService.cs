using System.Text;
using System.IO.Compression;
using Systekna.PixelDisplay.Application.Domain.Entities;

namespace Systekna.PixelDisplay.Application.Services;

/// <summary>
/// Serviço de exportação para hardware (Arduino/ESP32).
/// </summary>
public class HardwareExportApplicationService
{
    /// <summary>
    /// Gera um arquivo ZIP contendo todo o projeto pronto para a Arduino IDE.
    /// </summary>
    public byte[] GenerateProjectZip(PrototypeProject project)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            // 1. Arquivo principal .ino
            var mainCode = GenerateMainCode(project);
            var mainEntry = archive.CreateEntry("PixelDisplay240_Project.ino");
            using (var writer = new StreamWriter(mainEntry.Open()))
            {
                writer.Write(mainCode);
            }

            // 2. Arquivo de imagens (PROGMEM)
            var imagesH = GenerateImagesH(project);
            var imagesEntry = archive.CreateEntry("images.h");
            using (var writer = new StreamWriter(imagesEntry.Open()))
            {
                writer.Write(imagesH);
            }

            // 3. Pasta /data para LittleFS
            foreach (var asset in project.Assets.Where(a => a.StorageType == "littlefs"))
            {
                var dataEntry = archive.CreateEntry($"data/{asset.Name}.jpg");
                var bytes = Convert.FromBase64String(ExtractBase64(asset.DataUrl));
                using (var stream = dataEntry.Open())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            // 4. README
            var readmeEntry = archive.CreateEntry("README.md");
            using (var writer = new StreamWriter(readmeEntry.Open()))
            {
                writer.Write(GenerateReadme(project));
            }
        }
        return ms.ToArray();
    }

    private string GenerateMainCode(PrototypeProject project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/*");
        sb.AppendLine(" * PixelDisplay240 - Projeto Gerado Automaticamente");
        sb.AppendLine($" * Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($" * Telas: {project.Screens.Count}");
        sb.AppendLine(" */");
        sb.AppendLine();
        sb.AppendLine("#include <TFT_eSPI.h>");
        sb.AppendLine("#include <LittleFS.h>");
        sb.AppendLine("#include <TJpg_Decoder.h>");
        sb.AppendLine("#include \"images.h\"");
        sb.AppendLine();
        sb.AppendLine("TFT_eSPI tft = TFT_eSPI();");
        sb.AppendLine();

        // Declarações forward
        foreach (var screen in project.Screens)
        {
            var safeName = SanitizeName(screen.Name);
            sb.AppendLine($"void draw_{safeName}();");
        }
        sb.AppendLine();

        // Implementações das telas
        foreach (var screen in project.Screens)
        {
            var safeName = SanitizeName(screen.Name);
            sb.AppendLine($"void draw_{safeName}() {{");
            
            // Background
            if (!string.IsNullOrEmpty(screen.BackgroundAsset))
            {
                var asset = project.Assets.FirstOrDefault(a => a.Name == screen.BackgroundAsset);
                if (asset != null)
                {
                    if (asset.StorageType == "littlefs")
                        sb.AppendLine($"    TJpg_Decoder.drawJpgFile(LittleFS, \"/{asset.Name}.jpg\", 0, 0);");
                    else
                        sb.AppendLine($"    tft.pushImage(0, 0, 240, 240, {asset.Name});");
                }
            }
            else if (!string.IsNullOrEmpty(screen.BackgroundColor))
            {
                sb.AppendLine($"    tft.fillScreen({HtmlColorTo565(screen.BackgroundColor)});");
            }
            else
            {
                sb.AppendLine("    tft.fillScreen(TFT_BLACK);");
            }

            // Elementos
            foreach (var el in screen.Elements)
            {
                sb.AppendLine($"    // Element: {el.Name}");
                var color = HtmlColorTo565(el.Color);
                
                switch (el.Type.ToLower())
                {
                    case "fillrect":
                        sb.AppendLine($"    tft.fillRect({el.X}, {el.Y}, {el.W}, {el.H}, {color});");
                        break;
                    case "drawrect":
                        sb.AppendLine($"    tft.drawRect({el.X}, {el.Y}, {el.W}, {el.H}, {color});");
                        break;
                    case "fillroundrect":
                        sb.AppendLine($"    tft.fillRoundRect({el.X}, {el.Y}, {el.W}, {el.H}, 8, {color});");
                        break;
                    case "drawroundrect":
                        sb.AppendLine($"    tft.drawRoundRect({el.X}, {el.Y}, {el.W}, {el.H}, 8, {color});");
                        break;
                    case "fillcircle":
                    case "drawcircle":
                        int r = Math.Min(el.W, el.H) / 2;
                        string circleCmd = el.Type.ToLower().Contains("fill") ? "fillCircle" : "drawCircle";
                        sb.AppendLine($"    tft.{circleCmd}({el.X + el.W / 2}, {el.Y + el.H / 2}, {r}, {color});");
                        break;
                    case "filltriangle":
                        sb.AppendLine($"    tft.fillTriangle({el.X + el.W / 2}, {el.Y}, {el.X}, {el.Y + el.H}, {el.X + el.W}, {el.Y + el.H}, {color});");
                        break;
                    case "drawtriangle":
                        sb.AppendLine($"    tft.drawTriangle({el.X + el.W / 2}, {el.Y}, {el.X}, {el.Y + el.H}, {el.X + el.W}, {el.Y + el.H}, {color});");
                        break;
                    case "fillellipse":
                        sb.AppendLine($"    tft.fillEllipse({el.X + el.W / 2}, {el.Y + el.H / 2}, {el.W / 2}, {el.H / 2}, {color});");
                        break;
                    case "drawline":
                        sb.AppendLine($"    tft.drawLine({el.X}, {el.Y}, {el.X + el.W}, {el.Y + el.H}, {color});");
                        break;
                    case "drawstring":
                    case "drawcentrestring":
                        sb.AppendLine($"    tft.setTextColor({color}); tft.setTextSize({Math.Max(1, el.H / 8)});");
                        if (el.Type.ToLower().Contains("centre"))
                            sb.AppendLine($"    tft.drawCentreString(\"{el.Name}\", {el.X + el.W / 2}, {el.Y}, 2);");
                        else
                            sb.AppendLine($"    tft.drawString(\"{el.Name}\", {el.X}, {el.Y});");
                        break;
                    case "pushimage":
                        if (!string.IsNullOrEmpty(el.Asset))
                            sb.AppendLine($"    tft.pushImage({el.X}, {el.Y}, {el.W}, {el.H}, {el.Asset});");
                        break;
                }
            }
            
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Setup
        sb.AppendLine("void setup() {");
        sb.AppendLine("    Serial.begin(115200);");
        sb.AppendLine("    tft.init();");
        sb.AppendLine("    tft.setRotation(0);");
        sb.AppendLine("    tft.fillScreen(TFT_BLACK);");
        sb.AppendLine();
        sb.AppendLine("    if(!LittleFS.begin()) {");
        sb.AppendLine("        Serial.println(\"LittleFS Mount Failed\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    // JPEG decoder setup");
        sb.AppendLine("    TJpg_Decoder.setJpgScale(1);");
        sb.AppendLine("    TJpg_Decoder.setSwapBytes(true);");
        sb.AppendLine("    TJpg_Decoder.setCallback([](int16_t x, int16_t y, uint16_t w, uint16_t h, uint16_t* bitmap) {");
        sb.AppendLine("        tft.pushImage(x, y, w, h, bitmap);");
        sb.AppendLine("        return true;");
        sb.AppendLine("    });");
        sb.AppendLine();
        
        // Chama a primeira tela
        if (project.Screens.Count > 0)
        {
            var firstName = SanitizeName(project.Screens[0].Name);
            sb.AppendLine($"    draw_{firstName}();");
        }
        sb.AppendLine("}");
        sb.AppendLine();

        // Loop
        sb.AppendLine("void loop() {");
        sb.AppendLine("    // Add your navigation/interaction logic here");
        sb.AppendLine("    delay(100);");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateImagesH(PrototypeProject project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#ifndef IMAGES_H");
        sb.AppendLine("#define IMAGES_H");
        sb.AppendLine();
        sb.AppendLine("#include <pgmspace.h>");
        sb.AppendLine();

        foreach (var asset in project.Assets.Where(a => a.StorageType == "flash"))
        {
            sb.AppendLine($"// Image: {asset.Name} ({asset.Width}x{asset.Height})");
            sb.AppendLine($"// TODO: Convert to RGB565 array using image2cpp or similar tool");
            sb.AppendLine($"const uint16_t {asset.Name}[{asset.Width * asset.Height}] PROGMEM = {{");
            sb.AppendLine("    0x0000 // Placeholder - replace with actual image data");
            sb.AppendLine("};");
            sb.AppendLine();
        }

        sb.AppendLine("#endif // IMAGES_H");
        return sb.ToString();
    }

    private string GenerateReadme(PrototypeProject project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PixelDisplay240 Project");
        sb.AppendLine();
        sb.AppendLine("## Generated Files");
        sb.AppendLine();
        sb.AppendLine("- `PixelDisplay240_Project.ino` - Main Arduino sketch");
        sb.AppendLine("- `images.h` - Image arrays for PROGMEM storage");
        sb.AppendLine("- `data/` - Images for LittleFS storage (upload with ESP32 Filesystem Uploader)");
        sb.AppendLine();
        sb.AppendLine("## Requirements");
        sb.AppendLine();
        sb.AppendLine("- ESP32 board");
        sb.AppendLine("- TFT_eSPI library");
        sb.AppendLine("- TJpg_Decoder library");
        sb.AppendLine("- LittleFS library");
        sb.AppendLine();
        sb.AppendLine("## Screens");
        sb.AppendLine();
        foreach (var screen in project.Screens)
        {
            sb.AppendLine($"- **{screen.Name}** ({screen.Elements.Count} elements)");
        }
        sb.AppendLine();
        sb.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        return sb.ToString();
    }

    private string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Screen";
        return System.Text.RegularExpressions.Regex.Replace(name, "[^a-zA-Z0-9_]", "_");
    }

    private string ExtractBase64(string dataUrl)
    {
        if (string.IsNullOrEmpty(dataUrl)) return "";
        var parts = dataUrl.Split(',');
        return parts.Length > 1 ? parts[1] : parts[0];
    }

    private string HtmlColorTo565(string? htmlColor)
    {
        if (string.IsNullOrEmpty(htmlColor) || !htmlColor.StartsWith("#")) return "TFT_BLACK";
        
        try
        {
            string hex = htmlColor.Substring(1);
            if (hex.Length == 3) hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";

            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            int r5 = (r * 31) / 255;
            int g6 = (g * 63) / 255;
            int b5 = (b * 31) / 255;

            int rgb565 = (r5 << 11) | (g6 << 5) | b5;
            return "0x" + rgb565.ToString("X4");
        }
        catch
        {
            return "TFT_BLACK";
        }
    }
}

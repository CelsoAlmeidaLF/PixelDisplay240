using System.Text;
using System.IO.Compression;
using Systekna.Application.Domain.Entities;

namespace Systekna.Application.Services;

/// <summary>
/// Interface para o serviço de exportação de projetos para hardware.
/// </summary>
public interface IHardwareExportService
{
    byte[] GenerateProjectZip(PrototypeProject project);
    string GenerateMainCode(PrototypeProject project);
    string GenerateImagesHeader(PrototypeProject project);
}

/// <summary>
/// Serviço de exportação de projetos para hardware Arduino/ESP32.
/// Gera código C++ compatível com TFT_eSPI e LittleFS.
/// </summary>
public class HardwareExportService : IHardwareExportService
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
            var imagesH = GenerateImagesHeader(project);
            var imagesEntry = archive.CreateEntry("images.h");
            using (var writer = new StreamWriter(imagesEntry.Open()))
            {
                writer.Write(imagesH);
            }

            // 3. README
            var readmeEntry = archive.CreateEntry("README.md");
            using (var writer = new StreamWriter(readmeEntry.Open()))
            {
                writer.Write(GenerateReadme(project));
            }

            // 4. Pasta /data para LittleFS
            foreach (var asset in project.Assets.Where(a => a.StorageType == "littlefs"))
            {
                var dataEntry = archive.CreateEntry($"data/{asset.Name}.jpg");
                var bytes = Convert.FromBase64String(ExtractBase64(asset.DataUrl));
                using (var stream = dataEntry.Open())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
        }
        return ms.ToArray();
    }

    public string GenerateMainCode(PrototypeProject project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/**");
        sb.AppendLine(" * PixelDisplay240 - Projeto Gerado Automaticamente");
        sb.AppendLine($" * Data: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($" * Telas: {project.Screens.Count}");
        sb.AppendLine($" * Assets: {project.Assets.Count}");
        sb.AppendLine(" */");
        sb.AppendLine();
        sb.AppendLine("#include <TFT_eSPI.h>");
        sb.AppendLine("#include <LittleFS.h>");
        sb.AppendLine("#include <TJpg_Decoder.h>");
        sb.AppendLine("#include \"images.h\"");
        sb.AppendLine();
        sb.AppendLine("TFT_eSPI tft = TFT_eSPI();");
        sb.AppendLine();
        sb.AppendLine("// Forward declarations");
        foreach (var screen in project.Screens)
        {
            var cleanName = CleanName(screen.Name);
            sb.AppendLine($"void draw_{cleanName}();");
        }
        sb.AppendLine();

        // Gerar função para cada tela
        foreach (var screen in project.Screens)
        {
            var cleanName = CleanName(screen.Name);
            sb.AppendLine($"void draw_{cleanName}() {{");

            // Wallpaper/Background
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
                sb.AppendLine($"    // {el.Name}");
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
                    case "fillcircle":
                    case "drawcircle":
                        int r = Math.Min(el.W, el.H) / 2;
                        string cmd = el.Type.ToLower().Contains("fill") ? "fillCircle" : "drawCircle";
                        sb.AppendLine($"    tft.{cmd}({el.X + el.W / 2}, {el.Y + el.H / 2}, {r}, {color});");
                        break;
                    case "filltriangle":
                        sb.AppendLine($"    tft.fillTriangle({el.X + el.W / 2}, {el.Y}, {el.X}, {el.Y + el.H}, {el.X + el.W}, {el.Y + el.H}, {color});");
                        break;
                    case "fillellipse":
                        sb.AppendLine($"    tft.fillEllipse({el.X + el.W / 2}, {el.Y + el.H / 2}, {el.W / 2}, {el.H / 2}, {color});");
                        break;
                    case "drawline":
                        sb.AppendLine($"    tft.drawLine({el.X}, {el.Y}, {el.X + el.W}, {el.Y + el.H}, {color});");
                        break;
                    case "drawfasthline":
                        sb.AppendLine($"    tft.drawFastHLine({el.X}, {el.Y}, {el.W}, {color});");
                        break;
                    case "drawfastvline":
                        sb.AppendLine($"    tft.drawFastVLine({el.X}, {el.Y}, {el.H}, {color});");
                        break;
                    case "drawpixel":
                        sb.AppendLine($"    tft.drawPixel({el.X}, {el.Y}, {color});");
                        break;
                    case "drawstring":
                    case "drawcentrestring":
                        sb.AppendLine($"    tft.setTextColor({color});");
                        sb.AppendLine($"    tft.setTextSize({Math.Max(1, el.H / 8)});");
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
        sb.AppendLine("    if (!LittleFS.begin()) {");
        sb.AppendLine("        Serial.println(\"LittleFS Mount Failed\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    // Desenhar tela inicial");
        if (project.Screens.Any())
        {
            var firstScreen = project.Screens.First();
            sb.AppendLine($"    draw_{CleanName(firstScreen.Name)}();");
        }
        sb.AppendLine("}");
        sb.AppendLine();

        // Loop
        sb.AppendLine("void loop() {");
        sb.AppendLine("    // Adicione sua lógica de navegação aqui");
        sb.AppendLine("    delay(100);");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public string GenerateImagesHeader(PrototypeProject project)
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
            sb.AppendLine($"extern const uint16_t {asset.Name}[{asset.Width * asset.Height}] PROGMEM;");
            sb.AppendLine();
        }

        sb.AppendLine("#endif // IMAGES_H");
        return sb.ToString();
    }

    private string GenerateReadme(PrototypeProject project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PixelDisplay240 - Projeto Exportado");
        sb.AppendLine();
        sb.AppendLine("## Requisitos");
        sb.AppendLine("- Arduino IDE ou PlatformIO");
        sb.AppendLine("- Biblioteca TFT_eSPI configurada para seu display");
        sb.AppendLine("- Biblioteca TJpg_Decoder (para imagens JPEG)");
        sb.AppendLine("- LittleFS para armazenamento de arquivos");
        sb.AppendLine();
        sb.AppendLine("## Estrutura do Projeto");
        sb.AppendLine("```");
        sb.AppendLine("??? PixelDisplay240_Project.ino  # Código principal");
        sb.AppendLine("??? images.h                     # Declarações de imagens PROGMEM");
        sb.AppendLine("??? data/                        # Arquivos para LittleFS");
        sb.AppendLine("?   ??? *.jpg                    # Imagens JPEG");
        sb.AppendLine("??? README.md                    # Este arquivo");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Telas Incluídas");
        foreach (var screen in project.Screens)
        {
            sb.AppendLine($"- **{screen.Name}** ({screen.Elements.Count} elementos)");
        }
        sb.AppendLine();
        sb.AppendLine("## Upload de Arquivos");
        sb.AppendLine("1. Instale o plugin LittleFS para Arduino IDE");
        sb.AppendLine("2. Copie a pasta `data` para o diretório do sketch");
        sb.AppendLine("3. Use Tools > ESP32 Sketch Data Upload");
        sb.AppendLine();
        sb.AppendLine($"*Gerado em {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*");
        return sb.ToString();
    }

    private static string ExtractBase64(string dataUrl)
    {
        if (string.IsNullOrEmpty(dataUrl)) return "";
        var parts = dataUrl.Split(',');
        return parts.Length > 1 ? parts[1] : parts[0];
    }

    private static string CleanName(string name)
    {
        var clean = System.Text.RegularExpressions.Regex.Replace(name, "[^a-zA-Z0-9_]+", "_");
        if (string.IsNullOrEmpty(clean)) return "Screen";
        if (char.IsDigit(clean[0])) clean = "_" + clean;
        return clean;
    }

    private static string HtmlColorTo565(string htmlColor)
    {
        if (string.IsNullOrEmpty(htmlColor) || !htmlColor.StartsWith("#")) return "0x0000";

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
            return "0x0000";
        }
    }
}

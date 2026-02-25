using System.Text;
using System.IO.Compression;
using PixelDisplay240Api.Models;

namespace PixelDisplay240Api.Services
{
    public class HardwareExportService
    {
        /// <summary>
        /// Gera um arquivo ZIP contendo todo o projeto pronto para a Arduino IDE.
        /// </summary>
        public byte[] GenerateProjectZip(PrototypeProject project)
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                // 1. Arquivo principal .ino ou .cpp
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
                    var dataEntry = archive.CreateEntry($"data/{asset.Name}.jpg"); // Simplificado para JPG
                    var bytes = Convert.FromBase64String(ExtractBase64(asset.DataUrl));
                    using (var stream = dataEntry.Open())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            return ms.ToArray();
        }

        private string GenerateMainCode(PrototypeProject project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#include <TFT_eSPI.Current>");
            sb.AppendLine("#include <LittleFS.Current>");
            sb.AppendLine("#include <TJpg_Decoder.Current>");
            sb.AppendLine("#include \"images.h\"");
            sb.AppendLine();
            sb.AppendLine("TFT_eSPI tft = TFT_eSPI();");
            sb.AppendLine();

            foreach (var screen in project.Screens)
            {
                sb.AppendLine($"void draw_{screen.Name}() {{");
                
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

                foreach (var el in screen.Elements)
                {
                    sb.AppendLine($"    // Element: {el.Name}");
                    var color = HtmlColorTo565(el.Color);
                    
                    switch (el.Type.ToLower())
                    {
                        case "fillrect": sb.AppendLine($"    tft.fillRect({el.X}, {el.Y}, {el.W}, {el.H}, {color});"); break;
                        case "drawrect": sb.AppendLine($"    tft.drawRect({el.X}, {el.Y}, {el.W}, {el.H}, {color});"); break;
                        case "fillroundrect": sb.AppendLine($"    tft.fillRoundRect({el.X}, {el.Y}, {el.W}, {el.H}, 8, {color});"); break;
                        case "fillcircle": 
                        case "drawcircle":
                            int r = Math.Min(el.W, el.H) / 2;
                            string cmd = el.Type.ToLower().Contains("fill") ? "fillCircle" : "drawCircle";
                            sb.AppendLine($"    tft.{cmd}({el.X + el.W / 2}, {el.Y + el.H / 2}, {r}, {color});");
                            break;
                        case "filltriangle":
                            sb.AppendLine($"    tft.fillTriangle({el.X + el.W / 2}, {el.Y}, {el.X}, {el.Y + el.H}, {el.X + el.W}, {el.Y + el.H}, {color});");
                            break;
                        case "drawstring":
                        case "drawcentrestring":
                            sb.AppendLine($"    tft.setTextColor({color}); tft.setTextSize({Math.Max(1, el.H / 8)});");
                            if (el.Type.ToLower().Contains("centre"))
                                sb.AppendLine($"    tft.drawCentreString(\"{el.Name}\", {el.X + el.W / 2}, {el.Y}, 2);");
                            else
                                sb.AppendLine($"    tft.drawString(\"{el.Name}\", {el.X}, {el.Y});");
                            break;
                    }
                }
                
                sb.AppendLine("}");
                sb.AppendLine();
            }

            sb.AppendLine("void setup() {");
            sb.AppendLine("    tft.init();");
            sb.AppendLine("    tft.setRotation(0);");
            sb.AppendLine("    if(!LittleFS.begin()) { Serial.println(\"LittleFS Mount Failed\"); }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("void loop() { /* Logic here */ }");

            return sb.ToString();
        }

        private string GenerateImagesH(PrototypeProject project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#ifndef IMAGES_H");
            sb.AppendLine("#define IMAGES_H");
            sb.AppendLine("#include <pgmspace.h>");
            sb.AppendLine();

            foreach (var asset in project.Assets.Where(a => a.StorageType == "flash"))
            {
                sb.AppendLine($"// Image: {asset.Name} ({asset.Width}x{asset.Height})");
                sb.AppendLine($"const uint16_t {asset.Name}[] PROGMEM = {{");
                sb.AppendLine("    // RGB565 Data pending real implementation...");
                sb.AppendLine("    0x0000, 0xFFFF");
                sb.AppendLine("};");
                sb.AppendLine();
            }

            sb.AppendLine("#endif");
            return sb.ToString();
        }

        private string ExtractBase64(string dataUrl)
        {
            if (string.IsNullOrEmpty(dataUrl)) return "";
            var parts = dataUrl.Split(',');
            return parts.Length > 1 ? parts[1] : parts[0];
        }

        private string HtmlColorTo565(string htmlColor)
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
}

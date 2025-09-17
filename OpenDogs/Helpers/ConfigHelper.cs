using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenDogs.Helpers
{
    public static class ConfigHelper
    {
        private static readonly string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string carpetaApp = Path.Combine(documentos, "OpenDogs");
        private static readonly string rutaConfig = Path.Combine(carpetaApp, "config.json");

        public static void GuardarRutaConfig(string rutaCarpeta)
        {
            Directory.CreateDirectory(carpetaApp);
            var config = new AppConfig { CarpetaSeleccionada = rutaCarpeta };
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(rutaConfig, json);
        }

        public static string CargarRutaConfig()
        {
            if (!File.Exists(rutaConfig)) return null;
            var json = File.ReadAllText(rutaConfig);
            var config = JsonSerializer.Deserialize<AppConfig>(json);
            return config?.CarpetaSeleccionada;
        }

        public static string RutaGuardada => rutaConfig;
    }
    public class AppConfig
    {
        public string CarpetaSeleccionada { get; set; }
    }

}

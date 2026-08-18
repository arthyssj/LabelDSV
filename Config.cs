using System;
using System.IO;
using System.Text.Json;

namespace EtiquetasDSV
{
    /// <summary>
    /// Datos fijos de la etiqueta (titulo, direccion, formato de fecha,
    /// impresora e copias por defecto). Se guarda en un JSON dentro del
    /// perfil de Windows del usuario, igual que hacia la version en Python.
    /// </summary>
    public class Config
    {
        public string Titulo { get; set; } = "DSV";
        public string FromLinea1 { get; set; } = "Av. Chapultepec s/n Parque Industrial Colonial";
        public string FromLinea2 { get; set; } = "Reynosa, Tam. Mexico 88787";
        public string FormatoFecha { get; set; } = "dd-MM-yyyy";
        public int Copias { get; set; } = 4;
        public string Impresora { get; set; } = "";

        private static string RutaArchivo =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "etiquetas_dsv_config.json");

        public static Config Cargar()
        {
            try
            {
                if (File.Exists(RutaArchivo))
                {
                    string json = File.ReadAllText(RutaArchivo);
                    var cfg = JsonSerializer.Deserialize<Config>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch
            {
                // Si el archivo esta corrupto o no se puede leer, se usan
                // los valores por defecto en vez de tronar la app.
            }

            return new Config();
        }

        public void Guardar()
        {
            try
            {
                var opciones = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, opciones);
                File.WriteAllText(RutaArchivo, json);
            }
            catch
            {
                // Si no se puede guardar (permisos, disco lleno, etc.) la
                // app sigue funcionando con los valores en memoria.
            }
        }
    }
}

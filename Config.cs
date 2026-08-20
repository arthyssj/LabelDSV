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
        public string FormatoFecha { get; set; } = "dd-MM-yyyy";
        public int Copias { get; set; } = 4;
        public string Impresora { get; set; } = "";

        public string UltimaFechaReferencia { get; set; } = "";
        public int UltimoContadorReferencia { get; set; } = 0;

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

        /// <summary>Contador maximo del sufijo de referencia (RF...-999); al superarlo vuelve a 1.</summary>
        public const int MaxContadorReferencia = 999;

        /// <summary>
        /// Genera la siguiente referencia automatica: "RF" + dia de la semana
        /// estilo VBA (domingo=1 ... sabado=7) + fecha MMddyyyy + "-" +
        /// contador. El contador se reinicia en 1 cada vez que cambia el dia,
        /// y tambien al superar <see cref="MaxContadorReferencia"/> (999), para
        /// que la referencia nunca exceda el limite de 18 caracteres del campo.
        /// Se persiste en el mismo JSON de configuracion.
        /// Ejemplo: "RF0308182026-1" (martes 18-ago-2026, primera del dia).
        /// </summary>
        public string GenerarReferencia()
        {
            DateTime hoy = DateTime.Now.Date;
            string hoyStr = hoy.ToString("yyyy-MM-dd");

            if (UltimaFechaReferencia != hoyStr)
            {
                UltimaFechaReferencia = hoyStr;
                UltimoContadorReferencia = 0;
            }

            UltimoContadorReferencia++;
            if (UltimoContadorReferencia > MaxContadorReferencia)
                UltimoContadorReferencia = 1;

            Guardar();

            int diaSemana = (int)hoy.DayOfWeek + 1; // domingo=1 ... sabado=7
            return $"RF{diaSemana:D2}{hoy:MMddyyyy}-{UltimoContadorReferencia}";
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

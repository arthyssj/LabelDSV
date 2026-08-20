using System;
using System.Text;

namespace EtiquetasDSV
{
    /// <summary>
    /// Construye el ZPL de la etiqueta DSV: 4 x 6.5 in a 300 dpi, contenido
    /// rotado 90 grados (^A0R / ^BCR), header en negro con letras blancas
    /// (^GB relleno + ^FR), textos centrados con ^FB.
    ///
    /// Las coordenadas son identicas a la plantilla validada en
    /// labelary.com/viewer.html y usada en la macro de Excel.
    /// </summary>
    public static class ZplBuilder
    {
        public static string Construir(
            string parte,
            string cantidad,
            string referencia,
            string tipo,
            string notas,
            Config cfg,
            int copias)
        {
            parte = Limpiar(parte);
            cantidad = Limpiar(cantidad);
            referencia = Limpiar(referencia);
            tipo = Limpiar(tipo).ToUpperInvariant();
            notas = Limpiar(notas);

            string titulo = Limpiar(cfg.Titulo).ToUpperInvariant();
            if (string.IsNullOrEmpty(titulo)) titulo = "DSV";

            string from1 = Limpiar(cfg.FromLinea1);
            string from2 = Limpiar(cfg.FromLinea2);

            string formatoFecha = string.IsNullOrWhiteSpace(cfg.FormatoFecha)
                ? "dd-MM-yyyy"
                : cfg.FormatoFecha;
            string fecha = DateTime.Now.ToString(formatoFecha);

            if (copias < 1) copias = 1;

            var z = new StringBuilder();

            z.Append("^XA");
            z.Append("^CI28");
            z.Append("^PW1200");
            z.Append("^LL1950");
            z.Append("^LH0,0");

            // Header negro con letras blancas
            z.Append("^FO1070,45^GB130,1860,130^FS");
            z.Append("^FR^FO1085,60^A0R,60,60^FDEATON^FS");
            z.Append($"^FR^FO1085,0^FB1950,1,0,C^A0R,75,75^FD{titulo}^FS");

            // Part Number
            z.Append("^FO530,60^GB480,1090,5^FS");
            z.Append("^FO945,60^FB1090,1,0,C^A0R,45,45^FDPART NUMBER^FS");
            z.Append($"^FO840,60^FB1090,1,0,C^A0R,95,95^FD{parte}^FS");
            z.Append($"^FO620,260^BY4^BCR,180,Y,N,N^FD{parte}^FS");

            // Quantity
            z.Append("^FO530,1150^GB480,740,5^FS");
            z.Append("^FO945,1150^FB740,1,0,C^A0R,45,45^FDQUANTITY^FS");
            z.Append($"^FO840,1150^FB740,1,0,C^A0R,95,95^FD{cantidad}^FS");
            z.Append($"^FO620,1380^BY3^BCR,180,Y,N,N^FD{cantidad}^FS");

            // Reference
            z.Append("^FO435,60^A0R,45,45^FDREFERENCE^FS");
            z.Append($"^FO335,60^A0R,85,85^FD{referencia}^FS");
            z.Append($"^FO310,800^BY3^BCR,150,Y,N,N^FD{referencia}^FS");

            // Tipo
            z.Append("^FO350,1500^GB120,390,5^FS");
            z.Append($"^FO370,1500^FB390,1,0,C^A0R,70,70^FD{tipo}^FS");

            // From
            z.Append("^FO215,60^A0R,45,45^FDFROM^FS");
            z.Append($"^FO165,60^A0R,42,42^FD{from1}^FS");
            z.Append($"^FO120,60^A0R,42,42^FD{from2}^FS");

            // Notes y fecha
            z.Append("^FO50,60^A0R,45,45^FDNOTES:^FS");
            if (!string.IsNullOrEmpty(notas))
            {
                z.Append($"^FO50,250^FB1100,1,0,L^A0R,45,45^FD{notas}^FS");
            }
            z.Append($"^FO50,1400^A0R,45,45^FDDATE: {fecha}^FS");

            // Separadores
            z.Append("^FO506,60^GB4,1830,4^FS");
            z.Append("^FO270,60^GB4,1830,4^FS");
            z.Append("^FO100,60^GB4,1830,4^FS");

            z.Append($"^PQ{copias},0,0,N");
            z.Append("^XZ");

            return z.ToString();
        }

        /// <summary>
        /// Construye el ZPL de la etiqueta de Pallet: misma plantilla validada
        /// en zpl_pallet.txt (4 x 6.5 in a 300 dpi, sin rotar - ^A0N / ^BCN,
        /// a diferencia de la etiqueta principal que va rotada 90 grados).
        /// Solo se sustituyen los campos variables (titulo, pallet, reference,
        /// fecha, copias); el resto de las coordenadas son identicas al
        /// archivo de referencia.
        /// </summary>
        public static string ConstruirPallet(
            string referencia,
            string pallet,
            Config cfg,
            int copias)
        {
            referencia = Limpiar(referencia);
            pallet = Limpiar(pallet);

            string titulo = Limpiar(cfg.Titulo).ToUpperInvariant();
            if (string.IsNullOrEmpty(titulo)) titulo = "DSV";

            string formatoFecha = string.IsNullOrWhiteSpace(cfg.FormatoFecha)
                ? "dd-MM-yyyy"
                : cfg.FormatoFecha;
            string fecha = DateTime.Now.ToString(formatoFecha);

            if (copias < 1) copias = 1;

            var z = new StringBuilder();

            z.Append("^XA");
            z.Append("^CI28");
            z.Append("^PW1200");
            z.Append("^LL1950");
            z.Append("^LH0,0");

            // Header negro con letras blancas
            z.Append("^FO0,0^GB1200,150,150^FS");
            z.Append("^FR^FO60,35^A0N,55,55^FDEATON^FS");
            z.Append($"^FR^FO0,45^FB1200,1,0,C^A0N,70,70^FD{titulo}^FS");

            // Pallet
            z.Append("^FO90,190^GB1020,780,5^FS");
            z.Append("^FO0,250^FB1200,1,0,C^A0N,85,85^FDPALLET^FS");
            z.Append($"^FO0,330^FB1200,1,0,C^A0N,130,130^FD{pallet}^FS");
            z.Append($"^FO280,560^BY5^BCN,220,Y,N,N^FD{pallet}^FS");

            // Reference
            z.Append("^FO90,1010^GB1020,780,5^FS");
            z.Append("^FO0,1070^FB1200,1,0,C^A0N,75,75^FDREFERENCE^FS");
            z.Append($"^FO0,1150^FB1200,1,0,C^A0N,110,110^FD{referencia}^FS");
            z.Append($"^FO150,1380^BY4^BCN,220,Y,N,N^FD{referencia}^FS");

            // Fecha
            z.Append($"^FO60,1830^A0N,50,50^FDDATE: {fecha}^FS");

            z.Append($"^PQ{copias},0,0,N");
            z.Append("^XZ");

            return z.ToString();
        }

        /// <summary>
        /// ^ ~ y \ son caracteres de control en ZPL: hay que neutralizarlos
        /// para que no rompan el comando si vienen dentro de un dato capturado.
        /// </summary>
        private static string Limpiar(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            texto = texto.Trim();
            texto = texto.Replace("\u00A0", "");   // espacio duro
            texto = texto.Replace("\r\n", " ").Replace("\n", " ");
            texto = texto.Replace("\\", "/");
            texto = texto.Replace("^", "");
            texto = texto.Replace("~", "");

            return texto;
        }
    }
}

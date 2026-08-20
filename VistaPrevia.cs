using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EtiquetasDSV
{
    /// <summary>
    /// Dibuja una representacion aproximada de la etiqueta en un Canvas de
    /// WPF, usando las mismas coordenadas y dimensiones que ZplBuilder.cs
    /// (^PW1200 ^LL1950, contenido rotado 90 grados con ^A0R / ^BCR),
    /// transformadas a la orientacion "legible" tal como sale impresa.
    ///
    /// No es un renderizado pixel-perfecto del ZPL: es una guia visual para
    /// ubicar los datos antes de imprimir. Para verificar el diseno exacto
    /// se sigue usando labelary.com/viewer.html.
    /// </summary>
    public static class VistaPrevia
    {
        // Deben coincidir con ^PW1200 y ^LL1950 en ZplBuilder.cs.
        private const double AnchoImpresion = 1200; // ^PW
        private const double LargoImpresion = 1950; // ^LL

        public static void Dibujar(
            Canvas canvas,
            string parte,
            string cantidad,
            string referencia,
            string tipo,
            string notas,
            Config cfg)
        {
            canvas.Children.Clear();

            double escala = canvas.Width / LargoImpresion;
            canvas.Height = AnchoImpresion * escala;

            // Un punto (x,y) del sistema de coordenadas del ZPL (sin rotar)
            // pasa al sistema de la vista previa (rotada 90 grados, tal como
            // se lee la etiqueta ya impresa).
            (double X, double Y) Punto(double x, double y) => (y * escala, (AnchoImpresion - x) * escala);

            // Un recuadro ^GBw,h con origen (x,y) del ZPL.
            (double Left, double Top, double Ancho, double Alto) Recuadro(double x, double y, double w, double h) =>
                (y * escala, (AnchoImpresion - x - w) * escala, h * escala, w * escala);

            canvas.Children.Add(new Rectangle
            {
                Width = canvas.Width,
                Height = canvas.Height,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1.5
            }.PosicionadoEn(0, 0));

            string titulo = string.IsNullOrWhiteSpace(cfg.Titulo) ? "DSV" : cfg.Titulo.ToUpperInvariant();
            string fecha = DateTime.Now.ToString(
                string.IsNullOrWhiteSpace(cfg.FormatoFecha) ? "dd-MM-yyyy" : cfg.FormatoFecha);

            // ---------------- Header negro (^FO1070,45^GB130,1860,130) ----------------
            var header = Recuadro(1070, 45, 130, 1860);
            canvas.Children.Add(new Rectangle
            {
                Width = header.Ancho,
                Height = header.Alto,
                Fill = Brushes.Black
            }.PosicionadoEn(header.Left, header.Top));

            var pEaton = Punto(1085, 60);
            AgregarTextoAnclado(canvas, pEaton.X, header.Top + header.Alto / 2, "EATON", 9, true, Brushes.White);
            AgregarTextoCentrado(canvas, header.Left, header.Top + header.Alto / 2, header.Ancho,
                titulo, 15, true, Brushes.White);

            // ---------------- Part Number (^FO530,60^GB480,1090,5) ----------------
            var cajaPn = Recuadro(530, 60, 480, 1090);
            Marco(canvas, cajaPn);

            var pnLabel = Punto(945, 60);
            AgregarTextoCentrado(canvas, cajaPn.Left, pnLabel.Y, 1090 * escala, "PART NUMBER", 8, false);

            var pnValor = Punto(840, 60);
            AgregarTextoCentrado(canvas, cajaPn.Left, pnValor.Y, 1090 * escala,
                string.IsNullOrWhiteSpace(parte) ? "(vacio)" : parte, 15, true);

            var pnBarcode = Punto(620, 260);
            DibujarBarcode(canvas, pnBarcode.X, pnBarcode.Y, 420 * escala, 45 * escala, parte);

            // ---------------- Quantity (^FO530,1150^GB480,740,5) ----------------
            var cajaQty = Recuadro(530, 1150, 480, 740);
            Marco(canvas, cajaQty);

            var qtyLabel = Punto(945, 1150);
            AgregarTextoCentrado(canvas, cajaQty.Left, qtyLabel.Y, 740 * escala, "QUANTITY", 8, false);

            var qtyValor = Punto(840, 1150);
            AgregarTextoCentrado(canvas, cajaQty.Left, qtyValor.Y, 740 * escala,
                string.IsNullOrWhiteSpace(cantidad) ? "(vacio)" : cantidad, 15, true);

            var qtyBarcode = Punto(620, 1380);
            DibujarBarcode(canvas, qtyBarcode.X, qtyBarcode.Y, 280 * escala, 45 * escala, cantidad);

            // ---------------- Reference (sin ^FB, alineado a la izquierda) ----------------
            var refLabel = Punto(435, 60);
            AgregarTextoAnclado(canvas, refLabel.X, refLabel.Y, "REFERENCE", 8, false);

            var refValor = Punto(335, 60);
            AgregarTextoAnclado(canvas, refValor.X, refValor.Y,
                string.IsNullOrWhiteSpace(referencia) ? "(vacio)" : referencia, 12, true, maxWidth: 260 * escala);

            var refBarcode = Punto(310, 800);
            DibujarBarcode(canvas, refBarcode.X, refBarcode.Y, 260 * escala, 38 * escala, referencia);

            // ---------------- Tipo (^FO350,1500^GB120,390,5) ----------------
            var cajaTipo = Recuadro(350, 1500, 120, 390);
            Marco(canvas, cajaTipo);

            var tipoTexto = Punto(370, 1500);
            AgregarTextoCentrado(canvas, cajaTipo.Left, tipoTexto.Y, 390 * escala, tipo, 11, true);

            // ---------------- From ----------------
            var fromLabel = Punto(215, 60);
            AgregarTextoAnclado(canvas, fromLabel.X, fromLabel.Y, "FROM", 8, true);

            var from1 = Punto(165, 60);
            AgregarTextoAnclado(canvas, from1.X, from1.Y, cfg.FromLinea1, 8, false);

            var from2 = Punto(120, 60);
            AgregarTextoAnclado(canvas, from2.X, from2.Y, cfg.FromLinea2, 8, false);

            // ---------------- Notes y fecha (misma fila que ^FO50,60 / 250 / 1400) ----------------
            var notesLabel = Punto(50, 60);
            AgregarTextoAnclado(canvas, notesLabel.X, notesLabel.Y, "NOTES:", 8, true);

            if (!string.IsNullOrWhiteSpace(notas))
            {
                var notasPunto = Punto(50, 250);
                AgregarTextoAnclado(canvas, notasPunto.X, notasPunto.Y, notas, 8, false, maxWidth: 900 * escala);
            }

            var fechaPunto = Punto(50, 1400);
            AgregarTextoAnclado(canvas, fechaPunto.X, fechaPunto.Y, $"DATE: {fecha}", 8, false);

            // ---------------- Separadores (^GB4,1830,4) ----------------
            LineaSeparadora(canvas, Recuadro(506, 60, 4, 1830));
            LineaSeparadora(canvas, Recuadro(270, 60, 4, 1830));
            LineaSeparadora(canvas, Recuadro(100, 60, 4, 1830));
        }

        private static void Marco(Canvas canvas, (double Left, double Top, double Ancho, double Alto) r)
        {
            canvas.Children.Add(new Rectangle
            {
                Width = r.Ancho,
                Height = r.Alto,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            }.PosicionadoEn(r.Left, r.Top));
        }

        private static void LineaSeparadora(Canvas canvas, (double Left, double Top, double Ancho, double Alto) r)
        {
            canvas.Children.Add(new Rectangle
            {
                Width = r.Ancho,
                Height = Math.Max(r.Alto, 1),
                Fill = Brushes.Black
            }.PosicionadoEn(r.Left, r.Top));
        }

        /// <summary>Texto centrado horizontalmente en un ancho dado (equivalente a ^FB...,C), centrado verticalmente en yCentro.</summary>
        private static void AgregarTextoCentrado(Canvas canvas, double left, double yCentro, double ancho,
            string texto, double tamano, bool negrita, Brush? color = null)
        {
            var tb = new TextBlock
            {
                Text = texto ?? "",
                FontSize = tamano,
                FontWeight = negrita ? FontWeights.Bold : FontWeights.Normal,
                Foreground = color ?? Brushes.Black,
                Width = Math.Max(ancho, 1),
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, yCentro - tamano * 0.75);
            canvas.Children.Add(tb);
        }

        /// <summary>Texto anclado a la izquierda en (x, yCentro), centrado verticalmente (equivalente a un campo sin ^FB).</summary>
        private static void AgregarTextoAnclado(Canvas canvas, double x, double yCentro, string texto,
            double tamano, bool negrita, Brush? color = null, double? maxWidth = null)
        {
            var tb = new TextBlock
            {
                Text = texto ?? "",
                FontSize = tamano,
                FontWeight = negrita ? FontWeights.Bold : FontWeights.Normal,
                Foreground = color ?? Brushes.Black,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            if (maxWidth.HasValue)
                tb.MaxWidth = Math.Max(maxWidth.Value, 1);

            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, yCentro - tamano * 0.75);
            canvas.Children.Add(tb);
        }

        /// <summary>Marcador visual de codigo de barras (no es un codigo real, solo referencia grafica).</summary>
        private static void DibujarBarcode(Canvas canvas, double x, double y, double ancho, double alto, string valor)
        {
            ancho = Math.Max(ancho, 20);
            alto = Math.Max(alto, 12);

            var contenedor = new Canvas { Width = ancho, Height = alto };

            var rnd = new Random((valor ?? "").GetHashCode());
            double cursor = 0;
            while (cursor < ancho - 2)
            {
                double grosor = rnd.Next(2, 5);
                if (rnd.Next(3) != 0)
                {
                    contenedor.Children.Add(new Rectangle
                    {
                        Width = grosor,
                        Height = alto,
                        Fill = Brushes.Black
                    }.PosicionadoEn(cursor, 0));
                }
                cursor += grosor + 2;
            }

            Canvas.SetLeft(contenedor, x);
            Canvas.SetTop(contenedor, y);
            canvas.Children.Add(contenedor);
        }
    }

    internal static class UiElementExtensions
    {
        public static T PosicionadoEn<T>(this T elemento, double x, double y) where T : UIElement
        {
            Canvas.SetLeft(elemento, x);
            Canvas.SetTop(elemento, y);
            return elemento;
        }
    }
}

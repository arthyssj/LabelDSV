using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EtiquetasDSV
{
    /// <summary>
    /// Dibuja una representacion aproximada de la etiqueta en un Canvas de
    /// WPF, usando las mismas coordenadas conceptuales que el ZPL (traducidas
    /// a la orientacion "legible" horizontal, tal como sale impresa).
    ///
    /// No es un renderizado pixel-perfecto del ZPL: es una guia visual para
    /// ubicar los datos antes de imprimir. Para verificar el diseno exacto
    /// se sigue usando labelary.com/viewer.html.
    /// </summary>
    public static class VistaPrevia
    {
        // El ZPL trabaja en un lienzo de 1950 x 1200 puntos (ya orientado
        // como se lee la etiqueta). Escalamos a los pixeles del Canvas.
        private const double AnchoZpl = 1950;
        private const double AltoZpl = 1200;

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

            double escala = canvas.Width / AnchoZpl;
            double alto = AltoZpl * escala;
            canvas.Height = alto;

            double X(double v) => (AnchoZpl - v) * escala;
            double Y(double v) => v * escala;

            // Fondo / marco general
            canvas.Children.Add(new Rectangle
            {
                Width = canvas.Width,
                Height = canvas.Height,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            }.PosicionadoEn(0, 0));

            string titulo = string.IsNullOrWhiteSpace(cfg.Titulo) ? "DSV" : cfg.Titulo.ToUpperInvariant();
            string fecha = DateTime.Now.ToString(
                string.IsNullOrWhiteSpace(cfg.FormatoFecha) ? "dd-MM-yyyy" : cfg.FormatoFecha);

            // ---------------- Header negro ----------------
            double hx0 = X(1070 + 130);
            double hx1 = X(1070);
            canvas.Children.Add(new Rectangle
            {
                Width = hx1 - hx0,
                Height = canvas.Height,
                Fill = Brushes.Black
            }.PosicionadoEn(hx0, 0));

            AgregarTexto(canvas, X(1085) - 40, Y(60), "EATON", 11, true, Brushes.White);
            AgregarTexto(canvas, X(1085) - 60, alto / 2 - 10, titulo, 16, true, Brushes.White);

            // ---------------- Part Number ----------------
            Caja(canvas, X, Y, 60, 60, 1150, 540);
            AgregarTexto(canvas, X(1000), Y(90), "PART NUMBER", 9, false);
            AgregarTexto(canvas, X(880), Y(130), string.IsNullOrWhiteSpace(parte) ? "(vacio)" : parte, 15, true);
            AgregarTexto(canvas, X(600), Y(300), "|| ||| | |||| ||", 9, false);
            AgregarTexto(canvas, X(600), Y(330), parte, 8, false);

            // ---------------- Quantity ----------------
            Caja(canvas, X, Y, 1150, 60, 1890, 800);
            AgregarTexto(canvas, X(1750), Y(90), "QUANTITY", 9, false);
            AgregarTexto(canvas, X(1650), Y(130), string.IsNullOrWhiteSpace(cantidad) ? "(vacio)" : cantidad, 15, true);
            AgregarTexto(canvas, X(1400), Y(300), "|| ||| | |||| ||", 9, false);
            AgregarTexto(canvas, X(1400), Y(330), cantidad, 8, false);

            // ---------------- Reference ----------------
            AgregarTexto(canvas, X(60) - 100, Y(570), "REFERENCE", 9, false);
            AgregarTexto(canvas, X(60) - 140, Y(610), string.IsNullOrWhiteSpace(referencia) ? "(vacio)" : referencia, 13, true);
            AgregarTexto(canvas, X(60) - 100, Y(750), "|| ||| | |||| ||", 8, false);

            // ---------------- Tipo ----------------
            Caja(canvas, X, Y, 60, 800, 300, 940);
            AgregarTexto(canvas, X(60) - 30, Y(850), tipo, 11, true);

            // ---------------- From ----------------
            AgregarTexto(canvas, X(60) - 40, Y(970), "FROM", 8, false);
            AgregarTexto(canvas, X(60) - 40, Y(1000), cfg.FromLinea1, 8, false);
            AgregarTexto(canvas, X(60) - 40, Y(1025), cfg.FromLinea2, 8, false);

            // ---------------- Notes / fecha ----------------
            AgregarTexto(canvas, X(60) - 40, Y(1060), "NOTES:", 8, false);
            if (!string.IsNullOrWhiteSpace(notas))
                AgregarTexto(canvas, X(60) - 40, Y(1085), notas, 8, false);

            AgregarTexto(canvas, X(60) - 40, Y(1150), $"DATE: {fecha}", 8, false);
        }

        private static void Caja(Canvas canvas, Func<double, double> X, Func<double, double> Y,
            double x0, double y0, double x1, double y1)
        {
            canvas.Children.Add(new Rectangle
            {
                Width = X(x0) - X(x1),
                Height = Y(y1) - Y(y0),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            }.PosicionadoEn(X(x1), Y(y0)));
        }

        private static void AgregarTexto(Canvas canvas, double x, double y, string texto,
            double tamano, bool negrita, Brush? color = null)
        {
            var tb = new TextBlock
            {
                Text = texto ?? "",
                FontSize = tamano,
                FontWeight = negrita ? FontWeights.Bold : FontWeights.Normal,
                Foreground = color ?? Brushes.Black
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            canvas.Children.Add(tb);
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

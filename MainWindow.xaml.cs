using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EtiquetasDSV
{
    public partial class MainWindow : Window
    {
        private Config _cfg = Config.Cargar();
        private readonly ObservableCollection<FilaLote> _filasLote = new();
        private CancellationTokenSource? _ctsLote;

        public MainWindow()
        {
            InitializeComponent();

            GridLote.ItemsSource = _filasLote;
            _filasLote.CollectionChanged += (s, e) => ActualizarConteo();

            CargarConfigEnUI();
            RefrescarImpresoras();
            ActualizarVistaPrevia();
            ActualizarConteo();
        }

        // ================================================================
        // CONFIGURACION
        // ================================================================
        private void CargarConfigEnUI()
        {
            TxtTitulo.Text = _cfg.Titulo;
            TxtFrom1.Text = _cfg.FromLinea1;
            TxtFrom2.Text = _cfg.FromLinea2;
            TxtFormatoFecha.Text = _cfg.FormatoFecha;
            TxtCopias.Text = _cfg.Copias.ToString();
            TxtCopiasLote.Text = _cfg.Copias.ToString();
        }

        private void BtnGuardarConfig_Click(object sender, RoutedEventArgs e)
        {
            _cfg.Titulo = TxtTitulo.Text;
            _cfg.FromLinea1 = TxtFrom1.Text;
            _cfg.FromLinea2 = TxtFrom2.Text;
            _cfg.FormatoFecha = TxtFormatoFecha.Text;

            if (CmbImpresora.SelectedItem is string impresora)
                _cfg.Impresora = impresora;

            if (int.TryParse(TxtCopias.Text, out int copias))
                _cfg.Copias = copias;

            _cfg.Guardar();
            CustomMessageBox.Show("Configuracion guardada.", "Listo",
                MessageBoxButton.OK, MessageBoxImage.Information);

            ActualizarVistaPrevia();
        }

        // ================================================================
        // IMPRESORAS
        // ================================================================
        private void RefrescarImpresoras()
        {
            var impresoras = PrinterService.ListarImpresoras();

            CmbImpresora.ItemsSource = impresoras;
            CmbImpresoraLote.ItemsSource = impresoras;

            string preferida = !string.IsNullOrEmpty(_cfg.Impresora) && impresoras.Contains(_cfg.Impresora)
                ? _cfg.Impresora
                : impresoras.FirstOrDefault() ?? "";

            CmbImpresora.SelectedItem = preferida;
            CmbImpresoraLote.SelectedItem = preferida;
        }

        private void BtnActualizarImpresoras_Click(object sender, RoutedEventArgs e)
        {
            RefrescarImpresoras();
        }

        // ================================================================
        // TAB INDIVIDUAL
        // ================================================================
        private void Campo_Cambiado(object sender, RoutedEventArgs e) => ActualizarVistaPrevia();

        private void BtnVistaPrevia_Click(object sender, RoutedEventArgs e) => ActualizarVistaPrevia();

        private void ActualizarVistaPrevia()
        {
            if (CanvasPreview == null) return;

            string tipo = (CmbTipo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "IN-BOND";

            VistaPrevia.Dibujar(
                CanvasPreview,
                TxtParte?.Text ?? "",
                TxtCantidad?.Text ?? "",
                TxtReferencia?.Text ?? "",
                tipo,
                TxtNotas?.Text ?? "",
                _cfg);
        }

        private void BtnImprimirIndividual_Click(object sender, RoutedEventArgs e)
        {
            string impresora = CmbImpresora.SelectedItem as string ?? "";

            if (string.IsNullOrWhiteSpace(impresora))
            {
                CustomMessageBox.Show("Selecciona una impresora.", "Falta impresora",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtParte.Text))
            {
                CustomMessageBox.Show("Captura el Part Number.", "Falta dato",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int copias = int.TryParse(TxtCopias.Text, out int c) ? c : 4;
            string tipo = (CmbTipo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "IN-BOND";

            string zpl = ZplBuilder.Construir(
                TxtParte.Text, TxtCantidad.Text, TxtReferencia.Text,
                tipo, TxtNotas.Text, _cfg, copias);

            try
            {
                PrinterService.EnviarZpl(impresora, zpl);
                CustomMessageBox.Show($"{copias} etiquetas enviadas.", "Listo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(ex.Message, "Error al imprimir",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================================================================
        // TAB LOTE
        // ================================================================
        private void BtnAgregarFila_Click(object sender, RoutedEventArgs e)
        {
            _filasLote.Add(new FilaLote());
        }

        private void BtnEliminarFila_Click(object sender, RoutedEventArgs e)
        {
            if (GridLote.SelectedItem is FilaLote fila)
                _filasLote.Remove(fila);
        }

        private void BtnLimpiarTabla_Click(object sender, RoutedEventArgs e)
        {
            if (_filasLote.Count == 0) return;

            var resultado = CustomMessageBox.Show("¿Borrar todas las filas del lote?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
                _filasLote.Clear();
        }

        private void BtnPegar_Click(object sender, RoutedEventArgs e) => PegarDelPortapapeles();

        /// <summary>
        /// Pega texto tabulado (copiado de Excel) en filas nuevas de la tabla.
        /// Orden esperado de columnas: Part Number, Quantity, Reference, Tipo, Notes.
        /// </summary>
        private void PegarDelPortapapeles()
        {
            if (!Clipboard.ContainsText())
            {
                CustomMessageBox.Show("No hay texto en el portapapeles.", "Portapapeles vacio",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string datos = Clipboard.GetText();
            var lineas = datos.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            int agregadas = 0;
            foreach (var linea in lineas)
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;

                var partes = linea.Split('\t');
                string parte = partes.Length > 0 ? partes[0].Trim() : "";
                string cantidad = partes.Length > 1 ? partes[1].Trim() : "";
                string referencia = partes.Length > 2 ? partes[2].Trim() : "";
                string tipo = partes.Length > 3 && !string.IsNullOrWhiteSpace(partes[3])
                    ? partes[3].Trim() : "IN-BOND";
                string notas = partes.Length > 4 ? partes[4].Trim() : "";

                if (string.IsNullOrWhiteSpace(parte)) continue;

                _filasLote.Add(new FilaLote
                {
                    Parte = parte,
                    Cantidad = cantidad,
                    Referencia = referencia,
                    Tipo = tipo,
                    Notas = notas
                });
                agregadas++;
            }

            ActualizarConteo();

            if (agregadas == 0)
            {
                CustomMessageBox.Show(
                    "No se reconocieron filas validas. Copia las columnas Part Number, " +
                    "Quantity, Reference, Tipo y Notes directamente desde Excel.",
                    "Sin datos", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ActualizarConteo()
        {
            if (TxtConteo == null) return;

            int modelos = _filasLote.Count;
            int copias = int.TryParse(TxtCopiasLote?.Text, out int c) ? c : 1;
            int total = modelos * Math.Max(1, copias);

            TxtConteo.Text = $"Modelos en lote: {modelos}   |   Total de etiquetas: {total}";
        }

        private async void BtnImprimirLote_Click(object sender, RoutedEventArgs e)
        {
            if (_filasLote.Count == 0)
            {
                CustomMessageBox.Show("Agrega al menos una fila.", "Lote vacio",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string impresora = CmbImpresoraLote.SelectedItem as string ?? "";
            if (string.IsNullOrWhiteSpace(impresora))
            {
                CustomMessageBox.Show("Selecciona una impresora.", "Falta impresora",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int copias = int.TryParse(TxtCopiasLote.Text, out int cop) ? cop : 4;
            var filas = _filasLote.ToList();
            int total = filas.Count * Math.Max(1, copias);

            _ctsLote = new CancellationTokenSource();
            var token = _ctsLote.Token;

            BarraProgreso.Maximum = total;
            BarraProgreso.Value = 0;
            BtnImprimirLote.IsEnabled = false;
            BtnCancelarLote.IsEnabled = true;
            TxtEstadoLote.Text = $"Imprimiendo 0 de {total}...";

            int enviados = 0;

            try
            {
                await Task.Run(() =>
                {
                    foreach (var fila in filas)
                    {
                        if (token.IsCancellationRequested) break;

                        if (string.IsNullOrWhiteSpace(fila.Parte)) continue;

                        string zpl = ZplBuilder.Construir(
                            fila.Parte, fila.Cantidad, fila.Referencia,
                            fila.Tipo, fila.Notas, _cfg, copias);

                        PrinterService.EnviarZpl(impresora, zpl);
                        enviados += copias;

                        int enviadosActual = enviados;
                        Dispatcher.Invoke(() =>
                        {
                            BarraProgreso.Value = enviadosActual;
                            TxtEstadoLote.Text = $"Imprimiendo {enviadosActual} de {total}...";
                        });
                    }
                }, token);

                if (token.IsCancellationRequested)
                {
                    TxtEstadoLote.Text = $"Cancelado. Se alcanzaron a enviar {enviados} de {total}.";
                }
                else
                {
                    TxtEstadoLote.Text = $"Listo: {enviados} etiquetas enviadas.";
                    CustomMessageBox.Show($"{enviados} etiquetas enviadas.", "Lote terminado",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                TxtEstadoLote.Text = "Error durante la impresion.";
                CustomMessageBox.Show(ex.Message, "Error al imprimir",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnImprimirLote.IsEnabled = true;
                BtnCancelarLote.IsEnabled = false;
                _ctsLote?.Dispose();
                _ctsLote = null;
            }
        }

        private void BtnCancelarLote_Click(object sender, RoutedEventArgs e)
        {
            _ctsLote?.Cancel();
            TxtEstadoLote.Text = "Cancelando... (termina la etiqueta actual)";
        }

        private void BtnLimpiarConteo_Click(object sender, RoutedEventArgs e)
        {
            BarraProgreso.Value = 0;
            TxtEstadoLote.Text = "Listo.";
            ActualizarConteo();
        }
    }
}

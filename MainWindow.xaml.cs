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
        private readonly ObservableCollection<FilaPallet> _filasPallet = new();
        private CancellationTokenSource? _ctsLote;
        private CancellationTokenSource? _ctsPallet;

        public MainWindow()
        {
            InitializeComponent();

            GridLote.ItemsSource = _filasLote;
            _filasLote.CollectionChanged += (s, e) => ActualizarConteo();

            GridPallet.ItemsSource = _filasPallet;
            _filasPallet.CollectionChanged += (s, e) => ActualizarConteoPallet();

            CargarConfigEnUI();
            RefrescarImpresoras();
            ActualizarVistaPrevia();
            ActualizarContadoresIndividual();
            ActualizarConteo();
            ActualizarConteoPallet();
        }

        // ================================================================
        // CONFIGURACION
        // ================================================================
        private void CargarConfigEnUI()
        {
            TxtTitulo.Text = _cfg.Titulo;
            TxtFormatoFecha.Text = _cfg.FormatoFecha;
            TxtCopias.Text = _cfg.Copias.ToString();
            TxtCopiasLote.Text = _cfg.Copias.ToString();
            TxtCopiasPallet.Text = _cfg.Copias.ToString();

            TxtProximaReferencia.Text = ProximoContadorReferencia().ToString();
        }

        private int ProximoContadorReferencia()
        {
            string hoy = DateTime.Now.Date.ToString("yyyy-MM-dd");
            return _cfg.UltimaFechaReferencia == hoy ? _cfg.UltimoContadorReferencia + 1 : 1;
        }

        private void BtnReiniciarContadorRef_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtProximaReferencia.Text, out int proximo)
                || proximo < 1 || proximo > Config.MaxContadorReferencia)
            {
                CustomMessageBox.Show($"Captura un numero entre 1 y {Config.MaxContadorReferencia}.", "Dato invalido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _cfg.UltimaFechaReferencia = DateTime.Now.Date.ToString("yyyy-MM-dd");
            _cfg.UltimoContadorReferencia = proximo - 1;
            _cfg.Guardar();

            CustomMessageBox.Show($"Listo. La proxima referencia sera RF...-{proximo}.", "Contador actualizado",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnGuardarConfig_Click(object sender, RoutedEventArgs e)
        {
            _cfg.Titulo = TxtTitulo.Text;
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
            CmbImpresoraPallet.ItemsSource = impresoras;

            string preferida = !string.IsNullOrEmpty(_cfg.Impresora) && impresoras.Contains(_cfg.Impresora)
                ? _cfg.Impresora
                : impresoras.FirstOrDefault() ?? "";

            CmbImpresora.SelectedItem = preferida;
            CmbImpresoraLote.SelectedItem = preferida;
            CmbImpresoraPallet.SelectedItem = preferida;
        }

        private void BtnActualizarImpresoras_Click(object sender, RoutedEventArgs e)
        {
            RefrescarImpresoras();
        }

        // ================================================================
        // TAB INDIVIDUAL
        // ================================================================
        private void Campo_Cambiado(object sender, RoutedEventArgs e)
        {
            ActualizarVistaPrevia();
            ActualizarContadoresIndividual();
        }

        private void ActualizarContadoresIndividual()
        {
            // TxtContadorNotas es el ultimo de este grupo en el XAML: si ya
            // existe, los demas controles referenciados abajo tambien.
            if (TxtContadorNotas == null) return;

            TxtContadorParte.Text = $"{TxtParte.Text.Length}/30";
            TxtContadorCantidad.Text = $"{TxtCantidad.Text.Length}/10";
            TxtContadorReferencia.Text = $"{TxtReferencia.Text.Length}/18";
            TxtContadorNotas.Text = $"{TxtNotas.Text.Length}/55";
        }

        private void ChkReferenciaAuto_Checked(object sender, RoutedEventArgs e)
        {
            TxtReferencia.IsReadOnly = true;
            TxtReferencia.Text = _cfg.GenerarReferencia();
        }

        private void ChkReferenciaAuto_Unchecked(object sender, RoutedEventArgs e)
        {
            TxtReferencia.IsReadOnly = false;
        }

        private void BtnGenerarReferencia_Click(object sender, RoutedEventArgs e)
        {
            if (ChkReferenciaAuto.IsChecked == true)
                TxtReferencia.Text = _cfg.GenerarReferencia();
            else
                ChkReferenciaAuto.IsChecked = true;
        }

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
        // TAB PALLET (en lote)
        // ================================================================
        private void BtnAgregarFilaPallet_Click(object sender, RoutedEventArgs e)
        {
            _filasPallet.Add(new FilaPallet());
        }

        private void BtnEliminarFilaPallet_Click(object sender, RoutedEventArgs e)
        {
            if (GridPallet.SelectedItem is FilaPallet fila)
                _filasPallet.Remove(fila);
        }

        private void BtnDuplicarFilaPallet_Click(object sender, RoutedEventArgs e)
        {
            if (GridPallet.SelectedItem is not FilaPallet fila)
            {
                CustomMessageBox.Show("Selecciona una fila para duplicar.", "Sin seleccion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var copia = new FilaPallet
            {
                Pallet = fila.Pallet,
                Referencia = fila.Referencia
            };

            int indice = _filasPallet.IndexOf(fila);
            _filasPallet.Insert(indice + 1, copia);
        }

        private void BtnLimpiarTablaPallet_Click(object sender, RoutedEventArgs e)
        {
            if (_filasPallet.Count == 0) return;

            var resultado = CustomMessageBox.Show("¿Borrar todas las filas del lote de pallets?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
                _filasPallet.Clear();
        }

        private void BtnPegarPallet_Click(object sender, RoutedEventArgs e) => PegarDelPortapapelesPallet();

        /// <summary>
        /// Pega texto tabulado (copiado de Excel) en filas nuevas de la tabla.
        /// Orden esperado de columnas: Pallet, Reference.
        /// </summary>
        private void PegarDelPortapapelesPallet()
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
                string pallet = partes.Length > 0 ? partes[0].Trim() : "";
                string referencia = partes.Length > 1 ? partes[1].Trim() : "";

                if (string.IsNullOrWhiteSpace(pallet)) continue;

                _filasPallet.Add(new FilaPallet
                {
                    Pallet = pallet,
                    Referencia = referencia
                });
                agregadas++;
            }

            ActualizarConteoPallet();

            if (agregadas == 0)
            {
                CustomMessageBox.Show(
                    "No se reconocieron filas validas. Copia las columnas Pallet y " +
                    "Reference directamente desde Excel.",
                    "Sin datos", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ActualizarConteoPallet()
        {
            if (TxtConteoPallet == null) return;

            int pallets = _filasPallet.Count;
            int copias = int.TryParse(TxtCopiasPallet?.Text, out int c) ? c : 1;
            int total = pallets * Math.Max(1, copias);

            TxtConteoPallet.Text = $"Pallets en lote: {pallets}   |   Total de etiquetas: {total}";
        }

        private async void BtnImprimirLotePallet_Click(object sender, RoutedEventArgs e)
        {
            if (_filasPallet.Count == 0)
            {
                CustomMessageBox.Show("Agrega al menos una fila.", "Lote vacio",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string impresora = CmbImpresoraPallet.SelectedItem as string ?? "";
            if (string.IsNullOrWhiteSpace(impresora))
            {
                CustomMessageBox.Show("Selecciona una impresora.", "Falta impresora",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int copias = int.TryParse(TxtCopiasPallet.Text, out int cop) ? cop : 4;
            var filas = _filasPallet.ToList();
            int total = filas.Count * Math.Max(1, copias);

            _ctsPallet = new CancellationTokenSource();
            var token = _ctsPallet.Token;

            BarraProgresoPallet.Maximum = total;
            BarraProgresoPallet.Value = 0;
            BtnImprimirPallet.IsEnabled = false;
            BtnCancelarPallet.IsEnabled = true;
            TxtEstadoLotePallet.Text = $"Imprimiendo 0 de {total}...";

            int enviados = 0;

            try
            {
                await Task.Run(() =>
                {
                    foreach (var fila in filas)
                    {
                        if (token.IsCancellationRequested) break;

                        if (string.IsNullOrWhiteSpace(fila.Pallet)) continue;

                        string zpl = ZplBuilder.ConstruirPallet(
                            fila.Referencia, fila.Pallet, _cfg, copias);

                        PrinterService.EnviarZpl(impresora, zpl);
                        enviados += copias;

                        int enviadosActual = enviados;
                        Dispatcher.Invoke(() =>
                        {
                            BarraProgresoPallet.Value = enviadosActual;
                            TxtEstadoLotePallet.Text = $"Imprimiendo {enviadosActual} de {total}...";
                        });
                    }
                }, token);

                if (token.IsCancellationRequested)
                {
                    TxtEstadoLotePallet.Text = $"Cancelado. Se alcanzaron a enviar {enviados} de {total}.";
                }
                else
                {
                    TxtEstadoLotePallet.Text = $"Listo: {enviados} etiquetas enviadas.";
                    CustomMessageBox.Show($"{enviados} etiquetas enviadas.", "Lote terminado",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                TxtEstadoLotePallet.Text = "Error durante la impresion.";
                CustomMessageBox.Show(ex.Message, "Error al imprimir",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnImprimirPallet.IsEnabled = true;
                BtnCancelarPallet.IsEnabled = false;
                _ctsPallet?.Dispose();
                _ctsPallet = null;
            }
        }

        private void BtnCancelarLotePallet_Click(object sender, RoutedEventArgs e)
        {
            _ctsPallet?.Cancel();
            TxtEstadoLotePallet.Text = "Cancelando... (termina la etiqueta actual)";
        }

        private void BtnLimpiarConteoPallet_Click(object sender, RoutedEventArgs e)
        {
            BarraProgresoPallet.Value = 0;
            TxtEstadoLotePallet.Text = "Listo.";
            ActualizarConteoPallet();
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

        private void BtnDuplicarFila_Click(object sender, RoutedEventArgs e)
        {
            if (GridLote.SelectedItem is not FilaLote fila)
            {
                CustomMessageBox.Show("Selecciona una fila para duplicar.", "Sin seleccion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var copia = new FilaLote
            {
                Parte = fila.Parte,
                Cantidad = fila.Cantidad,
                Referencia = fila.Referencia,
                Tipo = fila.Tipo,
                Notas = fila.Notas
            };

            int indice = _filasLote.IndexOf(fila);
            _filasLote.Insert(indice + 1, copia);
        }

        /// <summary>
        /// Rellena la columna Reference solo en las filas que esten vacias,
        /// una referencia automatica distinta por fila (consecutiva). Las
        /// filas que ya traigan Reference (tecleada o pegada de Excel) no
        /// se tocan.
        /// </summary>
        private void BtnGenerarReferenciasLote_Click(object sender, RoutedEventArgs e)
        {
            if (_filasLote.Count == 0)
            {
                CustomMessageBox.Show("Agrega al menos una fila.", "Lote vacio",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var fila in _filasLote)
            {
                if (string.IsNullOrWhiteSpace(fila.Referencia))
                    fila.Referencia = _cfg.GenerarReferencia();
            }
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

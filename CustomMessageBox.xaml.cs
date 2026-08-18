using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace EtiquetasDSV
{
    /// <summary>
    /// Dialogo modal con el mismo tema oscuro de la app (reemplaza el
    /// System.Windows.MessageBox nativo, que siempre se ve claro/blanco
    /// sin importar el tema). Uso: CustomMessageBox.Show(...) con la misma
    /// firma que MessageBox.Show, retorna MessageBoxResult.
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public MessageBoxResult Resultado { get; private set; } = MessageBoxResult.None;

        private CustomMessageBox(string mensaje, string titulo, MessageBoxButton boton, MessageBoxImage icono)
        {
            InitializeComponent();

            TxtTitulo.Text = titulo;
            TxtMensaje.Text = mensaje;

            ConfigurarIcono(icono);
            ConfigurarBotones(boton);
        }

        private void ConfigurarIcono(MessageBoxImage icono)
        {
            var (simbolo, color) = icono switch
            {
                MessageBoxImage.Error => ("X", (Brush)FindResource("AccentRed")),
                MessageBoxImage.Warning => ("!", (Brush)FindResource("AccentAmber")),
                MessageBoxImage.Question => ("?", (Brush)FindResource("AccentBlue")),
                _ => ("i", (Brush)FindResource("AccentBlue")),
            };

            TxtIcono.Text = simbolo;
            IconoFondo.Background = color;
        }

        private void ConfigurarBotones(MessageBoxButton boton)
        {
            BtnSi.Visibility = Visibility.Collapsed;
            BtnNo.Visibility = Visibility.Collapsed;
            BtnCancelar.Visibility = Visibility.Collapsed;
            BtnOk.Visibility = Visibility.Collapsed;

            switch (boton)
            {
                case MessageBoxButton.YesNo:
                    BtnSi.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNoCancel:
                    BtnSi.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    BtnCancelar.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.OKCancel:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnCancelar.Visibility = Visibility.Visible;
                    break;
                default:
                    BtnOk.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Cerrar(MessageBoxResult resultado)
        {
            Resultado = resultado;
            Close();
        }

        private void BtnSi_Click(object sender, RoutedEventArgs e) => Cerrar(MessageBoxResult.Yes);
        private void BtnNo_Click(object sender, RoutedEventArgs e) => Cerrar(MessageBoxResult.No);
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Cerrar(MessageBoxResult.Cancel);
        private void BtnOk_Click(object sender, RoutedEventArgs e) => Cerrar(MessageBoxResult.OK);

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (BtnNo.Visibility == Visibility.Visible) Cerrar(MessageBoxResult.No);
                else if (BtnCancelar.Visibility == Visibility.Visible) Cerrar(MessageBoxResult.Cancel);
                else if (BtnOk.Visibility == Visibility.Visible) Cerrar(MessageBoxResult.OK);
            }
            else if (e.Key == Key.Enter)
            {
                if (BtnSi.Visibility == Visibility.Visible) Cerrar(MessageBoxResult.Yes);
                else if (BtnOk.Visibility == Visibility.Visible) Cerrar(MessageBoxResult.OK);
            }
        }

        public static MessageBoxResult Show(string mensaje, string titulo,
            MessageBoxButton boton = MessageBoxButton.OK, MessageBoxImage icono = MessageBoxImage.None)
        {
            var dialogo = new CustomMessageBox(mensaje, titulo, boton, icono);

            var owner = Application.Current?.MainWindow;
            if (owner != null && owner != dialogo && owner.IsLoaded)
            {
                dialogo.Owner = owner;
            }
            else
            {
                dialogo.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            dialogo.ShowDialog();
            return dialogo.Resultado;
        }
    }
}

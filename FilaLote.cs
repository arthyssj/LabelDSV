using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EtiquetasDSV
{
    /// <summary>Una fila editable de la tabla de impresion por lote.</summary>
    public class FilaLote : INotifyPropertyChanged
    {
        public const int MaxParte = 30;
        public const int MaxCantidad = 10;
        public const int MaxReferencia = 18;
        public const int MaxNotas = 55;

        private string _parte = "";
        private string _cantidad = "";
        private string _referencia = "";
        private string _tipo = "INBOND";
        private string _notas = "";

        public string Parte
        {
            get => _parte;
            set { _parte = Truncar(value, MaxParte); OnChanged(); }
        }

        public string Cantidad
        {
            get => _cantidad;
            set { _cantidad = Truncar(value, MaxCantidad); OnChanged(); }
        }

        public string Referencia
        {
            get => _referencia;
            set { _referencia = Truncar(value, MaxReferencia); OnChanged(); }
        }

        public string Tipo
        {
            get => _tipo;
            set { _tipo = value; OnChanged(); }
        }

        public string Notas
        {
            get => _notas;
            set { _notas = Truncar(value, MaxNotas); OnChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private static string Truncar(string? valor, int max) =>
            string.IsNullOrEmpty(valor) || valor.Length <= max ? valor ?? "" : valor.Substring(0, max);

        private void OnChanged([CallerMemberName] string? nombre = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
        }
    }
}

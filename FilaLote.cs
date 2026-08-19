using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EtiquetasDSV
{
    /// <summary>Una fila editable de la tabla de impresion por lote.</summary>
    public class FilaLote : INotifyPropertyChanged
    {
        private string _parte = "";
        private string _cantidad = "";
        private string _referencia = "";
        private string _tipo = "INBOND";
        private string _notas = "";

        public string Parte
        {
            get => _parte;
            set { _parte = value; OnChanged(); }
        }

        public string Cantidad
        {
            get => _cantidad;
            set { _cantidad = value; OnChanged(); }
        }

        public string Referencia
        {
            get => _referencia;
            set { _referencia = value; OnChanged(); }
        }

        public string Tipo
        {
            get => _tipo;
            set { _tipo = value; OnChanged(); }
        }

        public string Notas
        {
            get => _notas;
            set { _notas = value; OnChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnChanged([CallerMemberName] string? nombre = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
        }
    }
}

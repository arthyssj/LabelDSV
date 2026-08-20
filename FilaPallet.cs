using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EtiquetasDSV
{
    /// <summary>Una fila editable de la tabla de impresion de pallets en lote.</summary>
    public class FilaPallet : INotifyPropertyChanged
    {
        public const int MaxReferencia = 18;

        private string _pallet = "";
        private string _referencia = "";

        public string Pallet
        {
            get => _pallet;
            set { _pallet = value; OnChanged(); }
        }

        public string Referencia
        {
            get => _referencia;
            set { _referencia = Truncar(value, MaxReferencia); OnChanged(); }
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

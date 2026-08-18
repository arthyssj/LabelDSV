using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace EtiquetasDSV
{
    /// <summary>
    /// Enumera impresoras y envia ZPL crudo, todo via P/Invoke directo a
    /// winspool.drv. No depende de System.Drawing.Printing ni de ninguna
    /// referencia extra: solo la API nativa de Windows.
    /// </summary>
    public static class PrinterService
    {
        // ----------------------------------------------------------------
        // Estructuras e imports para IMPRIMIR (RAW)
        // ----------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFOA di);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        // ----------------------------------------------------------------
        // Estructuras e imports para LISTAR impresoras (EnumPrinters)
        // ----------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PRINTER_INFO_4
        {
            [MarshalAs(UnmanagedType.LPTStr)] public string pPrinterName;
            [MarshalAs(UnmanagedType.LPTStr)] public string? pServerName;
            public uint Attributes;
        }

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool EnumPrinters(
            int Flags, string? Name, uint Level,
            IntPtr pPrinterEnum, uint cbBuf,
            out uint pcbNeeded, out uint pcReturned);

        private const int PRINTER_ENUM_LOCAL = 0x00000002;
        private const int PRINTER_ENUM_CONNECTIONS = 0x00000004;

        /// <summary>Lista los nombres de las impresoras instaladas/conectadas en Windows.</summary>
        public static List<string> ListarImpresoras()
        {
            var lista = new List<string>();

            // Primera llamada: solo para saber cuantos bytes se necesitan.
            EnumPrinters(PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS, null, 4,
                IntPtr.Zero, 0, out uint bytesNecesarios, out _);

            if (bytesNecesarios == 0)
                return lista; // no hay impresoras

            IntPtr buffer = Marshal.AllocHGlobal((int)bytesNecesarios);
            try
            {
                bool ok = EnumPrinters(PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS, null, 4,
                    buffer, bytesNecesarios, out _, out uint numImpresoras);

                if (!ok)
                    return lista;

                int tamanoStruct = Marshal.SizeOf<PRINTER_INFO_4>();

                for (int i = 0; i < numImpresoras; i++)
                {
                    IntPtr punteroActual = IntPtr.Add(buffer, i * tamanoStruct);
                    var info = Marshal.PtrToStructure<PRINTER_INFO_4>(punteroActual);
                    if (!string.IsNullOrWhiteSpace(info.pPrinterName))
                        lista.Add(info.pPrinterName);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            lista.Sort();
            return lista;
        }

        /// <summary>Manda una cadena ZPL cruda a la impresora indicada.</summary>
        public static void EnviarZpl(string nombreImpresora, string zpl)
        {
            if (string.IsNullOrWhiteSpace(nombreImpresora))
                throw new ArgumentException("No se especifico una impresora.");

            byte[] datos = Encoding.UTF8.GetBytes(zpl);

            if (!OpenPrinter(nombreImpresora, out IntPtr hPrinter, IntPtr.Zero))
                throw new InvalidOperationException(
                    $"No se pudo abrir la impresora '{nombreImpresora}'. " +
                    "Verifica el nombre exacto en Dispositivos e impresoras.");

            try
            {
                var di = new DOCINFOA
                {
                    pDocName = "Etiqueta DSV",
                    pOutputFile = null,
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, ref di))
                    throw new InvalidOperationException("La impresora no acepto el trabajo (StartDocPrinter).");

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        throw new InvalidOperationException("Fallo StartPagePrinter.");

                    IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(datos.Length);
                    try
                    {
                        Marshal.Copy(datos, 0, pUnmanagedBytes, datos.Length);

                        if (!WritePrinter(hPrinter, pUnmanagedBytes, datos.Length, out int escritos))
                            throw new InvalidOperationException("Fallo al escribir en la impresora.");

                        if (escritos != datos.Length)
                            throw new InvalidOperationException(
                                $"El envio quedo incompleto ({escritos} de {datos.Length} bytes).");
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pUnmanagedBytes);
                    }

                    EndPagePrinter(hPrinter);
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
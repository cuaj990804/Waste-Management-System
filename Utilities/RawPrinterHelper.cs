using System;
using System.Runtime.InteropServices;

public static class RawPrinterHelper
{
    // Estructura necesaria para StartDocPrinter
    [StructLayout(LayoutKind.Sequential)]
    public struct DOC_INFO_1
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDataType;
    }

    [DllImport("winspool.Drv", SetLastError = true)]
    public static extern bool OpenPrinter(string printerName, out IntPtr phPrinter, IntPtr pd);

    [DllImport("winspool.Drv", SetLastError = true)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOC_INFO_1 di1);

    [DllImport("winspool.Drv", SetLastError = true)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    public static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);

    public static void SendBytesToPrinter(string printerName, byte[] data)
    {
        IntPtr hPrinter = IntPtr.Zero;
        DOC_INFO_1 di1 = new DOC_INFO_1();
        di1.pDocName = "Etiqueta RAW";
        di1.pDataType = "RAW"; // Tipo de dato para impresión directa

        try
        {
            // Abrir la impresora
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            // Iniciar documento
            if (!StartDocPrinter(hPrinter, 1, ref di1))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            // Iniciar página
            if (!StartPagePrinter(hPrinter))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            // Escribir datos
            if (!WritePrinter(hPrinter, data, data.Length, out int dwWritten))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            // Finalizar página y documento
            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
        }
        finally
        {
            // Cerrar la impresora
            if (hPrinter != IntPtr.Zero)
                ClosePrinter(hPrinter);
        }
    }
}
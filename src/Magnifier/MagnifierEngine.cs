using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace DigitalScope.Magnifier;

public static class MagnifierEngine
{

    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")] private static extern bool   DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")] private static extern bool   DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern int    GetDIBits(IntPtr hdc, IntPtr hbmp, uint startScan, uint cLines,
                                                                     IntPtr lpvBits, ref BITMAPINFO lpbmi, uint uUsage);
    [DllImport("gdi32.dll")] private static extern bool   StretchBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
                                                                      IntPtr hdcSrc,  int xSrc,  int ySrc,  int wSrc,  int hSrc,
                                                                      uint dwRop);
    [DllImport("gdi32.dll")] private static extern bool   SetStretchBltMode(IntPtr hdc, int iStretchMode);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int    ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("user32.dll")] private static extern int    GetSystemMetrics(int nIndex);

    private const uint SRCCOPY       = 0x00CC0020;
    private const int  HALFTONE      = 4;
    private const uint DIB_RGB_COLORS = 0;

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint   biSize;
        public int    biWidth;
        public int    biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint   biCompression;
        public uint   biSizeImage;
        public int    biXPelsPerMeter;
        public int    biYPelsPerMeter;
        public uint   biClrUsed;
        public uint   biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public uint[] bmiColors;
    }



    public static unsafe void UpdateFrame(WriteableBitmap wb,
                                          int sourceX, int sourceY,
                                          int sourceW, int sourceH)
    {
        if (wb is null) return;

        int outW = (int)wb.PixelWidth;
        int outH = (int)wb.PixelHeight;

        if (outW <= 0 || outH <= 0 || sourceW <= 0 || sourceH <= 0) return;

        IntPtr screenDC = GetDC(IntPtr.Zero);
        if (screenDC == IntPtr.Zero) return;

        IntPtr memDC  = IntPtr.Zero;
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBmp  = IntPtr.Zero;

        try
        {
            memDC   = CreateCompatibleDC(screenDC);
            hBitmap = CreateCompatibleBitmap(screenDC, outW, outH);
            oldBmp  = SelectObject(memDC, hBitmap);

            SetStretchBltMode(memDC, HALFTONE);

            StretchBlt(memDC,    0, 0, outW, outH,
                       screenDC, sourceX, sourceY, sourceW, sourceH,
                       SRCCOPY);

            wb.Lock();
            try
            {
                var bmi = new BITMAPINFO
                {
                    bmiHeader = new BITMAPINFOHEADER
                    {
                        biSize        = (uint)sizeof(BITMAPINFOHEADER),
                        biWidth       = outW,
                        biHeight      = -outH,   // negative = top-down
                        biPlanes      = 1,
                        biBitCount    = 32,
                        biCompression = 0,       // BI_RGB
                    },
                    bmiColors = new uint[1],
                };

                GetDIBits(memDC, hBitmap, 0, (uint)outH,
                          wb.BackBuffer, ref bmi, DIB_RGB_COLORS);

                wb.AddDirtyRect(new System.Windows.Int32Rect(0, 0, outW, outH));
            }
            finally
            {
                wb.Unlock();
            }
        }
        finally
        {
            if (oldBmp  != IntPtr.Zero) SelectObject(memDC, oldBmp);
            if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
            if (memDC   != IntPtr.Zero) DeleteDC(memDC);
            ReleaseDC(IntPtr.Zero, screenDC);
        }
    }

    public static (int X, int Y, int W, int H) ComputeSourceRect(
        int centerX, int centerY,
        int outW,  int outH,
        double zoom)
    {
        int screenW = GetSystemMetrics(SM_CXSCREEN);
        int screenH = GetSystemMetrics(SM_CYSCREEN);

        int srcW = (int)Math.Ceiling(outW / zoom);
        int srcH = (int)Math.Ceiling(outH / zoom);
        int srcX = Math.Clamp(centerX - srcW / 2, 0, screenW - srcW);
        int srcY = Math.Clamp(centerY - srcH / 2, 0, screenH - srcH);
        return (srcX, srcY, srcW, srcH);
    }
}

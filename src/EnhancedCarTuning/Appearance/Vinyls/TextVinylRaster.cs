using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace EnhancedCarTuning.Appearance.Vinyls
{
    internal static class TextVinylRaster
    {
        internal const int Width = 2048, Height = 512;
        internal static byte[] Render(TextVinylData data)
        {
            var result = new byte[Width * Height * 4];
            if (string.IsNullOrWhiteSpace(data.Text)) return result;
            using (var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var format = new StringFormat(StringFormat.GenericTypographic))
            using (var glyphs = new GraphicsPath())
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                format.Alignment = StringAlignment.Near;
                format.LineAlignment = StringAlignment.Near;
                format.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
                bool bold = (data.Style & 1) != 0, italic = (data.Style & 2) != 0;
                float size = 352;
                Font font = null;
                float stroke = 0;
                RectangleF bounds;
                try
                {
                    while (true)
                    {
                        font?.Dispose();
                        font = EmbeddedAssets.RasterFont(data.Font, size, FontStyle.Regular);
                        glyphs.Reset();
                        glyphs.AddString(data.Text, font.FontFamily, (int)font.Style, font.Size,
                            PointF.Empty, format);
                        if (italic)
                            using (var slant = new Matrix(1, 0, -.2f, 1, 0, 0)) glyphs.Transform(slant);
                        bounds = glyphs.GetBounds();
                        stroke = bold ? Math.Max(1.25f, size * .035f) : 0;
                        if ((bounds.Width + stroke <= Width - 64 && bounds.Height + stroke <= Height - 48) || size <= 12) break;
                        size *= .9f;
                    }
                    using (var center = new Matrix())
                    {
                        center.Translate((Width - bounds.Width) / 2 - bounds.X, (Height - bounds.Height) / 2 - bounds.Y);
                        glyphs.Transform(center);
                    }
                    if (bold)
                        using (var pen = new Pen(Color.White, stroke) { LineJoin = LineJoin.Round })
                            graphics.DrawPath(pen, glyphs);
                    graphics.FillPath(Brushes.White, glyphs);
                }
                finally { font?.Dispose(); }
                var bits = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var row = new byte[Width * 4];
                    bool visible = false;
                    for (int y = 0; y < Height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(bits.Scan0, y * bits.Stride), row, 0, row.Length);
                        for (int x = 0; x < Width; x++)
                        {
                            int offset = ((Height - 1 - y) * Width + x) * 4;
                            // Unity textures use straight alpha. Writing RGB=alpha here
                            // premultiplies the glyph and creates a dark/black fringe when
                            // Unity's bilinear filtering interpolates the transparent edge.
                            // White even under zero alpha prevents dark color bleeding.
                            result[offset] = result[offset + 1] = result[offset + 2] = 255;
                            result[offset + 3] = row[x * 4 + 3];
                            visible |= result[offset + 3] != 0;
                        }
                    }
                    if (!visible) throw new InvalidOperationException("Text raster has no visible glyphs.");
                }
                finally { bitmap.UnlockBits(bits); }
            }
            return result;
        }
    }
}

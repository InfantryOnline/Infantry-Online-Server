using System.Drawing;
using System.Drawing.Drawing2D;

namespace InfMapEditor.Helpers
{
    /// <summary>
    /// Provides Extension Methods for the System.Drawing.Bitmap class.
    /// </summary>
    public static class BitmapExtensions
    {
        /// <summary>
        /// Resizes the bitmap to the specified width and height.
        /// </summary>
        /// <param name="src">Source bitmap</param>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        /// <param name="cq"></param>
        /// <param name="im"></param>
        /// <param name="sm"></param>
        /// <returns>The resized bitmap</returns>
        public static Bitmap Resize(this Bitmap src, int width, int height, CompositingQuality cq = CompositingQuality.HighQuality, InterpolationMode im = InterpolationMode.HighQualityBicubic, SmoothingMode sm = SmoothingMode.HighQuality)
        {
            var dest = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(dest))
            {
                graphics.CompositingQuality = cq;
                graphics.InterpolationMode = im;
                graphics.SmoothingMode = sm;

                graphics.DrawImage(src, 0, 0, dest.Width, dest.Height);
            }

            return dest;
        }
    }
}

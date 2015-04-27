using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageExtractor
{
    /// <summary>
    /// Extracts images for each glyph from a aletheia page xml.
    /// </summary>
    public class ImageExtractor
    {
        private PageXml pageXml;

        /// <summary>
        /// Create a image extrator for a page xml passed in <paramref name="pageXml"/>.
        /// </summary>
        /// <param name="pageXml">The PageXml object to get the glyphs from-</param>
        public ImageExtractor(PageXml pageXml)
        {
            this.pageXml = pageXml;
        }

        /// <summary>
        /// Creates a image for every glyph in the page xml and saves this image to the folder provided in 
        /// <paramref name="outputFolderPath"/>
        /// </summary>
        /// <param name="inputImagePath">The image to cute the glyph out from.</param>
        /// <param name="outputFolderPath">Folder to save each glypg image in.</param>
        public void Extract(string inputImagePath, string outputFolderPath)
        {
            List<Glyph> glyphs = pageXml.GetGlyphs();
            Image sourceImage = Image.FromFile(inputImagePath);
            string sourceImageFileName = Path.GetFileNameWithoutExtension(inputImagePath);

            foreach (var currentGlyph in glyphs)
            {
                Bitmap image = GetSelectedArea(sourceImage, Color.White, currentGlyph.Points);
                string outputFileName = string.Format("{0}_{1}.tiff", sourceImageFileName, currentGlyph.ID);
                string path = Path.Combine(outputFolderPath, outputFileName);
                image.Save(path, ImageFormat.Tiff);
            }
        }

        /// <summary>
        /// Cuts a glyph based on the coords in <paramref name="points"/> out of the image <paramref name="sourceImage"/>. 
        /// </summary>
        /// <param name="sourceImage">The image from the ocr process</param>
        /// <param name="bg_color">The backgroundcolor for the glyph image</param>
        /// <param name="points">A List of points describing the glyph boundary polygon.</param>
        /// <returns>A Bitmap containg the glyph.</returns>
        private Bitmap GetSelectedArea(Image source, Color bg_color, List<Point> points)
        {
            using(Bitmap sourceBitmap = new Bitmap(source))
            {
                /*
                 * the canvas is used for manipulating the "sourceBitmap"
                 */
                using (Graphics canvas = Graphics.FromImage(sourceBitmap))
                {
                    //clear to whole image (just white background)
                    canvas.Clear(bg_color);

                    //create a brush which is the ocr image
                    using (Brush brush = new TextureBrush(source))
                    {
                        /*
                         * ...from this brush draw the polygon descriped by "points" to our white image
                         * at the exact same position as in the brush. This polygon is the glyph.
                         */
                        canvas.FillPolygon(brush, points.ToArray());

                        Rectangle glyphBouningRectangle = GetBoundingRectangle(points);

                        Bitmap result = new Bitmap(glyphBouningRectangle.Width, glyphBouningRectangle.Height);

                        /*
                         * copy the glyph from the white image to out result image.
                         */
                        using (Graphics result_gr = Graphics.FromImage(result))
                        {
                            Rectangle dest_rect = new Rectangle(0, 0, glyphBouningRectangle.Width, glyphBouningRectangle.Height);
                            result_gr.DrawImage(sourceBitmap, dest_rect,glyphBouningRectangle, GraphicsUnit.Pixel);
                        }

                        return result;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the bounding rectangle of the polygon defined by a list of points
        /// </summary>
        /// <param name="points">Points to create the bounding rectangle for.</param>
        /// <returns>The bounding rectangle</returns>
        private Rectangle GetBoundingRectangle(List<Point> points)
        {
            /*
             * To calculate the bounding rectangle of a polygon defined
             * by a list of points we need the points which have the 
             * highest/lowest X and Y coords. 
             */ 
            var xsorted = (from p in points
                           orderby p.X ascending
                           select p.X);

            var ysorted = (from p in points
                           orderby p.Y ascending
                           select p.Y);

            var xmin = xsorted.First();
            var xmax = xsorted.Last();

            var ymin = ysorted.First();
            var ymax = ysorted.Last();

            return new Rectangle(xmin, ymin, xmax - xmin, ymax - ymin);
        } 
    }
}

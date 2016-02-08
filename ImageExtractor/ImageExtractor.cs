using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BitMiracle.LibTiff.Classic;

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
                BitmapInvertColors(image);
                byte[] tifBytes = GetTiffImageBytes(image, true); 
                string outputFileName = string.Format("{0}_{1}.tif", sourceImageFileName, currentGlyph.ID);
                string path = Path.Combine(outputFolderPath, outputFileName);
                //image.Save(path, ImageFormat.Tiff);
                File.WriteAllBytes(path, tifBytes);
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

        /// <summary>
        /// Converts a bitmap to a tiff encoded memory stream.
        /// Source: http://stackoverflow.com/questions/33481937/corruption-when-creating-multipage-tiff-over-a-certain-small-size
        /// </summary>
        public byte[] GetTiffImageBytes(Bitmap img, bool byScanlines)
        {
            try
            {
                byte[] raster = GetImageRasterBytes(img);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (Tiff tif = Tiff.ClientOpen("InMemory", "w", ms, new TiffStream()))
                    {
                        if (tif == null)
                            return null;

                        tif.SetField(TiffTag.IMAGEWIDTH, img.Width);
                        tif.SetField(TiffTag.IMAGELENGTH, img.Height);
                        tif.SetField(TiffTag.COMPRESSION, Compression.CCITTFAX4);
                        tif.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISWHITE);

                        tif.SetField(TiffTag.ROWSPERSTRIP, img.Height);

                        tif.SetField(TiffTag.XRESOLUTION, img.HorizontalResolution);
                        tif.SetField(TiffTag.YRESOLUTION, img.VerticalResolution);

                        tif.SetField(TiffTag.SUBFILETYPE, 0);
                        tif.SetField(TiffTag.BITSPERSAMPLE, 1);
                        tif.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                        tif.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);

                        tif.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                        tif.SetField(TiffTag.T6OPTIONS, 0);
                        tif.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);

                        tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                        int tiffStride = tif.ScanlineSize();
                        int stride = raster.Length / img.Height;

                        if (byScanlines)
                        {
                            // raster stride MAY be bigger than TIFF stride (due to padding in raster bits) 
                            for (int i = 0, offset = 0; i < img.Height; i++)
                            {
                                bool res = tif.WriteScanline(raster, offset, i, 0);
                                if (!res)
                                    return null;

                                offset += stride;
                            }
                        }
                        else
                        {
                            if (tiffStride < stride)
                            {
                                // raster stride is bigger than TIFF stride 
                                // this is due to padding in raster bits 
                                // we need to create correct TIFF strip and write it into TIFF 

                                byte[] stripBits = new byte[tiffStride * img.Height];
                                for (int i = 0, rasterPos = 0, stripPos = 0; i < img.Height; i++)
                                {
                                    System.Buffer.BlockCopy(raster, rasterPos, stripBits, stripPos, tiffStride);
                                    rasterPos += stride;
                                    stripPos += tiffStride;
                                }

                                // Write the information to the file 
                                int n = tif.WriteEncodedStrip(0, stripBits, stripBits.Length);
                                if (n <= 0)
                                    return null;
                            }
                            else
                            {
                                // Write the information to the file 
                                int n = tif.WriteEncodedStrip(0, raster, raster.Length);
                                if (n <= 0)
                                    return null;
                            }
                        }
                    }

                    return ms.GetBuffer();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/33481937/corruption-when-creating-multipage-tiff-over-a-certain-small-size
        /// </summary>
        public byte[] GetImageRasterBytes(Bitmap img)
        {
            // Specify full image
            Rectangle rect = new Rectangle(0, 0, img.Width, img.Height);

            Bitmap bmp = img;
            byte[] bits = null;

            try
            {
                // Lock the managed memory 
                if (img.PixelFormat != PixelFormat.Format1bppIndexed)
                    bmp = convertToBitonal(img);

                BitmapData bmpdata = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);

                // Declare an array to hold the bytes of the bitmap.
                bits = new byte[bmpdata.Stride * bmpdata.Height];

                // Copy the sample values into the array.
                Marshal.Copy(bmpdata.Scan0, bits, 0, bits.Length);

                // Release managed memory
                bmp.UnlockBits(bmpdata);
            }
            finally
            {
                if (bmp != img)
                    bmp.Dispose();
            }

            return bits;
        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/33481937/corruption-when-creating-multipage-tiff-over-a-certain-small-size
        /// </summary>
        private Bitmap convertToBitonal(Bitmap original)
        {
            int sourceStride;
            byte[] sourceBuffer = extractBytes(original, out sourceStride);

            // Create destination bitmap
            Bitmap destination = new Bitmap(original.Width, original.Height,
                PixelFormat.Format1bppIndexed);

            destination.SetResolution(original.HorizontalResolution, original.VerticalResolution);

            // Lock destination bitmap in memory
            BitmapData destinationData = destination.LockBits(
                new Rectangle(0, 0, destination.Width, destination.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

            // Create buffer for destination bitmap bits 
            int imageSize = destinationData.Stride * destinationData.Height;
            byte[] destinationBuffer = new byte[imageSize];

            int sourceIndex = 0;
            int destinationIndex = 0;
            int pixelTotal = 0;
            byte destinationValue = 0;
            int pixelValue = 128;
            int height = destination.Height;
            int width = destination.Width;
            int threshold = 500;

            for (int y = 0; y < height; y++)
            {
                sourceIndex = y * sourceStride;
                destinationIndex = y * destinationData.Stride;
                destinationValue = 0;
                pixelValue = 128;

                for (int x = 0; x < width; x++)
                {
                    // Compute pixel brightness (i.e. total of Red, Green, and Blue values)
                    pixelTotal = sourceBuffer[sourceIndex + 1] + sourceBuffer[sourceIndex + 2] +
                        sourceBuffer[sourceIndex + 3];

                    if (pixelTotal > threshold)
                        destinationValue += (byte)pixelValue;

                    if (pixelValue == 1)
                    {
                        destinationBuffer[destinationIndex] = destinationValue;
                        destinationIndex++;
                        destinationValue = 0;
                        pixelValue = 128;
                    }
                    else
                    {
                        pixelValue >>= 1;
                    }

                    sourceIndex += 4;
                }

                if (pixelValue != 128)
                    destinationBuffer[destinationIndex] = destinationValue;
            }

            Marshal.Copy(destinationBuffer, 0, destinationData.Scan0, imageSize);
            destination.UnlockBits(destinationData);
            return destination;
        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/33481937/corruption-when-creating-multipage-tiff-over-a-certain-small-size
        /// </summary>
        private byte[] extractBytes(Bitmap original, out int stride)
        {
            Bitmap source = null;

            try
            {
                // If original bitmap is not already in 32 BPP, ARGB format, then convert 
                if (original.PixelFormat != PixelFormat.Format32bppArgb)
                {
                    source = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
                    source.SetResolution(original.HorizontalResolution, original.VerticalResolution);
                    using (Graphics g = Graphics.FromImage(source))
                    {
                        g.DrawImageUnscaled(original, 0, 0);
                    }
                }
                else
                {
                    source = original;
                }

                // Lock source bitmap in memory
                BitmapData sourceData = source.LockBits(
                    new Rectangle(0, 0, source.Width, source.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                // Copy image data to binary array 
                int imageSize = sourceData.Stride * sourceData.Height;
                byte[] sourceBuffer = new byte[imageSize];
                Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, imageSize);

                // Unlock source bitmap
                source.UnlockBits(sourceData);

                stride = sourceData.Stride;
                return sourceBuffer;
            }
            finally
            {
                if (source != original)
                    source.Dispose();
            }

        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/11428724/how-to-create-inverse-png-image
        /// </summary>
        public void BitmapInvertColors(Bitmap bitmapImage)
        {
            var bitmapRead = bitmapImage.LockBits(new Rectangle(0, 0, bitmapImage.Width, bitmapImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            var bitmapLength = bitmapRead.Stride * bitmapRead.Height;
            var bitmapBGRA = new byte[bitmapLength];
            Marshal.Copy(bitmapRead.Scan0, bitmapBGRA, 0, bitmapLength);
            bitmapImage.UnlockBits(bitmapRead);

            for (int i = 0; i < bitmapLength; i += 4)
            {
                bitmapBGRA[i] = (byte)(255 - bitmapBGRA[i]);
                bitmapBGRA[i + 1] = (byte)(255 - bitmapBGRA[i + 1]);
                bitmapBGRA[i + 2] = (byte)(255 - bitmapBGRA[i + 2]);
                //        [i + 3] = ALPHA.
            }

            var bitmapWrite = bitmapImage.LockBits(new Rectangle(0, 0, bitmapImage.Width, bitmapImage.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            Marshal.Copy(bitmapBGRA, 0, bitmapWrite.Scan0, bitmapLength);
            bitmapImage.UnlockBits(bitmapWrite);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace OtsuThreshold
{
    class Thining
    {
        public bool[][] ImageCheckToBool(Bitmap img)
        {

            //Bitmap img = new Bitmap(filename);
            if (img.PixelFormat != PixelFormat.Format24bppRgb)
            {
                throw new FormatException("The image must be in the 24bpp format.");
            }
            int[] array = new int[img.Width * img.Height - 1];
            BitmapData bmp = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            bool[][] s = new bool[bmp.Height][];

            unsafe
            {
                int PixelSize = 3;
                int count = 0;

                for (int y = 0; y < bmp.Height; y++)
                {
                    byte* row = (byte*)bmp.Scan0 + (y * bmp.Stride);
                    s[y] = new bool[bmp.Width];
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int r = (int)row[(x * PixelSize) + 2];
                        int g = (int)row[(x * PixelSize) + 1];
                        int b = (int)row[(x * PixelSize)];

                        float[] hsb = RGBtoHSB(r, g, b, null);
                        s[y][x] = hsb[2] < 0.3;
                    }
                }
            }
            img.UnlockBits(bmp);
            img.Dispose();
            GC.SuppressFinalize(bmp);

            return s;
        }

        public void tets(String file)
        {
            Bitmap img = new Bitmap(file);
            //if (img.PixelFormat != PixelFormat.Format24bppRgb)
            // {
            //   throw new FormatException("The image must be in the 24bpp format.");
            //}
            //int[] array = new int[img.Width * img.Height - 1];
            //BitmapData bmp = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);


            //assume this actually points to something (not zero!!)
            IntPtr pNative = IntPtr.Zero;

            //assume these are you image dimensions
            int w = img.Width; //width
            int h = img.Height; //height
            int ch = 3; //channels

            //image loop
            //use unsafe
            //this is very fast!! 
            unsafe
            {
                for (int r = 0; r < h; r++)
                {
                    byte* pI = (byte*)pNative.ToPointer() + r * w * ch; //pointer to start of row
                    for (int c = 0; c < w; c++)
                    {
                        //pI[c * ch] = 0; //red
                        //pI[c * ch + 1] = 0; //green
                        //pI[c * ch + 2] = 0; //blue

                        System.Console.Out.WriteLine(pI[c * ch] + " " + pI[c * ch + 1] + " " + pI[c * ch + 2]);

                        //img.GetPixel(r, c);
                        //also equivalent to *(pI + c*ch)  = 0 - i.e. using pointer arythmetic;
                    }
                }
            }

        }

        public static float[] RGBtoHSB(int r, int g, int b, float[] hsbvals)
        {
            float hue, saturation, brightness;
            if (hsbvals == null)
            {
                hsbvals = new float[3];
            }
            int cmax = (r > g) ? r : g;
            if (b > cmax) cmax = b;
            int cmin = (r < g) ? r : g;
            if (b < cmin) cmin = b;

            brightness = ((float)cmax) / 255.0f;

            if (cmax != 0)
                saturation = ((float)(cmax - cmin)) / ((float)cmax);

            else
                saturation = 0;

            if (saturation == 0)
                hue = 0;
            else
            {
                float redc = ((float)(cmax - r)) / ((float)(cmax - cmin));
                float greenc = ((float)(cmax - g)) / ((float)(cmax - cmin));
                float bluec = ((float)(cmax - b)) / ((float)(cmax - cmin));

                if (r == cmax)
                {
                    hue = bluec - greenc;
                }
                else if (g == cmax)
                {
                    hue = 2.0f + redc - bluec;
                }
                else
                {
                    hue = 4.0f + greenc - redc;
                    hue = hue / 6.0f;
                }
                if (hue < 0)
                {
                    hue = hue + 1.0f;
                }
            }

            hsbvals[0] = hue;
            hsbvals[1] = saturation;
            hsbvals[2] = brightness;

            return hsbvals;
        }

        public static bool[][] Image2Bool(Image img)
        {
            Bitmap bmp = new Bitmap(img);
            bool[][] s = new bool[bmp.Height][];
            int count = 1;
            for (int y = 0; y < bmp.Height; y++)
            {
                s[y] = new bool[bmp.Width];
                for (int x = 0; x < bmp.Width; x++)
                {
                    s[y][x] = bmp.GetPixel(x, y).GetBrightness() < 0.3;
                    //System.Console.Out.WriteLine(s[y][x]);
                    //if(x==127 && y==127)
                    //{
                    //System.Console.Out.WriteLine(count + " [" + x + "," + y + "] " + bmp.GetPixel(x, y) + " " + bmp.GetPixel(x, y).GetBrightness());
                    //}

                    count++;
                }

            }
            return s;
        }

        public Image Bool2Image(bool[][] s)
        {
            Bitmap bmp = new Bitmap(s[0].Length, s.Length);
            using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.White);
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (s[y][x])
                    {
                        bmp.SetPixel(x, y, Color.Black);
                    }
                }
            }
            return (Bitmap)bmp;
        }

        public static T[][] ArrayClone<T>(T[][] A)
        {
            return A.Select(a => a.ToArray()).ToArray();
        }

        public bool[][] ZhangSuenThinning(bool[][] s)
        {
            bool[][] temp = ArrayClone(s);  // make a deep copy to start.. 
            int count = 0;
            do  // the missing iteration
            {
                count = step(1, temp, s);
                temp = ArrayClone(s);      // ..and on each..
                count += step(2, temp, s);
                temp = ArrayClone(s);      // ..call!
            }
            while (count > 0);

            return s;
        }

        static int step(int stepNo, bool[][] temp, bool[][] s)
        {
            int count = 0;

            for (int a = 1; a < temp.Length - 1; a++)
            {
                for (int b = 1; b < temp[0].Length - 1; b++)
                {
                    if (SuenThinningAlg(a, b, temp, stepNo == 2))
                    {
                        // still changes happening?
                        if (s[a][b]) count++;
                        s[a][b] = false;
                    }
                }
            }
            return count;
        }

        static bool SuenThinningAlg(int x, int y, bool[][] s, bool even)
        {
            bool p2 = s[x][y - 1];
            bool p3 = s[x + 1][y - 1];
            bool p4 = s[x + 1][y];
            bool p5 = s[x + 1][y + 1];
            bool p6 = s[x][y + 1];
            bool p7 = s[x - 1][y + 1];
            bool p8 = s[x - 1][y];
            bool p9 = s[x - 1][y - 1];


            int bp1 = NumberOfNonZeroNeighbors(x, y, s);
            if (bp1 >= 2 && bp1 <= 6) //2nd condition
            {
                if (NumberOfZeroToOneTransitionFromP9(x, y, s) == 1)
                {
                    if (even)
                    {
                        if (!((p2 && p4) && p8))
                        {
                            if (!((p2 && p6) && p8))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (!((p2 && p4) && p6))
                        {
                            if (!((p4 && p6) && p8))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        static int NumberOfZeroToOneTransitionFromP9(int x, int y, bool[][] s)
        {
            bool p2 = s[x][y - 1];
            bool p3 = s[x + 1][y - 1];
            bool p4 = s[x + 1][y];
            bool p5 = s[x + 1][y + 1];
            bool p6 = s[x][y + 1];
            bool p7 = s[x - 1][y + 1];
            bool p8 = s[x - 1][y];
            bool p9 = s[x - 1][y - 1];



            int A = Convert.ToInt32((!p2 && p3)) + Convert.ToInt32((!p3 && p4)) +
                    Convert.ToInt32((!p4 && p5)) + Convert.ToInt32((!p5 && p6)) +
                    Convert.ToInt32((!p6 && p7)) + Convert.ToInt32((!p7 && p8)) +
                    Convert.ToInt32((!p8 && p9)) + Convert.ToInt32((!p9 && p2));

            return A;
        }
        static int NumberOfNonZeroNeighbors(int x, int y, bool[][] s)
        {
            int count = 0;
            if (s[x - 1][y]) count++;
            if (s[x - 1][y + 1]) count++;
            if (s[x - 1][y - 1]) count++;
            if (s[x][y + 1]) count++;
            if (s[x][y - 1]) count++;
            if (s[x + 1][y]) count++;
            if (s[x + 1][y + 1]) count++;
            if (s[x + 1][y - 1]) count++;
            return count;
        }

    }
}

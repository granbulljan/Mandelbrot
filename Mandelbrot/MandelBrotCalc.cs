using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Windows.Forms;


class MandelBrotCalc
{
    PictureBox pictureBox;

    const double MaxValueExtent = 2.0;
    const double MaxValueExtentSqr = 2.0 * 2.0;
    double MaxValueExtentLog = Math.Log(2.0);

    public int MaxIterations = 1000;
    public double beginX = 0;
    public double beginY = 0;
    public double GlobScale = 1;
    public bool ColorStyle2 = true;


    public List<Color> Colors = new List<Color>();
    private List<Color> Colors2 = new List<Color>();



    public MandelBrotCalc(PictureBox picturebox)
    {
        ColorStyle2 = false;
        Colors.Add(Color.Peru); Colors.Add(Color.Navy); Colors.Add(Color.AntiqueWhite);
        pictureBox = picturebox;
    }

    private Color CalcMandelbrotSetColor(ComplexNumber c)
    {
        int iteration = 0;
        ComplexNumber z = new ComplexNumber();
        do
        {
            z = z & c;
            iteration++;
        } while (z.Magnitude() < MaxValueExtentSqr && iteration < MaxIterations);

        if (iteration >= MaxIterations)
        {
            return Color.Black;
        }
        else
        {
            //return GetZebraColor(iteration);
            return niceColor(z, c, iteration);
        }
    }
    private Color niceColor(ComplexNumber z, ComplexNumber c, int iteration)
    {
        for (int i = 0; i < 3; i++)
        {
            z = z & c;
            iteration++;
        }

        double mu = iteration + 1 -
            Math.Log(Math.Log(z.Magnitude())) / MaxValueExtentLog;
        if (ColorStyle2)
        {
            mu = mu / MaxIterations * Colors.Count;
        }
        int clr1 = (int)mu;
        double t2 = mu - clr1;
        double t1 = 1 - t2;
        clr1 = clr1 % Colors.Count;
        int clr2 = (clr1 + 1) % Colors.Count;

        byte r = (byte)(Colors[clr1].R * t1 + Colors[clr2].R * t2);
        byte g = (byte)(Colors[clr1].G * t1 + Colors[clr2].G * t2);
        byte b = (byte)(Colors[clr1].B * t1 + Colors[clr2].B * t2);

        return Color.FromArgb(255, r, g, b);
    }

    private Color GetZebraColor(int iteration)
    {
        if (iteration % 2 == 0)
            return Color.White;
        else
            return Color.Black;
    }


    public void GenerateBitmap(Bitmap bitmap)
    {
        double scale = 2 * MaxValueExtent / Math.Min(bitmap.Width, bitmap.Height) * GlobScale;
        for (int i = 0; i < bitmap.Height; i++)
        {
            double y = (bitmap.Height / 2 - i) * scale - beginY;
            for (int j = 0; j < bitmap.Width; j++)
            {
                double x = (j - bitmap.Width / 2) * scale + beginX;
                bitmap.SetPixel(j, i, CalcMandelbrotSetColor(new ComplexNumber(x, y)));
            }
        }
    }


    

    public void SetNewBitmap(Bitmap image, Rectangle bounds)
    {
        pictureBox.BackgroundImage = image;
    }



}

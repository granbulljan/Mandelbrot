using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Generates bitmap of Mandelbrot Set and display it on the form.
/// </summary>
public class MandelbrotSetForm : Form
{
    Thread thread;
    const double MaxValueExtent = 2.0;
    const double MaxValueExtentSqr = 2.0 * 2.0;
    double MaxValueExtentLog = Math.Log(2.0);

    private Button OKButton;
    private PictureBox pictureBox2;
    private TextBox textBoxX;
    private TextBox textBoxY;
    private TextBox textBoxScale;
    private TextBox textBoxIter;
    private Button nice;
    private Button original;
    private Button SaveButton;
    private ComboBox ColourSelection;
    private ComboBox LocationSelection;

    int MaxIterations = 1000;
    double beginX = 0;
    double beginY = 0;
    double GlobScale = 1;
    bool ColorStyle2 = true;

    public List<Color> Colors = new List<Color>();
    private List<Color> Colors2 = new List<Color>();

    CultureInfo culture;

    public MandelbrotSetForm()
    {
        culture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";

        ColorStyle2 = false;
        Colors.Add(Color.Peru); Colors.Add(Color.Navy); Colors.Add(Color.AntiqueWhite);

        InitializeComponent();

        // form creation
        this.Text = "Mandelbrot Set Drawing";
        this.BackColor = System.Drawing.Color.Black;
        this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.ClientSize = new Size(700, 764);

        this.Load += new System.EventHandler(this.MainForm_Load);
        pictureBox2.MouseClick += new MouseEventHandler(picturBox2_Click);
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
            return Color.Beige;
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


    void GenerateBitmap(Bitmap bitmap) 
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
    

    private void MainForm_Load(object sender, EventArgs e)
    {
        thread = new Thread(() =>thread_Proc(pictureBox2.ClientSize));
        thread.IsBackground = true;
        thread.Start();

    }

    private void thread_Proc(Size clientSize) 
    {
        // start from small image to provide instant display for user
        Size size = clientSize;
        Rectangle rec;
        int width = 256;
        if (width * 2 < size.Width)
        {
            int height = width * size.Height / size.Width;
            rec = new Rectangle(0, 0, width, height);
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            GenerateBitmap(bitmap);
            this.BeginInvoke(new SetNewBitmapDelegate(SetNewBitmap), bitmap, rec);
            width *= 2;
        }
        // then generate final image
        int h = width * size.Height / size.Width;
        rec = new Rectangle(0, 0, width, h);
        Bitmap finalBitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format24bppRgb);
        GenerateBitmap(finalBitmap);
        this.BeginInvoke(new SetNewBitmapDelegate(SetNewBitmap), finalBitmap, rec);
    }

    void SetNewBitmap(Bitmap image, Rectangle bounds)
    {      
        pictureBox2.BackgroundImage = image;
    }

    delegate void SetNewBitmapDelegate(Bitmap image, Rectangle rec);

    private double StringToDouble(string inString)
    {       
        return Convert.ToDouble(inString, culture);
    }

    #region UI elements
    //Click on the fractal
    void picturBox2_Click(object sender, MouseEventArgs e)
    {
        thread.Abort();
        float x = (float)e.X / (float)pictureBox2.Width * 2 - 1;
        float y = (float)e.Y / (float)pictureBox2.Height * 2 - 1;
        beginX += x * GlobScale * 2;
        textBoxX.Text = beginX.ToString();
        beginY += y * GlobScale * 2;
        textBoxY.Text = beginY.ToString();
        if (e.Button == MouseButtons.Left)
            GlobScale /= 2;
        if (e.Button == MouseButtons.Right)
            GlobScale *= 2;
        textBoxScale.Text = GlobScale.ToString();
        MainForm_Load(sender, e);
    }

    //OK button
    private void button1_Click(object sender, EventArgs e)
    {
        thread.Abort();
        if (textBoxX.Text != null && textBoxY.Text != null && textBoxScale.Text != null && textBoxIter.Text != null)
        {
            //double x = double.Parse(textBoxX.Text, System.Globalization.NumberStyles.Number);
            double x = StringToDouble(textBoxX.Text);
            
            //double y = double.Parse(textBoxY.Text, System.Globalization.NumberStyles.Number);
            double y = StringToDouble(textBoxY.Text);
            beginX = x; beginY = y;

            //double scl = double.Parse(textBoxScale.Text, System.Globalization.NumberStyles.Number);
            double scl = StringToDouble(textBoxScale.Text);
            if (scl > 0)
                GlobScale = scl;
            else
            {
                GlobScale = 1;
                textBoxScale.Text = "1";
            }
            int max = int.Parse(textBoxIter.Text, System.Globalization.NumberStyles.Number);
            MaxIterations = max;
        }
        MainForm_Load(sender, e);
    }



    //Nice button
    private void button2_Click(object sender, EventArgs e)
    {
        thread.Abort();
        beginX = -1.0079;
        textBoxX.Text = "-1,0079";
        beginY = 0.3112109;
        textBoxY.Text = "0,3112109";
        GlobScale = 1.953125E-3;
        textBoxScale.Text = "1,953125E-3";
        MaxIterations = 1000;
        textBoxIter.Text = "1000";
        MainForm_Load(sender, e);
    }

    //Original button
    private void button3_Click(object sender, EventArgs e)
    {
        thread.Abort();
        beginX = 0;
        textBoxX.Text = "0";
        beginY = 0;
        textBoxY.Text = "0";
        GlobScale = 1;
        textBoxScale.Text = "1";
        MainForm_Load(sender, e);
    }

    private void InitializeComponent()
    {
            this.OKButton = new System.Windows.Forms.Button();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.textBoxX = new System.Windows.Forms.TextBox();
            this.textBoxY = new System.Windows.Forms.TextBox();
            this.textBoxScale = new System.Windows.Forms.TextBox();
            this.textBoxIter = new System.Windows.Forms.TextBox();
            this.nice = new System.Windows.Forms.Button();
            this.original = new System.Windows.Forms.Button();
            this.SaveButton = new System.Windows.Forms.Button();
            this.ColourSelection = new System.Windows.Forms.ComboBox();
            this.LocationSelection = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // OKButton
            // 
            this.OKButton.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.OKButton.ForeColor = System.Drawing.Color.Brown;
            this.OKButton.Location = new System.Drawing.Point(0, 12);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(128, 41);
            this.OKButton.TabIndex = 0;
            this.OKButton.Text = "OK";
            this.OKButton.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage;
            this.OKButton.UseVisualStyleBackColor = false;
            this.OKButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.Color.Black;
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox2.Location = new System.Drawing.Point(0, 64);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(700, 700);
            this.pictureBox2.TabIndex = 0;
            this.pictureBox2.TabStop = false;
            // 
            // textBoxX
            // 
            this.textBoxX.Location = new System.Drawing.Point(134, 7);
            this.textBoxX.Name = "textBoxX";
            this.textBoxX.Size = new System.Drawing.Size(100, 22);
            this.textBoxX.TabIndex = 2;
            this.textBoxX.Text = "0";
            // 
            // textBoxY
            // 
            this.textBoxY.Location = new System.Drawing.Point(134, 35);
            this.textBoxY.Name = "textBoxY";
            this.textBoxY.Size = new System.Drawing.Size(100, 22);
            this.textBoxY.TabIndex = 3;
            this.textBoxY.Text = "0";
            // 
            // textBoxScale
            // 
            this.textBoxScale.Location = new System.Drawing.Point(240, 7);
            this.textBoxScale.Name = "textBoxScale";
            this.textBoxScale.Size = new System.Drawing.Size(100, 22);
            this.textBoxScale.TabIndex = 4;
            this.textBoxScale.Text = "1";
            // 
            // textBoxIter
            // 
            this.textBoxIter.Location = new System.Drawing.Point(240, 35);
            this.textBoxIter.Name = "textBoxIter";
            this.textBoxIter.Size = new System.Drawing.Size(100, 22);
            this.textBoxIter.TabIndex = 5;
            this.textBoxIter.Text = "1000";
            // 
            // nice
            // 
            this.nice.AccessibleName = "Nice";
            this.nice.Location = new System.Drawing.Point(357, 36);
            this.nice.Name = "nice";
            this.nice.Size = new System.Drawing.Size(75, 23);
            this.nice.TabIndex = 6;
            this.nice.Text = "nice";
            this.nice.UseVisualStyleBackColor = true;
            this.nice.Click += new System.EventHandler(this.button2_Click);
            // 
            // original
            // 
            this.original.AccessibleName = "Original";
            this.original.Location = new System.Drawing.Point(357, 7);
            this.original.Name = "original";
            this.original.Size = new System.Drawing.Size(75, 23);
            this.original.TabIndex = 7;
            this.original.Text = "orig";
            this.original.UseVisualStyleBackColor = true;
            this.original.Click += new System.EventHandler(this.button3_Click);
            // 
            // SaveButton
            // 
            this.SaveButton.Location = new System.Drawing.Point(550, 8);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(75, 23);
            this.SaveButton.TabIndex = 8;
            this.SaveButton.Text = "Save";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // ColourSelection
            // 
            this.ColourSelection.FormattingEnabled = true;
            this.ColourSelection.Items.AddRange(new object[] {
            "Bands",
            "YellowBlue",
            "ROGYBIV",
            "Kayleigh"});
            this.ColourSelection.Location = new System.Drawing.Point(450, 7);
            this.ColourSelection.Name = "ColourSelection";
            this.ColourSelection.Size = new System.Drawing.Size(82, 24);
            this.ColourSelection.TabIndex = 9;
            this.ColourSelection.Text = "Bands";
            this.ColourSelection.SelectedIndexChanged += new System.EventHandler(this.ColourSelection_SelectedIndexChanged);
            // 
            // LocationSelection
            // 
            this.LocationSelection.FormattingEnabled = true;
            this.LocationSelection.Items.AddRange(new object[] {
            "Original",
            "Nice1",
            "Nice2",
            "Nice3",
            "Nice4"});
            this.LocationSelection.Location = new System.Drawing.Point(450, 37);
            this.LocationSelection.Name = "LocationSelection";
            this.LocationSelection.Size = new System.Drawing.Size(82, 24);
            this.LocationSelection.TabIndex = 10;
            this.LocationSelection.Text = "Original";
            this.LocationSelection.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // MandelbrotSetForm
            // 
            this.ClientSize = new System.Drawing.Size(957, 816);
            this.Controls.Add(this.LocationSelection);
            this.Controls.Add(this.ColourSelection);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.original);
            this.Controls.Add(this.nice);
            this.Controls.Add(this.textBoxIter);
            this.Controls.Add(this.textBoxScale);
            this.Controls.Add(this.textBoxY);
            this.Controls.Add(this.textBoxX);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.OKButton);
            this.Name = "MandelbrotSetForm";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

   

    private void button1_Click_1(object sender, EventArgs e)
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        pictureBox2.BackgroundImage.Save(desktop + "/fractalImage.png", ImageFormat.Png);
    }

    private void ColourSelection_SelectedIndexChanged(object sender, EventArgs e)
    {
        Colors = new List<Color>();
        if(ColourSelection.SelectedIndex ==0)
        {
            Colors.Add(Color.Peru); Colors.Add(Color.Navy); Colors.Add(Color.AntiqueWhite);
            ColorStyle2 = false;
        }
        if (ColourSelection.SelectedIndex == 1)
        {
            Colors.Add(Color.DarkBlue); Colors.Add(Color.Yellow); Colors.Add(Color.MistyRose); Colors.Add(Color.RoyalBlue);
            ColorStyle2 = true;
        }
        if (ColourSelection.SelectedIndex == 2)
        {
            Colors.Add(Color.Red); Colors.Add(Color.Orange); Colors.Add(Color.Green); Colors.Add(Color.Yellow); Colors.Add(Color.Blue); Colors.Add(Color.Indigo); Colors.Add(Color.Violet);
            ColorStyle2 = true;
        }
        if(ColourSelection.SelectedIndex == 3)
        {
            Colors.Add(Color.Turquoise); Colors.Add(Color.IndianRed); Colors.Add(Color.IndianRed);
            ColorStyle2 = true;
        }
    }

    private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
    {
        if(LocationSelection.SelectedIndex==0)
        {
            textBoxX.Text = "0";
            textBoxY.Text = "0";
            textBoxScale.Text = "1";
        }
        if (LocationSelection.SelectedIndex == 1)
        {
            textBoxX.Text = "-1,0079";
            textBoxY.Text = "0,3112109";
            textBoxScale.Text = "1,953125E-3";
        }
        if (LocationSelection.SelectedIndex == 2)
        {
            textBoxX.Text = "-0,158010477642529";
            textBoxY.Text = "1,03264985382091";
            textBoxScale.Text = "0,015625";
        }
        if (LocationSelection.SelectedIndex == 3)
        {
            textBoxX.Text = "-0,108625065165125";
            textBoxY.Text = "-0,901442828177505";
            textBoxScale.Text = "4,7675E-09";
        }
        if (LocationSelection.SelectedIndex == 4)
        {
            textBoxX.Text = "-1,09771751632127";
            textBoxY.Text = "-0,235023128485181";
            textBoxScale.Text = "1,52587890625E-05";
        }
    }

    #endregion
}

struct ComplexNumber
{
    public double Re;
    public double Im;

    public ComplexNumber(double re, double im)
    {
        this.Re = re;
        this.Im = im;
    }

    public static ComplexNumber operator +(ComplexNumber x, ComplexNumber y)
    {
        return new ComplexNumber(x.Re + y.Re, x.Im + y.Im);
    }

    public static ComplexNumber operator *(ComplexNumber x, ComplexNumber y)
    {
        return new ComplexNumber(x.Re * y.Re - x.Im * y.Im,
            x.Re * y.Im + x.Im * y.Re);
    }
    public static ComplexNumber operator &(ComplexNumber x, ComplexNumber y)
    {
        return new ComplexNumber(x.Re * x.Re - x.Im * x.Im + y.Re,
            2 * x.Re * x.Im + y.Im);
    }

    public double Magnitude()
    {
        return Re * Re + Im * Im;
    }
}
// Decompiled with JetBrains decompiler
// Type: VistaStyleProgressBar.ProgressBar
// Assembly: InfantryLauncher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: BCEC389A-35DE-4913-97AA-CBDD8E941EC4
// Assembly location: C:\Program Files (x86)\Infantry Online\InfantryLauncher.exe

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VistaStyleProgressBar
{
    [DefaultEvent("ValueChanged")]
    public class ProgressBar : UserControl
    {
        private int mGlowPosition = -325;
        private Timer mGlowAnimation = new Timer();
        private int mMaxValue = 100;
        private Color mStartColor = Color.FromArgb(210, 0, 0);
        private Color mEndColor = Color.FromArgb(0, 211, 40);
        private Color mHighlightColor = Color.White;
        private Color mBackgroundColor = Color.FromArgb(201, 201, 201);
        private bool mAnimate = true;
        private Color mGlowColor = Color.FromArgb(150, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue);
        private int mValue;
        private int mMinValue;

        [Category("Value")]
        [DefaultValue(0)]
        [Description("The value that is displayed on the progress bar.")]
        public int Value
        {
            get
            {
                return this.mValue;
            }
            set
            {
                if (value > this.MaxValue || value < this.MinValue)
                    return;
                this.mValue = value;
                if (value < this.MaxValue)
                    this.mGlowAnimation.Start();
                if (value == this.MaxValue)
                    this.mGlowAnimation.Stop();
                ProgressBar.ValueChangedHandler valueChangedHandler = this.ValueChanged;
                if (valueChangedHandler != null)
                    valueChangedHandler((object)this, new EventArgs());
                this.Invalidate();
            }
        }

        [DefaultValue(100)]
        [Description("The maximum value for the Value property.")]
        [Category("Value")]
        public int MaxValue
        {
            get
            {
                return this.mMaxValue;
            }
            set
            {
                this.mMaxValue = value;
                if (value > this.MaxValue)
                    this.Value = this.MaxValue;
                if (this.Value < this.MaxValue)
                    this.mGlowAnimation.Start();
                ProgressBar.MaxChangedHandler maxChangedHandler = this.MaxChanged;
                if (maxChangedHandler != null)
                    maxChangedHandler((object)this, new EventArgs());
                this.Invalidate();
            }
        }

        [Category("Value")]
        [DefaultValue(0)]
        [Description("The minimum value for the Value property.")]
        public int MinValue
        {
            get
            {
                return this.mMinValue;
            }
            set
            {
                this.mMinValue = value;
                if (value < this.MinValue)
                    this.Value = this.MinValue;
                ProgressBar.MinChangedHandler minChangedHandler = this.MinChanged;
                if (minChangedHandler != null)
                    minChangedHandler((object)this, new EventArgs());
                this.Invalidate();
            }
        }

        [Description("The start color for the progress bar.210, 000, 000 = Red\n210, 202, 000 = Yellow\n000, 163, 211 = Blue\n000, 211, 040 = Green\n")]
        [DefaultValue(typeof(Color), "210, 0, 0")]
        [Category("Bar")]
        public Color StartColor
        {
            get
            {
                return this.mStartColor;
            }
            set
            {
                this.mStartColor = value;
                this.Invalidate();
            }
        }

        [Description("The end color for the progress bar.210, 000, 000 = Red\n210, 202, 000 = Yellow\n000, 163, 211 = Blue\n000, 211, 040 = Green\n")]
        [Category("Bar")]
        [DefaultValue(typeof(Color), "0, 211, 40")]
        public Color EndColor
        {
            get
            {
                return this.mEndColor;
            }
            set
            {
                this.mEndColor = value;
                this.Invalidate();
            }
        }

        [DefaultValue(typeof(Color), "White")]
        [Category("Highlights and Glows")]
        [Description("The color of the highlights.")]
        public Color HighlightColor
        {
            get
            {
                return this.mHighlightColor;
            }
            set
            {
                this.mHighlightColor = value;
                this.Invalidate();
            }
        }

        [DefaultValue(typeof(Color), "201,201,201")]
        [Category("Highlights and Glows")]
        [Description("The color of the background.")]
        public Color BackgroundColor
        {
            get
            {
                return this.mBackgroundColor;
            }
            set
            {
                this.mBackgroundColor = value;
                this.Invalidate();
            }
        }

        [DefaultValue(typeof(bool), "true")]
        [Category("Highlights and Glows")]
        [Description("Whether the glow is animated or not.")]
        public bool Animate
        {
            get
            {
                return this.mAnimate;
            }
            set
            {
                this.mAnimate = value;
                if (value)
                    this.mGlowAnimation.Start();
                else
                    this.mGlowAnimation.Stop();
                this.Invalidate();
            }
        }

        [DefaultValue(typeof(Color), "150, 255, 255, 255")]
        [Category("Highlights and Glows")]
        [Description("The color of the glow.")]
        public Color GlowColor
        {
            get
            {
                return this.mGlowColor;
            }
            set
            {
                this.mGlowColor = value;
                this.Invalidate();
            }
        }

        public event ProgressBar.ValueChangedHandler ValueChanged;

        public event ProgressBar.MinChangedHandler MinChanged;

        public event ProgressBar.MaxChangedHandler MaxChanged;

        public ProgressBar()
        {
            this.InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.Selectable, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.BackColor = Color.Transparent;
            if (this.InDesignMode())
                return;
            this.mGlowAnimation.Tick += new EventHandler(this.mGlowAnimation_Tick);
            this.mGlowAnimation.Interval = 15;
            if (this.Value >= this.MaxValue)
                return;
            this.mGlowAnimation.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.Container != null)
                this.Container.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Name = "ProgressBar";
            this.Size = new Size(264, 32);
            this.Paint += new PaintEventHandler(this.ProgressBar_Paint);
        }

        private void DrawBackground(Graphics g)
        {
            Rectangle clientRectangle = this.ClientRectangle;
            --clientRectangle.Width;
            --clientRectangle.Height;
            GraphicsPath path = this.RoundRect((RectangleF)clientRectangle, 2f, 2f, 2f, 2f);
            g.FillPath((Brush)new SolidBrush(this.BackgroundColor), path);
        }

        private void DrawBackgroundShadows(Graphics g)
        {
            Rectangle rect1 = new Rectangle(2, 2, 10, this.Height - 5);
            LinearGradientBrush linearGradientBrush1 = new LinearGradientBrush(rect1, Color.FromArgb(30, 0, 0, 0), Color.Transparent, LinearGradientMode.Horizontal);
            --rect1.X;
            g.FillRectangle((Brush)linearGradientBrush1, rect1);
            Rectangle rect2 = new Rectangle(this.Width - 12, 2, 10, this.Height - 5);
            LinearGradientBrush linearGradientBrush2 = new LinearGradientBrush(rect2, Color.Transparent, Color.FromArgb(20, 0, 0, 0), LinearGradientMode.Horizontal);
            g.FillRectangle((Brush)linearGradientBrush2, rect2);
        }

        private void DrawBar(Graphics g)
        {
            g.FillRectangle((Brush)new SolidBrush(this.GetIntermediateColor()), new Rectangle(1, 2, this.Width - 3, this.Height - 3)
            {
                Width = (int)((double)this.Value * 1.0 / (double)(this.MaxValue - this.MinValue) * (double)this.Width)
            });
        }

        private void DrawBarShadows(Graphics g)
        {
            Rectangle rect1 = new Rectangle(1, 2, 15, this.Height - 3);
            LinearGradientBrush linearGradientBrush = new LinearGradientBrush(rect1, Color.White, Color.White, LinearGradientMode.Horizontal);
            linearGradientBrush.InterpolationColors = new ColorBlend(3)
            {
                Colors = new Color[3]
        {
          Color.Transparent,
          Color.FromArgb(40, 0, 0, 0),
          Color.Transparent
        },
                Positions = new float[3]
        {
          0.0f,
          0.2f,
          1f
        }
            };
            --rect1.X;
            g.FillRectangle((Brush)linearGradientBrush, rect1);
            Rectangle rect2 = new Rectangle(this.Width - 3, 2, 15, this.Height - 3);
            rect2.X = (int)((double)this.Value * 1.0 / (double)(this.MaxValue - this.MinValue) * (double)this.Width) - 14;
            g.FillRectangle((Brush)new LinearGradientBrush(rect2, Color.Black, Color.Black, LinearGradientMode.Horizontal)
            {
                InterpolationColors = new ColorBlend(3)
                {
                    Colors = new Color[3]
          {
            Color.Transparent,
            Color.FromArgb(40, 0, 0, 0),
            Color.Transparent
          },
                    Positions = new float[3]
          {
            0.0f,
            0.8f,
            1f
          }
                }
            }, rect2);
        }

        private void DrawHighlight(Graphics g)
        {
            Rectangle rect1 = new Rectangle(1, 1, this.Width - 1, 6);
            GraphicsPath path1 = this.RoundRect((RectangleF)rect1, 2f, 2f, 0.0f, 0.0f);
            g.SetClip(path1);
            LinearGradientBrush linearGradientBrush1 = new LinearGradientBrush(rect1, Color.White, Color.FromArgb(128, Color.White), LinearGradientMode.Vertical);
            g.FillPath((Brush)linearGradientBrush1, path1);
            g.ResetClip();
            Rectangle rect2 = new Rectangle(1, this.Height - 8, this.Width - 1, 6);
            GraphicsPath path2 = this.RoundRect((RectangleF)rect2, 0.0f, 0.0f, 2f, 2f);
            g.SetClip(path2);
            LinearGradientBrush linearGradientBrush2 = new LinearGradientBrush(rect2, Color.Transparent, Color.FromArgb(100, this.HighlightColor), LinearGradientMode.Vertical);
            g.FillPath((Brush)linearGradientBrush2, path2);
            g.ResetClip();
        }

        private void DrawInnerStroke(Graphics g)
        {
            Rectangle clientRectangle = this.ClientRectangle;
            ++clientRectangle.X;
            ++clientRectangle.Y;
            clientRectangle.Width -= 3;
            clientRectangle.Height -= 3;
            GraphicsPath path = this.RoundRect((RectangleF)clientRectangle, 2f, 2f, 2f, 2f);
            g.DrawPath(new Pen(Color.FromArgb(100, Color.White)), path);
        }

        private void DrawGlow(Graphics g)
        {
            Rectangle rect = new Rectangle(this.mGlowPosition, 0, 60, this.Height);
            LinearGradientBrush linearGradientBrush = new LinearGradientBrush(rect, Color.White, Color.White, LinearGradientMode.Horizontal);
            linearGradientBrush.InterpolationColors = new ColorBlend(4)
            {
                Colors = new Color[4]
        {
          Color.Transparent,
          this.GlowColor,
          this.GlowColor,
          Color.Transparent
        },
                Positions = new float[4]
        {
          0.0f,
          0.5f,
          0.6f,
          1f
        }
            };
            g.SetClip(new Rectangle(1, 2, this.Width - 3, this.Height - 3)
            {
                Width = (int)((double)this.Value * 1.0 / (double)(this.MaxValue - this.MinValue) * (double)this.Width)
            });
            g.FillRectangle((Brush)linearGradientBrush, rect);
            g.ResetClip();
        }

        private void DrawOuterStroke(Graphics g)
        {
            Rectangle clientRectangle = this.ClientRectangle;
            --clientRectangle.Width;
            --clientRectangle.Height;
            GraphicsPath path = this.RoundRect((RectangleF)clientRectangle, 2f, 2f, 2f, 2f);
            g.DrawPath(new Pen(Color.FromArgb(178, 178, 178)), path);
        }

        private GraphicsPath RoundRect(RectangleF r, float r1, float r2, float r3, float r4)
        {
            float x = r.X;
            float y = r.Y;
            float width = r.Width;
            float height = r.Height;
            GraphicsPath graphicsPath = new GraphicsPath();
            graphicsPath.AddBezier(x, y + r1, x, y, x + r1, y, x + r1, y);
            graphicsPath.AddLine(x + r1, y, x + width - r2, y);
            graphicsPath.AddBezier(x + width - r2, y, x + width, y, x + width, y + r2, x + width, y + r2);
            graphicsPath.AddLine(x + width, y + r2, x + width, y + height - r3);
            graphicsPath.AddBezier(x + width, y + height - r3, x + width, y + height, x + width - r3, y + height, x + width - r3, y + height);
            graphicsPath.AddLine(x + width - r3, y + height, x + r4, y + height);
            graphicsPath.AddBezier(x + r4, y + height, x, y + height, x, y + height - r4, x, y + height - r4);
            graphicsPath.AddLine(x, y + height - r4, x, y + r1);
            return graphicsPath;
        }

        private bool InDesignMode()
        {
            return LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        }

        private Color GetIntermediateColor()
        {
            Color startColor = this.StartColor;
            Color endColor = this.EndColor;
            float num1 = (float)this.Value * 1f / (float)(this.MaxValue - this.MinValue);
            int num2 = (int)startColor.A;
            int num3 = (int)startColor.R;
            int num4 = (int)startColor.G;
            int num5 = (int)startColor.B;
            int num6 = (int)endColor.A;
            int num7 = (int)endColor.R;
            int num8 = (int)endColor.G;
            int num9 = (int)endColor.B;
            int alpha = (int)Math.Abs((float)num2 + (float)(num2 - num6) * num1);
            int red = (int)Math.Abs((float)num3 - (float)(num3 - num7) * num1);
            int green = (int)Math.Abs((float)num4 - (float)(num4 - num8) * num1);
            int blue = (int)Math.Abs((float)num5 - (float)(num5 - num9) * num1);
            if (alpha > (int)byte.MaxValue)
                alpha = (int)byte.MaxValue;
            if (red > (int)byte.MaxValue)
                red = (int)byte.MaxValue;
            if (green > (int)byte.MaxValue)
                green = (int)byte.MaxValue;
            if (blue > (int)byte.MaxValue)
                blue = (int)byte.MaxValue;
            return Color.FromArgb(alpha, red, green, blue);
        }

        private void ProgressBar_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            this.DrawBackground(e.Graphics);
            this.DrawBackgroundShadows(e.Graphics);
            this.DrawBar(e.Graphics);
            this.DrawBarShadows(e.Graphics);
            this.DrawHighlight(e.Graphics);
            this.DrawInnerStroke(e.Graphics);
            this.DrawGlow(e.Graphics);
            this.DrawOuterStroke(e.Graphics);
        }

        private void mGlowAnimation_Tick(object sender, EventArgs e)
        {
            if (this.Animate)
            {
                this.mGlowPosition += 4;
                if (this.mGlowPosition > this.Width)
                    this.mGlowPosition = -300;
                this.Invalidate();
            }
            else
            {
                this.mGlowAnimation.Stop();
                this.mGlowPosition = -320;
            }
        }

        public delegate void ValueChangedHandler(object sender, EventArgs e);

        public delegate void MinChangedHandler(object sender, EventArgs e);

        public delegate void MaxChangedHandler(object sender, EventArgs e);
    }
}

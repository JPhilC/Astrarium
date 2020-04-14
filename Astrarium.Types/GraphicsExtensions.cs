﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrarium.Types
{
    /// <summary>
    /// Set of extensions for <see cref="Graphics"/> class.
    /// </summary>
    public static class GraphicsExtensions
    {
        public static void DrawXCross(this Graphics g, Pen pen, PointF p, float size)
        {
            g.DrawLine(pen, p.X - size, p.Y - size, p.X + size, p.Y + size);
            g.DrawLine(pen, p.X + size, p.Y - size, p.X - size, p.Y + size);
        }

        public static void DrawPlusCross(this Graphics g, Pen pen, PointF p, float size)
        {
            g.DrawLine(pen, p.X, p.Y - size, p.X, p.Y + size);
            g.DrawLine(pen, p.X + size, p.Y, p.X - size, p.Y);
        }

        public static void DrawStringOpaque(this Graphics g, string s, Font font, Brush textBrush, Brush bgBrush, PointF p, StringFormat format)
        {
            var size = g.MeasureString(s, font, p, format);
            PointF pBox = new PointF(p.X, p.Y);
            if (format.Alignment == StringAlignment.Center)
            {
                pBox.X -= size.Width / 2;
            }
            if (format.LineAlignment == StringAlignment.Center)
            {
                pBox.Y -= size.Height / 2;
            }
            g.FillRectangle(bgBrush, new RectangleF(pBox, size));
            g.DrawString(s, font, textBrush, p, format);
        }

        public static void DrawStringOpaque(this Graphics g, string s, Font font, Brush textBrush, Brush bgBrush, PointF p)
        {
            g.DrawStringOpaque(s, font, textBrush, bgBrush, p, StringFormat.GenericTypographic);
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, float x, float y, float width, float height, float cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (pen == null)
                throw new ArgumentNullException("pen");

            using (GraphicsPath path = RoundedRect(new RectangleF(x, y, width, height), cornerRadius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF bounds, float cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (pen == null)
                throw new ArgumentNullException("pen");

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            float diameter = radius * 2;
            SizeF size = new SizeF(diameter, diameter);
            RectangleF arc = new RectangleF(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}

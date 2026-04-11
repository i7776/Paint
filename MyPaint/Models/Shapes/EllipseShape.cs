using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;

namespace MyPaint.Models
{
    public class EllipseShape : Shape
    {
        public override void Draw(Graphics g)
        {
            var state = g.Save();

            int width = Math.Abs(EndPoint.X - StartPoint.X);
            int height = Math.Abs(EndPoint.Y - StartPoint.Y);
            int cx = (StartPoint.X + EndPoint.X) / 2;
            int cy = (StartPoint.Y + EndPoint.Y) / 2;

            g.TranslateTransform(cx, cy);
            g.RotateTransform(this.Angle);
            Rectangle rect = new Rectangle(-width / 2, -height / 2, width, height);

            if (FillColor.A > 0)
            {
                using (var brush = new SolidBrush(FillColor))
                {
                    g.FillEllipse(brush, rect);
                }
            }

            using (var pen = new Pen(Color, Thickness))
            {
                g.DrawEllipse(pen, rect);
            }

            g.Restore(state);
        }

        public override Shape Clone()
        {
            EllipseShape copy = new EllipseShape();
            copy.StartPoint = this.StartPoint;
            copy.EndPoint = this.EndPoint;
            copy.Thickness = this.Thickness;
            copy.Color = this.Color;
            copy.FillColor = this.FillColor;
            copy.Angle = this.Angle;
            return copy;
        }

        public override bool ContainPoint(Point p)
        {
            int left = Math.Min(StartPoint.X, EndPoint.X);
            int right = Math.Max(StartPoint.X, EndPoint.X);
            int top = Math.Min(StartPoint.Y, EndPoint.Y);
            int bottom = Math.Max(StartPoint.Y, EndPoint.Y);
            
            double centerX = (left + right) / 2;
            double centerY = (top + bottom) / 2;

            double tempX = p.X - centerX;
            double tempY = p.Y - centerY;

            double rad = -this.Angle * Math.PI / 180.0;

            double rotatedX = tempX * Math.Cos(rad) - tempY * Math.Sin(rad);
            double rotatedY = tempX * Math.Sin(rad) + tempY * Math.Cos(rad);

            double dx = rotatedX;
            double dy = rotatedY;

            int width = Math.Abs(EndPoint.X - StartPoint.X);
            int height = Math.Abs(EndPoint.Y - StartPoint.Y);

            double a = width / 2.0;
            double b = height / 2.0;

            return (dx * dx) / (a * a) + (dy * dy) / (b * b) <= 1.05;

        }

        public override void Move(int x, int y)
        {
            StartPoint = new Point(StartPoint.X + x, StartPoint.Y + y);
            EndPoint = new Point(EndPoint.X + x, EndPoint.Y + y);
        }
        
        public override Rectangle GetBounds()
        {
            int x = Math.Min(StartPoint.X, EndPoint.X);
            int y = Math.Min(StartPoint.Y, EndPoint.Y);
            int w = Math.Abs(StartPoint.X - EndPoint.X);
            int h = Math.Abs(StartPoint.Y - EndPoint.Y);
            return new Rectangle(x, y, Math.Max(w, 1), Math.Max(h, 1));
        }


    }
}

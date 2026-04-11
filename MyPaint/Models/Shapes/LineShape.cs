using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;

namespace MyPaint.Models.Shapes
{
    public class LineShape : Shape
    {
        public override void Draw(Graphics g)
        {
            var state = g.Save();
            Rectangle b = GetBounds();
            int cx = b.X + b.Width / 2;
            int cy = b.Y + b.Height / 2;

            g.TranslateTransform(cx, cy);
            g.RotateTransform(this.Angle);

            using (Pen pen = new Pen(this.Color, this.Thickness))
            {
                g.DrawLine(pen, StartPoint.X - cx, StartPoint.Y - cy, EndPoint.X - cx, EndPoint.Y - cy);
            }
            g.Restore(state);
        }


        public override Shape Clone()
        {
            LineShape copy = new LineShape();
            copy.StartPoint = this.StartPoint;
            copy.EndPoint = this.EndPoint;
            copy.Thickness = this.Thickness;
            copy.Color = this.Color;
            copy.FillColor = this.FillColor;
            return copy;
        }

        public override bool ContainPoint(Point p)
        {
            //разворачиваем мышку
            Point lp = RotatePointBack(p, this.Angle);

            double lineLength = Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));
            if (lineLength == 0) return false;

            double distance = Math.Abs((EndPoint.Y - StartPoint.Y) * lp.X - (EndPoint.X - StartPoint.X) * lp.Y + EndPoint.X * StartPoint.Y - EndPoint.Y * StartPoint.X) / lineLength;

            if (distance > (Thickness + 3)) return false; 

            double dotProduct = (lp.X - StartPoint.X) * (EndPoint.X - StartPoint.X) + (lp.Y - StartPoint.Y) * (EndPoint.Y - StartPoint.Y);
            return dotProduct >= 0 && dotProduct <= lineLength * lineLength;
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
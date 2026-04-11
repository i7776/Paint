using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MyPaint.Models.Shapes
{
    public abstract class Shape
    {
        public abstract void Draw(Graphics g);
        public abstract bool ContainPoint(Point p);
        public abstract void Move(int x, int y);
        public abstract Shape Clone();
        public Color Color { get; set; }
        public int Thickness { get; set; }
        public Color FillColor { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public float Angle { get; set; } = 0;
        public abstract Rectangle GetBounds();
        protected Point RotatePointBack(Point p, float angle)
        {
            if (angle == 0) return p;
            Rectangle b = GetBounds();
            Point center = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);

            double rad = -angle * Math.PI / 180.0; //обратный угол
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            double dx = p.X - center.X;
            double dy = p.Y - center.Y;

            return new Point(
                (int)(center.X + dx * cos - dy * sin),
                (int)(center.Y + dx * sin + dy * cos)
            );
        }

    }
}
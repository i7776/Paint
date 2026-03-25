using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MyPaint.Models.Shapes
{
    abstract class Shape
    {
        public abstract void Draw(Graphics g);
        public abstract bool ContainPoint(Point p);
        public abstract void Move(int x, int y);
        public abstract void Rotate(float angle, Point center);
        public abstract Shape Clone();
        public abstract void Resize(float scale);
        public Color Color { get; set; }
        public int Thickness { get; set; }
        public Color FillColor { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

    }
}
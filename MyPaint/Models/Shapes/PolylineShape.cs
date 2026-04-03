using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace MyPaint.Models.Shapes
{
    public class PolylineShape : Shape
    {

        public List<Point> Points { get; set; } = new List<Point>();
        public override void Draw(Graphics g)
        {
            if (Points.Count < 2)
            { 
               return;
            }

            using (Pen pen = new Pen(this.Color, this.Thickness))
            {
                g.DrawLines(pen, Points.ToArray());
            }
        }

        public override Shape Clone()
        {
            PolylineShape copy = new PolylineShape();
            copy.Points = new List<Point>(this.Points);
            copy.Color = this.Color;
            copy.Thickness = this.Thickness;
            copy.Angle = this.Angle;
            return copy;
        }

        public override void Move(int dx, int dy)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = new Point(Points[i].X + dx, Points[i].Y + dy);
            }
        }

        public override bool ContainPoint(Point p)
        {
            for (int i = 1; i < Points.Count; i++)
            {
                Point p1 = Points[i - 1];
                Point p2 = Points[i];

                double lineLength = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                if (lineLength == 0) continue;

                double distance = Math.Abs((p2.Y - p1.Y) * p.X - (p2.X - p1.X) * p.Y + p2.X * p1.Y - p2.Y * p1.X) / lineLength;

                if (distance <= 5.0) 
                {
                    double dotProduct = (p.X - p1.X) * (p2.X - p1.X) + (p.Y - p1.Y) * (p2.Y - p1.Y);
                    if (dotProduct >= 0 && dotProduct <= lineLength * lineLength)
                        return true;
                }
            }
            return false;
        }


        public override void Resize(float scale)
        {
            if (Points.Count == 0) return;

            float avgX = 0, avgY = 0;
            foreach (var p in Points) { avgX += p.X; avgY += p.Y; }
            Point center = new Point((int)avgX / Points.Count, (int)avgY / Points.Count);

            for (int i = 0; i < Points.Count; i++)
            {
                int newX = center.X + (int)((Points[i].X - center.X) * scale);
                int newY = center.Y + (int)((Points[i].Y - center.Y) * scale);
                Points[i] = new Point(newX, newY);
            }
        }


        public override void Rotate(float angle, Point center)
        {
            double rad = angle * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            for (int i = 0; i < Points.Count; i++)
            {
                int dx = Points[i].X - center.X;
                int dy = Points[i].Y - center.Y;

                int newX = (int)(center.X + dx * cos - dy * sin);
                int newY = (int)(center.Y + dx * sin + dy * cos);

                Points[i] = new Point(newX, newY);
            }
        }

    }
}

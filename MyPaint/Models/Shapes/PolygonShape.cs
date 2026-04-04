using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Linq;


namespace MyPaint.Models.Shapes
{
    public class PolygonShape : Shape
    {
        public List<Point> Points {  get; set; } = new List<Point>();
        public override void Draw(Graphics g)
        {
            if (Points.Count < 2) return;

            if (FillColor.A > 0 && Points.Count > 2)
            {
                using (var brush = new SolidBrush(FillColor))
                {
                    g.FillPolygon(brush, Points.ToArray());
                }
            }

            using (var pen = new Pen(Color, Thickness))
            {
                g.DrawPolygon(pen, Points.ToArray());
            }
        }

        public override Shape Clone()
        {
            PolygonShape copy = new PolygonShape();
            copy.Points = new List<Point>(this.Points);
            copy.Color = this.Color;
            copy.Thickness = this.Thickness;
            copy.FillColor = this.FillColor;
            copy.Angle = this.Angle;
            return copy;
        }

        public override bool ContainPoint(Point p)
        {
            if (Points.Count < 3) return false;

            bool result = false;
            int j = Points.Count - 1;

            for (int i = 0; i < Points.Count; i++)
            {
                // Проверяем, пересекает ли горизонтальный луч из точки P ребро (Points[i], Points[j])
                if ((Points[i].Y < p.Y && Points[j].Y >= p.Y || Points[j].Y < p.Y && Points[i].Y >= p.Y) &&
                    (Points[i].X + (double)(p.Y - Points[i].Y) / (Points[j].Y - Points[i].Y) * (Points[j].X - Points[i].X) < p.X))
                {
                    result = !result;
                }
                j = i;
            }

            if (result) return true;

            for (int i = 0; i < Points.Count; i++)
            {
                Point p1 = Points[i];
                Point p2 = Points[(i + 1) % Points.Count]; // Замыкаем последнюю точку на первую

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

        public override void Move(int x, int y)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = new Point(Points[i].X + x, Points[i].Y + y);
            }
        }

        public override void Resize(float scale)
        {
            if (Points.Count == 0) return;

            double avgX = Points.Average(p => p.X);
            double avgY = Points.Average(p => p.Y);
            Point center = new Point((int)avgX, (int)avgY);

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

        public override Rectangle GetBounds()
        {
            if (Points == null || Points.Count == 0)
                return new Rectangle(StartPoint.X, StartPoint.Y, 0, 0);

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var p in Points)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            int padding = 5;
            return new Rectangle(minX - padding, minY - padding, (maxX - minX) + padding * 2,(maxY - minY) + padding * 2);
        }

        public void ResizeByMouse(System.Drawing.Point anchor, System.Drawing.Point mousePos, List<System.Drawing.Point> originalPoints)
        {
            if (originalPoints == null || originalPoints.Count == 0) return;

            // находим границы фигуры
            int minX = originalPoints.Min(p => p.X);
            int maxX = originalPoints.Max(p => p.X);
            int minY = originalPoints.Min(p => p.Y);
            int maxY = originalPoints.Max(p => p.Y);

            float oldWidth = maxX - minX;
            float oldHeight = maxY - minY;

            if (oldWidth == 0) oldWidth = 1;
            if (oldHeight == 0) oldHeight = 1;

            // считаем коэффициенты масштабирования 
            float scaleX = (float)Math.Abs(mousePos.X - anchor.X) / oldWidth;
            float scaleY = (float)Math.Abs(mousePos.Y - anchor.Y) / oldHeight;

            // направление 
            int dirX = mousePos.X >= anchor.X ? 1 : -1;
            int dirY = mousePos.Y >= anchor.Y ? 1 : -1;

            for (int i = 0; i < originalPoints.Count; i++)
            {
                // расстояние от якоря до оригинальной точки
                float distOriginX = Math.Abs(originalPoints[i].X - anchor.X);
                float distOriginY = Math.Abs(originalPoints[i].Y - anchor.Y);

                // новая позиция
                int newX = anchor.X + (int)(distOriginX * scaleX) * dirX;
                int newY = anchor.Y + (int)(distOriginY * scaleY) * dirY;

                this.Points[i] = new System.Drawing.Point(newX, newY);
            }
        }
    }
}

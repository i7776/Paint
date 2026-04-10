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

            var state = g.Save();
            Rectangle b = GetBounds();
            int cx = b.X + b.Width / 2;
            int cy = b.Y + b.Height / 2;

            g.TranslateTransform(cx, cy);
            g.RotateTransform(this.Angle);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Пересчитываем точки относительно центра (0,0)
            var relativePoints = Points.Select(p => new System.Drawing.Point(p.X - cx, p.Y - cy)).ToArray();

            if (FillColor.A > 0 && this is PolygonShape)
            {
                using (var brush = new SolidBrush(FillColor))
                    g.FillPolygon(brush, relativePoints);
            }

            using (var pen = new Pen(Color, Thickness))
            {
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                if (this is PolygonShape) g.DrawPolygon(pen, relativePoints);
                else g.DrawLines(pen, relativePoints);
            }
            g.Restore(state);
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
            if (Points == null || Points.Count == 0) return new Rectangle(0, 0, 0, 0);
            int minX = Points.Min(p => p.X);
            int minY = Points.Min(p => p.Y);
            int maxX = Points.Max(p => p.X);
            int maxY = Points.Max(p => p.Y);
            // Никаких +5 или +10 здесь!
            return new Rectangle(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
        }

        public void ResizeByMouse(Point anchor, Point mouse, List<Point> originalPoints)
        {
            if (originalPoints == null || originalPoints.Count == 0) return;

            // Находим границы того состояния, которое было в момент нажатия мыши
            int minX = originalPoints.Min(p => p.X);
            int minY = originalPoints.Min(p => p.Y);
            int maxX = originalPoints.Max(p => p.X);
            int maxY = originalPoints.Max(p => p.Y);
            int oldW = Math.Max(1, maxX - minX);
            int oldH = Math.Max(1, maxY - minY);

            // Текущие размеры рамки ресайза
            int newLeft = Math.Min(anchor.X, mouse.X);
            int newTop = Math.Min(anchor.Y, mouse.Y);
            int newWidth = Math.Max(1, Math.Abs(mouse.X - anchor.X));
            int newHeight = Math.Max(1, Math.Abs(mouse.Y - anchor.Y));

            for (int i = 0; i < Points.Count; i++)
            {
                // Вычисляем процентное положение точки в старой рамке (от 0.0 до 1.0)
                float pctX = (float)(originalPoints[i].X - minX) / oldW;
                float pctY = (float)(originalPoints[i].Y - minY) / oldH;

                // Переносим этот процент в новую рамку
                int nx = (int)(newLeft + pctX * newWidth);
                int ny = (int)(newTop + pctY * newHeight); // Теперь переменная объявлена!

                Points[i] = new Point(nx, ny);
            }
        }

    }
}

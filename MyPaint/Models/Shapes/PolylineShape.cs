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
            if (Points.Count < 2) return;

            var oldMode = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (FillColor.A > 0 && Points.Count > 2)
            {
                using (var brush = new SolidBrush(FillColor))
                {
                    g.FillPolygon(brush, Points.ToArray());
                }
            }

            using (var pen = new Pen(Color, Thickness))
            {
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                g.DrawLines(pen, Points.ToArray());
            }

            g.SmoothingMode = oldMode;
        }


        public override Shape Clone()
        {
            PolylineShape copy = new PolylineShape();
            copy.Points = new List<Point>(this.Points);
            copy.Color = this.Color;
            copy.Thickness = this.Thickness;
            copy.FillColor = this.FillColor;
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
            if (Points.Count < 2) return false;

            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                // Добавляем все линии в путь
                path.AddLines(Points.ToArray());

                // 1. Проверка попадания в закрашенную область
                if (FillColor.A > 0)
                {
                    if (path.IsVisible(p)) return true;
                }

                using (var pen = new Pen(Color, Thickness + 5))
                {
                    if (path.IsOutlineVisible(p, pen)) return true;
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
            return new Rectangle(minX - padding, minY - padding, (maxX - minX) + padding * 2, (maxY - minY) + padding * 2);
        }

        public void ResizeByMouse(System.Drawing.Point anchor, System.Drawing.Point mousePos, List<System.Drawing.Point> originalPoints)
        {
            if (originalPoints == null || originalPoints.Count == 0) return;

            // 1. Находим границы ОРИГИНАЛЬНОЙ фигуры, чтобы понять масштаб
            int minX = originalPoints.Min(p => p.X);
            int maxX = originalPoints.Max(p => p.X);
            int minY = originalPoints.Min(p => p.Y);
            int maxY = originalPoints.Max(p => p.Y);

            float oldWidth = maxX - minX;
            float oldHeight = maxY - minY;

            // Защита от деления на ноль (если фигура - точка)
            if (oldWidth == 0) oldWidth = 1;
            if (oldHeight == 0) oldHeight = 1;

            // 2. Считаем коэффициенты масштабирования по X и Y
            float scaleX = (float)Math.Abs(mousePos.X - anchor.X) / oldWidth;
            float scaleY = (float)Math.Abs(mousePos.Y - anchor.Y) / oldHeight;

            // 3. Определяем направление (тянем ли мы в плюс или в минус от якоря)
            int dirX = mousePos.X >= anchor.X ? 1 : -1;
            int dirY = mousePos.Y >= anchor.Y ? 1 : -1;

            // 4. Пересчитываем каждую точку
            for (int i = 0; i < originalPoints.Count; i++)
            {
                // Расстояние от якоря до оригинальной точки
                float distOriginX = Math.Abs(originalPoints[i].X - anchor.X);
                float distOriginY = Math.Abs(originalPoints[i].Y - anchor.Y);

                // Новая позиция = Якорь + (Смещение * Масштаб * Направление)
                int newX = anchor.X + (int)(distOriginX * scaleX) * dirX;
                int newY = anchor.Y + (int)(distOriginY * scaleY) * dirY;

                this.Points[i] = new System.Drawing.Point(newX, newY);
            }
        }

    }
}

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

            //разворачиваем мышку
            Point lp = RotatePointBack(p, this.Angle);

            // алгоритм луча
            bool result = false;
            int j = Points.Count - 1;
            for (int i = 0; i < Points.Count; i++)
            {
                if ((Points[i].Y < lp.Y && Points[j].Y >= lp.Y || Points[j].Y < lp.Y && Points[i].Y >= lp.Y) &&
                    (Points[i].X + (double)(lp.Y - Points[i].Y) / (Points[j].Y - Points[i].Y) * (Points[j].X - Points[i].X) < lp.X))
                {
                    result = !result;
                }
                j = i;
            }
            return result;
        }


        public override void Move(int x, int y)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = new Point(Points[i].X + x, Points[i].Y + y);
            }
        }

        public override Rectangle GetBounds()
        {
            if (Points == null || Points.Count == 0) return new Rectangle(0, 0, 0, 0);
            int minX = Points.Min(p => p.X);
            int minY = Points.Min(p => p.Y);
            int maxX = Points.Max(p => p.X);
            int maxY = Points.Max(p => p.Y);
            return new Rectangle(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
        }

        public void ResizeByMouse(Point anchor, Point mouse, List<Point> originalPoints)
        {
            if (originalPoints == null || originalPoints.Count == 0) return;

            // находим границы того состояния, которое было в момент нажатия мыши
            int minX = originalPoints.Min(p => p.X);
            int minY = originalPoints.Min(p => p.Y);
            int maxX = originalPoints.Max(p => p.X);
            int maxY = originalPoints.Max(p => p.Y);
            int oldW = Math.Max(1, maxX - minX);
            int oldH = Math.Max(1, maxY - minY);

            // текущие размеры рамки ресайза
            int newLeft = Math.Min(anchor.X, mouse.X);
            int newTop = Math.Min(anchor.Y, mouse.Y);
            int newWidth = Math.Max(1, Math.Abs(mouse.X - anchor.X));
            int newHeight = Math.Max(1, Math.Abs(mouse.Y - anchor.Y));

            for (int i = 0; i < Points.Count; i++)
            {
                // процентное положение точки в старой рамке 
                float pctX = (float)(originalPoints[i].X - minX) / oldW;
                float pctY = (float)(originalPoints[i].Y - minY) / oldH;

                // переносим этот процент в новую рамку
                int nx = (int)(newLeft + pctX * newWidth);
                int ny = (int)(newTop + pctY * newHeight); 

                Points[i] = new Point(nx, ny);
            }
        }

    }
}

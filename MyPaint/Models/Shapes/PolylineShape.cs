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

            var state = g.Save();
            Rectangle b = GetBounds();
            int cx = b.X + b.Width / 2;
            int cy = b.Y + b.Height / 2;

            g.TranslateTransform(cx, cy);
            g.RotateTransform(this.Angle);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // пересчитываем точки относительно центра
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
                path.AddLines(Points.ToArray());
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
            int minX = originalPoints.Min(p => p.X);
            int minY = originalPoints.Min(p => p.Y);
            int oldW = Math.Max(1, originalPoints.Max(p => p.X) - minX);
            int oldH = Math.Max(1, originalPoints.Max(p => p.Y) - minY);

            int newLeft = Math.Min(anchor.X, mouse.X);
            int newTop = Math.Min(anchor.Y, mouse.Y);
            int newWidth = Math.Max(1, Math.Abs(mouse.X - anchor.X));
            int newHeight = Math.Max(1, Math.Abs(mouse.Y - anchor.Y));

            for (int i = 0; i < Points.Count; i++)
            {
                float pctX = (float)(originalPoints[i].X - minX) / oldW;
                float pctY = (float)(originalPoints[i].Y - minY) / oldH;
                Points[i] = new Point((int)(newLeft + pctX * newWidth), (int)(newTop + pctY * newHeight));
            }
        }

    }
}

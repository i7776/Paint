using MyPaint.Models.Shapes; 
using System;
using System.Drawing; 

namespace TrapezoidPlugin
{
    public class TrapezoidShape : Shape
    {
        public override void Draw(Graphics g)
        {
            var state = g.Save();

            Rectangle b = GetBounds();
            int cx = b.X + b.Width / 2;
            int cy = b.Y + b.Height / 2;

            g.TranslateTransform(cx, cy);
            g.RotateTransform(this.Angle);

            int w = b.Width;
            int h = b.Height;
            int offset = w / 4; // скос трапеции

            Point[] pts = {
                new Point(-w/2 + offset, -h/2), // верх лево
                new Point(w/2 - offset, -h/2),  // верх право
                new Point(w/2, h/2),            // низ право
                new Point(-w/2, h/2)            // низ лево
            };

            if (FillColor != Color.Transparent)
            {
                using (Brush br = new SolidBrush(FillColor))
                    g.FillPolygon(br, pts);
            }

            using (Pen p = new Pen(Color, Thickness))
                g.DrawPolygon(p, pts);

            g.Restore(state);
        }

        public override Rectangle GetBounds()
        {
            int x = Math.Min(StartPoint.X, EndPoint.X);
            int y = Math.Min(StartPoint.Y, EndPoint.Y);
            int w = Math.Abs(StartPoint.X - EndPoint.X);
            int h = Math.Abs(StartPoint.Y - EndPoint.Y);
            return new Rectangle(x, y, Math.Max(w, 1), Math.Max(h, 1));
        }

        public override bool ContainPoint(Point p)
        {
            return GetBounds().Contains(p);
        }

        public override void Move(int dx, int dy)
        {
            StartPoint = new Point(StartPoint.X + dx, StartPoint.Y + dy);
            EndPoint = new Point(EndPoint.X + dx, EndPoint.Y + dy);
        }

        public override Shape Clone()
        {
            return new TrapezoidShape
            {
                StartPoint = this.StartPoint,
                EndPoint = this.EndPoint,
                Color = this.Color,
                FillColor = this.FillColor,
                Thickness = this.Thickness,
                Angle = this.Angle
            };
        }
    }
}
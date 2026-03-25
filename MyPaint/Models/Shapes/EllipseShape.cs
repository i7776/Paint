using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;

namespace MyPaint.Models
{
    class EllipseShape : Shape
    {
        public override void Draw(Graphics g)
        {
            using (Pen pen = new Pen(this.Color, this.Thickness))
            {
                int x = Math.Min(StartPoint.X, EndPoint.X);
                int y = Math.Min(StartPoint.Y, EndPoint.Y);
                int width = Math.Abs(EndPoint.X - StartPoint.X);
                int height = Math.Abs(EndPoint.Y - StartPoint.Y);

                Rectangle rect = new Rectangle(x, y, width, height);

                if (FillColor != Color.Empty && FillColor != Color.Transparent)
                {
                    using (SolidBrush brush = new SolidBrush(FillColor))
                    {
                        g.FillRectangle(brush, rect);
                    }
                }

                g.DrawEllipse(pen, rect);
            }
        }

        public override Shape Clone()
        {
            EllipseShape copy = new EllipseShape();
            copy.StartPoint = this.StartPoint;
            copy.EndPoint = this.EndPoint;
            copy.Thickness = this.Thickness;
            copy.Color = this.Color;
            copy.FillColor = this.FillColor;
            return copy;
        }

        public override bool ContainPoint(Point p)
        {
            int left = Math.Min(StartPoint.X, EndPoint.X);
            int right = Math.Max(StartPoint.X, EndPoint.X);
            int top = Math.Min(StartPoint.Y, EndPoint.Y);
            int bottom = Math.Max(StartPoint.Y, EndPoint.Y);
            
            double centerX = (left + right) / 2;
            double centerY = (top + bottom) / 2;
            double radX = (right - left) / 2;
            double radY = (top - bottom) / 2;

            if (radX == 0 || radY == 0)
            {
                return false;
            }

            double dx = (p.X - centerX) / radX;
            double dy = (p.Y - centerY) / radY;

            double dist = dx * dx + dy * dy;
            if (dist < 1.0)
            {
                return true;
            }

            if(Math.Abs(dist - 1.0) < 0.1)
            {
                return true;
            }

            return false;

        }


        public override void Move(int x, int y)
        {
            StartPoint = new Point(StartPoint.X + x, StartPoint.Y + y);
            EndPoint = new Point(EndPoint.X + x, EndPoint.Y + y);
        }
        
        public override void Resize(float scale)
        {
            int newX = StartPoint.X + (int)((EndPoint.X - StartPoint.X) * scale);
            int newY = StartPoint.Y + (int)((EndPoint.Y - StartPoint.Y) * scale);
            EndPoint = new Point(newX, newY);
        }

        public override void Rotate(float angle, Point center)
        {
            double rad = angle * Math.PI / 180.0;

            int newStartX = (int)(center.X + (StartPoint.X - center.X) * Math.Cos(rad) - (StartPoint.Y - center.Y) * Math.Sin(rad));
            int newStartY = (int)(center.Y + (StartPoint.X - center.X) * Math.Sin(rad) + (StartPoint.Y - center.Y) * Math.Cos(rad));

            int newEndX = (int)(center.X + (EndPoint.X - center.X) * Math.Cos(rad) - (EndPoint.Y - center.Y) * Math.Sin(rad));
            int newEndY = (int)(center.Y + (EndPoint.X - center.X) * Math.Sin(rad) + (EndPoint.Y - center.Y) * Math.Cos(rad));

            StartPoint = new Point(newStartX, newStartY);
            EndPoint = new Point(newEndX, newEndY);
        }
    }
}

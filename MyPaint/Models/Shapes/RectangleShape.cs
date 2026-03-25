using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;

namespace MyPaint.Models
{
    class RectangleShape : Shape
    {
        public override void Draw(Graphics g)
        {
            Pen pen = new Pen(this.Color, this.Thickness);

            int x = Math.Min(StartPoint.X, EndPoint.X);
            int y = Math.Min(StartPoint.X, EndPoint.X);
            int width = Math.Abs(EndPoint.X - StartPoint.X);
            int height = Math.Abs(StartPoint.Y - StartPoint.Y);

            Rectangle rect = new Rectangle(x, y, width, height);
            g.DrawRectangle(pen, rect);
        }

        public override Shape Clone()
        {
            RectangleShape copy = new RectangleShape();
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

            if (p.X >= left && p.Y <= right && p.Y >= top && p.Y <= bottom)
            {
                return true;
            }
            return false ;
        }


        public override void Move(int x, int y)
        {
            StartPoint = new Point(StartPoint.X + x, StartPoint.Y + y);
            EndPoint = new Point(EndPoint.X + x, EndPoint.Y + y);
        }
        public override void Resize(float scale)
        {
            int centerX = (StartPoint.X - EndPoint.X) / 2;
            int centerY = (StartPoint.Y - EndPoint.Y) / 2;

            int halfwidth = Math.Abs(EndPoint.X - StartPoint.X) / 2;
            int halfheight = Math.Abs(EndPoint.Y - StartPoint.Y)/2;

            int newHalfwidth = (int)(halfwidth * scale);
            int newHalfheigth  = (int)(halfheight * scale);

            StartPoint = new Point(centerX - newHalfwidth, centerY - newHalfheigth);
            EndPoint = new Point(centerX +  newHalfwidth, centerY + newHalfheigth);

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

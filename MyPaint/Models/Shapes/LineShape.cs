using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;

namespace MyPaint.Models.Shapes
{
    class LineShape : Shape
    {
        public override void Draw(Graphics g)
        {
            Pen pen = new Pen(this.Color, this.Thickness);
            g.DrawLine(pen, StartPoint, EndPoint);
        }

        public override Shape Clone()
        {
            LineShape copy = new LineShape();
            copy.StartPoint = this.StartPoint;
            copy.EndPoint = this.EndPoint;
            copy.Thickness = this.Thickness;
            copy.Color = this.Color;
            return copy;
        }

        public override bool ContainPoint(Point p)
        {
            double lineLength = Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));

            if (lineLength == 0) return false;

            double distance = Math.Abs((EndPoint.Y - StartPoint.Y) * p.X - (EndPoint.X - StartPoint.X) * p.Y + EndPoint.X * StartPoint.Y - EndPoint.Y * StartPoint.X) / lineLength;

            if (distance > 5.0) return false;

            double dotProduct = (p.X - StartPoint.X) * (EndPoint.X - StartPoint.X) + (p.Y - StartPoint.Y) * (EndPoint.Y - StartPoint.Y);
            if (dotProduct < 0 || dotProduct > lineLength * lineLength) return false;

            return true;
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
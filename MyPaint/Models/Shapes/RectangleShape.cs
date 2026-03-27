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
            var state = g.Save(); 

            int width = Math.Abs(EndPoint.X - StartPoint.X);
            int height = Math.Abs(EndPoint.Y - StartPoint.Y);
            int cx = (StartPoint.X + EndPoint.X) / 2;
            int cy = (StartPoint.Y + EndPoint.Y) / 2;

            g.TranslateTransform(cx, cy); 
            g.RotateTransform(this.Angle);   

            Rectangle rect = new Rectangle(-width / 2, -height / 2, width, height);

            using (Pen pen = new Pen(this.Color, this.Thickness))
            {
                if (FillColor != Color.Empty)
                {
                    using (SolidBrush brush = new SolidBrush(FillColor))
                        g.FillRectangle(brush, rect);
                }
                g.DrawRectangle(pen, rect);
            }

            g.Restore(state); 
        }


        public override Shape Clone()
        {
            RectangleShape copy = new RectangleShape();
            copy.StartPoint = this.StartPoint;
            copy.EndPoint = this.EndPoint;
            copy.Thickness = this.Thickness;
            copy.Color = this.Color;
            copy.FillColor = this.FillColor;
            copy.Angle = this.Angle; 
            return copy;
        }

        public override bool ContainPoint(Point p)
        {
            // находим центр
            float cx = (StartPoint.X + EndPoint.X) / 2f;
            float cy = (StartPoint.Y + EndPoint.Y) / 2f;

            // переводим угол в радианы и делаем его ОТРИЦАТЕЛЬНЫМ
            double rad = -this.Angle * Math.PI / 180.0;

            // вращаем точку клика p вокруг центра cx, cy в обратную сторону
            double tempX = p.X - cx;
            double tempY = p.Y - cy;

            double rotatedX = tempX * Math.Cos(rad) - tempY * Math.Sin(rad);
            double rotatedY = tempX * Math.Sin(rad) + tempY * Math.Cos(rad);

            // используем твою обычную логику проверки границ
            int width = Math.Abs(EndPoint.X - StartPoint.X);
            int height = Math.Abs(EndPoint.Y - StartPoint.Y);

            // точка в границы прямоугольника относительно его центра
            return Math.Abs(rotatedX) <= width / 2.0 && Math.Abs(rotatedY) <= height / 2.0;
        }

        public override void Move(int x, int y)
        {
            StartPoint = new Point(StartPoint.X + x, StartPoint.Y + y);
            EndPoint = new Point(EndPoint.X + x, EndPoint.Y + y);
        }
        public override void Resize(float scale)
        {
            int centerX = (StartPoint.X + EndPoint.X) / 2;
            int centerY = (StartPoint.Y + EndPoint.Y) / 2;

            int halfwidth = Math.Abs(EndPoint.X - StartPoint.X) / 2;
            int halfheight = Math.Abs(EndPoint.Y - StartPoint.Y)/2;

            int newHalfWidth = Math.Max(1, (int)(halfwidth * scale));  
            int newHalfHeight = Math.Max(1, (int)(halfheight * scale)); 

            StartPoint = new Point(centerX - newHalfWidth, centerY - newHalfHeight);
            EndPoint = new Point(centerX +  newHalfWidth, centerY + newHalfHeight);

        }

        public override void Rotate(float angle, Point center)
        {
            this.Angle += angle;
        }

    }
}

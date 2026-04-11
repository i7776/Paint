using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace MyPaint
{
    public partial class MainWindow 
    {
        private void Render()
        {
            int width = (int)Math.Max(CanvasImage.ActualWidth, 800);
            int height = (int)Math.Max(CanvasImage.ActualHeight, 600);

            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    if (_project.Layers.Count == 0)
                    {
                        g.Clear(Color.DarkGray);
                        using (Font font = new Font("Arial", 16))
                        {
                            g.DrawString("Создайте слой, чтобы начать рисовать", font, Brushes.White, new PointF(width / 4, height / 2));
                        }
                    }
                    else
                    {
                        g.Clear(Color.White);
                        //красивыве линии - не лесенкой
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                        // рисуем проект 
                        _project.Draw(g);

                        if (_project.ActiveLayer != null && _project.ActiveLayer.IsVisible)
                        {
                            _tempShape?.Draw(g);
                        }

                        if (_currTool == "Select" && _selectedShape != null)
                        {
                            Rectangle bounds = _selectedShape.GetBounds();
                            var state = g.Save();

                            // центр для вращения рамки
                            int cx = bounds.X + bounds.Width / 2;
                            int cy = bounds.Y + bounds.Height / 2;

                            g.TranslateTransform(cx, cy);
                            g.RotateTransform(_selectedShape.Angle);

                            // рисуем относительно центра
                            int halfW = bounds.Width / 2;
                            int halfH = bounds.Height / 2;
                            Rectangle localRect = new Rectangle(-halfW, -halfH, bounds.Width, bounds.Height);

                            using (Pen framePen = new Pen(Color.Blue, 1))
                            {
                                framePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                                g.DrawRectangle(framePen, localRect);
                            }

                            // квадратики ресайза
                            int s = 6;
                            Point[] handles = new Point[] {
                                new Point(localRect.Left, localRect.Top),     
                                new Point(localRect.Right, localRect.Top),    
                                new Point(localRect.Left, localRect.Bottom),  
                                new Point(localRect.Right, localRect.Bottom)  
                            };

                            foreach (var hp in handles)
                            {
                                g.FillRectangle(Brushes.White, hp.X - s / 2, hp.Y - s / 2, s, s);
                                g.DrawRectangle(Pens.DodgerBlue, hp.X - s / 2, hp.Y - s / 2, s, s);
                            }

                            // антенна для вращения
                            int d = 8;
                            int rotX = 0; // центр по X
                            int rotY = -halfH - 20;
                            g.DrawLine(Pens.DodgerBlue, 0, -halfH, rotX, rotY);
                            g.FillEllipse(Brushes.White, rotX - d / 2, rotY - d / 2, d, d);
                            g.DrawEllipse(Pens.DodgerBlue, rotX - d / 2, rotY - d / 2, d, d);

                            g.Restore(state);
                        }

                    }
                }

                // вывод на экран
                CanvasImage.Source = BitmapToImageSource(bmp);
            }
        }

        private System.Drawing.Point RotatePoint(System.Drawing.Point p, System.Drawing.Point center, float angle)
        {
            if (angle == 0) return p;
            double rad = angle * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            double dx = p.X - center.X;
            double dy = p.Y - center.Y;

            int newX = (int)Math.Round(center.X + dx * cos - dy * sin);
            int newY = (int)Math.Round(center.Y + dx * sin + dy * cos);

            return new System.Drawing.Point(newX, newY);
        }


        private BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
            //сохраняем в оперативную память
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                //считать картинку и запомнить
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private void UpdateLayersList()
        {
            var currentActive = _project.ActiveLayer;

            // обновляем список
            LayersList.ItemsSource = null;
            LayersList.ItemsSource = _project.Layers;

            // возвращаем выделение в списке
            LayersList.SelectedItem = currentActive;
        }
    }
}

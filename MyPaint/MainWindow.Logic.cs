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

                            using (Pen framePen = new Pen(Color.Blue, 1))
                            {
                                framePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                                g.DrawRectangle(framePen, bounds);
                            }

                            // квадратики для изменения размера
                            int s = 6; // размер квадратика
                            using (Brush handleBrush = new SolidBrush(Color.White))
                            {
                                Point[] handles = new Point[] {
                                    new Point(bounds.X, bounds.Y),
                                    new Point(bounds.Right, bounds.Y),
                                    new Point(bounds.X, bounds.Bottom),
                                    new Point(bounds.Right, bounds.Bottom)};

                                // рисование квадратика
                                foreach (var hp in handles)
                                {
                                    g.FillRectangle(handleBrush, hp.X - s / 2, hp.Y - s / 2, s, s);
                                    g.DrawRectangle(Pens.DodgerBlue, hp.X - s / 2, hp.Y - s / 2, s, s);
                                }
                            }

                            int d = 8;
                            using (Brush handleBrush = new SolidBrush(Color.White))
                            {
                                int topCenX = bounds.X + bounds.Width / 2;
                                int topCenY = bounds.Y;

                                int rotX = topCenX;
                                int rotY = topCenY - 20;

                                g.DrawLine(Pens.DodgerBlue, topCenX, topCenY, rotX, rotY);
                                g.FillEllipse(handleBrush, rotX - d / 2, rotY - d / 2, d, d);
                                g.DrawEllipse(Pens.DodgerBlue, rotX - d / 2, rotY - d / 2, d, d);
                            }
                        }
                    }
                }

                // вывод на экран
                CanvasImage.Source = BitmapToImageSource(bmp);
            }
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

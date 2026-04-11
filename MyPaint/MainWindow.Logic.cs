using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MyPaint.Models.Shapes;
using System.Windows.Media.Imaging;

namespace MyPaint
{
    public partial class MainWindow 
    {
        private void Render()
        {
            if (_project == null || CanvasImage == null) return;

            int width = 1000;
            int height = 800;

            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    if (_project.Layers.Count == 0)
                    {
                        g.Clear(System.Drawing.Color.DarkGray);
                    }
                    else
                    {
                        g.Clear(System.Drawing.Color.White);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                        //рисуем все слои проекта
                        _project.Draw(g);

                        //рисуем временную фигуру
                        if (_project.ActiveLayer != null && _project.ActiveLayer.IsVisible)
                        {
                            _tempShape?.Draw(g);
                        }

                        //рисуем рамку выделения
                        if (_currTool == "Select")
                        {
                            foreach (var shape in _selectedShapes)
                            {
                                DrawSelectionFrame(g, shape);
                            }

                            // 4. Рисуем саму «растягивающуюся» рамку выбора (лассо)
                            if (_isSelectingBox)
                            {
                                using (var p = new System.Drawing.Pen(System.Drawing.Color.DeepSkyBlue, 1))
                                {
                                    p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                                    g.DrawRectangle(p, _selectionRect);
                                }
                            }
                        }
                    }
                }

                CanvasImage.Source = BitmapToImageSource(bmp);
            }
        }

        
        private void DrawSelectionFrame(Graphics g, Shape shape)
        {
            if (shape == null) return;
            Rectangle bounds = shape.GetBounds();
            var state = g.Save();

            int cx = bounds.X + bounds.Width / 2;
            int cy = bounds.Y + bounds.Height / 2;

            g.TranslateTransform(cx, cy);
            g.RotateTransform(shape.Angle);

            int halfW = bounds.Width / 2;
            int halfH = bounds.Height / 2;
            Rectangle localRect = new Rectangle(-halfW, -halfH, bounds.Width, bounds.Height);

            using (System.Drawing.Pen framePen = new System.Drawing.Pen(System.Drawing.Color.Blue, 1))
            {
                framePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawRectangle(framePen, localRect);
            }

            int s = 6;
            System.Drawing.Point[] handles = {
                new System.Drawing.Point(localRect.Left, localRect.Top),
                new System.Drawing.Point(localRect.Right, localRect.Top),
                new System.Drawing.Point(localRect.Left, localRect.Bottom),
                new System.Drawing.Point(localRect.Right, localRect.Bottom)
            };

            foreach (var hp in handles)
            {
                g.FillRectangle(System.Drawing.Brushes.White, hp.X - s / 2, hp.Y - s / 2, s, s);
                g.DrawRectangle(System.Drawing.Pens.DodgerBlue, hp.X - s / 2, hp.Y - s / 2, s, s);
            }

            int d = 8;
            int rotY = -halfH - 20;
            g.DrawLine(System.Drawing.Pens.DodgerBlue, 0, -halfH, 0, rotY);
            g.FillEllipse(System.Drawing.Brushes.White, -d / 2, rotY - d / 2, d, d);
            g.DrawEllipse(System.Drawing.Pens.DodgerBlue, -d / 2, rotY - d / 2, d, d);

            g.Restore(state);
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
            if (_project == null || LayersList == null) return;
            var currentActive = _project.ActiveLayer;

            // обновляем список
            LayersList.ItemsSource = null;
            LayersList.ItemsSource = _project.Layers;

            // возвращаем выделение в списке
            LayersList.SelectedItem = currentActive;
        }
    }
}

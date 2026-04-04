using MyPaint.Models;
using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing; 
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // для событий мыши WPF
using System.Windows.Media.Imaging;

namespace MyPaint
{
    public partial class MainWindow : Window
    {
        private DrawingProject _project;
        private string _currentTool = "Select";
        private Shape? _selectedShape;
        private Shape? _tempShape;
        private System.Drawing.Point _lastMousePos;
        private System.Drawing.Color _currColor  = System.Drawing.Color.Black;
        private System.Drawing.Color _currFillColor = System.Drawing.Color.Transparent;
        private int _currThickness = 2;
        

        public MainWindow()
        {
            InitializeComponent();
            _project = new DrawingProject();

            UpdateLayersList();

            this.Loaded += (s, e) => Render();
        }

        #region Отрисовка
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
                            g.DrawString("Создайте слой, чтобы начать рисовать",
                                         font, Brushes.White, new PointF(width / 4, height / 2));
                        }
                    }
                    else
                    {
                        g.Clear(Color.White);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                        // рисуем проект 
                        _project.Draw(g);

                        if (_project.ActiveLayer != null && _project.ActiveLayer.IsVisible)
                        {
                            _tempShape?.Draw(g);
                        }
                    }
                    CanvasImage.Source = BitmapToImageSource(bmp);
                }

                // вывод на экран
                CanvasImage.Source = BitmapToImageSource(bmp);
            }
        }

        private BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
        #endregion

        #region Мышь (Логика рисования под твои классы)

        private bool _isDrawingPoly = false; // флаг для многоточечного рисования

        // когда кликаем по списку слоев, меняем активный слой в проекте
        private void LayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LayersList.SelectedItem is Layer selectedLayer)
            {
                _project.ActiveLayer = selectedLayer;
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(_project.ActiveLayer == null)
            {
                return;
            }

            if (!_project.ActiveLayer.IsVisible) 
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            var p = e.GetPosition(CanvasImage);
            var drawingPoint = new System.Drawing.Point((int)p.X, (int)p.Y);
            _lastMousePos = drawingPoint;

            if (_currentTool == "Select")
            {
                _selectedShape = _project.GetShapeAt(drawingPoint);

                if (_selectedShape != null)
                {
                    // Обновляем текущие цвета в палитре, чтобы они соответствовали выбранной фигуре
                    _currColor = _selectedShape.Color;
                    _currFillColor = _selectedShape.FillColor;
                    _currThickness = _selectedShape.Thickness;

                    // Обновляем визуальные индикаторы в UI
                    CurrentColorRect.Fill = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(_currColor.A, _currColor.R, _currColor.G, _currColor.B));
                    ThicknessSlider.Value = _currThickness;
                    UpdateFillIndicator();
                }
            }
            else if (_currentTool == "Polyline" || _currentTool == "Polygon")
            {
                if (!_isDrawingPoly) // первый клик
                {
                    _isDrawingPoly = true;
                    _tempShape = CreateShape(_currentTool, drawingPoint);

                    // инициализируем список и добавляем 2 точки: 
                    // одна зафиксирована, вторая будет следовать за мышью
                    if (_tempShape is PolylineShape poly)
                    {
                        poly.Points = new List<System.Drawing.Point> { drawingPoint, drawingPoint };
                    }
                    else if (_tempShape is PolygonShape pg)
                    {
                        pg.Points = new List<System.Drawing.Point> { drawingPoint, drawingPoint };
                    }
                }
                else // последующие клики
                {
                    if (_tempShape is PolylineShape poly) poly.Points.Add(drawingPoint);
                    if (_tempShape is PolygonShape pg) pg.Points.Add(drawingPoint);
                }
            }
            else 
            {
                _tempShape = CreateShape(_currentTool, drawingPoint);
            }
            Render();
        }


        // отпускание
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            // если это ломаная/многоугольник — НЕ завершаем рисование по MouseUp
            if (_currentTool == "Polyline" || _currentTool == "Polygon") return;

            if (_tempShape != null)
            {
                _project.ActiveLayer?.Shapes.Add(_tempShape);
                _tempShape = null;
            }
            Render();
        }

        // завершение ломаной правой кнопкой мыши
        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingPoly && _tempShape != null)
            {
                // удаляем последнюю точку, которая просто следовала за курсором
                if (_tempShape is PolylineShape poly && poly.Points.Count > 1)
                    poly.Points.RemoveAt(poly.Points.Count - 1);
                else if (_tempShape is PolygonShape pg && pg.Points.Count > 1)
                    pg.Points.RemoveAt(pg.Points.Count - 1);

                // добавляем в активный слой
                _project.ActiveLayer?.Shapes.Add(_tempShape);

                _tempShape = null;
                _isDrawingPoly = false;
                Render();
            }
        }

        private void LayerCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Render(); 
        }

        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var p = e.GetPosition(CanvasImage);
            var currentPoint = new System.Drawing.Point((int)p.X, (int)p.Y);

            // логика для ломаной 
            if (_isDrawingPoly && _tempShape != null)
            {
                if (_tempShape is PolylineShape poly && poly.Points.Count > 0)
                {
                    poly.Points[poly.Points.Count - 1] = currentPoint;
                }
                else if (_tempShape is PolygonShape pg && pg.Points.Count > 0)
                {
                    pg.Points[pg.Points.Count - 1] = currentPoint;
                }
                Render();
                return; // выходим, чтобы не срабатывала логика обычных фигур
            }

            // логика для обычных фигур и выделения (нужна зажатая кнопка)
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_currentTool == "Select" && _selectedShape != null)
                {
                    int dx = currentPoint.X - _lastMousePos.X;
                    int dy = currentPoint.Y - _lastMousePos.Y;
                    _selectedShape.Move(dx, dy);
                    _lastMousePos = currentPoint;
                }
                else if (_tempShape != null)
                {
                    _tempShape.EndPoint = currentPoint;
                }
                Render();
            }
        }

        #endregion

        #region Вспомогательное

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var wpfColor = ((System.Windows.Media.SolidColorBrush)button.Background).Color;
                _currColor = System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);

                CurrentColorRect.Fill = button.Background;

                if (_currentTool == "Select" && _selectedShape != null)
                {
                    _selectedShape.Color = _currColor; 
                    Render(); 
                }
            }
        }


        // Выбор произвольного цвета через системное окно
        private void MoreColors_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dialog.Color;
                _currColor = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);

                // Обновляем предпросмотр
                CurrentColorRect.Fill = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B));

                if (_currentTool == "Select" && _selectedShape != null)
                {
                    _selectedShape.Color = _currColor;
                    Render();
                }
            }
        }

        private Shape? CreateShape(string type, System.Drawing.Point start)
        {
            Shape? s = type switch
            {
                "Line" => new LineShape(),
                "Rect" => new RectangleShape(),
                "Ellipse" => new EllipseShape(),
                "Polyline" => new PolylineShape(), 
                "Polygon" => new PolygonShape(),   
                _ => null
            };

            if (s != null)
            {
                s.StartPoint = start;
                s.EndPoint = start;
                s.Color = _currColor;
                s.Thickness = _currThickness;
                s.FillColor = _currFillColor;
            }
            return s;
        }

        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = (string)((FrameworkElement)sender).Tag;
            _selectedShape = null;
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

        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            // создаем новый слой
            var newLayer = new Layer(_project.Layers.Count, $"Слой {_project.Layers.Count + 1}");
            _project.Layers.Add(newLayer);
             
            _project.ActiveLayer = newLayer;

            UpdateLayersList();
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _currThickness = (int)e.NewValue;

            if (_currentTool == "Select" && _selectedShape != null)
            {
                _selectedShape.Thickness = _currThickness;
                Render();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShape != null)
            {
                _project.RemoveShape(_selectedShape);
                _selectedShape = null;
                Render();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete) Delete_Click(this, new RoutedEventArgs());
        }

        private void DeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            if (LayersList.SelectedItem is Layer selectedLayer)
            {
                if (_project.Layers.Count <= 1)
                {
                    System.Windows.MessageBox.Show("Нельзя удалить последний слой!", "Внимание",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                _project.Layers.Remove(selectedLayer);

                if (_project.ActiveLayer == selectedLayer)
                {
                    _project.ActiveLayer = _project.Layers.Count > 0 ? _project.Layers[0] : null;
                }

                UpdateLayersList();
                Render();
            }
        }

        private void SetFillColor_Click(object sender, RoutedEventArgs e)
        {
            _currFillColor = _currColor; 
            UpdateFillIndicator();
        }

        private void ClearFill_Click(object sender, RoutedEventArgs e)
        {
            _currFillColor = System.Drawing.Color.Transparent;
            UpdateFillIndicator();
        }

        private void UpdateFillIndicator()
        {
            if (_currFillColor == System.Drawing.Color.Transparent)
            {
                CurrentFillRect.Fill = System.Windows.Media.Brushes.Transparent;
                FillStatusText.Visibility = Visibility.Visible;
            }
            else
            {
                CurrentFillRect.Fill = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(_currFillColor.A, _currFillColor.R, _currFillColor.G, _currFillColor.B));
                FillStatusText.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyFillToSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShape != null)
            {
                _selectedShape.FillColor = _currFillColor;
                Render(); // Перерисовываем холст
            }
            else
            {
                System.Windows.MessageBox.Show("Сначала выберите фигуру инструментом 'Выделение' (мышка)");
            }
        }

        #endregion
    }
}

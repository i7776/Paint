using MyPaint.Models;
using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing; 
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Для событий мыши WPF
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

        public MainWindow()
        {
            InitializeComponent();
            _project = new DrawingProject();

            // Инициализация списка слоев
            UpdateLayersList();

            // Подписываемся на событие загрузки окна, чтобы сразу отрисовать чистый холст
            this.Loaded += (s, e) => Render();
        }

        #region Отрисовка
        private void Render()
        {
            // Берем размеры из Image контрола
            int width = (int)Math.Max(CanvasImage.ActualWidth, 800);
            int height = (int)Math.Max(CanvasImage.ActualHeight, 600);

            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // 1. Рисуем проект (все слои и фигуры в них)
                    _project.Draw(g);

                    if (_project.ActiveLayer != null && _project.ActiveLayer.IsVisible)
                    {
                        _tempShape?.Draw(g);
                    }
                }

                // Вывод на экран
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

        private bool _isDrawingPoly = false; // Флаг для многоточечного рисования

        // 1. Когда кликаем по списку слоев, меняем активный слой в проекте
        private void LayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LayersList.SelectedItem is Layer selectedLayer)
            {
                _project.ActiveLayer = selectedLayer;
            }
        }

        // 2. Логика MouseDown (Нажатие)
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(CanvasImage);
            var drawingPoint = new System.Drawing.Point((int)p.X, (int)p.Y);
            _lastMousePos = drawingPoint;

            if (_currentTool == "Select")
            {
                _selectedShape = _project.GetShapeAt(drawingPoint);
            }
            else if (_currentTool == "Polyline" || _currentTool == "Polygon")
            {
                // Если это первый клик для ломаной
                if (!_isDrawingPoly)
                {
                    _tempShape = CreateShape(_currentTool, drawingPoint);
                    _isDrawingPoly = true;
                }
                else // Если мы уже в процессе добавления точек
                {
                    if (_tempShape is PolylineShape poly) poly.Points.Add(drawingPoint);
                    if (_tempShape is PolygonShape pg) pg.Points.Add(drawingPoint);
                }
            }
            else // Обычные фигуры (Линия, Прямоугольник)
            {
                _tempShape = CreateShape(_currentTool, drawingPoint);
            }
            Render();
        }

        // 3. Логика MouseUp (Отпускание)
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Если это ломаная/многоугольник — НЕ завершаем рисование по MouseUp
            if (_currentTool == "Polyline" || _currentTool == "Polygon") return;

            if (_tempShape != null)
            {
                // ДОБАВЛЯЕМ ТОЛЬКО В АКТИВНЫЙ СЛОЙ
                _project.ActiveLayer?.Shapes.Add(_tempShape);
                _tempShape = null;
            }
            Render();
        }

        // 4. Завершение ломаной правой кнопкой мыши
        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingPoly && _tempShape != null)
            {
                _project.ActiveLayer?.Shapes.Add(_tempShape);
                _tempShape = null;
                _isDrawingPoly = false;
                Render();
            }
        }
        private void LayerCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Render(); // Просто перерисовываем всё. Если слой скрыт, он не отрисуется.
        }

        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var p = e.GetPosition(CanvasImage);
            var currentPoint = new System.Drawing.Point((int)p.X, (int)p.Y);

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
                    // Для обычных фигур
                    _tempShape.EndPoint = currentPoint;

                    // ДЛЯ ЛОМАНОЙ И МНОГОУГОЛЬНИКА:
                    // Проверяем, есть ли у фигуры свойство Points (через рефлексию или приведение типов)
                    if (_tempShape is PolylineShape polyline)
                    {
                        // Если точек еще нет, добавляем начальную
                        if (polyline.Points == null) polyline.Points = new List<System.Drawing.Point>();
                        if (polyline.Points.Count < 2)
                        {
                            polyline.Points.Clear();
                            polyline.Points.Add(_tempShape.StartPoint);
                            polyline.Points.Add(currentPoint);
                        }
                        else
                        {
                            // Обновляем последнюю точку во время движения
                            polyline.Points[polyline.Points.Count - 1] = currentPoint;
                        }
                    }
                    else if (_tempShape is PolygonShape polygon)
                    {
                        if (polygon.Points == null) polygon.Points = new List<System.Drawing.Point>();
                        if (polygon.Points.Count < 2)
                        {
                            polygon.Points.Clear();
                            polygon.Points.Add(_tempShape.StartPoint);
                            polygon.Points.Add(currentPoint);
                        }
                        else
                        {
                            polygon.Points[polygon.Points.Count - 1] = currentPoint;
                        }
                    }
                }
                Render();
            }
        }

        

        #endregion

        #region Вспомогательное

        private Shape? CreateShape(string type, System.Drawing.Point start)
        {
            Shape? s = type switch
            {
                "Line" => new LineShape(),
                "Rect" => new RectangleShape(),
                "Ellipse" => new EllipseShape(),
                "Polyline" => new PolylineShape(), // Если есть в папке
                "Polygon" => new PolygonShape(),   // Если есть в папке
                _ => null
            };

            if (s != null)
            {
                s.StartPoint = start;
                s.EndPoint = start;
                s.Color = Color.Black;
                s.Thickness = 2;
                s.FillColor = Color.Transparent;
            }
            return s;
        }

        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = (string)((FrameworkElement)sender).Tag;
            _selectedShape = null;
        }

        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            var newLayer = new Layer(_project.Layers.Count, $"Слой {_project.Layers.Count + 1}");
            _project.Layers.Add(newLayer);

            // По желанию: сразу делать новый слой активным
            _project.ActiveLayer = newLayer;

            UpdateLayersList();
        }


        private void UpdateLayersList()
        {
            LayersList.ItemsSource = null;
            LayersList.ItemsSource = _project.Layers;
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
        #endregion
    }
}

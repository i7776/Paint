using MyPaint.Models;
using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing; 
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; 
using System.Windows.Media.Imaging;

namespace MyPaint
{
    public partial class MainWindow : Window
    {
        private DrawingProject _project;
        private string _currTool = "Select";
        private Shape? _selectedShape;
        private Shape? _tempShape;
        private System.Drawing.Point _lastMousePos;
        private System.Drawing.Color _currColor  = System.Drawing.Color.Black;
        private System.Drawing.Color _currFillColor = System.Drawing.Color.Transparent;
        private int _currThickness = 2;
        private bool _isResizing = false;
        private int _resizeIndex = -1;
        private bool _isRotating = false;
        private System.Drawing.Point _resizeAnchorPoint;
        private List<System.Drawing.Point> _originalPoints; // копия точек до начала ресайза
        private System.Drawing.Point _originalStart; // Для прямоугольников/эллипсов
        private System.Drawing.Point _originalEnd;




        public MainWindow()
        {
            InitializeComponent();
            _project = new DrawingProject();

            UpdateLayersList();

            this.Loaded += (s, e) => Render();
        }

        #region Мышь 

        private bool _isDrawingPoly = false; // флаг для многоточечного рисования

        // когда кликаем по списку слоев, меняем активный слой в проекте
        private void LayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LayersList.SelectedItem is Layer selectedLayer)
            {
                _project.ActiveLayer = selectedLayer;
            }
        }

        //удалить
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                Delete_Click(this, new RoutedEventArgs());
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

            if (_currTool == "Select")
            {
                if (_selectedShape != null) 
                {
                    // Внутри Canvas_MouseDown, в блоке if (_selectedShape != null)
                    Rectangle bounds = _selectedShape.GetBounds();
                    int cx = bounds.X + bounds.Width / 2;
                    int cy = bounds.Y + bounds.Height / 2;
                    var center = new System.Drawing.Point(cx, cy);

                    // ВАЖНО: Вращаем точку мыши ОБРАТНО углу фигуры
                    var localMouse = RotatePoint(drawingPoint, center, -_selectedShape.Angle);

                    // Теперь проверяем попадание, используя localMouse и ОБЫЧНЫЕ bounds (без поворота)
                    int s = 8;
                    int offset = 20;

                    // Проверка маркера вращения
                    Rectangle rotRect = new Rectangle(cx - s / 2, bounds.Y - offset, s, s);
                    // Так как rotRect мы считали в мировых координатах, а localMouse в локальных, 
                    // проще проверить маркер вращения так:
                    if (new Rectangle(-s / 2, -bounds.Height / 2 - offset, s, s).Contains(localMouse.X - cx, localMouse.Y - cy))
                    {
                        _isRotating = true;
                        return;
                    }

                    // Маркеры ресайза (теперь проверяем относительно localMouse)
                    System.Drawing.Point[] handles = new System.Drawing.Point[] {
    new System.Drawing.Point(bounds.X, bounds.Y),
    new System.Drawing.Point(bounds.Right, bounds.Y),
    new System.Drawing.Point(bounds.X, bounds.Bottom),
    new System.Drawing.Point(bounds.Right, bounds.Bottom)
};

                    for (int i = 0; i < handles.Length; i++)
                    {
                        if (new Rectangle(handles[i].X - s, handles[i].Y - s, s * 2, s * 2).Contains(localMouse))
                        {
                            _isResizing = true;
                            _resizeIndex = i;

                            // Сохраняем исходные данные фигуры ПЕРЕД началом изменения
                            _originalStart = _selectedShape.StartPoint;
                            _originalEnd = _selectedShape.EndPoint;

                            // Назначаем якорь (точка, которая ДОЛЖНА стоять на месте на экране)
                            if (i == 0) _resizeAnchorPoint = new System.Drawing.Point(bounds.Right, bounds.Bottom);
                            if (i == 1) _resizeAnchorPoint = new System.Drawing.Point(bounds.X, bounds.Bottom);
                            if (i == 2) _resizeAnchorPoint = new System.Drawing.Point(bounds.Right, bounds.Y);
                            if (i == 3) _resizeAnchorPoint = new System.Drawing.Point(bounds.X, bounds.Y);

                            if (_selectedShape is PolygonShape pg) _originalPoints = pg.Points.ToList();
                            else if (_selectedShape is PolylineShape pl) _originalPoints = pl.Points.ToList();

                            return;
                        }


                    }

                }
                _selectedShape = _project.GetShapeAt(drawingPoint);

                if (_selectedShape != null)
                {
                    // обновляем текущие цвета в палитре, чтобы они соответствовали выбранной фигуре
                    _currColor = _selectedShape.Color;
                    _currFillColor = _selectedShape.FillColor;
                    _currThickness = _selectedShape.Thickness;

                    // Обновляем визуальные индикаторы в UI
                    CurrentColorRect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(_currColor.A, _currColor.R, _currColor.G, _currColor.B));
                    ThicknessSlider.Value = _currThickness;
                    UpdateFillIndicator();
                }
            }
            else if (_currTool == "Polyline" || _currTool == "Polygon")
            {
                if (!_isDrawingPoly) 
                {
                    _isDrawingPoly = true;
                    _tempShape = CreateShape(_currTool, drawingPoint);

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
                _tempShape = CreateShape(_currTool, drawingPoint);
            }
            Render();
        }


        // отпускание
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            _isResizing = false; // Сбрасываем
            _isRotating = false; // Сбрасываем
            _resizeIndex = -1;

            // если это ломаная/многоугольник — НЕ завершаем рисование по MouseUp
            if (_currTool == "Polyline" || _currTool == "Polygon") return;

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
                {
                    poly.Points.RemoveAt(poly.Points.Count - 1);
                }   
                else if (_tempShape is PolygonShape pg && pg.Points.Count > 1)
                {
                    pg.Points.RemoveAt(pg.Points.Count - 1);
                }

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
                if (_currTool == "Select" && _selectedShape != null)
                {
                    // В MainWindow.Logic.cs (или где у вас MouseMove)

                    if (_isResizing)
                    {
                        // 1. Получаем текущие границы и центр ДО изменения
                        Rectangle oldBounds = _selectedShape.GetBounds();
                        System.Drawing.Point oldCenter = new System.Drawing.Point(
                            oldBounds.X + oldBounds.Width / 2,
                            oldBounds.Y + oldBounds.Height / 2
                        );

                        // 2. Находим, где ЯКОРЬ находится на экране СЕЙЧАС (с учетом текущего поворота)
                        System.Drawing.Point worldAnchorBefore = RotatePoint(_resizeAnchorPoint, oldCenter, _selectedShape.Angle);

                        // 3. Переводим мышь из экранных координат в локальные (отменяем поворот)
                        var localMouse = RotatePoint(currentPoint, oldCenter, -_selectedShape.Angle);

                        // 4. ПРИМЕНЯЕМ РЕЗАЙЗ
                        if (_selectedShape is PolygonShape poly)
                        {
                            poly.ResizeByMouse(_resizeAnchorPoint, localMouse, _originalPoints);
                        }
                        else if (_selectedShape is PolylineShape line)
                        {
                            line.ResizeByMouse(_resizeAnchorPoint, localMouse, _originalPoints);
                        }
                        else
                        {
                            // Для Rectangle/Ellipse: якорь - это один угол, мышь - другой
                            _selectedShape.StartPoint = _resizeAnchorPoint;
                            _selectedShape.EndPoint = localMouse;
                        }

                        // 5. МАГИЯ КОМПЕНСАЦИИ:
                        // После ресайза центр фигуры изменился. Нам нужно найти новый центр.
                        Rectangle newBounds = _selectedShape.GetBounds();
                        System.Drawing.Point newCenter = new System.Drawing.Point(
                            newBounds.X + newBounds.Width / 2,
                            newBounds.Y + newBounds.Height / 2
                        );

                        // Где наш неподвижный якорь оказался БЫ на экране при новом центре?
                        System.Drawing.Point worldAnchorAfter = RotatePoint(_resizeAnchorPoint, newCenter, _selectedShape.Angle);

                        // Насколько якорь "уплыл" из-за смены центра?
                        int dx = worldAnchorBefore.X - worldAnchorAfter.X;
                        int dy = worldAnchorBefore.Y - worldAnchorAfter.Y;

                        // Двигаем фигуру на это расстояние, чтобы вернуть якорь в исходную точку на экране
                        _selectedShape.Move(dx, dy);

                        Render();
                    }



                    else if (_isRotating) // Добавляем этот блок
                    {
                        var bounds = _selectedShape.GetBounds();
                        // Центр фигуры
                        var center = new System.Drawing.Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

                        // Вычисляем угол в радианах между центром и мышкой
                        double radians = Math.Atan2(currentPoint.Y - center.Y, currentPoint.X - center.X);
                        // Переводим в градусы
                        double degrees = radians * (180.0 / Math.PI);

                        // Так как маркер ("антенна") находится сверху, а Atan2 считает от оси X (вправо),
                        // нам нужно сместить результат на 90 градусов, чтобы 0 был наверху.
                        _selectedShape.Angle = (float)degrees + 90;

                        Render();
                    }
                    else
                    {
                        // Обычное перемещение
                        int dx = currentPoint.X - _lastMousePos.X;
                        int dy = currentPoint.Y - _lastMousePos.Y;
                        _selectedShape.Move(dx, dy);
                        _lastMousePos = currentPoint;
                    }
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

                if (_currTool == "Select" && _selectedShape != null)
                {
                    _selectedShape.Color = _currColor; 
                    Render(); 
                }
            }
        }


        // выбор произвольного цвета через системное окно
        private void MoreColors_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dialog.Color;
                _currColor = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);

                // обновляем предпросмотр
                CurrentColorRect.Fill = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B));

                if (_currTool == "Select" && _selectedShape != null)
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

        // переключение элементов
        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            _currTool = (string)((FrameworkElement)sender).Tag;
            _selectedShape = null;
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

            if (_currTool == "Select" && _selectedShape != null)
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
                Render(); 
            }
            else
            {
                System.Windows.MessageBox.Show("Сначала выберите фигуру инструментом 'Выделение' (мышка)");
            }
        }

        #endregion
    }
}

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
        private List<System.Drawing.Point> _originalPoints; // копия точек до ресайза
        private System.Drawing.Point _originalStart;
        private System.Drawing.Point _originalEnd;
        bool _isEditingFillColor = false;
        private System.Drawing.Point _worldAnchorPoint;


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


        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_selectedShape != null)
            {
                int dx = 0;
                int dy = 0;
                int step = 1;
                bool isArrowPressed = false;

                switch (e.Key)
                {
                    case Key.Up:
                        dy = -step;
                        isArrowPressed = true;
                        break;
                    case Key.Down:
                        dy = step;
                        isArrowPressed = true;
                        break;
                    case Key.Left:
                        dx = -step;
                        isArrowPressed = true;
                        break;
                    case Key.Right:
                        dx = step;
                        isArrowPressed = true;
                        break;
                    case Key.Delete:
                    case Key.Back:
                        Delete_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                }

                if (isArrowPressed)
                {
                    _selectedShape.Move(dx, dy);
                    Render();
                    e.Handled = true;
                }
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
                    Rectangle bounds = _selectedShape.GetBounds();
                    int cx = bounds.X + bounds.Width / 2;
                    int cy = bounds.Y + bounds.Height / 2;
                    var center = new System.Drawing.Point(cx, cy);

                    // вращаем точку мыши обратно углу фигуры!!
                    var localMouse = RotatePoint(drawingPoint, center, -_selectedShape.Angle);

                    // проверяем попадание через localMouse и bounds 
                    int s = 8;
                    int offset = 20;

                    // проверка маркера вращения
                    Rectangle rotRect = new Rectangle(cx - s / 2, bounds.Y - offset, s, s);
                    //  rotRect в мировых координатах localMouse в локальных 
                    if (new Rectangle(-s / 2, -bounds.Height / 2 - offset, s, s).Contains(localMouse.X - cx, localMouse.Y - cy))
                    {
                        _isRotating = true;
                        return;
                    }

                    // маркеры ресайза
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

                            // берем текущие границы
                            bounds = _selectedShape.GetBounds();
                            int x1 = bounds.X;
                            int y1 = bounds.Y;
                            int x2 = bounds.Right;
                            int y2 = bounds.Bottom;

                            // локальный якорь 
                            if (_resizeIndex == 0) _resizeAnchorPoint = new System.Drawing.Point(x2, y2); 
                            if (_resizeIndex == 1) _resizeAnchorPoint = new System.Drawing.Point(x1, y2); 
                            if (_resizeIndex == 2) _resizeAnchorPoint = new System.Drawing.Point(x2, y1); 
                            if (_resizeIndex == 3) _resizeAnchorPoint = new System.Drawing.Point(x1, y1); 

                            // занимаем позицию мирового якоря
                            var currentCenter = new System.Drawing.Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                            _worldAnchorPoint = RotatePoint(_resizeAnchorPoint, currentCenter, _selectedShape.Angle);

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

                    // обновляем визуальные индикаторы
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

            _isResizing = false; 
            _isRotating = false; 
            _resizeIndex = -1;

            // если это ломаная/многоугольник — не завершаем рисование 
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
                    if (_isResizing)
                    {
                        // текущий центр
                        Rectangle oldBounds = _selectedShape.GetBounds();
                        System.Drawing.Point oldCenter = new System.Drawing.Point(oldBounds.X + oldBounds.Width / 2, oldBounds.Y + oldBounds.Height / 2);

                        //  мышь в локальные координаты относ. старого центра
                        var localMouse = RotatePoint(currentPoint, oldCenter, -_selectedShape.Angle);

                        // меняем размеры
                        if (_selectedShape is PolygonShape poly)
                            poly.ResizeByMouse(_resizeAnchorPoint, localMouse, _originalPoints);
                        else if (_selectedShape is PolylineShape line)
                            line.ResizeByMouse(_resizeAnchorPoint, localMouse, _originalPoints);
                        else
                        {
                            _selectedShape.StartPoint = _resizeAnchorPoint;
                            _selectedShape.EndPoint = localMouse;
                        }

                        // после изменения координат центр фигуры сместился, локальный якорь в мировых координатах
                        Rectangle newBounds = _selectedShape.GetBounds();
                        System.Drawing.Point newCenter = new System.Drawing.Point(newBounds.X + newBounds.Width / 2, newBounds.Y + newBounds.Height / 2);
                        System.Drawing.Point currentAnchorPos = RotatePoint(_resizeAnchorPoint, newCenter, _selectedShape.Angle);

                        int dx = _worldAnchorPoint.X - currentAnchorPos.X;
                        int dy = _worldAnchorPoint.Y - currentAnchorPos.Y;

                        // двигаем всю фигуру: вернуть якорь на место
                        _selectedShape.Move(dx, dy);

                        Render();
                    }


                    else if (_isRotating) 
                    {
                        var bounds = _selectedShape.GetBounds();
                        var center = new System.Drawing.Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

                        // угол в радианах между центром и мышкой
                        double radians = Math.Atan2(currentPoint.Y - center.Y, currentPoint.X - center.X);
                        double degrees = radians * (180.0 / Math.PI);

                        // нужно сместить результат на 90 градусов, чтобы 0 был наверху.
                        _selectedShape.Angle = (float)degrees + 90;

                        Render();
                    }
                    else
                    {
                        // перемещение
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

        private void CurrentColorRect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isEditingFillColor = false;
            StrokeColorBorder.BorderBrush = System.Windows.Media.Brushes.Yellow;
            FillColorBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
        }

        private void CurrentFillRect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isEditingFillColor = true;
            FillColorBorder.BorderBrush = System.Windows.Media.Brushes.Yellow;
            StrokeColorBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
        }

        #endregion

        #region Вспомогательное

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button == null) return;

            var wpfColor = ((System.Windows.Media.SolidColorBrush)button.Background).Color;
            System.Drawing.Color newColor = System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);

            if (_isEditingFillColor)
            {
                // меняем только заливку
                _currFillColor = newColor;
                if (_selectedShape != null)
                {
                    _selectedShape.FillColor = newColor;
                }
            }
            else
            {
                // меняем только контур
                _currColor = newColor;
                if (_selectedShape != null)
                {
                    _selectedShape.Color = newColor;
                }
            }

            UpdateFillIndicator(); 
            Render();
            
            this.Focus();
        }



        // выбор произвольного цвета через системное окно
        private void MoreColors_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dialog.Color;
                System.Drawing.Color newColor = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);

                if (_isEditingFillColor)
                {
                    _currFillColor = newColor;
                    if (_selectedShape != null) _selectedShape.FillColor = newColor;
                }
                else
                {
                    _currColor = newColor;
                    if (_selectedShape != null) _selectedShape.Color = newColor;

                    // обновляем квадратик основного цвета
                    CurrentColorRect.Fill = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B));
                }

                UpdateFillIndicator();
                Render();
                this.Focus();
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
      
        private void MoveLayerUp_Click(object sender, RoutedEventArgs e)
        {
            if (LayersList.SelectedItem is Layer selectedLayer)
            {
                _project.MoveLayerUp(selectedLayer); // реализуй в DrawingProject
                UpdateLayersList();
                LayersList.SelectedItem = selectedLayer;
                Render();
            }
        }

        private void MoveLayerDown_Click(object sender, RoutedEventArgs e)
        {
            if (LayersList.SelectedItem is Layer selectedLayer)
            {
                _project.MoveLayerDown(selectedLayer); // реализуй в DrawingProject
                UpdateLayersList();
                LayersList.SelectedItem = selectedLayer;
                Render();
            }
        }

        #endregion
    }
}

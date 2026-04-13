using MyPaint.Commands;
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
        private List<Shape> _selectedShapes = new List<Shape>();
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
        private bool _isSelectingBox = false;
        private System.Drawing.Point _selectionStart;
        private System.Drawing.Rectangle _selectionRect;
        private UndoRedoManager _undoRedoManager = new UndoRedoManager();
        private List<Shape> _shapesBeforeTransform = new List<Shape>();
        private List<Shape> _clipboard = new List<Shape>(); // CtrlC / CtrlV


        private Shape? PrimarySelected => _selectedShapes.Count > 0 ? _selectedShapes[_selectedShapes.Count - 1] : null;

        public MainWindow()
        {
            _project = new DrawingProject();
            _selectedShapes = new List<Shape>();

            InitializeComponent();
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
            //сtrlZ
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Z)
            {
                _undoRedoManager.Undo();
                UpdateLayersList();
                Render();
            }
            //сtrlY
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Y)
            {
                _undoRedoManager.Redo();
                UpdateLayersList();
                Render();
            }
            //сtrlC
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.C)
            {
                _clipboard = _selectedShapes.Select(s => s.Clone()).ToList();
            }
            //сtrlV
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.V)
            {
                if (_clipboard.Count > 0)
                {
                    _selectedShapes.Clear();
                    foreach (var s in _clipboard)
                    {
                        var copy = s.Clone();
                        copy.Move(10, 10);
                        _undoRedoManager.Execute(new AddShapeCommand(_project.ActiveLayer, copy));
                        _selectedShapes.Add(copy);
                    }
                    Render();
                }
            }

            if (_selectedShapes.Count > 0)
            {
                int dx = 0;
                int dy = 0;
                int step = Keyboard.IsKeyDown(Key.LeftShift) ? 10 : 1;
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
                    foreach (var shape in _selectedShapes)
                    {
                        shape.Move(dx, dy);
                    }
                    Render();
                    e.Handled = true;
                }
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_project.ActiveLayer == null)
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

            // для обращения к последней выбранной фигуре
            var primarySelected = _selectedShapes.Count > 0 ? _selectedShapes[_selectedShapes.Count - 1] : null;

            if (_currTool == "Select")
            {
                if (primarySelected != null)
                {
                    Rectangle bounds = primarySelected.GetBounds();
                    int cx = bounds.X + bounds.Width / 2;
                    int cy = bounds.Y + bounds.Height / 2;
                    var center = new System.Drawing.Point(cx, cy);

                    // вращаем точку мыши обратно углу фигуры!!
                    var localMouse = RotatePoint(drawingPoint, center, -primarySelected.Angle);

                    // проверяем попадание через localMouse и bounds 
                    int s = 8;
                    int offset = 20;

                    // проверка маркера вращения
                    // rotRect в мировых координатах localMouse в локальных 
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
                            bounds = primarySelected.GetBounds();
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
                            _worldAnchorPoint = RotatePoint(_resizeAnchorPoint, currentCenter, primarySelected.Angle);

                            if (primarySelected is PolygonShape pg) _originalPoints = pg.Points.ToList();
                            else if (primarySelected is PolylineShape pl) _originalPoints = pl.Points.ToList();

                            return;
                        }
                    }
                }

                // если по маркерам не попали, ищем саму фигуру под мышкой
                var hitShape = _project.GetShapeAt(drawingPoint);
                bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

                if (hitShape != null)
                {
                    if (isCtrl)
                    {
                        // если зажат Ctrl — инвертируем выбор конкретной фигуры
                        if (_selectedShapes.Contains(hitShape)) _selectedShapes.Remove(hitShape);
                        else _selectedShapes.Add(hitShape);
                    }
                    else
                    {
                        // если кликнули по фигуре без Ctrl
                        // если она уже была в группе — не сбрасываем (чтобы можно было тащить группу)
                        // если она новая — выбираем только её
                        if (!_selectedShapes.Contains(hitShape))
                        {
                            _selectedShapes.Clear();
                            _selectedShapes.Add(hitShape);
                        }
                    }

                    // используем новую выбранную фигуру для обновления интерфейса
                    var current = _selectedShapes.Count > 0 ? _selectedShapes[_selectedShapes.Count - 1] : null;
                    if (current != null)
                    {
                        // обновляем текущие цвета в палитре, чтобы они соответствовали выбранной фигуре
                        _currColor = current.Color;
                        _currFillColor = current.FillColor;
                        _currThickness = current.Thickness;

                        // обновляем визуальные индикаторы
                        CurrentColorRect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(_currColor.A, _currColor.R, _currColor.G, _currColor.B));
                        ThicknessSlider.Value = _currThickness;
                        UpdateFillIndicator();
                    }
                }
                else
                {
                    // кликнули в пустоту — начинаем рисовать рамку выделения
                    if (!isCtrl)
                    {
                        _selectedShapes.Clear();
                    }

                    _isSelectingBox = true;
                    _selectionStart = drawingPoint;
                    _selectionRect = new System.Drawing.Rectangle(drawingPoint.X, drawingPoint.Y, 0, 0);
                }

                _shapesBeforeTransform = _selectedShapes.Select(s => s.Clone()).ToList();
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
            if (e.ChangedButton == MouseButton.Left)
            {
                if (_isSelectingBox)
                {
                    foreach (var layer in _project.Layers)
                    {
                        if (!layer.IsVisible || layer.IsLocked) continue;
                        foreach (var shape in layer.Shapes)
                        {
                            // Если границы фигуры пересекаются с рамкой выбора
                            if (_selectionRect.IntersectsWith(shape.GetBounds()))
                            {
                                if (!_selectedShapes.Contains(shape)) _selectedShapes.Add(shape);
                            }
                        }
                    }
                    _isSelectingBox = false;
                    _selectionRect = new System.Drawing.Rectangle(0, 0, 0, 0);
                }

                if (_isResizing || _isRotating || (_selectedShapes.Count > 0 && _currTool == "Select"))
                {
                    var news = _selectedShapes.Select(s => s.Clone()).ToList();
                    // сравнение состояний
                    if (_shapesBeforeTransform.Count > 0 && _shapesBeforeTransform.Count == news.Count)
                    {
                        _undoRedoManager.Execute(new TransformCommand(_selectedShapes, _shapesBeforeTransform, news));
                    }
                }

                if (_tempShape != null && !(_currTool == "Polyline" || _currTool == "Polygon"))
                {
                    _undoRedoManager.Execute(new AddShapeCommand(_project.ActiveLayer, _tempShape));
                    _tempShape = null;
                }

                _isResizing = false;
                _isRotating = false;
                _resizeIndex = -1;
                Render();
                
            }
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
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_currTool == "Select")
                {
                    //рамка
                    if (_isSelectingBox)
                    {
                        int x = Math.Min(_selectionStart.X, currentPoint.X);
                        int y = Math.Min(_selectionStart.Y, currentPoint.Y);
                        int w = Math.Abs(_selectionStart.X - currentPoint.X);
                        int h = Math.Abs(_selectionStart.Y - currentPoint.Y);
                        _selectionRect = new System.Drawing.Rectangle(x, y, w, h);
                    }
                   
                    else if (_isResizing && PrimarySelected != null)
                    {
                        Rectangle oldBounds = PrimarySelected.GetBounds();
                        System.Drawing.Point oldCenter = new System.Drawing.Point(oldBounds.X + oldBounds.Width / 2, oldBounds.Y + oldBounds.Height / 2);
                        var localMouse = RotatePoint(currentPoint, oldCenter, -PrimarySelected.Angle);

                        if (PrimarySelected is PolygonShape poly)
                            poly.ResizeByMouse(_resizeAnchorPoint, localMouse, _originalPoints);
                        else if (PrimarySelected is PolylineShape line)
                            line.ResizeByMouse(_resizeAnchorPoint, localMouse, _originalPoints);
                        else
                        {
                            PrimarySelected.StartPoint = _resizeAnchorPoint;
                            PrimarySelected.EndPoint = localMouse;
                        }

                        Rectangle newBounds = PrimarySelected.GetBounds();
                        System.Drawing.Point newCenter = new System.Drawing.Point(newBounds.X + newBounds.Width / 2, newBounds.Y + newBounds.Height / 2);
                        System.Drawing.Point currentAnchorPos = RotatePoint(_resizeAnchorPoint, newCenter, PrimarySelected.Angle);

                        int dx = _worldAnchorPoint.X - currentAnchorPos.X;
                        int dy = _worldAnchorPoint.Y - currentAnchorPos.Y;
                        PrimarySelected.Move(dx, dy);
                    }
                    else if (_isRotating && PrimarySelected != null)
                    {
                        var bounds = PrimarySelected.GetBounds();
                        var center = new System.Drawing.Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                        double radians = Math.Atan2(currentPoint.Y - center.Y, currentPoint.X - center.X);
                        double degrees = radians * (180.0 / Math.PI);
                        PrimarySelected.Angle = (float)degrees + 90;
                    }
                    //перемещение
                    else if (_selectedShapes.Count > 0)
                    {
                        int dx = currentPoint.X - _lastMousePos.X;
                        int dy = currentPoint.Y - _lastMousePos.Y;
                        foreach (var s in _selectedShapes)
                        {
                            s.Move(dx, dy);
                        }
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

            if (_selectedShapes.Count > 0)
            {
                var olds = _selectedShapes.Select(s => s.Clone()).ToList();

                foreach (var shape in _selectedShapes)
                {
                    if (_isEditingFillColor) shape.FillColor = newColor;
                    else shape.Color = newColor;
                }

                var news = _selectedShapes.Select(s => s.Clone()).ToList();
                _undoRedoManager.Execute(new PropertyChangeCommand(_selectedShapes, olds, news));
            }
            else
            {
                // Если ничего не выбрано — просто меняем текущий цвет кисти (это не в Undo)
                if (_isEditingFillColor) _currFillColor = newColor;
                else
                {
                    _currColor = newColor;
                    CurrentColorRect.Fill = new System.Windows.Media.SolidColorBrush(wpfColor);
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

                if (_selectedShapes.Count > 0)
                {
                    var olds = _selectedShapes.Select(s => s.Clone()).ToList();

                    foreach (var shape in _selectedShapes)
                    {
                        if (_isEditingFillColor) shape.FillColor = newColor;
                        else shape.Color = newColor;
                    }

                    var news = _selectedShapes.Select(s => s.Clone()).ToList();
                    _undoRedoManager.Execute(new PropertyChangeCommand(_selectedShapes, olds, news));
                }
                else
                {
                    if (_isEditingFillColor) _currFillColor = newColor;
                    else _currColor = newColor;
                }

                if (!_isEditingFillColor)
                {
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
            _selectedShapes.Clear();
        }

        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            // создаем новый слой
            var newLayer = new Layer(_project.Layers.Count, $"Слой {_project.Layers.Count + 1}");
            _undoRedoManager.Execute(new AddLayerCommand(_project, newLayer, () => {
                UpdateLayersList();
                Render();
            }));
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _currThickness = (int)e.NewValue;

            if (_selectedShapes.Count > 0)
            {
                var olds = _selectedShapes.Select(s => s.Clone()).ToList();
                foreach (var shape in _selectedShapes)
                {
                    shape.Thickness = _currThickness;
                }
                var news = _selectedShapes.Select(s => s.Clone()).ToList();

                _undoRedoManager.Execute(new PropertyChangeCommand(_selectedShapes, olds, news));
            }
            Render();
        }


        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            //удаляем каждую выбранную фигуру из проекта
            if (_selectedShapes.Count > 0)
            {
                // создаем команду, передавая копию списка выбранных фигур
                var command = new RemoveShapesCommand(_project, new List<Shape>(_selectedShapes));

                _undoRedoManager.Execute(command);
                _selectedShapes.Clear();
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

                _undoRedoManager.Execute(new RemoveLayerCommand(_project, selectedLayer, () => {
                    UpdateLayersList();
                    Render();
                }));
            }
        }

        private void ClearFill_Click(object sender, RoutedEventArgs e)
        {
            _currFillColor = System.Drawing.Color.Transparent;
            if (_selectedShapes.Count > 0)
            {
                var olds = _selectedShapes.Select(s => s.Clone()).ToList();

                foreach (var shape in _selectedShapes)
                {
                    shape.FillColor = System.Drawing.Color.Transparent;
                }

                var news = _selectedShapes.Select(s => s.Clone()).ToList();

                _undoRedoManager.Execute(new PropertyChangeCommand(_selectedShapes, olds, news));
            }
            UpdateFillIndicator();
            Render();
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
                int idx = _project.Layers.IndexOf(selectedLayer);
                if (idx > 0)
                {
                    _undoRedoManager.Execute(new ReorderLayerCommand(_project, selectedLayer, -1, () => {
                        UpdateLayersList();
                        LayersList.SelectedItem = selectedLayer;
                        Render();
                    }));
                }
            }
        }

        private void MoveLayerDown_Click(object sender, RoutedEventArgs e)
        {
            if (LayersList.SelectedItem is Layer selectedLayer)
            {
                int idx = _project.Layers.IndexOf(selectedLayer);
                if (idx < _project.Layers.Count - 1)
                {
                    _undoRedoManager.Execute(new ReorderLayerCommand(_project, selectedLayer, 1, () => {
                        UpdateLayersList();
                        LayersList.SelectedItem = selectedLayer;
                        Render();
                    }));
                }
            }
        }

        private void MoveToActiveLayer_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShapes.Count == 0 || _project.ActiveLayer == null)
            {
                System.Windows.MessageBox.Show("Сначала выделите фигуры и выберите слой в списке!");
                return;
            }

            var targetLayer = _project.ActiveLayer;
            var command = new MoveShapesToLayerCommand(_project, new List<Shape>(_selectedShapes), targetLayer);

            _undoRedoManager.Execute(command);
            _selectedShapes.Clear();
            Render();
            System.Windows.MessageBox.Show($"Фигуры перенесены на слой '{targetLayer.Name}'. Можно отменить через Ctrl+Z.");
        }



        #endregion
    }
}

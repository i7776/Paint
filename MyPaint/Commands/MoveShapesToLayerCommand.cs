using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Text;
using MyPaint.Models;

namespace MyPaint.Commands
{
    public class MoveShapesToLayerCommand : ICommand
    {
        private readonly List<Shape> _shapes;
        private readonly Layer _targetLayer;
        private readonly List<(Shape shape, Layer sourceLayer)> _moveMap;

        public MoveShapesToLayerCommand(DrawingProject project, List<Shape> shapes, Layer targetLayer)
        {
            _shapes = shapes;
            _targetLayer = targetLayer;
            _moveMap = new List<(Shape, Layer)>();

            //ищем текущий слой фигуры
            foreach (var shape in _shapes)
            {
                var sourceLayer = project.Layers.Find(l => l.Shapes.Contains(shape));
                if (sourceLayer != null && sourceLayer != targetLayer)
                {
                    _moveMap.Add((shape, sourceLayer));
                }
            }
        }

        public void Execute()
        {
            foreach (var move in _moveMap)
            {
                move.sourceLayer.Shapes.Remove(move.shape);
                _targetLayer.Shapes.Add(move.shape);
            }
        }

        public void Undo()
        {
            foreach (var move in _moveMap)
            {
                // удаляем из целевого возвращаем в исходный
                _targetLayer.Shapes.Remove(move.shape);
                move.sourceLayer.Shapes.Add(move.shape);
            }
        }
    }
}

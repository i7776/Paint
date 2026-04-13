using MyPaint.Models;
using System;
using System.Collections.Generic;
using System.Text;
using MyPaint.Models.Shapes;

namespace MyPaint.Commands
{
    public class AddShapeCommand : ICommand
    {
        private readonly Layer _layer;
        private readonly Shape _shape;

        public AddShapeCommand(Layer layer, Shape shape)
        {
            _layer = layer;
            _shape = shape;
        }

        public void Execute() => _layer.Shapes.Add(_shape);
        public void Undo() => _layer.Shapes.Remove(_shape);
    }

}

using MyPaint.Models;
using System;
using System.Collections.Generic;
using System.Text;
using MyPaint.Models.Shapes;


namespace MyPaint.Commands
{
    public class RemoveShapesCommand : ICommand
    {
        private readonly DrawingProject _project;
        private readonly List<(Layer layer, Shape shape)> _removedShapes;

        public RemoveShapesCommand(DrawingProject project, List<Shape> shapes)
        {
            _project = project;
            _removedShapes = new List<(Layer, Shape)>();

            foreach (var s in shapes)
            {
                foreach (var l in project.Layers)
                {
                    if (l.Shapes.Contains(s))
                    {
                        _removedShapes.Add((l, s));
                        break;
                    }
                }
            }
        }

        public void Execute()
        {
            foreach (var item in _removedShapes) 
                item.layer.Shapes.Remove(item.shape);
        }

        public void Undo()
        {
            foreach (var item in _removedShapes) 
                item.layer.Shapes.Add(item.shape);
        }
    }
}

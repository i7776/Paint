using MyPaint.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyPaint.Commands
{
    public class RemoveLayerCommand : ICommand
    {
        private DrawingProject _project;
        private Layer _layer;
        private int _index;
        private Action _updateUI;

        public RemoveLayerCommand(DrawingProject project, Layer layer, Action updateUI)
        {
            _project = project;
            _layer = layer;
            _index = project.Layers.IndexOf(layer);
            _updateUI = updateUI;
        }

        public void Execute() { _project.Layers.Remove(_layer); _updateUI(); }
        public void Undo() { _project.Layers.Insert(_index, _layer); _updateUI(); }
    }
}

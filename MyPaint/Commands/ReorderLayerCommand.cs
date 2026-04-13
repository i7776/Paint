using MyPaint.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyPaint.Commands
{
    public class ReorderLayerCommand : ICommand
    {
        private DrawingProject _project;
        private Layer _layer;
        private int _oldIndex;
        private int _newIndex;
        private Action _updateUI;

        public ReorderLayerCommand(DrawingProject project, Layer layer, int direction, Action updateUI)
        {
            _project = project;
            _layer = layer;
            _oldIndex = project.Layers.IndexOf(layer);
            _newIndex = _oldIndex + direction;
            _updateUI = updateUI;
        }

        public void Execute() 
        { 
            Move(_oldIndex, _newIndex); 
        }
        public void Undo() 
        { 
            Move(_newIndex, _oldIndex); 
        }

        private void Move(int from, int to)
        {
            if (to < 0 || to >= _project.Layers.Count) return;
            _project.Layers.RemoveAt(from);
            _project.Layers.Insert(to, _layer);
            _updateUI();
        }
    }
}
    
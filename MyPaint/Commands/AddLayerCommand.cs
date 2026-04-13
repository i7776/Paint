using MyPaint.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyPaint.Commands
{
    public class AddLayerCommand : ICommand
    {
        private DrawingProject _project;
        private Layer _layer;
        private Action _updateUI;

        public AddLayerCommand(DrawingProject project, Layer layer, Action updateUI)
        {
            _project = project;
            _layer = layer;
            _updateUI = updateUI;
        }

        public void Execute() 
        { 
            _project.Layers.Add(_layer); 
            _project.ActiveLayer = _layer; 
            _updateUI(); 
        }
        public void Undo() 
        { 
            _project.Layers.Remove(_layer); 
            _updateUI(); 
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using MyPaint.Models.Shapes;

namespace MyPaint.Commands
{
    public class PropertyChangeCommand : ICommand
    {
        private readonly List<Shape> _targetShapes;
        private readonly List<Shape> _oldStates;
        private readonly List<Shape> _newStates;

        public PropertyChangeCommand(List<Shape> targets, List<Shape> olds, List<Shape> news)
        {
            _targetShapes = targets;
            _oldStates = olds;
            _newStates = news;
        }

        public void Execute() => ApplyState(_newStates);
        public void Undo() => ApplyState(_oldStates);

        private void ApplyState(List<Shape> states)
        {
            if (states.Count != _targetShapes.Count) return;

            for (int i = 0; i < _targetShapes.Count; i++)
            {
                _targetShapes[i].Color = states[i].Color;
                _targetShapes[i].FillColor = states[i].FillColor;
                _targetShapes[i].Thickness = states[i].Thickness;
            }
        }
    }
}

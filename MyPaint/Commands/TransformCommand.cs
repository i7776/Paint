using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyPaint.Commands
{
    public class TransformCommand : ICommand
    {
        private readonly List<Shape> _targetShapes;
        private readonly List<Shape> _oldStates;
        private readonly List<Shape> _newStates;

        public TransformCommand(List<Shape> targets, List<Shape> olds, List<Shape> news)
        {
            _targetShapes = targets; //ссылки
            _oldStates = olds;       //клоны до
            _newStates = news;       //клоны после
        }

        public void Execute() => ApplyState(_newStates);
        public void Undo() => ApplyState(_oldStates);

        private void ApplyState(List<Shape> states)
        {
            if (states.Count != _targetShapes.Count) return;

            for (int i = 0; i < _targetShapes.Count; i++)
            {
                var s = _targetShapes[i];
                var state = states[i];

                s.StartPoint = state.StartPoint;
                s.EndPoint = state.EndPoint;
                s.Angle = state.Angle;

                if (s is PolygonShape ps && state is PolygonShape pState)
                    ps.Points = new List<System.Drawing.Point>(pState.Points);
                if (s is PolylineShape pl && state is PolylineShape plState)
                    pl.Points = new List<System.Drawing.Point>(plState.Points);
            }
        }

    }
}

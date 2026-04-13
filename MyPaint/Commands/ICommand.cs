using System;
using System.Collections.Generic;
using System.Text;

namespace MyPaint.Commands
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }
}

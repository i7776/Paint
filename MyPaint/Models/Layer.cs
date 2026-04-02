using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing; 

namespace MyPaint.Models
{
    public class Layer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsVisible { get; set; }
        public bool IsLocked { get; set; }
        public List<Shape> Shapes { get; set; }

        public Layer(int id, string name)
        {
            Id = id;
            Name = name;
            IsVisible = true;
            IsLocked = false;
            Shapes = new List<Shape>();
        }

        public void Draw(Graphics g)
        {
            if (!IsVisible) return; 

            foreach (var shape in Shapes)
            {
                shape.Draw(g);
            }
        }
    }
}

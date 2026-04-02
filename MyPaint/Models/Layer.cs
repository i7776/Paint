using MyPaint.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyPaint.Models
{
    public class Layer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsVisible { get; set; }
        public bool IsLocked { get; set; }
        public List<Shape> Shapes { get; set; }

        public Layer() 
        {
            Id = Id;
            Name = Name;
            IsVisible = true;
            IsLocked = false;
            Shapes = new List<Shape>();
        }

        
    }
}

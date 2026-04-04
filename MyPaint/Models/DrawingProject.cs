using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MyPaint.Models.Shapes;

namespace MyPaint.Models
{
    public class DrawingProject
    {
        public List<Layer> Layers { get; set; }
        public Layer ActiveLayer { get; set; }

        public DrawingProject()
        {
            Layers = new List<Layer>();

            var firstLayer = new Layer(0, "Слой 1");
            Layers.Add(firstLayer);

            ActiveLayer = firstLayer;
        }
        
        public void Draw(Graphics g)
        {
            foreach (var layer in Layers)
            {
                if (layer.IsVisible)
                {
                    layer.Draw(g);
                }
            }
        }

        public Shape GetShapeAt(Point p)
        {
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];

                if (!layer.IsVisible || layer.IsLocked)
                {
                    continue;
                }

                for (int j = layer.Shapes.Count - 1; j >= 0; j--)
                {
                    if (layer.Shapes[j].ContainPoint(p))
                    {
                        return layer.Shapes[j];
                    }
                }
            }
            return null;
        }

        public void RemoveShape(Shape shape)
        {
            if (shape == null) return;

            foreach (var layer in Layers)
            {
                if (layer.IsLocked || !layer.IsVisible)
                {
                    continue;
                }

                if (layer.Shapes.Contains(shape))
                {
                    layer.Shapes.Remove(shape);
                    break;
                }
            }
        }
    }
}

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
            for (int i = Layers.Count - 1; i >= 0; i--) //от верхнего слоя к нижнему
            {
                if (!Layers[i].IsVisible) continue;
                for (int j = Layers[i].Shapes.Count - 1; j >= 0; j--) //от верхней фигуры к нижней
                {
                    if (Layers[i].Shapes[j].ContainPoint(p))
                        return Layers[i].Shapes[j];
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

        public void MoveLayerUp(Layer layer)
        {
            int index = Layers.IndexOf(layer);
            if (index > 0) 
            {
                Layers.RemoveAt(index);
                Layers.Insert(index - 1, layer);
            }
        }

        public void MoveLayerDown(Layer layer)
        {
            int index = Layers.IndexOf(layer);
            if (index < Layers.Count - 1)
            {
                Layers.RemoveAt(index);
                Layers.Insert(index + 1, layer);
            }
        }

    }
}

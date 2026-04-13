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
        }

        public static DrawingProject CreateNew()
        {
            var project = new DrawingProject();
            var firstLayer = new Layer(0, "Слой 1");
            project.Layers.Add(firstLayer);
            project.ActiveLayer = firstLayer;
            return project;
        }

        public void Draw(Graphics g)
        {
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                if (Layers[i].IsVisible)
                {
                    Layers[i].Draw(g);
                }
            }
        }


        public Shape GetShapeAt(Point p)
        {
            for (int i = 0; i < Layers.Count; i++)
            {
                if (!Layers[i].IsVisible || Layers[i].IsLocked) continue;

                for (int j = Layers[i].Shapes.Count - 1; j >= 0; j--)
                {
                    if (Layers[i].Shapes[j].ContainPoint(p))
                        return Layers[i].Shapes[j];
                }
            }
            return null;
        }
    }
}

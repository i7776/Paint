using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MyPaint.Models.Shapes;

namespace MyPaint.Models
{
    public class DrawingProject
    {
        public List<Layer> Layers { get; set; }

        // Это свойство теперь можно и читать, и записывать (есть set)
        public Layer ActiveLayer { get; set; }

        public DrawingProject()
        {
            Layers = new List<Layer>();

            // Создаем первый слой
            var defaultLayer = new Layer(0, "Слой 1");
            Layers.Add(defaultLayer);

            // Сразу назначаем его активным, чтобы не было null
            ActiveLayer = defaultLayer;
        }

        public void Draw(Graphics g)
        {
            foreach (var layer in Layers)
            {
                // Рисуем слой только если он видим
                if (layer.IsVisible)
                {
                    layer.Draw(g);
                }
            }
        }

        public Shape GetShapeAt(Point p)
        {
            // Ищем фигуру с верхних слоев к нижним
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                // Если слой скрыт или заблокирован, на нем нельзя выбрать фигуру
                if (!layer.IsVisible || layer.IsLocked) continue;

                for (int j = layer.Shapes.Count - 1; j >= 0; j--)
                {
                    if (layer.Shapes[j].ContainPoint(p))
                        return layer.Shapes[j];
                }
            }
            return null;
        }

        public void RemoveShape(Shape shape)
        {
            if (shape == null) return;

            foreach (var layer in Layers)
            {
                if (layer.IsLocked) continue;

                if (layer.Shapes.Contains(shape))
                {
                    layer.Shapes.Remove(shape);
                    break;
                }
            }
        }
    }
}

using MyPaint.Models;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Drawing;

namespace MyPaint.Services
{
    // поиск классов фигур в проекте и плагинах
    public class TypeNameBinder : ISerializationBinder
    {
        // какому классу в коде соответствует запись в файле
        public Type BindToType(string assemblyName, string typeName)
        {
            string name = typeName.Split(',')[0].Trim();
            //смотрим где класс в библиотеке
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = assembly.GetType(name);
                if (t != null) 
                    return t;
            }
            return null;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.FullName;
        }
    }

    // сохранение и загрузку цветов
    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteValue(ColorTranslator.ToHtml(value));
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string s = reader.Value as string;
            return s == null ? Color.Black : ColorTranslator.FromHtml(s);
        }
    }

    public static class ProjectSerializer
    {
        private static JsonSerializerSettings GetSettings()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto, // автоматически пишет тип
                Formatting = Formatting.Indented,
                SerializationBinder = new TypeNameBinder(),
                // сохраняем ActiveLayer как ссылку
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,    // для слоев от пустого экрана
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore                //от бесконечных циклов
            };
            settings.Converters.Add(new ColorConverter());
            return settings;
        }

        public static void SaveToFile(string path, DrawingProject project)
        {
            string json = JsonConvert.SerializeObject(project, GetSettings());
            File.WriteAllText(path, json);
        }

        public static DrawingProject LoadFromFile(string path)
        {
            string json = File.ReadAllText(path);
            var settings = GetSettings();
            // если фигура из плагина не найдена пропускаем её
            settings.Error = (s, e) => { e.ErrorContext.Handled = true; };

            return JsonConvert.DeserializeObject<DrawingProject>(json, settings);
        }
    }
}
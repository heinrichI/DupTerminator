using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DupTerminator.WPF.Helper
{
    public class SerializeHelper<T>
    {
        static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        /// <summary>
        /// Запись в файл
        /// </summary>
        public static void Save(T target, string fileName)
        {
            string directory = Path.GetDirectoryName(fileName);
            if (!String.IsNullOrEmpty(directory)
                && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string jsonString = JsonSerializer.Serialize<T>(target, _options);
            File.WriteAllText(fileName, jsonString);
        }

        public static T Load(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists && fileInfo.Length > 0)
            {
                var text = File.ReadAllText(filePath);
                T t = JsonSerializer.Deserialize<T>(text, _options)!;
                return t;
            }

            return default(T);
        }
    }
}

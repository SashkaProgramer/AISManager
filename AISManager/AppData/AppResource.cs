using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Resources;
using System.Text;

namespace AISManager.AppData
{
    public static class AppResource
    {
        private static readonly Assembly s_assembly = Assembly.GetExecutingAssembly();

        public static MemoryStream GetResource(string resourceName)
        {
            Stream? resourceStream = s_assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                throw new MissingManifestResourceException($"Ресурс '{resourceName}' не был найден.");
            }

            var memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public static Icon GetIcon(string resourceName)
        {
            using MemoryStream stream = GetResource(resourceName);
            return new Icon(stream);
        }

        public static Bitmap GetBitmap(string resourceName)
        {
            using MemoryStream stream = GetResource(resourceName);
            return new Bitmap(stream);
        }

        public static string GetString(string resourceName)
        {
            using MemoryStream stream = GetResource(resourceName);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public static SoundPlayer GetSoundPlayer(string resourceName)
        {
            MemoryStream stream = GetResource(resourceName);
            return new SoundPlayer(stream);
        }

        public static IEnumerable<string> GetAllResourceNames()
        {
            return s_assembly.GetManifestResourceNames();
        }
    }
}

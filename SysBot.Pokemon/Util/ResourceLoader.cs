using System.IO;
using System.Reflection;

namespace SysBot.Pokemon
{
    public static class ResourceLoader
    {
        public static string GetEmbeddedResource(string namespacename, string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            if (assembly == null)
                return string.Empty;
            var resourceName = namespacename + "." + filename;
#pragma warning disable CS8600, CS8604
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new(stream);
            string result = reader.ReadToEnd();
            return result;
#pragma warning restore CS8600, CS8604
        }
    }
}

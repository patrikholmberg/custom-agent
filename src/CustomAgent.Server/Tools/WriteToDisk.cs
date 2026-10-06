using System.ComponentModel;
using System.Reflection;

namespace CustomAgent.Server.Tools
{
    public class WriteToDisk
    {
        [Description("Will write provided content to a file with provided name")]
        public void WriteContentToFile(string fileName, string content)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            File.WriteAllText(Path.Combine(path,fileName), content);
        }
    }
}

using Newtonsoft.Json;

namespace MfgDocs.Api.Extensions;
using System.Diagnostics;

public static class FontInstaller
{
    public static void EnsureFontsAvailable()
    {
        string sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Assets", "Fonts");
        string targetDir = "/usr/share/fonts/truetype/custom";
        Console.WriteLine("source" + sourceDir);   
        Console.WriteLine("target" +targetDir);   
        if (!Directory.Exists(sourceDir))
            return;

        var ddiirr =  Directory.CreateDirectory(targetDir);
        Console.WriteLine($"ddiiirr::::\n\n" + JsonConvert.SerializeObject(ddiirr));   
        foreach (var fontFile in Directory.GetFiles(sourceDir, "*.ttf"))
        {
            string targetPath = Path.Combine(targetDir, Path.GetFileName(fontFile));
            Console.WriteLine($"path targetted::::\n\n" + JsonConvert.SerializeObject(targetPath));   
            File.Copy(fontFile, targetPath, true);
        }

        try
        {
            Console.WriteLine($"starting process");   
            var process = Process.Start("fc-cache", "-fv");
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Font cache rebuild failed: " + ex.Message);
        }
    }
}

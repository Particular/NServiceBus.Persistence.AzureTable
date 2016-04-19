using System.IO;

public class IO
{
    public static void DeleteDirectory(string target_dir)
    {
        var files = Directory.GetFiles(target_dir);
        var dirs = Directory.GetDirectories(target_dir);

        foreach (var file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (var dir in dirs)
        {
            DeleteDirectory(dir);
        }

        Directory.Delete(target_dir, false);
    }
}
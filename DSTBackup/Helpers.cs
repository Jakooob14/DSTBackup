using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DSTBackup;

public class INIFile
{
    public string path { get; private set; }

    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string section, string key,string val,string filePath);
    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key,string def, StringBuilder retVal, int size,string filePath);

    public INIFile(string INIPath)
    {
        path = INIPath;
    }
    public void IniWriteValue(string Section, string Key, string Value)
    {
        WritePrivateProfileString(Section, Key, Value, this.path);
    }
    
    public string IniReadValue(string Section,string Key)
    {
        StringBuilder temp = new StringBuilder(255);
        int i = GetPrivateProfileString(Section, Key, "", temp, 255, this.path);
        return temp.ToString();
    }
}

public static class HelpersStatic
{

    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
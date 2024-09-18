using System.IO;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DSTBackup;

public class Config
{
    public string? BackupPath { get; set; }
    public int MaxWorlds { get; set; }

    public Config(string? backupPath = null, int maxWorlds = 5)
    {
        BackupPath = backupPath;
        MaxWorlds = maxWorlds;
    }

    public void Read()
    {
        try
        {
            using (StreamReader file = File.OpenText("DSTBackupConfig.json"))
            {
                string json = file.ReadToEnd();
                Config config = JsonConvert.DeserializeObject<Config>(json);
                
                BackupPath = config.BackupPath;
                MaxWorlds = config.MaxWorlds;
            }
        }
        catch (FileNotFoundException e)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Documents\Don't Starve Together Backups");
            
            Write(new Config(path, 5));
            
            BackupPath = path;
        }
    }
    
    public static void Write(Config config)
    {
        string json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText("DSTBackupConfig.json", json);
    }
}

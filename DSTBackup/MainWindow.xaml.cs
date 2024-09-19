using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace DSTBackup;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    class World
    {
        public string Name { get; set; }
        public string Path { get; set; }
        
        public World(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }

    readonly int _checkTime = 1;
    
    List<World?> _worlds = new List<World?>();
    World? _worldToBackup = null;
    DateTime _oldLastModified = DateTime.MinValue;
    bool _backupStarted = false;
    Config config = new Config();
    
    DispatcherTimer timer = new DispatcherTimer();
    
    public MainWindow()
    {
        InitializeComponent();

        Debugger.Launch();
        

        config.Read();
        PathInput.Text = config.BackupPath;
        MaxWorldsInput.Text = config.MaxWorlds.ToString();
        
        string worldPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Documents\Klei\DoNotStarveTogether");
        Debug.WriteLine($"[{DateTime.Now}] Initialized | World Path: {worldPath}");

        string[] idFolders = Directory.GetDirectories(worldPath);

        foreach (string idFolderPath in idFolders)
        {
            if (idFolderPath.Contains("backup")) continue;
            Debug.WriteLine($"[{DateTime.Now}] Foreach id | {idFolderPath}");
            
            string[] worldFolders = Directory.GetDirectories(idFolderPath + "\\CloudSaves");
            foreach (string worldFolderPath in worldFolders)
            {
                Debug.WriteLine($"[{DateTime.Now}] Foreach world | {worldFolderPath}");
                INIFile iniFile = new INIFile(worldFolderPath + @"\cluster.ini");
                string world = iniFile.IniReadValue("NETWORK", "cluster_name");
                if (world == "") continue;
                world = Regex.Replace(world, "[^a-zA-Z0-9 ]", "");
                WorldInput.Items.Add(world);
                _worlds.Add(new World(world, worldFolderPath));
                Debug.WriteLine($"[{DateTime.Now}] Added world {world} in {worldFolderPath}");
            }
        }
        
        timer.Interval = TimeSpan.FromSeconds(_checkTime);
        timer.Tick += timer_Tick;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var folderDialog = new OpenFolderDialog
        {
            // Set options here
        };

        if (folderDialog.ShowDialog() == true)
        {
            PathInput.Text = folderDialog.FolderName;
        }
    }

    private void StartBackupButton_Click(object sender, RoutedEventArgs e)
    {
        switch (_backupStarted)
        {
            case false:
            {
                try
                {
                    _worldToBackup = _worlds.First(item => item.Name == WorldInput.Text);
                    Debug.WriteLine("Clicked Start Backup | _worldToBackup: " + _worldToBackup.Path);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    return;
                }
        
                if (_worldToBackup == null) return;
                
                _backupStarted = true;
                StartBackupButton.Content = "Stop Backup";
                WorldInput.IsEnabled = false;
                PathInput.IsEnabled = false;
                BrowseButton.IsEnabled = false;
                MaxWorldsInput.IsEnabled = false;
                

                config.BackupPath = PathInput.Text;
                config.MaxWorlds = Convert.ToInt32(MaxWorldsInput.Text);
                Config.Write(config);
                
                Debug.WriteLine($"[{DateTime.Now}] Timer started | Backup Path: {config.BackupPath} | MaxWorlds: {config.MaxWorlds}");
                timer.Start();
                break;
            }
            case true:
            {
                _backupStarted = false;
                StartBackupButton.Content = "Start Backup";
                WorldInput.IsEnabled = true;
                PathInput.IsEnabled = true;
                BrowseButton.IsEnabled = true;
                MaxWorldsInput.IsEnabled = true;

                _oldLastModified = DateTime.MinValue;
                
                Debug.WriteLine("Clicked Stop Backup and stopped Timer");
                timer.Stop();
                break;
            }
        }
    }

    private void timer_Tick(object? sender, EventArgs e)
    {
        Debug.WriteLine($"[{DateTime.Now}] Start tick | _worldToBackup: {_worldToBackup.Path}");
        if (_worldToBackup == null) return;
        FileInfo masterInfo = new FileInfo(_worldToBackup.Path + @"\Master.zip");
        FileInfo cavesInfo = new FileInfo(_worldToBackup.Path + @"\Caves.zip");

        if (masterInfo.LastWriteTime > _oldLastModified)
        {
            _oldLastModified = masterInfo.LastWriteTime;
            Debug.WriteLine($"[{DateTime.Now}] Master older");
        } else if (cavesInfo.LastWriteTime > _oldLastModified)
        {
            _oldLastModified = cavesInfo.LastWriteTime;
            Debug.WriteLine($"[{DateTime.Now}] Caves older");
        }
        else
        {
            Debug.WriteLine($"[{DateTime.Now}] Else triggered");
            return;
        }

        
        string idPattern = @"DoNotStarveTogether\\(\d+)\\CloudSaves";
        string worldPattern = @"CloudSaves\\(.+)";
        
        Match idMatch = Regex.Match(_worldToBackup.Path, idPattern);
        Match worldMatch = Regex.Match(_worldToBackup.Path, worldPattern);
        Debug.WriteLine($"[{DateTime.Now}] Regex start");

        if (worldMatch.Success && idMatch.Success)
        {
            Debug.WriteLine($"[{DateTime.Now}] Regex match success");
            string idResult = idMatch.Groups[1].Value;
            string worldResult = worldMatch.Groups[1].Value;
            
            if(!Directory.Exists(config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name)){
                Directory.CreateDirectory(config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name);
                Debug.WriteLine($"[{DateTime.Now}] Created directory {config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name}");
            }

            int fileCount = Directory.EnumerateFiles(config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name).Count();
            Debug.WriteLine($"[{DateTime.Now}] ! " + config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name + "\\" + worldResult + " | File count " + fileCount);
            if (fileCount >= config.MaxWorlds)
            {
                foreach (var fi in new DirectoryInfo(@config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name).GetFiles().OrderByDescending(x => x.LastWriteTime).Skip(config.MaxWorlds - 1))
                    fi.Delete(); Debug.WriteLine($"[{DateTime.Now}] Deleted {config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name}");
            }
            ZipFile.CreateFromDirectory(_worldToBackup.Path, config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name + "\\" + worldResult + DateTime.Now.ToString(" yyyy-MM-dd hh_mm_ss") + ".zip");
            Debug.WriteLine($"[{DateTime.Now}] Zipped folder");
        }
    }

    private void MaxWorldsInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }
}
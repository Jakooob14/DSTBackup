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
using Path = System.IO.Path;
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

    private readonly int _checkTime = 1;
    private readonly string _id = "DST Backup";
    
    List<World?> _worlds = new List<World?>();
    World? _worldToBackup = null;
    DateTime _oldLastModified = DateTime.MinValue;
    bool _backupStarted = false;
    Config config = new Config();
    
    DispatcherTimer timer = new DispatcherTimer();
    
    public MainWindow()
    {
        InitializeComponent();

        MessageBox.Show("test277");
        try
        {
            config.Read();
            PathInput.Text = config.BackupPath;
            MaxWorldsInput.Text = config.MaxWorlds.ToString();
        
            string worldPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Documents\Klei\DoNotStarveTogether");
            Debugging.Log($"Initialized | World Path: {worldPath}");

            string[] idFolders = Directory.GetDirectories(worldPath);

            foreach (string idFolderPath in idFolders)
            {
                if (idFolderPath.Contains("backup")) continue;
                Debugging.Log($"Foreach id | {idFolderPath}");
            
                string[] worldFolders = Directory.GetDirectories(idFolderPath + "\\CloudSaves");
                foreach (string worldFolderPath in worldFolders)
                {
                    Debugging.Log($"Foreach world | {worldFolderPath}");
                    INIFile iniFile = new INIFile(worldFolderPath + @"\cluster.ini");
                    string world = iniFile.IniReadValue("NETWORK", "cluster_name");
                    if (world == "") continue;
                    world = Regex.Replace(world, "[^a-zA-Z0-9 ]", "_");
                    WorldInput.Items.Add(world);
                    _worlds.Add(new World(world, worldFolderPath));
                    Debugging.Log($"Added world {world} in {worldFolderPath}");
                }
            }
        }
        catch (Exception exception)
        {
            Debugging.Log("Error at Initialize: " + exception, Debugging.LoggingType.Error);
            MessageBox.Show("Error triggered! Look in the log file for more info.\n" + exception, _id, MessageBoxButton.OK, MessageBoxImage.Error);
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
        try
        {
            switch (_backupStarted)
            {
                case false:
                {
                    try
                    {
                        _worldToBackup = _worlds.First(item => item.Name == WorldInput.Text);
                        Debugging.Log("Clicked Start Backup | _worldToBackup: " + _worldToBackup.Path);
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

                    Debugging.Log($"Timer started | Backup Path: {config.BackupPath} | MaxWorlds: {config.MaxWorlds}");
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

                    Debugging.Log("Clicked Stop Backup and stopped Timer");
                    timer.Stop();
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            Debugging.Log("Error at StartBackupButton_Click: " + exception, Debugging.LoggingType.Error);
            MessageBox.Show("Error triggered! Look in the log file for more info.\n" + exception, _id, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            Debugging.Log($"Start tick | _worldToBackup: {_worldToBackup.Path}");
            if (_worldToBackup == null) return;
            FileInfo masterInfo = new FileInfo(_worldToBackup.Path + @"\Master.zip");
            FileInfo cavesInfo = new FileInfo(_worldToBackup.Path + @"\Caves.zip");

            if (masterInfo.LastWriteTime > _oldLastModified)
            {
                _oldLastModified = masterInfo.LastWriteTime;
                Debugging.Log($"Master older");
            }
            else if (cavesInfo.LastWriteTime > _oldLastModified)
            {
                _oldLastModified = cavesInfo.LastWriteTime;
                Debugging.Log($"Caves older");
            }
            else
            {
                Debugging.Log($"Else triggered");
                return;
            }


            string idPattern = @"DoNotStarveTogether\\(\d+)\\CloudSaves";
            string worldPattern = @"CloudSaves\\(.+)";

            Match idMatch = Regex.Match(_worldToBackup.Path, idPattern);
            Match worldMatch = Regex.Match(_worldToBackup.Path, worldPattern);
            Debugging.Log($"Regex start");

            if (worldMatch.Success && idMatch.Success)
            {
                Debugging.Log($"Regex match success");
                string idResult = idMatch.Groups[1].Value;
                string worldResult = worldMatch.Groups[1].Value;

                if (!Directory.Exists(config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name))
                {
                    Directory.CreateDirectory(config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name);
                    Debugging.Log(
                        $"Created directory {config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name}");
                }

                int fileCount = Directory
                    .EnumerateFiles(config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name).Count();
                Debugging.Log($"! " + config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name + "\\" +
                              worldResult + " | File count " + fileCount);
                if (fileCount >= config.MaxWorlds)
                {
                    foreach (var fi in new DirectoryInfo(@config.BackupPath + "\\" + idResult + "\\" +
                                                         _worldToBackup.Name).GetFiles()
                                 .OrderByDescending(x => x.LastWriteTime).Skip(config.MaxWorlds - 1))
                        fi.Delete();
                    Debugging.Log($"Deleted {config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name}");
                }

                
                ZipFile.CreateFromDirectory(_worldToBackup.Path, Path.Combine(config.BackupPath, idResult, _worldToBackup.Name, worldResult + DateTime.Now.ToString(" yyyy-MM-dd hh_mm_ss")) + ".zip");
                Debugging.Log($"Zipped folder to {config.BackupPath + "\\" + idResult + "\\" + _worldToBackup.Name + "\\" + worldResult + DateTime.Now.ToString(" yyyy-MM-dd HH_mm_ss") + ".zip"}");
            }
        }
        catch (Exception exception)
        {
            Debugging.Log("Error at timer_Tick: " + exception, Debugging.LoggingType.Error);
            MessageBox.Show("Error triggered! Look in the log file for more info.\n" + exception, _id, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MaxWorldsInput_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }
}
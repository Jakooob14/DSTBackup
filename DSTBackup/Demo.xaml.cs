using System.IO;
using System.Security.Policy;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace DSTBackup;

public partial class Demo : Window
{
    private byte _step = 0;

    private bool _opened = false;
    
    
    private string _worldFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        @"Documents\Klei\DoNotStarveTogether\420696969\CloudSaves\D3M0W0R1D0000000");
    
    private DispatcherTimer _timer = new DispatcherTimer();
    public Demo()
    {
        InitializeComponent();

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += TimerOnTick;
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            File.Create(Path.Combine(_worldFolder, "Master.zip")).Close();
            File.WriteAllText(Path.Combine(_worldFolder, "Master.zip"), BitConverter.ToString(System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(DateTime.Now.ToString()))).Replace("-", ""));
            
            if (!_opened)
            {
                _opened = true;
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }
        catch (Exception exception)
        {
            Debugging.Log("An error occured: " + exception, Debugging.LoggingType.Error, "DEMO");
            MessageBox.Show("Error triggered! Look in the log file for more info.\n" + exception, MainWindow.Id, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartDemoButton_Click(object sender, RoutedEventArgs e)
    {
        StartDemoButton.IsEnabled = false;
        
        try
        {
            Directory.CreateDirectory(_worldFolder);
            File.Create(Path.Combine(_worldFolder, "cluster.ini")).Close();
            File.WriteAllText(Path.Combine(_worldFolder, "cluster.ini"), "[NETWORK]\ncluster_name = Demo World");
            
            _timer.Start();
        }
        catch (Exception exception)
        {
            Debugging.Log("An error occured: " + exception, Debugging.LoggingType.Error, "DEMO");
            MessageBox.Show("Error triggered! Look in the log file for more info.\n" + exception, MainWindow.Id, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
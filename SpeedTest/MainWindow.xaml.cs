using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SpeedTest
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient httpClient;
        private readonly DispatcherTimer timer;
        private bool isTesting = false;
        private double maxSpeed = 1000; 

        public MainWindow()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += Timer_Tick;
            
            Loaded += MainWindow_Loaded;
            Loaded += (s, e) => 
            {
                
                if (SpeedBarCanvas != null)
                {
                    SpeedBarCanvas.SizeChanged += (sender, args) => 
                    {
                        UpdateSpeedBarMarks(maxSpeed);
                        UpdateSpeedBar(currentDisplaySpeed);
                    };
                   
                    if (SpeedBarCanvas.ActualWidth == 0)
                    {
                        SpeedBarCanvas.Width = 800;
                    }
                }
                InitializeSpeedometer();
            };
        }

        private void InitializeSpeedometer()
        {
            
            SpeedText.Text = "0";
            UpdateSpeedBar(0);
            UpdateSpeedBarMarks(1000); 
        }

        private void UpdateSpeedBarMarks(double maxSpeed)
        {
           
            if (SpeedBarCanvas == null) return;
            
            var canvasWidth = SpeedBarCanvas.ActualWidth > 0 ? SpeedBarCanvas.ActualWidth : 800;
            var scaleMax = 1000.0; 
            
            
            if (Mark100 != null)
            {
                var pos100 = (100.0 / scaleMax) * canvasWidth;
                Mark100.X1 = pos100;
                Mark100.X2 = pos100;
                if (Text100 != null)
                    Canvas.SetLeft(Text100, pos100 - 12);
            }
            
            
            var mark250 = SpeedBarCanvas.Children.OfType<FrameworkElement>()
                .FirstOrDefault(e => e.Name == "Mark250") as Line;
            var text250 = SpeedBarCanvas.Children.OfType<FrameworkElement>()
                .FirstOrDefault(e => e.Name == "Text250") as TextBlock;
            if (mark250 != null)
            {
                var pos250 = (250.0 / scaleMax) * canvasWidth;
                mark250.X1 = pos250;
                mark250.X2 = pos250;
                if (text250 != null)
                    Canvas.SetLeft(text250, pos250 - 18);
            }
            
           
            var mark500 = SpeedBarCanvas.Children.OfType<FrameworkElement>()
                .FirstOrDefault(e => e.Name == "Mark500") as Line;
            var text500 = SpeedBarCanvas.Children.OfType<FrameworkElement>()
                .FirstOrDefault(e => e.Name == "Text500") as TextBlock;
            if (mark500 != null)
            {
                var pos500 = (500.0 / scaleMax) * canvasWidth;
                mark500.X1 = pos500;
                mark500.X2 = pos500;
                if (text500 != null)
                    Canvas.SetLeft(text500, pos500 - 18);
            }
            
           
            var mark750 = SpeedBarCanvas.Children.OfType<FrameworkElement>()
                .FirstOrDefault(e => e.Name == "Mark750") as Line;
            var text750 = SpeedBarCanvas.Children.OfType<FrameworkElement>()
                .FirstOrDefault(e => e.Name == "Text750") as TextBlock;
            if (mark750 != null)
            {
                var pos750 = (750.0 / scaleMax) * canvasWidth;
                mark750.X1 = pos750;
                mark750.X2 = pos750;
                if (text750 != null)
                    Canvas.SetLeft(text750, pos750 - 18);
            }
            
           
            if (Mark1000 != null)
            {
                var pos1000 = canvasWidth;
                Mark1000.X1 = pos1000;
                Mark1000.X2 = pos1000;
                if (Text1000 != null)
                    Canvas.SetLeft(Text1000, pos1000 - 18);
            }
        }

        private void UpdateSpeedBar(double speed)
        {
            if (SpeedBarCanvas == null || SpeedBarActive == null) return;
            
            var canvasWidth = SpeedBarCanvas.ActualWidth > 0 ? SpeedBarCanvas.ActualWidth : 800;
            var scaleMax = 1000.0; 
            var normalizedSpeed = Math.Min(speed / scaleMax, 1.0);
            var barWidth = normalizedSpeed * canvasWidth;
            
            SpeedBarActive.Width = barWidth;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadNetworkInfo();
        }

        private async System.Threading.Tasks.Task LoadNetworkInfo()
        {
            try
            {
               
                var response = await httpClient.GetStringAsync("http://ip-api.com/json/?fields=status,message,query,country,isp");
                var json = JsonSerializer.Deserialize<JsonElement>(response);
                
                if (json.TryGetProperty("query", out var ip))
                    IpText.Text = ip.GetString() ?? "Неизвестно";
                
                if (json.TryGetProperty("isp", out var isp))
                    IspText.Text = isp.GetString() ?? "Неизвестно";
                
                if (json.TryGetProperty("country", out var country))
                    CountryText.Text = country.GetString() ?? "Неизвестно";
            }
            catch (Exception ex)
            {
                IpText.Text = "Ошибка загрузки";
                IspText.Text = "Ошибка загрузки";
                CountryText.Text = "Ошибка загрузки";
                Debug.WriteLine($"Ошибка загрузки информации: {ex.Message}");
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (isTesting) return;

            isTesting = true;
            StartButton.IsEnabled = false;
            StatusText.Text = "Запуск теста...";

            try
            {
                
                StatusText.Text = "Измерение задержки...";
                var ping = await TestPing();
                PingText.Text = $"{ping} мс";

               
                StatusText.Text = "Измерение скорости загрузки...";
                var downloadSpeed = await TestDownloadSpeed();
                DownloadText.Text = $"{downloadSpeed:F2} Мбит/с";
                UpdateSpeedometer(downloadSpeed);

             
                StatusText.Text = "Измерение скорости отдачи...";
                var uploadSpeed = await TestUploadSpeed();
                UploadText.Text = $"{uploadSpeed:F2} Мбит/с";

                StatusText.Text = "Тест завершен!";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка при тестировании";
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isTesting = false;
                StartButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task<int> TestPing()
        {
            try
            {
                var ping = new Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 3000);
                return (int)reply.RoundtripTime;
            }
            catch
            {
                return 0;
            }
        }

        private async System.Threading.Tasks.Task<double> TestDownloadSpeed()
        {
            
            var testUrls = new[]
            {
                "https://speed.cloudflare.com/__down?bytes=10485760", 
                "https://speed.cloudflare.com/__down?bytes=10485760",
                "https://speed.cloudflare.com/__down?bytes=10485760"
            };

            var speeds = new List<double>();

            foreach (var url in testUrls)
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    
                    
                    var totalBytes = 0L;
                    var buffer = new byte[8192];
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        int bytesRead;
                        var lastUpdate = stopwatch.ElapsedMilliseconds;
                        
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            totalBytes += bytesRead;
                            
                            
                            var now = stopwatch.ElapsedMilliseconds;
                            if (now - lastUpdate > 100 && now > 0)
                            {
                                var currentSpeed = (totalBytes * 8.0 / 1000000.0) / (now / 1000.0);
                                UpdateSpeedometer(currentSpeed);
                                lastUpdate = now;
                            }
                        }
                    }
                    
                    stopwatch.Stop();

                    if (stopwatch.ElapsedMilliseconds > 0)
                    {
                        var speedMbps = (totalBytes * 8.0 / 1000000.0) / (stopwatch.ElapsedMilliseconds / 1000.0);
                        speeds.Add(speedMbps);
                    }
                }
                catch
                {
                    
                }
            }

            return speeds.Count > 0 ? speeds.Average() : 0;
        }

        private async System.Threading.Tasks.Task<double> TestUploadSpeed()
        {
            
            var testData = new byte[5 * 1024 * 1024];
            new Random().NextBytes(testData);
            var content = new ByteArrayContent(testData);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await httpClient.PostAsync("https://speed.cloudflare.com/__up", content);
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 0)
                {
                    var speedMbps = (testData.Length * 8.0 / 1000000.0) / (stopwatch.ElapsedMilliseconds / 1000.0);
                    return speedMbps;
                }
            }
            catch
            {
                
                return await TestUploadSpeedAlternative();
            }

            return 0;
        }

        private async System.Threading.Tasks.Task<double> TestUploadSpeedAlternative()
        {
            
            var testData = new byte[1024 * 1024];
            new Random().NextBytes(testData);
            var speeds = new List<double>();

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var content = new ByteArrayContent(testData);
                    var stopwatch = Stopwatch.StartNew();
                    var response = await httpClient.PostAsync($"https://httpbin.org/post", content);
                    stopwatch.Stop();

                    if (stopwatch.ElapsedMilliseconds > 0)
                    {
                        var speedMbps = (testData.Length * 8.0 / 1000000.0) / (stopwatch.ElapsedMilliseconds / 1000.0);
                        speeds.Add(speedMbps);
                    }
                }
                catch
                {
                   
                }
            }

            return speeds.Count > 0 ? speeds.Average() : 0;
        }

        private void UpdateSpeedometer(double speed)
        {
            
            if (speed > maxSpeed)
            {
                maxSpeed = speed;
            }

            
            AnimateSpeedometer(speed);
        }

        private double currentDisplaySpeed = 0;
        private double targetSpeed = 0;

        private void AnimateSpeedometer(double target)
        {
            targetSpeed = target;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var difference = targetSpeed - currentDisplaySpeed;
            
            if (Math.Abs(difference) < 0.1)
            {
                currentDisplaySpeed = targetSpeed;
                timer.Stop();
            }
            else
            {
                
                currentDisplaySpeed += difference * 0.1;
                
                
                UpdateSpeedBar(currentDisplaySpeed);
                SpeedText.Text = currentDisplaySpeed.ToString("F0");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            httpClient?.Dispose();
            timer?.Stop();
            base.OnClosed(e);
        }
    }
}

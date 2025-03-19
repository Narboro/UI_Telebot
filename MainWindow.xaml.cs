using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using UI_Telebot.Model;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;
using SWF = System.Windows.Forms;

namespace UI_Telebot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Bindings Bindings { get; set; } = new Bindings();
        private readonly IConfiguration _configuration;
        Telebot tbt;
        SWF.NotifyIcon icon = new();

        public MainWindow()
        {
            InitializeComponent();
            CreateTrayIcon();
            this.DataContext = this;

            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();
            tbt = new Telebot(_configuration);
        }

        private void CreateTrayIcon()
        {
            icon = new SWF.NotifyIcon
            {
                Icon = ProjectResources.MyIcon,
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = {
                    new ToolStripMenuItem("Open", null, onClick: Open),
                    new ToolStripMenuItem("Exit", null, onClick: Exit),
                    }
                }
            };

            icon.MouseClick += MouseClickOpen;
        }
        void Exit(object? sender, EventArgs e)
        {
            icon.Dispose(); // Dispose of the notify icon when closing the application
            System.Windows.Application.Current.Shutdown();
        }

        void MouseClickOpen(object? sender, SWF.MouseEventArgs e)
        {
            if (e.Button == SWF.MouseButtons.Left)
            {
                Show();
                WindowState = WindowState.Normal;
            }
        }

        void Open(object? sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Cancel the closing event
            Hide(); // Hide the window instead of closing it
            WindowState = WindowState.Minimized; // Optionally minimize the window
        }

        protected override void OnClosed(EventArgs e)
        {
            //icon.Dispose(); // Dispose of the notify icon when closing the application
            base.OnClosed(e);
        }

        private void StartBot(object sender, RoutedEventArgs e)
        {
            
            if (Bindings.ButtonName == "Остановить Телеграм-бот")
            {
                tbt.StopBot();
                Color customColor = (Color)ColorConverter.ConvertFromString("#3F2DCA");
                SolidColorBrush customBrush = new SolidColorBrush(customColor);
                btn_start.Foreground = customBrush;
                Bindings.ButtonName = "Запустить Телеграм-бот";
                Bindings.BotState = "Бот остановлен";
                return;
            }

            Task tsk = tbt.ExecuteAsync(Bindings, btn_start);
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            Window_Closing(new object(), new System.ComponentModel.CancelEventArgs());
        }

        private void CloseApplication(object sender, RoutedEventArgs e)
        {
            Exit(new object(), new EventArgs());
        }

        private void CopyBooksBase(object sender, RoutedEventArgs e)
        {
            String sourceFilePath = (_configuration["BotConfiguration:LIBRARY_FILEPATH"] ?? "") + (_configuration["BotConfiguration:LIBRARY_FILENAME"] ?? "");
            String destinationFilePath = (_configuration["BotConfiguration:LIBRARY_FILEPATH"] ?? "") + (_configuration["BotConfiguration:LIBRARY_FILENAME"]?.Replace(".xlsx", DateTime.Now.ToString("_yyyy-MM-dd_HH-mm-ss") + ".xlsx") ?? "");

            if (File.Exists(sourceFilePath))
            {
                try
                {
                    File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
                    System.Windows.MessageBox.Show("Создана копия базы книг", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    //logger.Error(ex);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Файл базы не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
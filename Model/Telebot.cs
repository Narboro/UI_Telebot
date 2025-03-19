using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Configuration;
using System.Windows.Controls;
using Message = Telegram.Bot.Types.Message;
using NLog;
using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using System.Collections;

namespace UI_Telebot.Model
{
    internal class Telebot(IConfiguration configuration)
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource Cts { get; set; }
        AnsweringSystem? answeringSystem;

        public async Task ExecuteAsync(Bindings Bindings, System.Windows.Controls.Button btn_start)
        {
            var botToken = configuration["BotConfiguration:BotToken"] ?? "";
            var apiUrl = configuration["BotConfiguration:ApiUrl"] ?? "";

            Cts = new CancellationTokenSource();
            TelegramBotClient bot;

            try
            {
                bot = new TelegramBotClient(botToken, cancellationToken: Cts.Token);
                ExcelDbReader excelDbReader = new ExcelDbReader();
                DataTable booksDatabase = excelDbReader.ReadExcel(configuration);
                answeringSystem = new AnsweringSystem(booksDatabase, bot, configuration);
                var me = await bot.GetMe();
                bot.OnError += OnError;
                bot.OnMessage += OnMessage;
                bot.OnUpdate += OnUpdate;
                Color customColor = (Color)ColorConverter.ConvertFromString("#52DA1F");
                SolidColorBrush customBrush = new SolidColorBrush(customColor);
                btn_start.Foreground = customBrush;
                Bindings.BotState = "Бот запущен";
                Bindings.ButtonName = "Остановить Телеграм-бот";
            }
            catch (Exception ex) {
                System.Windows.MessageBox.Show(ex.Message, "Ошибка",MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Error(ex);
            }

            // method to handle errors in polling or in your OnMessage/OnUpdate code
            async Task OnError(Exception exception, HandleErrorSource source)
            {
                //System.Windows.MessageBox.Show(exception.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); //отключено как лишнее
                logger.Error(exception.Message);
            }

            // method that handle messages received by the bot:
            async Task OnMessage(Message msg, UpdateType type)
            {
                logger.Info($"{msg.From} - {msg.Text}");
                await answeringSystem.OnMessageGetAsync(msg, type);
            }

            // method that handle other types of updates received by the bot:
            async Task OnUpdate(Update update)
            {
                if (update is { CallbackQuery: { } query }) // non-null CallbackQuery
                {
                    await answeringSystem.OnUpdateGetAsync(update);
                }
            }
        }

        public void StopBot()
        {
            Cts.Cancel();
        }
    }
}

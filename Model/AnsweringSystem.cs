using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Message = Telegram.Bot.Types.Message;
using System.Collections;

namespace UI_Telebot.Model
{
    class AnsweringSystem
    {
        private DataTable booksDatabase;
        private TelegramBotClient bot;
        private readonly IConfiguration _configuration;
        private int NUMBER_OF_ROWS = 5;
        private string administratorUser;
        Dictionary<string, string[]> booksWithIds = new Dictionary<string, string[]>();
        private string[] pickedBook { get; set; } = [];

        public AnsweringSystem(DataTable booksDatabase, TelegramBotClient bot, IConfiguration configuration)
        {
            this.booksDatabase = booksDatabase;
            this.bot = bot;
            _configuration = configuration;
            int.TryParse(_configuration["BotConfiguration:NUMBER_OF_ROWS"], out NUMBER_OF_ROWS);
            administratorUser = _configuration["BotConfiguration:Administrator"] ?? "никто";

            foreach (DataRow dataRow in booksDatabase.Rows)
            {
                booksWithIds.TryAdd(dataRow["Id"].ToString() ?? "error", [(dataRow["Название"] as string) ?? "error", (dataRow["код"] as string) ?? "error", (dataRow["кому выдано"] as string) ?? ""]);
            }
        }

        public async Task OnMessageGetAsync(Message msg, UpdateType type)
        {
            //if (msg?.Chat?.Id.ToString() == administratorUser) //TODO admin panel
            //{
            //    await AdminMessages(msg, bot);
            //}
            if (msg.Text == "/start")
            {
                await bot.SendMessage(msg.Chat, _configuration["BotConfiguration:GreetingsMessage"] ?? "ошибка ошибка",
                        replyMarkup: new InlineKeyboardMarkup().AddButtons("Список книг"));
            }
            else
            {
                //if (msg?.Chat?.Id.ToString() != administratorUser) await bot.SendMessage(msg.Chat, "", replyMarkup: new ReplyKeyboardRemove());

                await bot.SendMessage(msg.Chat, $"Вы ввели: {msg.Text}");

                if (booksWithIds.ContainsKey(msg.Text ?? ""))
                {
                    pickedBook = booksWithIds[msg.Text ?? ""];
                    string? isAvailable = (!String.IsNullOrEmpty(pickedBook[2]) ? (pickedBook[2]).Contains("не выдается", StringComparison.OrdinalIgnoreCase) ? _configuration["BotConfiguration:NonBooking"] ?? "ошибка ошибка" : _configuration["BotConfiguration:AlreadyBooked"] ?? "ошибка ошибка" : "");
                    if (isAvailable != "")
                    {
                        await bot.SendMessage(msg.Chat, (_configuration["BotConfiguration:BookingReject"] ?? "ошибка ошибка") + " <i><b>" + isAvailable + "</b></i>", ParseMode.Html,
                            replyMarkup: new InlineKeyboardMarkup().AddButtons("Список книг"));
                        return;
                    }
                    await bot.SendMessage(msg.Chat, (_configuration["BotConfiguration:BookingConfirmation"] ?? "ошибка ошибка") + " " + pickedBook[0],
                            replyMarkup: new InlineKeyboardMarkup().AddButtons("Подтверждаю", "Список книг"));
                }
                else
                {
                    await SearchBooksDB(msg);
                }
            }
        }

        public async Task OnUpdateGetAsync(Update update)
        {
            if (update is { CallbackQuery: { } query }) // non-null CallbackQuery
            {
                await bot.AnswerCallbackQuery(query.Id, $"Вы выбрали: {query.Data}");

                if (query.Data == "Список книг")
                {
                    await ReturnLibraryData(query.Message!);
                    await bot.SendMessage(query.Message!.Chat, _configuration["BotConfiguration:GetBookMessage"] ?? "ошибка ошибка",
                        replyMarkup: new InlineKeyboardMarkup().AddButtons("Список книг"));
                }
                else
                {
                    if (pickedBook.Length  == 0)
                    {
                        await bot.SendMessage(query.Message!.Chat, $"Книга не выбрана.");
                        return;
                    }
                    //await bot.SendMessage(query.Message!.Chat, $"User {query.From} clicked on {query.Data}");
                    await bot.SendMessage(query.Message!.Chat, $"Вы выбрали книгу: \n\n{pickedBook?[0]}");
                    await bot.SendMessage(query.Message!.Chat, $"Сообщение отправлено администратору. Он свяжется с вами в ближайшее время.");
                    await bot.SendMessage(administratorUser, $"Пользователь {query.From} хочет взять книгу:\n\n{pickedBook?[0]} - {pickedBook?[1]}");
                }
            }
        }

        private async Task ReturnLibraryData(Message msg)
        {
            string messageChunk = "";
            for (int i = 0; i <= booksDatabase?.Rows.Count; i++)
            {
                if (i == booksDatabase.Rows.Count)
                {
                    await bot.SendMessage(msg.Chat, messageChunk, ParseMode.Html);
                    messageChunk = "";
                    break;
                }
                string? isAvailable = (!String.IsNullOrEmpty(booksDatabase?.Rows[i]?["кому выдано"] as string) ? (booksDatabase?.Rows[i]?["кому выдано"] as string).Contains("не выдается", StringComparison.OrdinalIgnoreCase) ? $" - <i><b>{_configuration["BotConfiguration:NonBooking"] ?? "ошибка ошибка"}</b></i>" : $" - <i><b>{_configuration["BotConfiguration:AlreadyBooked"] ?? "ошибка ошибка"}</b></i>" : "");
                if (i > 0 && i % NUMBER_OF_ROWS == 0)
                {
                    await bot.SendMessage(msg.Chat, messageChunk, ParseMode.Html);
                    messageChunk = "";
                    messageChunk += $"{booksDatabase?.Rows[i]["Id"]} - {booksDatabase?.Rows[i]["Название"]}{isAvailable}\n";
                }
                else
                {
                    messageChunk += $"{booksDatabase?.Rows[i]["Id"]} - {booksDatabase?.Rows[i]["Название"]}{isAvailable}\n";
                }
            }
        }

        private async Task SearchBooksDB(Message msg)
        {
            var selectedBooks = from row in booksDatabase.AsEnumerable()
                                where row.Field<string>("Название").Contains(msg!.Text, StringComparison.OrdinalIgnoreCase)
                                select row;

            if (selectedBooks.Count() > 0)
            {
                string messageChunk = "Результаты поиска книг:\n\n";
                foreach (var row in selectedBooks)
                {
                    string? isAvailable = (!String.IsNullOrEmpty(row["кому выдано"] as string) ? (row["кому выдано"] as string).Contains("не выдается", StringComparison.OrdinalIgnoreCase) ? $" - <i><b>{_configuration["BotConfiguration:NonBooking"] ?? "ошибка ошибка"}</b></i>" : $" - <i><b>{_configuration["BotConfiguration:AlreadyBooked"] ?? "ошибка ошибка"}</b></i>" : "");
                    messageChunk += $"{row["Id"]} - {row["Название"]}{isAvailable}\n";
                }
                await bot.SendMessage(msg.Chat, messageChunk, ParseMode.Html);
                await bot.SendMessage(msg.Chat, _configuration["BotConfiguration:GetBookMessage"] ?? "ошибка ошибка",
                        replyMarkup: new InlineKeyboardMarkup().AddButtons("Список книг"));
            }
            else
            {
                await bot.SendMessage(msg.Chat, _configuration["BotConfiguration:NotFoundBook"] ?? "ошибка ошибка");
                await bot.SendMessage(msg.Chat, _configuration["BotConfiguration:GreetingsMessage"] ?? "ошибка ошибка",
                    replyMarkup: new InlineKeyboardMarkup().AddButtons("Список книг"));
            }
        }

        //private async Task AdminMessages(Message msg, TelegramBotClient bot)
        //{
            //if (msg.Text == "/admin")
            //{
            //    await bot.SendMessage(msg.Chat, "Панель администратора активирована", replyMarkup: new string[][]
                //{
                //    ["Help me"],
                //    ["Call me ☎️", "Write me ✉️"]
                //});
            //}
        //}
    }
}

using System.Data;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Message = Telegram.Bot.Types.Message;
using System.IO;

namespace UI_Telebot.Model
{
    class AnsweringSystem
    {
        private DataTable booksDatabase;
        private TelegramBotClient bot;
        private readonly IConfiguration _configuration;
        private int NUMBER_OF_ROWS = 5;
        private string administratorUser;
        private string workflowStatus {  get; set; }
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
            if (msg?.Chat?.Id.ToString() == administratorUser && msg.Text == "/admin") //TODO admin panel
            {
                workflowStatus = "AdminPanelActive";
                await bot.SendMessage(msg.Chat, "Панель администратора активирована", replyMarkup: new string[][]
                {
                ["Выданные книги", "Просроченные книги"],
                ["Выдать книгу", "Сдать книгу"],
                ["Сделать копию базы"]
                });
                return;
            }
            if(msg?.Chat?.Id.ToString() == administratorUser && workflowStatus == "AdminPanelActive")
            {
                await AdminMessages(msg, bot);
                return;
            }
            if (msg?.Chat?.Id.ToString() == administratorUser && workflowStatus == "BookIssue")
            {
                await BookIssue(msg, DateTime.Now.ToString("dd.MM.yyyy"));
                await bot.SendMessage(msg.Chat, "Книга выдана" , ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                workflowStatus = "";
                return;
            }
            if(msg?.Chat?.Id.ToString() == administratorUser && workflowStatus == "ReturnBook")
            {
                await BookIssue(msg, "");
                await bot.SendMessage(msg.Chat, "Книга возвращена", ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                workflowStatus = "";
                return;
            }
            if (msg?.Text == "/start")
            {
                await bot.SendMessage(msg.Chat, _configuration["BotConfiguration:GreetingsMessage"] ?? "ошибка ошибка",
                        replyMarkup: new InlineKeyboardMarkup().AddButtons("Список книг"));
            }
            else
            {
                await bot.SendMessage(msg?.Chat, $"Вы ввели: {msg?.Text}");

                if (booksWithIds.ContainsKey(msg.Text ?? ""))
                {
                    pickedBook = booksWithIds[msg.Text ?? ""];
                    string? isAvailable = (!string.IsNullOrEmpty(pickedBook[2]) ? (pickedBook[2]).Contains("не выдается", StringComparison.OrdinalIgnoreCase) ? _configuration["BotConfiguration:NonBooking"] ?? "ошибка ошибка" : _configuration["BotConfiguration:AlreadyBooked"] ?? "ошибка ошибка" : "");
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

                string? forAdmin = (msg?.Chat?.Id.ToString() == administratorUser) ? " - " + booksDatabase?.Rows[i]?["код"].ToString() : "";
                string ? isAvailable = (!string.IsNullOrEmpty(booksDatabase?.Rows[i]?["кому выдано"] as string) ? (booksDatabase?.Rows[i]?["кому выдано"] as string).Contains("не выдается", StringComparison.OrdinalIgnoreCase) ? $" - <i><b>{_configuration["BotConfiguration:NonBooking"] ?? "ошибка ошибка"}</b></i>" : $" - <i><b>{_configuration["BotConfiguration:AlreadyBooked"] ?? "ошибка ошибка"}</b></i>" : "");
                if (i > 0 && i % NUMBER_OF_ROWS == 0)
                {
                    await bot.SendMessage(msg.Chat, messageChunk, ParseMode.Html);
                    messageChunk = "";
                    messageChunk += $"{booksDatabase?.Rows[i]["Id"]}{forAdmin} - {booksDatabase?.Rows[i]["Название"]}{isAvailable}\n";
                }
                else
                {
                    messageChunk += $"{booksDatabase?.Rows[i]["Id"]}{forAdmin} - {booksDatabase?.Rows[i]["Название"]}{isAvailable}\n";
                }
            }
        }

        private async Task SearchBooksDB(Message msg)
        {
            var selectedBooks = from row in booksDatabase.AsEnumerable()
                                where row.Field<string?>("Название").Contains(msg?.Text, StringComparison.OrdinalIgnoreCase)
                                select row;

            if (selectedBooks.Count() > 0)
            {
                string messageChunk = "Результаты поиска книг:\n\n";
                foreach (var row in selectedBooks)
                {
                    string? isAvailable = (!string.IsNullOrEmpty(row["кому выдано"] as string) ? (row["кому выдано"] as string).Contains("не выдается", StringComparison.OrdinalIgnoreCase) ? $" - <i><b>{_configuration["BotConfiguration:NonBooking"] ?? "ошибка ошибка"}</b></i>" : $" - <i><b>{_configuration["BotConfiguration:AlreadyBooked"] ?? "ошибка ошибка"}</b></i>" : "");
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

        private async Task BookIssue(Message msg, string action)
        {
            List<string>? enteredValue = msg?.Text?.Split(";").ToList();
            if (action == "")
            {
                enteredValue?.Add((string)null);
                enteredValue?.Add((string)null);
                enteredValue?.Add((string)null);
            } else
            {
                enteredValue?.Add(action);
            }

            new ExcelDbReader().WriteExcel(_configuration, enteredValue);
            booksDatabase = new ExcelDbReader().ReadExcel(_configuration);
        }

        private async Task AdminMessages(Message msg, TelegramBotClient bot)
        {
            string messageChunk = "";
            if (workflowStatus == "AdminPanelActive" && msg?.Chat?.Id.ToString() == administratorUser)
            {
                workflowStatus = "";
                switch (msg.Text)
                {
                    case "Выданные книги":
                        messageChunk = "Id - Название - Код - Кому выдано - Дата выдачи - Дней в выдаче\n\n<b>Выданные книги:</b>\n\n";
                        var bookedBooks = from row in booksDatabase.AsEnumerable()
                                          where row.Field<System.DateTime?>("дата выдачи") is not null
                                          select row;

                        messageChunk += PrepareQuery(bookedBooks);
                        await bot.SendMessage(msg.Chat, messageChunk, ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                        break;
                    case "Просроченные книги":
                        messageChunk = "Id - Название - Код - Кому выдано - Дата выдачи - Дней в выдаче\n\n<b>Просроченные книги:</b>\n\n";
                        var expiredBooks = from row in booksDatabase.AsEnumerable()
                                           where row.Field<double?>("дней") > 90
                                           select row;

                        messageChunk += PrepareQuery(expiredBooks);
                        await bot.SendMessage(msg.Chat, messageChunk, ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                        break;
                    case "Выдать книгу":
                        messageChunk = "Введите выдачу книги через точку с запятой в формате ниже:\n\n<b>Код ; Кому выдано ; Телефон</b>";
                        await bot.SendMessage(msg.Chat, messageChunk, ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                        workflowStatus = "BookIssue";
                        break;
                    case "Сдать книгу":
                        messageChunk = "Введите код книги, которую хотите сдать.";
                        await bot.SendMessage(msg.Chat, messageChunk, ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
                        workflowStatus = "ReturnBook";
                        break;
                    case "Сделать копию базы":
                        string sourceFilePath = (_configuration["BotConfiguration:LIBRARY_FILEPATH"] ?? "") + (_configuration["BotConfiguration:LIBRARY_FILENAME"] ?? "");
                        string destinationFilePath = (_configuration["BotConfiguration:LIBRARY_FILEPATH"] ?? "") + (_configuration["BotConfiguration:LIBRARY_FILENAME"]?.Replace(".xlsx", DateTime.Now.ToString("_yyyy-MM-dd_HH-mm-ss") + ".xlsx") ?? "");

                        if (File.Exists(sourceFilePath))
                        {
                            try
                            {
                                File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
                                await bot.SendMessage(msg.Chat, "Копия базы создана.", replyMarkup: new ReplyKeyboardRemove());
                            }
                            catch (Exception ex)
                            {
                                await bot.SendMessage(msg.Chat, "Ошибка!", replyMarkup: new ReplyKeyboardRemove());
                            }
                        }
                        else
                        {
                            await bot.SendMessage(msg.Chat, "Файл базы не найден", replyMarkup: new ReplyKeyboardRemove());
                        }
                        break;
                    default:
                        await bot.SendMessage(msg.Chat, "Неизвестная команда.", replyMarkup: new ReplyKeyboardRemove());
                        break;
                }
            }
        }

        private string PrepareQuery(EnumerableRowCollection<DataRow> query)
        {
            string? messageChunk = "";
            foreach (var row in query)
            {
                messageChunk += $"{row["Id"]} - {row["Название"]} - {row["Код"]} - {row["кому выдано"]} - <i><b>{row["дата выдачи"]?.ToString()?.Substring(0, 10)}</b></i> - {row["дней"]}\n";
            }
            return messageChunk;
        }
    }
}

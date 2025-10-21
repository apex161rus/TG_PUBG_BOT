// Подключаем нужные пространства имён
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Xml;

var xmlDoc = new XmlDocument();
string Config()
{
    string dockerPath = "config.xml";
    if (File.Exists(dockerPath))
    {
        Console.WriteLine($"✅ Использую Docker-путь: {dockerPath}");
        return dockerPath;
    }
    string macPath = "/Users/vladislavfurazkin/Desktop/доки/тестовый Бот/pubg_bot_restart/config.xml";
    if (File.Exists(macPath))
    {
        Console.WriteLine($"✅ Использую Mac-путь: {macPath}");
        return macPath;
    }

    Console.WriteLine("⚠️ Файл не найден ни по одному пути!");
    return string.Empty; // или можно вернуть дефолтный путь
}


xmlDoc.Load(Config());
string token = xmlDoc.SelectSingleNode("/Configuration/BotSettings/Token")?.InnerText;
string botUsername = xmlDoc.SelectSingleNode("/Configuration/BotSettings/BotUsername")?.InnerText;

using var cts = new CancellationTokenSource();

// Чтение списка админов
List<long> adminIds = new List<long>();
XmlNodeList adminNodes = xmlDoc.SelectNodes("/Configuration/BotSettings/Admins/Admin");
foreach (XmlNode adminNode in adminNodes)
{
    if (long.TryParse(adminNode.InnerText, out long adminId))
    {
        adminIds.Add(adminId);
    }
}


Console.WriteLine($"Token: {token}");
Console.WriteLine($"Bot VERS: {botUsername}");
Console.WriteLine($"Admins: {string.Join(", ", adminIds)}");

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("Ошибка: Токен не найден в config.xml!");
    return;
}

if (adminIds.Count == 0)
{
    Console.WriteLine("Предупреждение: Список админов пуст!");
}


// using var cts = new CancellationTokenSource();

// Функция проверки админских прав
// bool IsAdmin(long userId) => adminIds.Contains(userId);

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("Ошибка: не передан токен бота!");
    return;
}
// ---------------------------
// Главное хранилище событий
// ---------------------------
// ВАЖНО: инициализируем ДО того, как передаём обработчики в StartReceiving,
// чтобы избежать ошибки CS0165 (переменная должна быть явно инициализирована
// до момента возможного её использования).
var events = new Dictionary<string, EventData>();

// Хранилище для защиты от частых кликов
var userClickTimestamps = new Dictionary<long, DateTime>();

// ---------------------------
// Создаём клиента бота
// ---------------------------
var bot = new TelegramBotClient(token, cancellationToken: cts.Token);

// ---------------------------
// Локальная функция: обработчик апдейтов
// ---------------------------
// Объявляем функцию до StartReceiving — это безопасно и читаемо.
async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    try
    {
        // Обрабатываем разные типы апдейтов
        switch (update.Type)
        {
            case UpdateType.Message:
            {
                //ID — просто выходим
                // long idNamsDarkApexNet = 5976500787;
                long idNamsDarkApexNet = 597;
                long idNamsVolunqw = 1004195686;
                var msg = update.Message;

                if (msg?.Text == null) return; // если текста нет — ничего не делаем

                string text = msg.Text.Trim(); // текст сообщения без лишних пробелов
                long chatId = msg.Chat.Id;     // id чата, откуда пришло сообщение

                Console.WriteLine($"ID {msg.From?.Id} | Username {msg.From.Username} | text {msg.Text}");
                var userId = msg.From?.Id ?? 0; // ID пользователя
                    bool isVipUser = (userId == adminIds[1] || userId == adminIds[0]);


                int rand = Random.Shared.Next(2, 6);

                string text1 = msg.Text.Trim().ToLower();
                string[] keywords = { "селива", "селева", "ну че там", "че когда", "когда" };

                    // если сообщение содержит ключевое слово
                    if (keywords.Any(k => text1.Contains(k)))
                    {
                        if (isVipUser)
                        {
                            string basePath = AppContext.BaseDirectory;
                            string voicePath = Path.Combine(basePath, "Stickers", $"sticker.webm");
                            string pathMacOS = "/Users/vladislavfurazkin/Desktop/доки/тестовый Бот/pubg_bot_restart/Stickers/sticker.webm";
                            // VIP → отправляем стикер
                            using var fileStream1 = File.OpenRead(GetPath(voicePath,pathMacOS));
                            await botClient.SendSticker(
                                chatId: msg.Chat.Id,
                                sticker: new InputFileStream(fileStream1, "sticker.webm")
                            );
                        }
                        else
                        {
                            string basePath = AppContext.BaseDirectory;
                            string voicePath = Path.Combine(basePath, "Voices", $"{rand}.ogg");
                            string pathMacOS = $"/Users/vladislavfurazkin/Desktop/доки/тестовый Бот/pubg_bot_restart/Voices/{rand}.ogg";
                            // остальные → отправляем голосовое сообщение
                            using var filePath = File.OpenRead(GetPath(voicePath,pathMacOS));
                            await botClient.SendVoice(
                                chatId: msg.Chat.Id,
                                voice: new InputFileStream(filePath),
                                replyParameters: msg.MessageId
                            );
                        }
                    }

                    
                    // Простая валидация формата времени: часы 0-23, минуты 00-59
                    // Допускаем 1 или 2 цифры для часа, например "7:05" или "19:00"
                    var regex = new Regex(@"\b([01]?\d|2[0-3]):[0-5]\d\b");
                    // ищем совпадения
                    Match match = regex.Match(text);
                    if (match.Success)
                    {

                        string time = match.Value; // само время, например "10:24"

                        // Ключ события — комбинируем chatId и время,
                        // чтобы одно и то же время в двух разных чатах не мешало друг другу
                        string key = $"{chatId}|{time}";

                        // Если событие уже создано в этом чате — уведомим и вернёмся
                        if (events.ContainsKey(key))
                        {
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: $"Событие на {time} уже существует в этом чате.",
                                cancellationToken: cancellationToken
                            );
                            Console.WriteLine($"Попытка создать уже существующее событие {time} в чате {chatId}");
                            return;
                        }

                        // Формируем inline-кнопки: Подписаться / Отписаться
                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                        InlineKeyboardButton.WithCallbackData("✅ Подписаться", $"subscribe|{time}"),
                        InlineKeyboardButton.WithCallbackData("❌ Отписаться", $"unsubscribe|{time}")
                    });

                        string basePath = AppContext.BaseDirectory;
                        string videoPath = Path.Combine(basePath, "Video", $"играть.MOV");
                        string videoPath1 = "/Users/vladislavfurazkin/Desktop/доки/тестовый Бот/pubg_bot_restart/Video/играть.MOV";
                        // открываем поток


                        if (isVipUser)
                        {
                            await using Stream stream = File.OpenRead(GetPath(videoPath, videoPath1));
                            // отправляем видео
                            await bot.SendVideoNote(msg.Chat, stream);


                            // Отправляем сообщение (форма события)
                            var sent = await botClient.SendMessage(
                            chatId: chatId,
                            text: $"🕒 Селева на {time}\n\nПодписчики:\n(пока нет)\n\nОтписавшиеся:\n(пока нет)",
                            replyMarkup: inlineKeyboard,
                            cancellationToken: cancellationToken
                            );

                            // Сохраняем событие в словаре
                            events[key] = new EventData
                            {
                                ChatId = chatId,
                                Time = time,
                                MessageId = sent.MessageId
                            };

                            Console.WriteLine($"Создано событие {time} в чате {chatId}");
                        }
                        else
                        {
                            string voicePath = Path.Combine(basePath, "Voices", $"{rand}.ogg");
                            string pathMacOS = $"/Users/vladislavfurazkin/Desktop/доки/тестовый Бот/pubg_bot_restart/Voices/{rand}.ogg";
                            // остальные → отправляем голосовое сообщение
                            using var filePath = File.OpenRead(GetPath(voicePath,pathMacOS));
                            await botClient.SendVoice(
                                chatId: msg.Chat.Id,
                                voice: new InputFileStream(filePath),
                                replyParameters: msg.MessageId
                            );
                        }
                    }

                return;
            }

            case UpdateType.CallbackQuery:
            {
                var callback = update.CallbackQuery!;
                if (callback.Data == null || callback.Message == null) return;

                // Данные в callback: action|time (например "subscribe|19:00")
                var parts = callback.Data.Split('|');
                if (parts.Length != 2) return;

                string action = parts[0];         // "subscribe" или "unsubscribe"
                string time = parts[1];           // "19:00"
                long chatId = callback.Message.Chat.Id;
                string key = $"{chatId}|{time}";  // тот же ключ, что и при создании

                // Берём имя пользователя (для отображения)
                string userName = string.IsNullOrEmpty(callback.From.Username)
                    ? callback.From.FirstName ?? callback.From.Id.ToString()
                    : "@" + callback.From.Username;
                    
                
                // ---------------------------
                // Антиспам по кликам
                // ---------------------------
                long userId = callback.From.Id;
                if (userClickTimestamps.TryGetValue(userId, out var lastClick))
                {
                    if ((DateTime.UtcNow - lastClick).TotalSeconds < 2) // лимит — 2 секунды
                    {
                        await botClient.AnswerCallbackQuery(
                            callback.Id,
                            "❗ Вы слишком часто нажимаете кнопку. Пойдите нахуй.",
                            showAlert: true,
                            cancellationToken: cancellationToken
                        );
                        return;
                    }
                }

                userClickTimestamps[userId] = DateTime.UtcNow;

                if (!events.ContainsKey(key))
                    {
                        // Событие могло быть удалено — сообщаем и выходим
                        await botClient.AnswerCallbackQuery(callback.Id, "Событие больше не существует.", cancellationToken: cancellationToken);
                        return;
                    }

                var ev = events[key];

                    // Обновляем списки в зависимости от действия
                    if (action == "subscribe")
                    {
                        ev.UnsubscriberIds.Remove(callback.From.Id);
                        ev.UnsubscriberNames.Remove(callback.From.Id);

                        ev.SubscriberIds.Add(callback.From.Id);
                        ev.SubscriberNames[callback.From.Id] = userName; // сохраняем имя

                        Console.WriteLine($"{userName} подписался на {time} в чате {chatId}");
                    }
                    else if (action == "unsubscribe")
                    {
                        ev.SubscriberIds.Remove(callback.From.Id);
                        ev.SubscriberNames.Remove(callback.From.Id);

                        ev.UnsubscriberIds.Add(callback.From.Id);
                        ev.UnsubscriberNames[callback.From.Id] = userName;

                        await botClient.SendMessage(
                        chatId: chatId,
                        text: $"{userName} отписался {time}",
                        cancellationToken: cancellationToken);
                        Console.WriteLine($"{userName} отписался от {time} в чате {chatId}");
                    }



                    string subscribersList = ev.SubscriberNames.Count > 0 
                        ? string.Join("\n", ev.SubscriberNames.Values) 
                        : "(пока нет)";

                    string unsubscribersList = ev.UnsubscriberNames.Count > 0 
                        ? string.Join("\n", ev.UnsubscriberNames.Values) 
                        : "(пока нет)";

                    string newText = $"🕒 Селава на {time}\n\n" +
                                    $"Завики:\n{subscribersList}\n\n" +
                                    $"Отписавшиеся:\n{unsubscribersList}";

                // Редактируем исходное сообщение, чтобы форма была актуальной
                await botClient.EditMessageText(
                    chatId: ev.ChatId,
                    messageId: ev.MessageId,
                    text: newText,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Подписаться на селева", $"subscribe|{time}"),
                        InlineKeyboardButton.WithCallbackData("❌ Отписаться", $"unsubscribe|{time}")
                    }),
                    cancellationToken: cancellationToken
                );

                // Подтверждаем нажатие (убирает "часики" в интерфейсе)
                await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);

                return;
            }

            default:
                // Другие типы апдейтов пока не обрабатываем
                return;
        }
    }
    catch (Exception ex)
    {
        // Логируем исключение в консоль
        Console.WriteLine($"Ошибка в UpdateHandler: {ex}");
    }
}

// ---------------------------
// Обработчик ошибок (для StartReceiving)
// ---------------------------
Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
{
    // Формируем понятное сообщение об ошибке
    var ErrorMessage = error switch
    {
        ApiRequestException apiReqEx => $"Telegram API Error:\n[{apiReqEx.ErrorCode}]\n{apiReqEx.Message}",
        _ => error.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

async Task NotifySubscribers(EventData ev, string message)
{
    foreach (var userId in ev.SubscriberIds)
    {
        try
        {
            string basePath = AppContext.BaseDirectory;
                            string voicePath = Path.Combine(basePath, "Stickers", $"vova.webp");
                            string pathMacOS = "/Users/vladislavfurazkin/Desktop/доки/тестовый Бот/pubg_bot_restart/Stickers/vova.webp";
                            // VIP → отправляем стикер
                            using var fileStream1 = File.OpenRead(GetPath(voicePath,pathMacOS));
                            await bot.SendSticker(
                                chatId: userId,
                                sticker: new InputFileStream(fileStream1)
                            );

            await bot.SendMessage(
                chatId: userId,
                text: message
            );
            Console.WriteLine($"Уведомление отправлено пользователю {userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось отправить {userId}: {ex.Message}");
        }
    }
    ev.Notified = true; // отметка, что уведомление уже отправлено
}

string GetPath(string dockerPath, string macPath)
{
    if (File.Exists(dockerPath))
    {
        Console.WriteLine($"✅ Использую Docker-путь: {dockerPath}");
        return dockerPath;
    }

    if (File.Exists(macPath))
    {
        Console.WriteLine($"✅ Использую Mac-путь: {macPath}");
        return macPath;
    }

    Console.WriteLine("⚠️ Файл не найден ни по одному пути!");
    return string.Empty; // или можно вернуть дефолтный путь
}

//3️⃣ Таймер проверки событий
//Добавим задачу, которая работает в фоне и раз в минуту проверяет события:
_ = Task.Run(async () =>
{
    // var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            // Берём текущее московское время
            // var nowMoscow = TimeZoneInfo.ConvertTime(DateTime.UtcNow, moscowTimeZone);
            // Берём текущее московское время как UTC+3
            var nowMoscow = DateTime.UtcNow.AddHours(3);

            foreach (var kvp in events)
            {
                var ev = kvp.Value;

                // Если уведомление уже отправлено — пропускаем
                if (ev.Notified) continue;

                // Парсим время события
                if (!TimeSpan.TryParse(ev.Time, out var eventTime)) continue;
                Console.WriteLine(!TimeSpan.TryParse(ev.Time, out var eventTime1));

                // Формируем дату события по московскому времени
                var eventDateTime = new DateTime(nowMoscow.Year, nowMoscow.Month, nowMoscow.Day,
                                                 eventTime.Hours, eventTime.Minutes, 0);
                Console.WriteLine(eventDateTime);

                // Если событие уже прошло — пропускаем
                if (nowMoscow > eventDateTime) continue;

                // Если до события <= 1 минуты — уведомляем
                var minutesToEvent = (eventDateTime - nowMoscow).TotalMinutes;
                if (minutesToEvent <= 3 && minutesToEvent > 0)
                {
                    string message = $"⚽ Напоминание! Селева в {ev.Time} через 3 минуту!";
                    await NotifySubscribers(ev, message);
                    Console.WriteLine($"Уведомление отправлено для события {ev.Time}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в таймере уведомлений: {ex}");
        }

        // Проверяем каждые 5 секунд, чтобы не пропустить момент
        await Task.Delay(6_0000, cts.Token);
    }
});



// ---------------------------
// Запуск бота: получаем информацию о боте и стартуем получение апдейтов
// ---------------------------
// Важно: StartReceiving вызываем после того, как все переменные (events, обработчики) уже объявлены/инициализированы.
var me = await bot.GetMe();
bot.StartReceiving(UpdateHandler, ErrorHandler, cancellationToken: cts.Token);

Console.WriteLine($"Бот запущен: @{me.Username}");
// Держим приложение оживлённым, пока не придёт сигнал отмены
await Task.Delay(Timeout.Infinite, cts.Token);

class EventData
{
    public long ChatId { get; set; }
    public string Time { get; set; } = "";
    public int MessageId { get; set; }

    public HashSet<long> SubscriberIds { get; set; } = new();
    public HashSet<long> UnsubscriberIds { get; set; } = new();

    // Словари ID -> имя для отображения в сообщении
    public Dictionary<long, string> SubscriberNames { get; set; } = new();
    public Dictionary<long, string> UnsubscriberNames { get; set; } = new();

    public bool Notified { get; set; } = false;
}
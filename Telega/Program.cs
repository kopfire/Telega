using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telega.Helpers.JSON;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telega
{
    class Program
    {

        static async Task<string> PostURI(Uri u, HttpContent c)
        {
            var response = string.Empty;
            using (var client = new HttpClient())
            {
                HttpResponseMessage result = await client.PostAsync(u, c);
                if (result.IsSuccessStatusCode)
                {
                    response = result.StatusCode.ToString();
                }
            }
            return response;
        }

        static readonly HttpClient httpClient = new HttpClient();


        static int count = 0;

        static void Main(string[] args)
        {
            var botClient = new TelegramBotClient("1837593586:AAGJCGUa3LY9U05r_h8iI-1ZUM91njSzLkI");
            using var cts = new CancellationTokenSource();
            botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
    cts.Token);
            Console.ReadLine();

            Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(ErrorMessage);
                return Task.CompletedTask;
            }

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                if (update.Type != UpdateType.Message)
                    return;
                if (update.Message.Type != MessageType.Text)
                    return;

                var chatId = update.Message.Chat.Id;

                
                Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatId} {update.Message.Chat.FirstName}.");

                Message message = update.Message;

                if (message.Text == "Да." && count > 10) 
                {
                    await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: update.Message.Chat.FirstName +  ", хватит спамить!!");                    
                }
                else if (message.Text == "Да.")
                {
                    count++;
                    using (var stream = System.IO.File.OpenRead("/path/to/voice-nfl_commentary.ogg"))
                    {
                        await botClient.SendVoiceAsync(
                          chatId: chatId,
                          voice: stream,
                          duration: 2
                        );

                    }
                }
                else if (message.Text == "/check")
                {
                    var jsonPost = new JsonPost { Command = "check", User = message.Chat.Id };

                    var jsonString = JsonSerializer.Serialize(jsonPost);
                    var data = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    var url = "http://localhost:5000/api/telegram/message";
                    using var client = new HttpClient();

                    var response = await client.PostAsync(url, data);
                    if (response.Content != null)
                    {
                        DateTime dayNow = DateTime.Today;
                        var cal = new GregorianCalendar();
                        var weekNumber = cal.GetWeekOfYear(dayNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
                        Console.WriteLine(dayNow.DayOfWeek);
                        Console.WriteLine(weekNumber);
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseContent);

                        var timeTable = JsonSerializer.Deserialize<TimeTable>(responseContent);

                        Console.WriteLine(timeTable);

                        Console.WriteLine(timeTable.Weeks.Length);
                        int c = 0;
                        foreach (Week week in timeTable.Weeks)
                        {
                            if (week.Number == weekNumber % 2)
                            {
                                Console.WriteLine(week.Number);
                                Console.WriteLine(week.Days);
                                foreach (Day day in week.Days)
                                {
                                    Console.WriteLine(day.Name);
                                    Console.WriteLine(dayNow.DayOfWeek);
                                    if (day.Name.Equals(dayNow.DayOfWeek.ToString()))
                                    {
                                        string responseMessage = "";
                                        foreach (Lesson lesson in day.Lessons)
                                        {
                                            responseMessage += lesson.Number + " пара\n";
                                            responseMessage += lesson.Name + "\n";
                                            responseMessage += lesson.Teacher + "\n";
                                            responseMessage += lesson.Audience + "\n\n";
                                        }

                                        c = 1;
                                        await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: responseMessage);
                                    }
                                }
                            }
                        }
                        if (c == 0)
                        {
                            await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: "Завтра нет пар!");
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "lol");
                    }
                    
                    
                }
                else
                {
                    
                    
                }
            }
        }
    }
}

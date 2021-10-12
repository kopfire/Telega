﻿using System;
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
            using (TaskService ts = new TaskService())
            {
                // Create a new task definition and assign properties
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Does something";

                // Create a trigger that will fire the task at this time every other day
                td.Triggers.Add(new DailyTrigger { DaysInterval = 2 });

                // Create an action that will launch Notepad whenever the trigger fires
                td.Actions.Add(new ExecAction("notepad.exe", "c:\\test.log", null));

                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(@"Test", td);

                // Remove the task we just created
                ts.RootFolder.DeleteTask("Test");
            }
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
                else if (message.Text == "/checktoday")
                {
                    var jsonPost = new JsonPost { Command = "checktoday", User = message.Chat.Id };

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
                        var responseContent = await response.Content.ReadAsStringAsync();

                        var timeTable = JsonSerializer.Deserialize<TimeTable>(responseContent);

                        int c = 0;
                        foreach (Week week in timeTable.Weeks)
                        {
                            if (week.Number == weekNumber % 2)
                            {
                                foreach (Day day in week.Days)
                                {
                                    if (day.Name.Equals(dayNow.DayOfWeek.ToString()))
                                    {
                                        string responseMessage = "*Расписание на сегодня:\n\n";
                                        foreach (Lesson lesson in day.Lessons)
                                        {
                                            responseMessage += lesson.Number + " пара*\n";
                                            responseMessage += lesson.Name + " - ";
                                            responseMessage += lesson.Teacher + "\nАудитория - ";
                                            responseMessage += lesson.Audience + "\n\n";
                                        }

                                        c = 1;
                                        await botClient.SendTextMessageAsync(
                                            parseMode: ParseMode.Markdown,
                                            chatId: chatId,
                                            text: responseMessage, 
                                            replyMarkup: new ReplyKeyboardRemove());
                                    }
                                }
                            }
                        }
                        if (c == 0)
                        {
                            await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: "Сегодня нет пар!");
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "lol");
                    }
                    
                    
                }
                else if (message.Text == "/checktomorrow")
                {
                    var jsonPost = new JsonPost { Command = "checkTomorrow", User = message.Chat.Id };

                    var jsonString = JsonSerializer.Serialize(jsonPost);
                    var data = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    var url = "http://localhost:5000/api/telegram/message";
                    using var client = new HttpClient();

                    var response = await client.PostAsync(url, data);
                    if (response.Content != null)
                    {
                        DateTime dayNow = DateTime.Today.AddDays(1);
                        var cal = new GregorianCalendar();
                        var weekNumber = cal.GetWeekOfYear(dayNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        var timeTable = JsonSerializer.Deserialize<TimeTable>(responseContent);

                        int c = 0;
                        foreach (Week week in timeTable.Weeks)
                        {
                            if (week.Number == weekNumber % 2)
                            {
                                foreach (Day day in week.Days)
                                {
                                    if (day.Name.Equals(dayNow.DayOfWeek.ToString()))
                                    {
                                        string responseMessage = "*Расписание на завтра:\n\n";
                                        foreach (Lesson lesson in day.Lessons)
                                        {
                                            responseMessage += lesson.Number + " пара*\n";
                                            responseMessage += lesson.Name + " - ";
                                            responseMessage += lesson.Teacher + "\nАудитория - ";
                                            responseMessage += lesson.Audience + "\n\n";
                                        }

                                        c = 1;
                                        await botClient.SendTextMessageAsync(
                                            parseMode: ParseMode.Markdown,
                                            chatId: chatId,
                                            text: responseMessage,
                                            replyMarkup: new ReplyKeyboardRemove());
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

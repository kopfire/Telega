﻿using System;
using System.Collections.Generic;
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

        static void Main(string[] args)
        {
            Dictionary<int, string> days = new Dictionary<int, string>(6);
            days.Add(1, "Понедельник");
            days.Add(2, "Вторник");
            days.Add(3, "Среда");
            days.Add(4, "Четверг");
            days.Add(5, "Пятница");
            days.Add(6, "Суббота");

            var botClient = new TelegramBotClient("1837593586:AAElIgx9Anhpm3tz58zjGNlwO0zcsBxTNdY");
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

                if (message.Text == "/check")
                {
                    var jsonPost = new JsonPost { Command = "checktoday", User = message.Chat.Id };

                    var jsonString = JsonSerializer.Serialize(jsonPost);
                    var data = new StringContent(jsonString, Encoding.UTF8, "application/json");
                    var url = "http://localhost:5000/api/telegram/message";
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add();
                    var response = await client.GetAsync(url);

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri("some url"),
                        Content = new StringContent("some json", Encoding.UTF8, ContentType.Json),
                    };

                    var response = await client.SendAsync(request).ConfigureAwait(false);

                    int lol = 0;
                    Console.WriteLine(response);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                    if (!responseContent.Equals("null"))
                    {
                        DateTime dayNow = DateTime.Today;
                        var cal = new GregorianCalendar();
                        var weekNumber = cal.GetWeekOfYear(dayNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);

                        var timeTable = JsonSerializer.Deserialize<TimeTable>(responseContent);
                        foreach (Week week in timeTable.Weeks)
                        {
                            if (week.Number == weekNumber % 2)
                            {
                                string responseMessage = "*Расписание на неделю:*";
                                await botClient.SendTextMessageAsync(
                                    parseMode: ParseMode.Markdown,
                                    chatId: chatId,
                                    text: responseMessage,
                                    replyMarkup: new ReplyKeyboardRemove());
                                foreach (Day day in week.Days)
                                {
                                    responseMessage = "";
                                    responseMessage += "*" + days[day.Number] + "*\n\n";
                                    lol = 0;
                                    foreach (Lesson lesson in day.Lessons)
                                    {
                                        lol++;
                                        responseMessage += "*" + lesson.Number + " пара*\n";
                                        responseMessage += lesson.Name + " - ";
                                        responseMessage += lesson.Teacher + "\nАудитория - ";
                                        responseMessage += lesson.Audience + "\n\n";
                                    }
                                    if (lol != 0)
                                    {
                                        await botClient.SendTextMessageAsync(
                                            parseMode: ParseMode.Markdown,
                                            chatId: chatId,
                                            text: responseMessage,
                                            replyMarkup: new ReplyKeyboardRemove());
                                    }

                                }
                            }
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вашего расписания нет в базе данных");
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
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                    if (responseContent != null)
                    {
                        DateTime dayNow = DateTime.Today;
                        var cal = new GregorianCalendar();
                        var weekNumber = cal.GetWeekOfYear(dayNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);

                        var timeTable = JsonSerializer.Deserialize<TimeTable>(responseContent);

                        int c = 0;
                        foreach (Week week in timeTable.Weeks)
                        {
                            if (week.Number == weekNumber % 2)
                            {
                                foreach (Day day in week.Days)
                                {
                                    if (day.Number == (int)dayNow.DayOfWeek)
                                    {
                                        string responseMessage = "*Расписание на сегодня:\n\n*";
                                        foreach (Lesson lesson in day.Lessons)
                                        {

                                            responseMessage += "*" + lesson.Number + " пара*\n";
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
                            text: "Вашего расписания нет в базе данных");
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

                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (!responseContent.Equals("null"))
                    {
                        DateTime dayNow = DateTime.Today.AddDays(1);
                        var cal = new GregorianCalendar();
                        var weekNumber = cal.GetWeekOfYear(dayNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);

                        var timeTable = JsonSerializer.Deserialize<TimeTable>(responseContent);
                        int c = 0;
                        foreach (Week week in timeTable.Weeks)
                        {
                            if (week.Number == weekNumber % 2)
                            {
                                foreach (Day day in week.Days)
                                {
                                    if (day.Number == (int)dayNow.DayOfWeek)
                                    {
                                        string responseMessage = "*Расписание на завтра:*\n\n";
                                        foreach (Lesson lesson in day.Lessons)
                                        {
                                            
                                            responseMessage += "*" + lesson.Number + " пара*\n";
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
                            text: "Вашего расписания нет в базе данных");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                                            parseMode: ParseMode.Markdown,
                                            chatId: chatId,
                                            text: "Используй команды из меню",
                                            replyMarkup: new ReplyKeyboardRemove());

                }
            }
        }
    }
}
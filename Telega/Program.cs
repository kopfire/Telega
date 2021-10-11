using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
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

                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                    new KeyboardButton[][]
                    {
                        new KeyboardButton[] { "Да.", "Нет.", KeyboardButton.WithRequestLocation("Хде я")},

                    })
                {
                    ResizeKeyboard = true
                };

                
                Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatId} {update.Message.Chat.FirstName}.");

                if (update.Message.Text == "Да." && count > 10) 
                {
                    await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: update.Message.Chat.FirstName +  ", хватит спамить!!",
                            replyMarkup: replyKeyboardMarkup);                    
                }
                else if (update.Message.Text == "Да.")
                {
                    count++;
                    using (var stream = System.IO.File.OpenRead("/path/to/voice-nfl_commentary.ogg"))
                    {
                        await botClient.SendVoiceAsync(
                          chatId: chatId,
                          voice: stream,
                          duration: 2,
                          replyMarkup: replyKeyboardMarkup
                        );

                    }
                }
                else if (update.Message.Text == "/check")
                {
                    var person = new Check { UserName = "Admin", Password = "Admin" };

                    var jsonString = JsonSerializer.Serialize<Check>(person);
                    var data = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    var url = "http://localhost:5000/api/user/login";
                    using var client = new HttpClient();

                    var response = await client.PostAsync(url, data);                

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "lol",
                        replyMarkup: replyKeyboardMarkup);
                }
                else
                {
                    
                    count = 0;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Да что ты говоришь!",
                        replyMarkup: replyKeyboardMarkup); 
                }
            }
        }
    }
}

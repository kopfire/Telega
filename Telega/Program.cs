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
                else if (update.Message.Text == "/start")
                {
                    string n1 = await PostURI("start", chatId);
                    Console.WriteLine(n1);

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

        static async Task<string> PostURI(string command, long chatId)
        {

            var jsonPost = new JsonPost { Command = command, User = chatId };
            var jsonString = JsonSerializer.Serialize<JsonPost>(jsonPost);
            Console.WriteLine(jsonString);


            var person = new Check { UserName = "Admin", Password = "Admin" };

            var jsonStringTest = JsonSerializer.Serialize<Check>(person);
            var data = new StringContent(jsonStringTest, Encoding.UTF8, "application/json");

            var url = "http://localhost:5000/api/user/login";
            using var client = new HttpClient();

            var response = await client.PostAsync(url, data);

            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
            Console.WriteLine(1);
            return response.Content.ReadAsStringAsync().Result;
        }
    }
}

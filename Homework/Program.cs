using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Newtonsoft.Json;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using Telegram.Bot.Types.InputFiles;

namespace Homework
{
    class Program
    {
        static string token = "2144777601:AAFiWU_uo47gRm62GiWugioo9rK_7TK8ebw";
        static TelegramBotClient client;

        /// <summary>
        /// Функциональные кнопки
        /// </summary>
        private static IReplyMarkup GetButtons()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>{new KeyboardButton { Text ="Показать список загруженных файлов"}, new KeyboardButton {Text="Показать список команд" } }
                }
            };
        }
        static void Main(string[] args)
        {
            //Создание клиента
            client = new TelegramBotClient(token);
            //Старт слушания входящих сообщений
            client.StartReceiving();
            //Реакция на входящие сообщения
            client.OnMessage += ReactionOnMessage;

            Console.ReadKey();
            client.StopReceiving();
        }
        /// <summary>
        /// Метод, реализовывающий реакцию телеграм бота на входящие сообщения
        /// </summary>
        static async void ReactionOnMessage(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            await client.SendTextMessageAsync(msg.Chat.Id, "Выберите функцию или загрузите файл", replyMarkup: GetButtons());
            await Task.Delay(10);
            //Выбор реакции бота, в зависимости от типа присылаемого сообщения
            switch (msg.Type)
            {
                case (Telegram.Bot.Types.Enums.MessageType.Text):
                    //Реакция бота на старт общения
                    if (msg.Text == "/start")
                        await client.SendTextMessageAsync(msg.Chat.Id, "Добро пожаловать в бота - файловое хранилище");
                    //Реакция бота на нажатие функциональной кнопки
                    if (msg.Text == "Показать список загруженных файлов")
                        FileInDirectory(client, msg.Chat.Id.ToString());
                    //Реакция бота на нажатие функциональной кнопки
                    if (msg.Text == "Показать список команд")
                        await client.SendTextMessageAsync(msg.Chat.Id, "/start - для начала общения с ботом\n" +
                            "/Download i - для скачивания файла, где i - его номер из списка загруженных файлов\n" +
                            "/DownloadAll - скачать все загруженные файлы");
                    //Реакция бота на команду скачать файл
                    if (msg.Text.StartsWith("/Download "))
                    {
                        //Обработка неккоректно введеного индекса
                        if (int.TryParse(msg.Text.Substring(10), out int i))
                            Download(client, msg.Chat.Id.ToString(), i);

                        else await client.SendTextMessageAsync(msg.Chat.Id, "Введите корректный индекс");
                    }
                    //Реакция бота на команду скачать все файлы
                    if (msg.Text == "/DownloadAll")
                        DownloadAll(client, msg.Chat.Id.ToString());
                    break;     
                    
                case (Telegram.Bot.Types.Enums.MessageType.Photo): 

                    await client.SendTextMessageAsync(msg.Chat.Id, "Найс фото!"); 
                    break;

                case (Telegram.Bot.Types.Enums.MessageType.Video): 

                    await client.SendTextMessageAsync(msg.Chat.Id, "Найс видео!"); 
                    break;
                //Реакция бота на присланный документ
                case (Telegram.Bot.Types.Enums.MessageType.Document):

                    UploadFile(client, msg.Chat.Id.ToString(), msg.Document.FileId, msg.Document.FileName);
                    break;

                default: break;
            }
        }
        /// <summary>
        /// Метод, реализующий отправку выбранного пользователем файла с сервера в чат
        /// </summary>
        /// <param name="client">Телеграм бот клиент</param>
        /// <param name="chatid">Id чата</param>
        /// <param name="i">Порядковый номер файла из списка</param>
        static async void Download(TelegramBotClient client, string chatid, int i)
        {
            var directory = Directory.CreateDirectory(chatid);
            var files = Directory.GetFiles(chatid);
            try
            {
                using (FileStream fs = new FileStream(files[i - 1], FileMode.Open))
                {
                    InputOnlineFile iof = new InputOnlineFile(fs);
                    iof.FileName = files[i - 1].Substring(10);
                    var send = await client.SendDocumentAsync(chatid, iof);
                }
            }
            //Обработка выхода номера фйла из диапазона существующих
            catch
            {
                await client.SendTextMessageAsync(chatid, "Введите номер из Вашего диапазона файлов");
            }
        }
        /// <summary>
        /// Метод, реализующий отправку всех файлов пользователя с сервера в чат
        /// </summary>
        /// <param name="client">Телеграм бот клиент</param>
        /// <param name="chatid">Id чата</param>
        static async void DownloadAll(TelegramBotClient client, string chatid)
        {
            CheckDirectory(chatid);
            var files = Directory.GetFiles(chatid.ToString());
            foreach (var f in files)
            {
                using (FileStream fs = new FileStream(f, FileMode.Open))
                {
                    InputOnlineFile iof = new InputOnlineFile(fs);
                    iof.FileName = f.Substring(10);
                    var send = await client.SendDocumentAsync(chatid, iof);
                    await Task.Delay(10);
                }
            }
        }
        /// <summary>
        /// Метод, реализующий скачку отправленного в чат файла пользователя на сервер
        /// </summary>
        /// <param name="client">Телеграм бот клиент</param>
        /// <param name="chatid">Id чата</param>
        /// <param name="fileid">Id файла</param>
        /// <param name="FileName">Имя файла</param>
        static async void UploadFile (TelegramBotClient client, string chatid, string fileid, string FileName)
        {
            var file = await client.GetFileAsync(fileid);
            CheckDirectory(chatid);
            using (FileStream fs = new FileStream(chatid+@"\"+FileName, FileMode.Create))
            {
                await client.DownloadFileAsync(file.FilePath, fs);
            }
        }
        /// <summary>
        /// Метод, выводящий список загруженных на сервер файлов пользователя
        /// </summary>
        /// <param name="client">Телеграм бот клиент</param>
        /// <param name="chatid">Id чата</param>
        static async void FileInDirectory(TelegramBotClient client, string chatid)
        {
            CheckDirectory(chatid);
            var files = Directory.GetFiles(chatid);
            int count = 1;
            foreach(var f in files)
            {
                await client.SendTextMessageAsync(chatid, $"{count}){f.Substring(11)}");
                count += 1;
                await Task.Delay(10);
            }
        }
        /// <summary>
        /// Проверка существования папки пользователя на сервере и создание ее в случае отсутствия
        /// </summary>
        /// <param name="chatid">Id пользователя</param>
        static void CheckDirectory(string chatid)
        {
            if (Directory.Exists(chatid) == false) Directory.CreateDirectory(chatid);
        }
    }
}
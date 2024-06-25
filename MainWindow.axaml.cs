using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using connect;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using models;

namespace broadcast_messenger_client_dotnet;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; }
    public ObservableCollection<Message> Messages { 
        get { return _messages ?? (_messages = new ObservableCollection<Message>());}
    }
    private ObservableCollection<Message> _messages;
    public ObservableCollection<User> Users { 
        get { return _users ?? (_users = new ObservableCollection<User>()); }
    }
    private ObservableCollection<User> _users;

    public Message message { get; set; }
    public User user {get; set;}
    public User SelectedUser { get; set; }

    public void AppendUserInUserList(string username)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            //Console.WriteLine($"AppendFunc {username}");
            bool inList = false;
            int i = 0;
            //Console.WriteLine($"len UsersList {Users.Count}");
            // Исправлено условие для корректной проверки в пределах существующих пользователей
            while (i < Users.Count && !inList) {
                if (Users[i].Username == username){
                    inList = true;
                }
                i++;
            }
            if (inList) {
                //Console.WriteLine("InList");
                Users[i-1].isOnline = true;
            } else {
                //Console.WriteLine("AddToList");
                // Добавление нового пользователя и обновление UI
                User newUser = new User(username, true);
                Users.Add(newUser);
                //UsersScroller.ScrollToEnd();
                // Обновление списка пользователей в UI
                UsersList.Items.Add(newUser);

            }
        });
    }

    public void DeleteUserFromUserList(string username)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            bool inList = false;
            int i = 0;
            while (!inList || i < Users.Count) {
                if (Users[i].Username == username){
                    inList = true;
                }
                i++;
            }
            if(inList) Users[i-1].isOnline = false;
        });
    }

    public void AppendChatMessage(string message, string username)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            User? foundUser = Users.FirstOrDefault(user => user.Username == username);
            if (foundUser != null)
            {
                //Chat.Text += $"[{username}]:\n{message}\n";
                foundUser.AddInHistory($"[{username}]:\n{message}\n");
            }
            //Chat.Text += message + Environment.NewLine;
            ChatScroller.ScrollToEnd();
        });
    }

    public async void ClickSendButtonHandler(object? sender, RoutedEventArgs args) {
        if (Chat.Text.Equals("Введите юзернейм") && !string.IsNullOrWhiteSpace(Message.Text))
        {
            UdpTcpClient.Username = Message.Text;
            Message.Text = "";
            Chat.Text = "";
            Message.IsEnabled = false;
            SendButton.IsEnabled = false;
        }
        else if (!string.IsNullOrWhiteSpace(Message.Text) && SelectedUser != null && SelectedUser.isOnline)
        {
            string messageToSend = Message.Text;
            Chat.Text += $"[ВЫ]:\n{messageToSend}\n";
            SelectedUser.AddInHistory($"[ВЫ]:\n{messageToSend}\n");
            Console.WriteLine($"Отправка сообщения '{messageToSend}' пользователю {SelectedUser.Username}");
            await Program.client.SendMessageToUserByUsername(messageToSend, SelectedUser.Username);
            Message.Text = ""; // Очистка поля ввода после отправки
            ChatScroller.ScrollToEnd(); // Прокрутка чата вниз
        }
    }

    public async void ClickSendFileButtonHandler(object? sender, RoutedEventArgs args) {
        var storageProvider = this.StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            AllowMultiple = false
        });
        if (result != null && result.Count > 0 && SelectedUser != null && SelectedUser.isOnline) {
            string selectedFilePath = result[0].Path.LocalPath;
            Console.WriteLine($"Выбранный файл: {selectedFilePath}");
            string mes = $"[ВЫ]:\nФайлик!!!{selectedFilePath}\n";
            SelectedUser.AddInHistory(mes);
            await Program.client.SendFileToUserByUsername(selectedFilePath, SelectedUser.Username);
        }
    }

    public void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersList.SelectedItem is User selectedUser)
        {
            SelectedUser = selectedUser; // Сохраняем выбранного пользователя
            UsernameDialog.Content = selectedUser.Username; // Обновление Label с именем пользователя
            UpdateChatForSelectedUser(selectedUser); // Обновление чата для выбранного пользователя
            SendButton.IsEnabled = selectedUser.isOnline; // Активация кнопки отправки, если пользователь онлайн
            Message.IsEnabled = selectedUser.isOnline; // Активация поля ввода сообщения, если пользователь онлайн
            SendFileButton.IsEnabled = true;
        }
    }

    private void UpdateChatForSelectedUser(User user)
    {
        // Здесь должна быть логика для обновления TextBlock `Chat` на основе выбранного пользователя
        // Например, вы можете загрузить историю сообщений для этого пользователя
        Chat.Text = user.History;
        // Добавьте реальные сообщения, если они есть
    }

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        UsersList.ItemsSource = Users;
        SendButton.IsDefault=true;
        SendFileButton.IsDefault=false;
        Chat.Text = "Введите юзернейм";
    }

}
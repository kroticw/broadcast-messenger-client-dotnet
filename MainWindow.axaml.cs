using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

    public void AppendChatMessage(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            Chat.Text += message + Environment.NewLine;
            ChatScroller.ScrollToEnd();
        });
    }

    public async void ClickSendButtonHandler(object? sender, RoutedEventArgs args) {
        if (Chat.Text.Equals("Введите юзернейм") && !Message.Text.Equals(""))
        {
            UdpTcpClient.Username = Message.Text;
            Message.Text = "";
            Chat.Text = "";
            Message.IsEnabled = false;
            SendButton.IsEnabled = false;
        }
        else if (Message.Text.Equals(""))
        {
            string ?mes = Message.Text;
            Chat.Text += $"[ВЫ]:\n{mes}\n";
            SelectedUser.AddInHistory($"[ВЫ]:\n{mes}\n");
            Console.WriteLine($"Нажатие кнопки отправления сообщения {mes} юзеру {SelectedUser.Username}");
            await Program.client.SendMessageToUserByUsername(mes, SelectedUser.Username);
            Message.Text = "";
            ChatScroller.ScrollToEnd();
        }
    }

    public void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersList.SelectedItem is User selectedUser)
        {
            UsernameDialog.Content = selectedUser.Username; // Обновление Label с именем пользователя
            UpdateChatForSelectedUser(selectedUser); // Обновление чата для выбранного пользователя
            if(!selectedUser.isOnline)
            {
                SendButton.IsEnabled=false;
                Message.IsEnabled=false;
            }
            else 
            {
                SendButton.IsEnabled=true;
                Message.IsEnabled=true;
                UsersList.IsEnabled=true;
            }
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
        Chat.Text = "Введите юзернейм";
        UsersList.IsEnabled=false;
    }

}
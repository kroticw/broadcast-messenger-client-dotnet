using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
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
            bool inList = false;
            int i = 0;
            while (!inList || i < Users.Count) {
                if (Users[i].Username == username){
                    inList = true;
                }
                i++;
            }
            if(inList) Users[i-1].isOnline = true;
            else Users.Add(new User(username, true));
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
        if (Message.Text != "")
        {
            string ?mes = Message.Text;
            Chat.Text += $"[ВЫ]:\n{mes}\n";
            await Program.client.SendMessageToUserByUsername(mes, "asdas");
            Message.Text = "";
            ChatScroller.ScrollToEnd();
        }
    }

    public void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersList.SelectedItem is User selectedUser)
        {
            if(selectedUser.isOnline)
            {
                UsernameDialog.Content = selectedUser.Username; // Обновление Label с именем пользователя
                UpdateChatForSelectedUser(selectedUser); // Обновление чата для выбранного пользователя
            }
        }
    }

    private void UpdateChatForSelectedUser(User user)
    {
        // Здесь должна быть логика для обновления TextBlock `Chat` на основе выбранного пользователя
        // Например, вы можете загрузить историю сообщений для этого пользователя
        Chat.Text = $"История сообщений с {user.Username}:\n";
        // Добавьте реальные сообщения, если они есть
    }

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        UsersList.ItemsSource = Users;
        SendButton.IsDefault=true;
        Users.Add(new User("Igor", true));
        Users.Add(new User("Vadim", false));
    }

    public void SendMessage(string text, User sender)
    {
        var message = new Message { Sender = sender, Text = text, Timestamp = DateTime.Now };
        Messages.Add(message);
        // Обновление представления, если необходимо
    }

    public void AddUser(User user)
    {
        Users.Add(user);
    }
}
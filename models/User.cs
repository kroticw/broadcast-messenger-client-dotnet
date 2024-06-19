namespace models;

public class User
{
    public string Username { get; set; }
    public bool isOnline { get; set; }
    public string History { get; set; }

    public User(string username, bool isOnline) {
        Username = username;
        this.isOnline = isOnline;
        History = "";
    }

    public void AddInHistory(string message) {
        History += message;
    }
}
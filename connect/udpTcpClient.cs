using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using broadcast_messenger_client_dotnet;

namespace connect;

public class UdpTcpClient
{
    private UdpClient udpClient;
    private TcpClient tcpClient;
    private TcpListener tcpListener;
    public NetworkStream tcpStream;
    private int tcpPort = 8888; // TCP порт для прослушивания
    private int udpPort = 8889; // UDP порт для broadcast

    public UdpTcpClient()
    {
        udpClient = new UdpClient();
        tcpListener = new TcpListener(IPAddress.Any, tcpPort);
        tcpListener.Start();
    }

    public async Task StartAsync()
    {
        Task broadcastTask = BroadcastConnectionInfo();
        Task<TcpClient> acceptTask = tcpListener.AcceptTcpClientAsync();

        await Task.WhenAny(broadcastTask, acceptTask);

        if (acceptTask.IsCompleted)
        {
            tcpClient = acceptTask.Result;
            udpClient.Close(); // Закрытие UDP порта после установления TCP соединения
            Console.WriteLine("TCP connection established.");
            tcpStream = tcpClient.GetStream();
            await HandleTcpConnectionAsync();
        }
    }

    private async Task BroadcastConnectionInfo()
    {
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Broadcast, udpPort);
        byte[] data = Encoding.UTF8.GetBytes($"CONNECT:{Program.SelfUsername}:{tcpPort}");
        udpClient.EnableBroadcast = true;

        while (!tcpListener.Pending())
        {
            udpClient.Send(data, data.Length, ipEndPoint);
            Console.WriteLine("Broadcasting connection info...");
            await Task.Delay(1000); // Пауза в 1 секунду между отправками
        }
    }

    private void ParseAndAction(string receivedMessage) {
        string[] message = receivedMessage.Split(";;;");
        if(message[0] == "server" && message[1].Length > 2){
            switch (message[1][..2])
            {
                case "nu": //новый пользователь
                    MainWindow.Instance.AppendUserInUserList(message[0]);
                    break;
                case "du": //пользователь отключился
                    MainWindow.Instance.DeleteUserFromUserList(message[0]);
                    break;
                default:
                    MainWindow.Instance.AppendChatMessage($"[{message[0]}]:\n{message[1]}");
                    break;
            }
        }
        else MainWindow.Instance.AppendChatMessage($"[{message[0]}]:\n{message[1]}");
    }

    private async Task HandleTcpConnectionAsync()
    {
        try
        {
            while(true) {
                byte[] buffer = new byte[1024];
                int n = await tcpStream.ReadAsync(buffer);
                if (n > 0) {
                    if (buffer[0] == 0) continue;
                    int newInt = Array.IndexOf(buffer, (byte)0);
                    Console.WriteLine($"n: {newInt}");
                    if (newInt != -1) Array.Resize(ref buffer, newInt);
                    string receivedMessage = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine($"Received: {receivedMessage}");
                    ParseAndAction(receivedMessage);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP error: {ex.Message}");
        }
        finally
        {
            RestartConnection();
        }
    }

    private async void RestartConnection()
    {
        tcpClient.Close();
        tcpListener.Stop();
        Console.WriteLine("TCP connection lost. Restarting UDP broadcast...");
        await StartAsync(); // Перезапуск процесса для нового соединения
    }

    public async Task SendMessageToUserByUsername(string message, string username)
    {
        if (tcpClient == null || !tcpClient.Connected)
        {
            Console.WriteLine("TCP клиент не подключен.");
            return;
        }
        try{
            string mes = $"{Program.SelfUsername};;;{username};;;{message}";
            byte[] data = Encoding.UTF8.GetBytes(mes);
            await tcpStream.WriteAsync(data, 0, data.Length);
            Console.WriteLine("Сообщение отправлено: " + mes);
        }
        catch(Exception ex)
        {
            Console.WriteLine("Ошибка при отправке сообщения: " + ex.Message);
        }
    }
}
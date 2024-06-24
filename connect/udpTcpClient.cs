using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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
        udpClient.EnableBroadcast = true;

        while(Program.SelfUsername == "") {}

        Client client = new Client();
        string host = Dns.GetHostName();
        IPHostEntry ip = Dns.GetHostEntry(host);
        client.ClientIp = ip.AddressList[5].ToString();
        client.ClientPort = tcpPort.ToString();
        string json = JsonSerializer.Serialize(client);
        byte[] data = Encoding.UTF8.GetBytes(json);

        while (!tcpListener.Pending())
        {
            udpClient.Send(data, data.Length, ipEndPoint);
            Console.WriteLine("Broadcasting connection info...");
            await Task.Delay(1000); // Пауза в 1 секунду между отправками
        }
    }

    private void ParseAndAction(ClientServerMessage receivedMessage) {
        if(receivedMessage.from.Equals("server")){
            if(receivedMessage.serviceType.Equals("new_user") && !receivedMessage.serviceData.Equals("")) {
                MainWindow.Instance.AppendUserInUserList(receivedMessage.serviceData);
            } else if (receivedMessage.serviceType.Equals("del_user") && !receivedMessage.serviceData.Equals("")) {
                MainWindow.Instance.DeleteUserFromUserList(receivedMessage.serviceData);
            }
        } else if (!receivedMessage.message.Equals("")) {
            MainWindow.Instance.AppendChatMessage($"[{receivedMessage.from}]:\n{receivedMessage.message}");
        }
    }

    private async Task HandleTcpConnectionAsync()
    {
        //Console.WriteLine("HandleTcpConnectionAsync");
        try
        {
            while(true) {
                byte[] size = new byte[4];
                int n = await tcpStream.ReadAsync(size);
                if (n > 0) {
                    int sizeInInt = BitConverter.ToInt32(size, 0);
                    byte[] buffer = new byte[sizeInInt];

                    n = await tcpStream.ReadAsync(buffer);
                    if (buffer[0] == 0) continue;

                    int newInt = Array.IndexOf(buffer, (byte)0);
                    Console.WriteLine($"n: {newInt}");

                    if (newInt != -1) Array.Resize(ref buffer, newInt);

                    string receivedMessage = Encoding.UTF8.GetString(buffer);
                    ClientServerMessage? message = JsonSerializer.Deserialize<ClientServerMessage>(buffer);

                    Console.WriteLine($"Received: {receivedMessage}");

                    ClientServerMessage mes = new ClientServerMessage();
                    mes.from = Client.Username;
                    mes.to = "2";
                    // if (message != null) {
                    //     ParseAndAction(message);
                    // }
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
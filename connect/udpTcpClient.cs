using System;
using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using broadcast_messenger_client_dotnet;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace connect;

public class UdpTcpClient
{
    private UdpClient udpClient;
    private TcpClient tcpClient;
    private TcpListener tcpListener;
    public NetworkStream tcpStream;
    private int tcpPort = 8888; // TCP порт для прослушивания
    private int udpPort = 8889; // UDP порт для broadcast
    Client client = new Client();

    public static string Username = "";

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
        Console.WriteLine("111");
        while(Username.Equals("")) {}
        Console.WriteLine($"Username is Set; Username {client.Username}");
        
        string host = Dns.GetHostName();
        IPHostEntry ip = Dns.GetHostEntry(host);
        string IP="";
        foreach (IPAddress ip1 in ip.AddressList){
            if (ip1.ToString().Contains("192.168")){
                IP = ip1.ToString();
            }
        }
        
        client.Username = Username;
        client.ClientIp = IP;
        client.ClientPort = tcpPort.ToString();
        string json = System.Text.Json.JsonSerializer.Serialize(client);
        Console.WriteLine(json);
        byte[] data = Encoding.UTF8.GetBytes(json);

        while (!tcpListener.Pending())
        {
            udpClient.Send(data, data.Length, ipEndPoint);
            Console.WriteLine("Broadcasting connection info...");
            await Task.Delay(1000); // Пауза в 1 секунду между отправками
        }
    }

    //byte[] buff = new byte[65536];

    private async Task ReceiveFile(string filename, string filelenght, string type) {
        using var file = File.Create("file." + type);

        byte[] buf = new byte[int.Parse(filelenght)];
        await tcpStream.ReadAsync(buf);
        await file.WriteAsync(buf);
    }


    private async Task ParseAndAction(ClientServerMessage receivedMessage) {
        //Console.WriteLine($"{receivedMessage.from} {receivedMessage.to} {receivedMessage.serviceType} {receivedMessage.serviceData}");
        if(string.Compare(receivedMessage.from, "server") == 0){
            //Console.WriteLine("SystemMessage");
            if(string.Compare(receivedMessage.serviceType, "new_user") == 0) {
                //Console.WriteLine(receivedMessage.serviceData);
                MainWindow.Instance.AppendUserInUserList(receivedMessage.serviceData);
            } else if (string.Compare(receivedMessage.serviceType, "del_user") == 0) {
                MainWindow.Instance.DeleteUserFromUserList(receivedMessage.serviceData);
            }
        } else if (string.Compare(receivedMessage.serviceType, "file") == 0) {
                await ReceiveFile(receivedMessage.serviceType, receivedMessage.serviceData, receivedMessage.message);
        } else {
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
                int n = tcpStream.Read(size); //считываем длину сообщения
                if (n > 0) {
                    if (n == 1) continue;
                    Console.WriteLine($"n {n}"); 
                    int sizeInInt = BitConverter.ToInt32(size, 0);
                    Console.WriteLine($"sizeInInt: {sizeInInt}");
                    byte[] buffer = new byte[sizeInInt];

                    n = await tcpStream.ReadAsync(buffer); //считываем сообщение
                    if (buffer[0] == (byte)0) continue;
                    
                    Console.WriteLine($"next n {n}");
                    int newInt = Array.IndexOf(buffer, (byte)0);
                    Console.WriteLine($"newInt: {newInt}");
                    
                    if (newInt != -1) continue;
                    //     Array.Resize(ref buffer, newInt+1);

                    string receivedMessage = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine(receivedMessage);
                    
                    ClientServerMessage? message = JsonConvert.DeserializeObject<ClientServerMessage>(receivedMessage);
                    
                    if (message.from != "") {
                        await ParseAndAction(message);
                    }
                }
                await Task.Delay(200);
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
            ClientServerMessage mesObj = new ClientServerMessage
            {
                from = client.Username,
                to = username,
                message = message
            };
            string mesJson = JsonConvert.SerializeObject(mesObj);
            byte[] data = Encoding.UTF8.GetBytes(mesJson);
            await tcpStream.WriteAsync(data, 0, data.Length);
            Console.WriteLine("Сообщение отправлено: " + mesJson);
        }
        catch(Exception ex)
        {
            Console.WriteLine("Ошибка при отправке сообщения: " + ex.Message);
        }
    }

    public async Task SendFileToUserByUsername(string filePath, string username)
    {
        if (tcpClient == null || !tcpClient.Connected)
        {
            Console.WriteLine("TCP клиент не подключен.");
            return;
        } 
        try {
            using var file = File.OpenRead(filePath);
            var lengthFile = file.Length;
            ClientServerMessage mesObj = new ClientServerMessage
            {
                from = client.Username,
                to = username,
                serviceType = "file",
                serviceData = lengthFile.ToString(),
                message = filePath.Substring(filePath.Length - 3),
            };
            string mesJson = JsonConvert.SerializeObject(mesObj);
            byte[] data = Encoding.UTF8.GetBytes(mesJson);
            await tcpStream.WriteAsync(data, 0, data.Length);
            Console.WriteLine("Сообщение отправлено: " + mesJson);
            await Task.Delay(100);
            await file.CopyToAsync(tcpStream);
        } catch(Exception ex)
        {
            Console.WriteLine("Ошибка при отправке файла: " + ex.Message);
        }
    }
}
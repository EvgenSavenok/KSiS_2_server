using System.Net;
using System.Net.Sockets;
using System.Text;
class Server
{
    private int nextClientID = 0;
    Dictionary<int, Socket> clientsSockets = new Dictionary<int, Socket>();
    Dictionary<int, string> clientsNames = new Dictionary<int, string>();
    Dictionary<int, int> localModeClients = new Dictionary<int, int>();
    private IPEndPoint? GetData()
    {
        int port;
        bool isCorrect = true;
        IPEndPoint localEndPoint = null;
        do
        {
            isCorrect = true;
            try
            { 
                Console.WriteLine("Введите номер порта:");
                port = Int32.Parse(Console.ReadLine());
                if (port < 1025)
                    isCorrect = false;
                Console.WriteLine("Введите IP адрес сервера:");
                IPAddress ipAddress = IPAddress.Parse(Console.ReadLine());
                localEndPoint = new IPEndPoint(ipAddress, port);
            }
            catch (SocketException)
            {
                isCorrect = false;  
            }
            if (!isCorrect)
                Console.WriteLine("Номер порта или IP некорректны!\n" +
                    "Попробуйте еще раз.");
        }
        while (!isCorrect);
        return localEndPoint;
    }
    private Socket? TryConnectToPort(IPEndPoint? localEndPoint)
    {
        Socket listener = null;
        try
        {
            listener = new Socket(localEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine($"Порт {localEndPoint.Port} успешно подключен");
        }
        catch (SocketException e)
        {
            Console.WriteLine(e.ToString());
        }
        return listener;
    }
    void SendToAllClients(byte[] message, int excludingClientID = -1)
    {
        foreach (var pair in clientsSockets)
        {
            if (pair.Key != excludingClientID)
                pair.Value.Send(message);
        }
    }
    private void StartListening(Socket listener)
    {
        while (true) 
        {
            Socket handler = listener.Accept();
            int clientID = nextClientID++;
            clientsSockets.Add(clientID, handler);
            byte[] nameBuffer = new byte[1024];
            int nameBytesRec = handler.Receive(nameBuffer);
            string clientName = Encoding.UTF8.GetString(nameBuffer, 0, nameBytesRec);
            clientsNames.Add(clientID, clientName);
            string connectMessage = $"Клиент {clientName} с id {clientID} подключился к чату";
            Console.WriteLine(connectMessage);
            string message = $"Пользователь {clientName} вошел в чат";
            byte[] msg = Encoding.UTF8.GetBytes(message);
            SendToAllClients(msg, clientID);
        }
    }
    private void TryToStartListening(IPEndPoint localEndPoint, Socket listener)
    {
        const int NUM_OF_BACKLOGS = 5;
        try 
        {
            listener.Bind(localEndPoint);
            listener.Listen(NUM_OF_BACKLOGS);
            Console.WriteLine("Ожидается подключение пользователей...");
            StartListening(listener);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        finally
        {
            Disconnect();
        }
    }
    public void StartWorkingOfServer()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("Добро пожаловать на сервер ChatSirinity!");
        IPEndPoint localEndPoint = GetData();
        Socket listener = TryConnectToPort(localEndPoint);
        TryToStartListening(localEndPoint, listener);
    }
    private void Disconnect()
    {
        /*foreach (var client in clients)
        {
           // client.Close(); 
        }*/ 
    }
}
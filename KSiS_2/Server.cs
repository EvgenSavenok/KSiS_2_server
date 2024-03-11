﻿using System.Net;
using System.Net.Sockets;
using System.Text;
class Server
{
    int nextClientID = 0;
    string prevMessage = null;
    bool isLocalChatStarted = false, isFirstMessage = true;
    Dictionary<int, Socket> clientsSockets = new Dictionary<int, Socket>();
    Dictionary<int, string> clientsNames = new Dictionary<int, string>();
    Dictionary<int, int> localChats = new Dictionary<int, int>();
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
    private void DisconnectClient(Socket clientSocket, int clientID, string clientName)
    {
        clientSocket.Shutdown(SocketShutdown.Both);
        clientSocket.Close();
        clientsSockets.Remove(clientID);
        clientsNames.Remove(clientID);
        Console.WriteLine($"Пользователь с ID {clientID} и именем {clientName} покинул чат.");
    }
    private void OutputTableOfUsers(string clientName, int clientID)
    {
        StringBuilder sb = new StringBuilder();
        Console.WriteLine($"Пользователь {clientName} с ID {clientID} запросил список всех пользователей.");
        sb.AppendLine("Список всех пользователей:");
        foreach (var line in clientsNames)
            sb.AppendLine($"Имя: {line.Value}, ID: {line.Key}");
        byte[] byteMessage = Encoding.UTF8.GetBytes(sb.ToString());
        clientsSockets[clientID].Send(byteMessage);
    }
    private void SendToClient(byte[] message, int clientID)
    {
        if (clientsSockets.ContainsKey(clientID))
        {
            clientsSockets[clientID].Send(message);
        }
    }
    private void StartLocalChatWithClient(string receiveMessage, Socket clientSocket, int clientID)
    {
        byte[] byteMessage;
        if (isFirstMessage)
        {
            byteMessage = Encoding.UTF8.GetBytes($"Вы вошли в чат с {clientsNames[Int32.Parse(receiveMessage)]}");
            SendToClient(byteMessage, clientID);
            byteMessage = Encoding.UTF8.GetBytes($"Пользователь {clientsNames[clientID]}" +
                $" пригласил Вас в чат.\nЕсли Вы хотите выйти из этого чата, наберите /exit.");
            SendToClient(byteMessage, localChats[clientID]);
            isFirstMessage = false;
            localChats.Add(Int32.Parse(receiveMessage), clientID);
        }
        if (receiveMessage.ToLower() == "/exit")
        {
            string exitChatMessage = $"Вы вышли из чата с {clientsNames[localChats[clientID]]}";
            byte[] exitChatMsg = Encoding.UTF8.GetBytes(exitChatMessage);
            SendToClient(exitChatMsg, clientID);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Клиент {clientsNames[clientID]} прервал с Вами общение.");
            sb.AppendLine($"Для продолжения общения с другими пользователями введите /1, для выхода из приложения введите /2.");
            exitChatMsg = Encoding.UTF8.GetBytes(sb.ToString());
            SendToClient(exitChatMsg, localChats[clientID]);
            int value = localChats[clientID];
            localChats.Remove(clientID);
            localChats.Remove(value);
            isLocalChatStarted = false;
            isFirstMessage = true;
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{clientsNames[clientID]}: ");
            sb.Append(receiveMessage);
            byteMessage = Encoding.UTF8.GetBytes(sb.ToString());
            SendToClient(byteMessage, localChats[clientID]);
        }
    }
    private void CheckMessage(string receivedMessage, Socket clientSocket, int clientID, string clientName)
    {
        if (receivedMessage == "/1")
        {
            OutputTableOfUsers(clientName, clientID);
            prevMessage = receivedMessage;
        }
        else if (receivedMessage == "/2")
        {
            DisconnectClient(clientSocket, clientID, clientName);
            return;
        }
        if ((prevMessage == "/1") && (receivedMessage != prevMessage))
        {
            try
            {
                if (clientsNames.ContainsKey(Int32.Parse(receivedMessage)) && (clientID != Int32.Parse(receivedMessage)))
                {
                    isLocalChatStarted = true;
                    localChats.Add(clientID, Int32.Parse(receivedMessage)); 
                }
                else
                {
                    string errorMessage = $"Вы не можете написать самому себе " +
                        $"или пользователя с таким ID не существует.";
                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                    SendToClient(errorBytes, clientID);
                }
            }
            catch (Exception) 
            {
                Console.WriteLine($"Клиент c ID {clientID} ввел некорректный ID.");
            }
        }
        if (isLocalChatStarted)
        {
            prevMessage = null;
            StartLocalChatWithClient(receivedMessage, clientSocket, clientID);
        }
    }
    private void HandleClient(Socket clientSocket, int clientID, string clientName)
    {
        try
        {
            while (true)
            {
                byte[] buffer = new byte[1024];
                int numOfBytes = clientSocket.Receive(buffer);
                if (numOfBytes == 0)
                {
                    DisconnectClient(clientSocket, clientID, clientName);
                    break;
                }
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, numOfBytes);
                Console.WriteLine($"Получено от клиента с ID {clientID}: {receivedMessage}");
                CheckMessage(receivedMessage, clientSocket, clientID, clientName);
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine(e.ToString());
        }
    }
    private void StartListening(Socket listener)
    {
        while (true) 
        {
            Socket clientSocket = listener.Accept();
            int clientID = nextClientID++;
            clientsSockets.Add(clientID, clientSocket);
            byte[] nameBuffer = new byte[1024];
            int nameBytesRec = clientSocket.Receive(nameBuffer);
            string clientName = Encoding.UTF8.GetString(nameBuffer, 0, nameBytesRec);
            clientsNames.Add(clientID, clientName);
            string connectMessage = $"Клиент {clientName} с ID {clientID} подключился к чату";
            Console.WriteLine(connectMessage);
            Task.Run(() => HandleClient(clientSocket, clientID, clientName));
        }
    }
    private void TryToStartListening(IPEndPoint localEndPoint, Socket listener)
    {
        const int NUM_OF_BACKLOGS = 20;
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
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

namespace ChatServer
{
    public partial class ServerForm : Form
    {
        private TcpListener clientListener;
        private TcpListener serverListener;
        private Thread clientThread;
        private Thread serverCommThread;
        private bool isRunning = false;
        private ListBox listBoxMessages;
        private Button buttonStart;
        private Button buttonStop;
        private Label labelStatus;
        
        // Клиенты
        private Dictionary<Socket, ClientInfo> connectedClients = new Dictionary<Socket, ClientInfo>();
        
        // Для алгоритма хулигана
        private int serverId;
        private int coordinatorId = -1;
        private bool isCoordinator = false;
        private bool isElectionInProgress = false;
        private bool receivedOk = false;
        
        private Dictionary<int, ServerConnection> serverConnections = new Dictionary<int, ServerConnection>();
        private List<int> allServerIds = new List<int>();
        
        private TextBox textBoxServerId;
        private TextBox textBoxServersList;
        private Button buttonStartElection;
        private Label labelCoordinator;
        
        private class ClientInfo
        {
            public string Nickname { get; set; }
            public NetworkStream Stream { get; set; }
        }
        
        private class ServerConnection
        {
            public TcpClient Client { get; set; }
            public NetworkStream Stream { get; set; }
            public int RemoteServerId { get; set; }
            public Thread ReceiveThread { get; set; }
        }

        public ServerForm()
        {
            InitializeComponent();
            Text = "Сервер чата с алгоритмом хулигана";
            Size = new System.Drawing.Size(800, 600);
        }

        private void InitializeComponent()
        {
            listBoxMessages = new ListBox();
            buttonStart = new Button();
            buttonStop = new Button();
            labelStatus = new Label();
            
            Label labelServerId = new Label();
            textBoxServerId = new TextBox();
            Label labelServers = new Label();
            textBoxServersList = new TextBox();
            buttonStartElection = new Button();
            labelCoordinator = new Label();

            // ListBox
            listBoxMessages.Location = new System.Drawing.Point(12, 150);
            listBoxMessages.Size = new System.Drawing.Size(760, 400);
            listBoxMessages.Font = new System.Drawing.Font("Consolas", 10);

            // Button Start
            buttonStart.Text = "Запустить сервер";
            buttonStart.Location = new System.Drawing.Point(12, 12);
            buttonStart.Size = new System.Drawing.Size(150, 30);
            buttonStart.Click += ButtonStart_Click;

            // Button Stop
            buttonStop.Text = "Остановить сервер";
            buttonStop.Location = new System.Drawing.Point(170, 12);
            buttonStop.Size = new System.Drawing.Size(150, 30);
            buttonStop.Enabled = false;
            buttonStop.Click += ButtonStop_Click;

            // Label Status
            labelStatus.Text = "Сервер остановлен";
            labelStatus.Location = new System.Drawing.Point(330, 17);
            labelStatus.Size = new System.Drawing.Size(150, 20);
            labelStatus.ForeColor = System.Drawing.Color.Red;

            // Server ID
            labelServerId.Text = "ID сервера:";
            labelServerId.Location = new System.Drawing.Point(12, 50);
            labelServerId.Size = new System.Drawing.Size(80, 20);
            
            textBoxServerId.Location = new System.Drawing.Point(95, 47);
            textBoxServerId.Size = new System.Drawing.Size(50, 20);
            textBoxServerId.Text = "0";
            
            // Servers list
            labelServers.Text = "Серверы (ID:порт):";
            labelServers.Location = new System.Drawing.Point(12, 80);
            labelServers.Size = new System.Drawing.Size(150, 20);
            
            textBoxServersList.Location = new System.Drawing.Point(170, 77);
            textBoxServersList.Size = new System.Drawing.Size(300, 20);
            textBoxServersList.Text = "0:9001,1:9002,2:9003";
            
            // Start Election button
            buttonStartElection.Text = "Начать выборы";
            buttonStartElection.Location = new System.Drawing.Point(480, 75);
            buttonStartElection.Size = new System.Drawing.Size(100, 25);
            buttonStartElection.Enabled = false;
            buttonStartElection.Click += ButtonStartElection_Click;
            
            // Coordinator label
            labelCoordinator.Text = "Координатор: неизвестен";
            labelCoordinator.Location = new System.Drawing.Point(12, 110);
            labelCoordinator.Size = new System.Drawing.Size(300, 20);
            labelCoordinator.ForeColor = System.Drawing.Color.Blue;

            // Form
            Controls.Add(listBoxMessages);
            Controls.Add(buttonStart);
            Controls.Add(buttonStop);
            Controls.Add(labelStatus);
            Controls.Add(labelServerId);
            Controls.Add(textBoxServerId);
            Controls.Add(labelServers);
            Controls.Add(textBoxServersList);
            Controls.Add(buttonStartElection);
            Controls.Add(labelCoordinator);
        }

        private void ButtonStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(textBoxServerId.Text, out serverId))
                {
                    MessageBox.Show("Введите корректный ID сервера");
                    return;
                }
                
                // Парсим список серверов
                ParseServersList();
                
                // Запускаем сервер для клиентов (порт 8888 + ID)
                int clientPort = 8888 + serverId;
                clientListener = new TcpListener(IPAddress.Any, clientPort);
                clientListener.Start();
                
                // Запускаем сервер для межсерверной коммуникации
                int serverPort = GetServerPort(serverId);
                serverListener = new TcpListener(IPAddress.Any, serverPort);
                serverListener.Start();
                
                isRunning = true;
                buttonStart.Enabled = false;
                buttonStop.Enabled = true;
                buttonStartElection.Enabled = true;
                labelStatus.Text = "Сервер запущен";
                labelStatus.ForeColor = System.Drawing.Color.Green;
                
                AddMessage($"Сервер ID {serverId} запущен");
                AddMessage($"Порт для клиентов: {clientPort}");
                AddMessage($"Порт для серверов: {serverPort}");
                AddMessage("");

                // Поток для приема клиентов
                clientThread = new Thread(AcceptClients);
                clientThread.IsBackground = true;
                clientThread.Start();
                
                // Поток для приема соединений от других серверов
                serverCommThread = new Thread(AcceptServers);
                serverCommThread.IsBackground = true;
                serverCommThread.Start();
                
                // Подключаемся к другим серверам
                Thread.Sleep(1000);
                ConnectToOtherServers();
                
                // Начинаем выборы
                Thread.Sleep(2000);
                StartElection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска сервера: {ex.Message}");
            }
        }

        private int GetServerPort(int id)
        {
            // Получаем порт для сервера из списка
            string[] servers = textBoxServersList.Text.Split(',');
            foreach (string server in servers)
            {
                string[] parts = server.Trim().Split(':');
                if (parts.Length >= 2 && int.Parse(parts[0]) == id)
                {
                    return int.Parse(parts[1]);
                }
            }
            return 9000 + id; // По умолчанию
        }

        private void ParseServersList()
        {
            allServerIds.Clear();
            string[] servers = textBoxServersList.Text.Split(',');
            foreach (string server in servers)
            {
                string[] parts = server.Trim().Split(':');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
                {
                    allServerIds.Add(id);
                }
            }
            allServerIds.Sort();
            AddMessage($"Все серверы в системе: {string.Join(", ", allServerIds)}");
        }

        private void AcceptServers()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = serverListener.AcceptTcpClient();
                    Thread handlerThread = new Thread(() => HandleIncomingServer(client));
                    handlerThread.IsBackground = true;
                    handlerThread.Start();
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        AddMessage($"Ошибка приема сервера: {ex.Message}");
                }
            }
        }

        private void HandleIncomingServer(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                
                // Получаем ID сервера
                byte[] idBuffer = new byte[4];
                int bytesRead = stream.Read(idBuffer, 0, 4);
                if (bytesRead == 4)
                {
                    int remoteServerId = BitConverter.ToInt32(idBuffer, 0);
                    
                    ServerConnection conn = new ServerConnection
                    {
                        Client = client,
                        Stream = stream,
                        RemoteServerId = remoteServerId,
                        ReceiveThread = new Thread(() => ReceiveFromServer(remoteServerId, stream))
                    };
                    
                    lock (serverConnections)
                    {
                        serverConnections[remoteServerId] = conn;
                    }
                    
                    AddMessage($"Сервер {remoteServerId} подключился к нам");
                    
                    conn.ReceiveThread.IsBackground = true;
                    conn.ReceiveThread.Start();
                }
            }
            catch (Exception ex)
            {
                AddMessage($"Ошибка обработки входящего сервера: {ex.Message}");
            }
        }

        private void ConnectToOtherServers()
        {
            string[] servers = textBoxServersList.Text.Split(',');
            foreach (string server in servers)
            {
                string[] parts = server.Trim().Split(':');
                if (parts.Length >= 2)
                {
                    int remoteId = int.Parse(parts[0]);
                    int remotePort = int.Parse(parts[1]);
                    
                    if (remoteId != serverId && !serverConnections.ContainsKey(remoteId))
                    {
                        try
                        {
                            AddMessage($"Попытка подключиться к серверу {remoteId}:{remotePort}");
                            
                            TcpClient client = new TcpClient();
                            client.Connect("127.0.0.1", remotePort);
                            NetworkStream stream = client.GetStream();
                            
                            // Отправляем свой ID
                            byte[] idData = BitConverter.GetBytes(serverId);
                            stream.Write(idData, 0, 4);
                            stream.Flush();
                            
                            ServerConnection conn = new ServerConnection
                            {
                                Client = client,
                                Stream = stream,
                                RemoteServerId = remoteId,
                                ReceiveThread = new Thread(() => ReceiveFromServer(remoteId, stream))
                            };
                            
                            lock (serverConnections)
                            {
                                serverConnections[remoteId] = conn;
                            }
                            
                            AddMessage($"Подключились к серверу {remoteId}");
                            
                            conn.ReceiveThread.IsBackground = true;
                            conn.ReceiveThread.Start();
                        }
                        catch (Exception ex)
                        {
                            AddMessage($"Не удалось подключиться к серверу {remoteId}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void ReceiveFromServer(int remoteServerId, NetworkStream stream)
        {
            byte[] buffer = new byte[4096];
            
            while (isRunning)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ProcessServerMessage(message, remoteServerId);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    AddMessage($"Ошибка приема от сервера {remoteServerId}: {ex.Message}");
                    break;
                }
            }
            
            // Сервер отключился
            lock (serverConnections)
            {
                if (serverConnections.ContainsKey(remoteServerId))
                {
                    serverConnections.Remove(remoteServerId);
                    AddMessage($"Сервер {remoteServerId} отключился");
                    
                    // Если это был координатор, начинаем выборы
                    if (remoteServerId == coordinatorId)
                    {
                        AddMessage("Координатор отключился, начинаем выборы...");
                        coordinatorId = -1;
                        isCoordinator = false;
                        StartElection();
                    }
                }
            }
        }

        private void ProcessServerMessage(string message, int senderId)
        {
            string[] parts = message.Split(':');
            string command = parts[0];
            
            AddMessage($"Получено от сервера {senderId}: {message}");
            
            switch (command)
            {
                case "ELECTION":
                    // Отправляем OK
                    SendToServer(senderId, $"OK:{serverId}");
                    AddMessage($"Отправил OK серверу {senderId}");
                    
                    // Если мы больше отправителя, начинаем свои выборы
                    if (serverId > senderId && !isElectionInProgress && !isCoordinator)
                    {
                        AddMessage($"Айди больше {senderId}, начинаю свои выборы");
                        StartElection();
                    }
                    break;
                    
                case "OK":
                    receivedOk = true;
                    isElectionInProgress = false;
                    AddMessage($"Получен OK от сервера {senderId}, прекращаю выборы");
                    break;
                    
                case "COORDINATOR":
                    int newCoordId = int.Parse(parts[1]);
                    coordinatorId = newCoordId;
                    isCoordinator = (coordinatorId == serverId);
                    isElectionInProgress = false;
                    receivedOk = false;
                    
                    AddMessage($"Новый координатор: сервер {coordinatorId}");
                    UpdateCoordinatorLabel();
                    
                    if (isCoordinator)
                    {
                        AddMessage("Новый координатор");
                    }
                    break;
                    
                case "BROADCAST":
                    // Пересылаем сообщение клиентам
                    string chatMessage = string.Join(":", parts.Skip(1));
                    AddMessage($"Сообщение от координатора: {chatMessage}");
                    BroadcastToClients(chatMessage, null);
                    break;
            }
        }

        private void SendToServer(int targetServerId, string message)
        {
            lock (serverConnections)
            {
                if (serverConnections.ContainsKey(targetServerId))
                {
                    try
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        serverConnections[targetServerId].Stream.Write(data, 0, data.Length);
                        serverConnections[targetServerId].Stream.Flush();
                    }
                    catch (Exception ex)
                    {
                        AddMessage($"Ошибка отправки серверу {targetServerId}: {ex.Message}");
                    }
                }
            }
        }

        private void BroadcastToServers(string message, int excludeServerId = -1)
        {
            lock (serverConnections)
            {
                foreach (var kvp in serverConnections)
                {
                    if (kvp.Key != excludeServerId)
                    {
                        try
                        {
                            byte[] data = Encoding.UTF8.GetBytes(message);
                            kvp.Value.Stream.Write(data, 0, data.Length);
                            kvp.Value.Stream.Flush();
                        }
                        catch (Exception ex)
                        {
                            AddMessage($"Ошибка отправки серверу {kvp.Key}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void ButtonStartElection_Click(object sender, EventArgs e)
        {
            StartElection();
        }

        private void StartElection()
        {
            if (isElectionInProgress)
            {
                AddMessage("Выборы уже идут...");
                return;
            }
            
            isElectionInProgress = true;
            receivedOk = false;
            
            AddMessage($"Начинаем выборы (ID={serverId})");
            
            // Отправляем ELECTION всем серверам с большим ID
            int higherServers = 0;
            
            lock (serverConnections)
            {
                foreach (int remoteId in allServerIds)
                {
                    if (remoteId > serverId && serverConnections.ContainsKey(remoteId))
                    {
                        SendToServer(remoteId, $"ELECTION:{serverId}");
                        AddMessage($"Отправил ELECTION серверу {remoteId}");
                        higherServers++;
                    }
                }
            }
            
            if (higherServers == 0)
            {
                AddMessage("Нет серверов с большим ID, я становлюсь координатором");
                Thread.Sleep(2000); // Небольшая задержка
                BecomeCoordinator();
            }
            else
            {
                AddMessage($"Ожидаю ответы от {higherServers} серверов...");
                
                // Запускаем таймер ожидания
                Thread timerThread = new Thread(() => WaitForElectionResults());
                timerThread.IsBackground = true;
                timerThread.Start();
            }
        }

        private void WaitForElectionResults()
        {
            int waited = 0;
            while (waited < 5000 && isElectionInProgress && !receivedOk)
            {
                Thread.Sleep(500);
                waited += 500;
            }
            
            if (isElectionInProgress)
            {
                if (receivedOk)
                {
                    AddMessage("Получен OK, прекращаю выборы");
                    isElectionInProgress = false;
                }
                else
                {
                    AddMessage("Никто не ответил, становлюсь координатором!");
                    BecomeCoordinator();
                }
            }
        }

        private void BecomeCoordinator()
        {
            coordinatorId = serverId;
            isCoordinator = true;
            isElectionInProgress = false;
            receivedOk = false;
            
            AddMessage($"Новый координатор (ID={serverId})");
            
            UpdateCoordinatorLabel();
            
            // Уведомляем все серверы
            BroadcastToServers($"COORDINATOR:{serverId}");
            AddMessage("Уведомил все серверы о победе");
        }

        private void UpdateCoordinatorLabel()
        {
            if (labelCoordinator.InvokeRequired)
            {
                labelCoordinator.Invoke(new Action(UpdateCoordinatorLabel));
            }
            else
            {
                if (coordinatorId >= 0)
                {
                    labelCoordinator.Text = $"Координатор: сервер {coordinatorId} {(isCoordinator ? "(Я)" : "")}";
                    labelCoordinator.ForeColor = isCoordinator ? System.Drawing.Color.Green : System.Drawing.Color.Blue;
                }
            }
        }

        private void AcceptClients()
        {
            while (isRunning)
            {
                try
                {
                    Socket clientSocket = clientListener.AcceptSocket();
                    Thread clientThread = new Thread(() => HandleClient(clientSocket));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        AddMessage($"Ошибка при подключении клиента: {ex.Message}");
                }
            }
        }

        private void HandleClient(Socket clientSocket)
        {
            string clientNickname = "Неизвестный";
            NetworkStream stream = null;
            
            try
            {
                stream = new NetworkStream(clientSocket);
                
                // Получаем ник
                byte[] nickBuffer = new byte[256];
                int bytesRead = stream.Read(nickBuffer, 0, nickBuffer.Length);
                
                if (bytesRead > 0)
                {
                    string nickMessage = Encoding.UTF8.GetString(nickBuffer, 0, bytesRead);
                    
                    if (nickMessage.StartsWith("NICK:"))
                    {
                        clientNickname = nickMessage.Substring(5);
                        
                        lock (connectedClients)
                        {
                            connectedClients[clientSocket] = new ClientInfo 
                            { 
                                Nickname = clientNickname,
                                Stream = stream
                            };
                        }
                        
                        string joinMessage = $"{clientNickname} присоединился к чату";
                        AddMessage(joinMessage);
                        
                        if (isCoordinator)
                        {
                            BroadcastToClients(joinMessage, clientSocket);
                            BroadcastToServers($"BROADCAST:{joinMessage}");
                        }
                        
                        ShowParticipants();
                    }
                    else
                    {
                        clientSocket.Close();
                        return;
                    }
                }

                // Прием сообщений
                byte[] buffer = new byte[1024];
                while (isRunning && clientSocket.Connected)
                {
                    int bytesReadMsg = stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesReadMsg > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesReadMsg);
                        
                        if (message.StartsWith("MSG:"))
                        {
                            string textMessage = message.Substring(4);
                            string formattedMessage = $"[{clientNickname}]: {textMessage}";
                            
                            AddMessage(formattedMessage);
                            
                            if (isCoordinator)
                            {
                                BroadcastToClients(formattedMessage, clientSocket);
                                BroadcastToServers($"BROADCAST:{formattedMessage}");
                            }
                            else if (coordinatorId >= 0 && serverConnections.ContainsKey(coordinatorId))
                            {
                                SendToServer(coordinatorId, $"BROADCAST:{formattedMessage}");
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                AddMessage($"Ошибка с клиентом {clientNickname}: {ex.Message}");
            }
            finally
            {
                bool wasRemoved = false;
                lock (connectedClients)
                {
                    if (connectedClients.ContainsKey(clientSocket))
                    {
                        connectedClients.Remove(clientSocket);
                        wasRemoved = true;
                    }
                }
                
                stream?.Close();
                clientSocket.Close();
                
                if (wasRemoved)
                {
                    string leaveMessage = $"{clientNickname} покинул чат";
                    AddMessage(leaveMessage);
                    
                    if (isCoordinator)
                    {
                        BroadcastToClients(leaveMessage, null);
                        BroadcastToServers($"BROADCAST:{leaveMessage}");
                    }
                    
                    ShowParticipants();
                }
            }
        }

        private void BroadcastToClients(string message, Socket excludeSocket)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            
            lock (connectedClients)
            {
                foreach (var kvp in connectedClients)
                {
                    if (excludeSocket != null && kvp.Key == excludeSocket)
                        continue;
                    
                    try
                    {
                        if (kvp.Key.Connected)
                        {
                            kvp.Value.Stream.Write(data, 0, data.Length);
                            kvp.Value.Stream.Flush();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddMessage($"Ошибка отправки клиенту {kvp.Value.Nickname}: {ex.Message}");
                    }
                }
            }
        }

        private void ShowParticipants()
        {
            lock (connectedClients)
            {
                if (connectedClients.Count > 0)
                {
                    var nicknames = connectedClients.Values.Select(c => c.Nickname).ToList();
                    AddMessage($"Участники ({connectedClients.Count}): {string.Join(", ", nicknames)}");
                }
            }
        }

        private void AddMessage(string message)
        {
            if (listBoxMessages.InvokeRequired)
            {
                listBoxMessages.Invoke(new Action<string>(AddMessage), message);
            }
            else
            {
                listBoxMessages.Items.Add($"{DateTime.Now:HH:mm:ss} - {message}");
                listBoxMessages.SelectedIndex = listBoxMessages.Items.Count - 1;
                listBoxMessages.SelectedIndex = -1;
            }
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void StopServer()
        {
            isRunning = false;
            
            lock (connectedClients)
            {
                foreach (var client in connectedClients.Keys)
                {
                    try { client.Close(); } catch { }
                }
                connectedClients.Clear();
            }
            
            lock (serverConnections)
            {
                foreach (var conn in serverConnections.Values)
                {
                    try { conn.Stream.Close(); } catch { }
                    try { conn.Client.Close(); } catch { }
                }
                serverConnections.Clear();
            }
            
            clientListener?.Stop();
            serverListener?.Stop();
            
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            buttonStartElection.Enabled = false;
            labelStatus.Text = "Сервер остановлен";
            labelStatus.ForeColor = System.Drawing.Color.Red;
            
            AddMessage("Сервер остановлен");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopServer();
            base.OnFormClosing(e);
        }
    }

    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ServerForm());
        }
    }
}
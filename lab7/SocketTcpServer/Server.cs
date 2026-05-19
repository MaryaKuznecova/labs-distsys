using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

namespace SocketTcpServer
{
    class Program
    {
        static string address = "127.0.0.1";
        static int port = 8005;
        
        // Очередь запросов к ресурсу
        static Queue<Socket> requestQueue = new Queue<Socket>();
        // Флаг, занят ли ресурс
        static bool resourceBusy = false;
        // Текущий процесс, использующий ресурс
        static Socket currentProcess = null;
        
        static void Main(string[] args)
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            try
            {
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(10);
                Console.WriteLine("Координатор запущен. Ожидание подключений...");
                Console.WriteLine("Для выхода нажмите Ctrl+C");
                
                // Запускаем поток для обработки ввода команд
                Thread commandThread = new Thread(ProcessCommands);
                commandThread.IsBackground = true;
                commandThread.Start();
                
                while (true)
                {
                    try
                    {
                        // Принимаем подключение (блокирующий режим)
                        Socket handler = listenSocket.Accept();
                        Console.WriteLine($"Новый процесс подключился: {handler.RemoteEndPoint}");
                        
                        // Создаем отдельный поток для обработки каждого клиента
                        Thread clientThread = new Thread(HandleClient);
                        clientThread.Start(handler);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при принятии подключения: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
            }
            finally
            {
                listenSocket.Close();
            }
        }
        
        static void ProcessCommands()
        {
            while (true)
            {
                string command = Console.ReadLine();
                if (command.ToLower() == "status")
                {
                    lock (requestQueue)
                    {
                        Console.WriteLine($"Состояние координатора");
                        Console.WriteLine($"Ресурс занят: {resourceBusy}");
                        Console.WriteLine($"Текущий процесс: {(currentProcess != null ? currentProcess.RemoteEndPoint.ToString() : "нет")}");
                        Console.WriteLine($"Очередь запросов: {requestQueue.Count}");
                    }
                }
            }
        }
        
        static void HandleClient(object clientObj)
        {
            Socket clientSocket = (Socket)clientObj;
            bool clientActive = true;
            
            try
            {
                while (clientActive)
                {
                    byte[] data = new byte[256];
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    
                    try
                    {
                        // Получаем данные (с таймаутом)
                        clientSocket.ReceiveTimeout = 1000; // 1 секунда
                        bytes = clientSocket.Receive(data);
                        
                        if (bytes == 0)
                        {
                            // Клиент закрыл соединение
                            Console.WriteLine($"Процесс {clientSocket.RemoteEndPoint} отключился (закрыл соединение)");
                            break;
                        }
                        
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        
                        // Проверяем, есть ли еще данные
                        while (clientSocket.Available > 0)
                        {
                            bytes = clientSocket.Receive(data);
                            builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        }
                        
                        string receivedMessage = builder.ToString();
                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] От процесса {clientSocket.RemoteEndPoint}: {receivedMessage}");
                        
                        // Разбираем команды от клиента
                        if (receivedMessage == "REQUEST_RESOURCE")
                        {
                            lock (requestQueue)
                            {
                                if (!resourceBusy)
                                {
                                    // Ресурс свободен - даем разрешение
                                    resourceBusy = true;
                                    currentProcess = clientSocket;
                                    SendMessage(clientSocket, "GRANTED");
                                    Console.WriteLine($"Разрешение предоставлено процессу {clientSocket.RemoteEndPoint}");
                                }
                                else
                                {
                                    // Ресурс занят - ставим в очередь
                                    requestQueue.Enqueue(clientSocket);
                                    Console.WriteLine($"Процесс {clientSocket.RemoteEndPoint} поставлен в очередь (позиция: {requestQueue.Count})");
                                }
                            }
                        }
                        else if (receivedMessage == "RELEASE_RESOURCE")
                        {
                            lock (requestQueue)
                            {
                                if (currentProcess == clientSocket)
                                {
                                    resourceBusy = false;
                                    currentProcess = null;
                                    Console.WriteLine($"Процесс {clientSocket.RemoteEndPoint} освободил ресурс");
                                    
                                    // Проверяем очередь
                                    if (requestQueue.Count > 0)
                                    {
                                        Socket nextProcess = requestQueue.Dequeue();
                                        resourceBusy = true;
                                        currentProcess = nextProcess;
                                        SendMessage(nextProcess, "GRANTED");
                                        Console.WriteLine($"Разрешение предоставлено следующему процессу из очереди");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Предупреждение: процесс {clientSocket.RemoteEndPoint} пытается освободить ресурс, который ему не принадлежит");
                                }
                            }
                        }
                        else if (receivedMessage.ToLower() == "exit")
                        {
                            Console.WriteLine($"Процесс {clientSocket.RemoteEndPoint} завершает работу");
                            clientActive = false;
                            
                            // Если отключается процесс, который держал ресурс
                            lock (requestQueue)
                            {
                                if (currentProcess == clientSocket)
                                {
                                    resourceBusy = false;
                                    currentProcess = null;
                                    
                                    if (requestQueue.Count > 0)
                                    {
                                        Socket nextProcess = requestQueue.Dequeue();
                                        resourceBusy = true;
                                        currentProcess = nextProcess;
                                        SendMessage(nextProcess, "GRANTED");
                                        Console.WriteLine($"Разрешение предоставлено следующему процессу из очереди");
                                    }
                                }
                                else
                                {
                                    // Удаляем из очереди если был там
                                    Queue<Socket> newQueue = new Queue<Socket>();
                                    foreach (Socket s in requestQueue)
                                    {
                                        if (s != clientSocket)
                                            newQueue.Enqueue(s);
                                    }
                                    requestQueue = newQueue;
                                }
                            }
                        }
                    }
                    catch (SocketException se)
                    {
                        // Таймаут или другие ошибки сокета
                        if (se.SocketErrorCode == SocketError.TimedOut)
                        {
                            // Просто продолжаем ждать
                            continue;
                        }
                        else
                        {
                            Console.WriteLine($"Ошибка сокета для процесса {clientSocket.RemoteEndPoint}: {se.Message}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке клиента {clientSocket.RemoteEndPoint}: {ex.Message}");
            }
            finally
            {
                try
                {
                    // Освобождаем ресурсы если клиент их держал
                    lock (requestQueue)
                    {
                        if (currentProcess == clientSocket)
                        {
                            resourceBusy = false;
                            currentProcess = null;
                            
                            if (requestQueue.Count > 0)
                            {
                                Socket nextProcess = requestQueue.Dequeue();
                                resourceBusy = true;
                                currentProcess = nextProcess;
                                SendMessage(nextProcess, "GRANTED");
                                Console.WriteLine($"Разрешение предоставлено следующему процессу из очереди");
                            }
                        }
                        
                        // Удаляем из очереди если был там
                        Queue<Socket> newQueue = new Queue<Socket>();
                        foreach (Socket s in requestQueue)
                        {
                            if (s != clientSocket)
                                newQueue.Enqueue(s);
                        }
                        requestQueue = newQueue;
                    }
                    
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    Console.WriteLine($"Соединение с процессом {clientSocket.RemoteEndPoint} закрыто");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при закрытии соединения: {ex.Message}");
                }
            }
        }
        
        static void SendMessage(Socket socket, string message)
        {
            try
            {
                byte[] data = Encoding.Unicode.GetBytes(message);
                socket.Send(data);
                Console.WriteLine($"Отправлено сообщение '{message}' процессу {socket.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
            }
        }
    }
} 
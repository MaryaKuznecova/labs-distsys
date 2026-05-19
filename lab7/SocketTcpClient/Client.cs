using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketTcpClient
{
    class Program
    {
        static string address = "127.0.0.1";
        static int port = 8005;
        static bool hasResource = false;
        static bool waitingForResource = false;
        static Socket clientSocket;
        
        static void Main(string[] args)
        {
            try
            {
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                
                Console.WriteLine("Попытка подключения к координатору...");
                clientSocket.Connect(ipPoint);
                
                Console.WriteLine("Подключение к координатору установлено");
                Console.WriteLine("\nДоступные команды:");
                Console.WriteLine("  request - запросить ресурс");
                Console.WriteLine("  release - освободить ресурс");
                Console.WriteLine("  status  - показать статус");
                Console.WriteLine("  exit    - завершить работу");
                
                // Запускаем поток для прослушивания входящих сообщений
                Thread receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();
                
                while (true)
                {
                    Console.Write("\n> ");
                    string command = Console.ReadLine().ToLower().Trim();
                    
                    if (command == "request")
                    {
                        if (!hasResource && !waitingForResource)
                        {
                            waitingForResource = true;
                            Console.Write("Запрос ресурса... ");
                            SendMessage("REQUEST_RESOURCE");
                        }
                        else if (hasResource)
                        {
                            Console.WriteLine("У вас уже есть ресурс! Сначала освободите его (команда 'release')");
                        }
                        else if (waitingForResource)
                        {
                            Console.WriteLine("Вы уже в очереди на получение ресурса");
                        }
                    }
                    else if (command == "release")
                    {
                        if (hasResource)
                        {
                            Console.Write("Освобождение ресурса... ");
                            SendMessage("RELEASE_RESOURCE");
                            // Не сбрасываем hasResource сразу, дождемся подтверждения?
                            // Лучше сбросить сразу, т.к. мы инициировали освобождение
                            hasResource = false;
                            Console.WriteLine("Ресурс освобожден");
                        }
                        else
                        {
                            Console.WriteLine("У вас нет ресурса для освобождения");
                        }
                    }
                    else if (command == "status")
                    {
                        Console.WriteLine($"Статус процесса: {(hasResource ? "имеет ресурс" : (waitingForResource ? "в очереди" : "свободен"))}");
                    }
                    else if (command == "exit")
                    {
                        Console.Write("Завершение работы... ");
                        if (hasResource)
                        {
                            SendMessage("RELEASE_RESOURCE");
                        }
                        SendMessage("exit");
                        
                        // Даем время на отправку сообщения
                        Thread.Sleep(500);
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Неизвестная команда. Доступные команды: request, release, status, exit");
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"\n Ошибка подключения: {ex.Message}");
                Console.WriteLine("  Убедитесь, что сервер запущен и доступен по адресу 127.0.0.1:8005");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n Ошибка: {ex.Message}");
            }
            finally
            {
                if (clientSocket != null && clientSocket.Connected)
                {
                    try
                    {
                        clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                    }
                    catch { }
                }
            }
            
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
        
        static void ReceiveMessages()
        {
            byte[] data = new byte[256];
            
            while (clientSocket != null && clientSocket.Connected)
            {
                try
                {
                    // Проверяем, есть ли данные для чтения (неблокирующий режим)
                    if (clientSocket.Available > 0)
                    {
                        int bytes = clientSocket.Receive(data);
                        if (bytes > 0)
                        {
                            string message = Encoding.Unicode.GetString(data, 0, bytes);
                            
                            // Проверяем, не пришло ли составное сообщение
                            while (clientSocket.Available > 0)
                            {
                                bytes = clientSocket.Receive(data);
                                message += Encoding.Unicode.GetString(data, 0, bytes);
                            }
                            
                            // Обрабатываем полученное сообщение
                            if (message == "GRANTED")
                            {
                                hasResource = true;
                                waitingForResource = false;
                                Console.WriteLine("\nРазрешение получено");
                                Console.WriteLine("  Процесс начал работу с ресурсом");
                                Console.WriteLine("  (используйте 'release' для освобождения)");
                                Console.Write("\n> "); // Восстанавливаем приглашение
                            }
                            else
                            {
                                Console.WriteLine($"\n[Сообщение от координатора]: {message}");
                                Console.Write("\n> ");
                            }
                        }
                    }
                    else
                    {
                        // Небольшая задержка, чтобы не нагружать процессор
                        Thread.Sleep(100);
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode != SocketError.TimedOut)
                    {
                        Console.WriteLine($"\nОшибка приема: {ex.Message}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nОшибка: {ex.Message}");
                    break;
                }
            }
        }
        
        static void SendMessage(string message)
        {
            try
            {
                if (clientSocket != null && clientSocket.Connected)
                {
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    clientSocket.Send(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nОшибка отправки: {ex.Message}");
            }
        }
    }
}
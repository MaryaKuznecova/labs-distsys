using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    public partial class Form1 : Form
    {
        private TcpListener listener;
        private List<TcpClient> clients = new List<TcpClient>(); // Список всех клиентов
        private ListBox listBoxLog;
        private Button btnStart;

        public Form1()
        {
            this.Text = "Сервер Чата v2.0";
            this.Width = 400; this.Height = 300;
            listBoxLog = new ListBox { Dock = DockStyle.Top, Height = 200 };
            btnStart = new Button { Dock = DockStyle.Bottom, Text = "Запустить сервер", Height = 40 };
            btnStart.Click += (s, e) => StartServer();
            this.Controls.Add(listBoxLog);
            this.Controls.Add(btnStart);
        }

        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();
            btnStart.Enabled = false;
            listBoxLog.Items.Add("Сервер запущен. Ожидание участников...");

            // Поток для приема новых подключений
            Thread acceptThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        lock (clients) { clients.Add(client); }
                        
                        this.Invoke((MethodInvoker)(() => listBoxLog.Items.Add("Новый участник подключился.")));

                        Thread clientThread = new Thread(HandleClient);
                        clientThread.IsBackground = true;
                        clientThread.Start(client);
                    }
                    catch { break; }
                }
            });
            acceptThread.IsBackground = true;
            acceptThread.Start();
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Клиент отключился

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    this.Invoke((MethodInvoker)(() => listBoxLog.Items.Add(message)));

                    // РАССЫЛКА ВСЕМ (Broadcast)
                    Broadcast(message);
                }
            }
            catch { }
            finally
            {
                lock (clients) { clients.Add(client); clients.Remove(client); }
                client.Close();
            }
        }

        private void Broadcast(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            lock (clients)
            {
                foreach (TcpClient client in clients)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    catch { /* Если клиент недоступен, его удалит HandleClient */ }
                }
            }
        }
    }
}
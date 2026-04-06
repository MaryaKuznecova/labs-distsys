using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SocketServer
{
    // Наследуемся от Form, чтобы это было окном
    public partial class Form1 : Form
    {
        // Элементы управления (создаем кодом, чтобы не было ошибок дизайнера)
        private ListBox listBoxMessages;
        private Button btnStartServer;
        private TcpListener listener;
        private Thread listenThread;

        public Form1()
        {
            // Настройка окна
            this.Text = "Сервер Чат v1.0";
            this.Width = 450;
            this.Height = 350;

            // Список сообщений
            listBoxMessages = new ListBox { Left = 10, Top = 10, Width = 410, Height = 230 };
            this.Controls.Add(listBoxMessages);

            // Кнопка запуска
            btnStartServer = new Button { Left = 10, Top = 250, Width = 410, Height = 40, Text = "Запустить сервер" };
            btnStartServer.Click += btnStartServer_Click;
            this.Controls.Add(btnStartServer);
        }

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            // 1. Создание сервера (шаг 1 алгоритма)
            try
            {
                listenThread = new Thread(ListenForClients);
                listenThread.IsBackground = true;
                listenThread.Start();

                btnStartServer.Enabled = false;
                btnStartServer.Text = "Сервер запущен (Порт 8888)";
                listBoxMessages.Items.Add("Сервер ожидает подключений...");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка запуска: " + ex.Message);
            }
        }

        private void ListenForClients()
        {
            // Настройка прослушивания
            listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();

            while (true)
            {
                try
                {
                    // 3. Выбор клиентского сокета из очереди (шаг 3 алгоритма)
                    Socket clientSock = listener.AcceptSocket();

                    // 6. Чтение данных из сокета (шаг 6 алгоритма)
                    byte[] buff = new byte[1024];
                    int bytesRead = clientSock.Receive(buff);
                    string message = Encoding.UTF8.GetString(buff, 0, bytesRead);

                    // Вывод в список (через Invoke, т.к. это другой поток)
                    this.Invoke((MethodInvoker)(() => {
                        listBoxMessages.Items.Add(message);
                    }));

                    // 10. Отключение от сокета клиента (шаг 10 алгоритма)
                    clientSock.Close();
                }
                catch { break; }
            }
        }

        // При закрытии формы останавливаем сервер (шаг 11)
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (listener != null) listener.Stop();
            base.OnFormClosing(e);
        }
    }
}
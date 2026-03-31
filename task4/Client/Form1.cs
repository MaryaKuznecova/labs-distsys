using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private ListBox listBoxChat;
        private TextBox txtMsg, txtNick, txtIP;
        private Button btnConnect, btnSend;

        public Form1()
        {
            this.Text = "Клиент Чата v2.0";
            this.Width = 400; this.Height = 400;

            txtIP = new TextBox { Text = "127.0.0.1", Left = 10, Top = 10, Width = 100 };
            txtNick = new TextBox { Text = "Ник", Left = 120, Top = 10, Width = 100 };
            btnConnect = new Button { Text = "Войти", Left = 230, Top = 8, Width = 100 };
            btnConnect.Click += (s, e) => ConnectToServer();

            listBoxChat = new ListBox { Left = 10, Top = 40, Width = 360, Height = 200 };
            txtMsg = new TextBox { Left = 10, Top = 250, Width = 260 };
            btnSend = new Button { Text = "Отправить", Left = 280, Top = 248, Width = 90, Enabled = false };
            btnSend.Click += (s, e) => SendMessage();

            this.Controls.AddRange(new Control[] { txtIP, txtNick, btnConnect, listBoxChat, txtMsg, btnSend });
        }

        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                client.Connect(txtIP.Text, 8888);
                stream = client.GetStream();

                btnConnect.Enabled = false;
                btnSend.Enabled = true;

                // Поток для ПОЛУЧЕНИЯ сообщений от сервера
                Thread receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                listBoxChat.Items.Add("Вы вошли в чат!");
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    this.Invoke((MethodInvoker)(() => listBoxChat.Items.Add(message)));
                }
            }
            catch { MessageBox.Show("Соединение с сервером потеряно."); }
        }

        private void SendMessage()
        {
            if (string.IsNullOrEmpty(txtMsg.Text)) return;

            string fullMsg = $"{txtNick.Text}: {txtMsg.Text}";
            byte[] data = Encoding.UTF8.GetBytes(fullMsg);
            stream.Write(data, 0, data.Length);
            txtMsg.Clear();
        }
    }
}

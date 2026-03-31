using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace SocketClient
{
    public partial class Form1 : Form
    {
        private TextBox txtIP;
        private TextBox txtMessage;
        private Button btnSend;
        private Label lblIP, lblMsg;

        public Form1()
        {
            // Настройка окна
            this.Text = "Клиент Чат v1.0";
            this.Width = 350;
            this.Height = 280;

            lblIP = new Label { Text = "IP адрес сервера:", Left = 10, Top = 10, Width = 200 };
            txtIP = new TextBox { Text = "127.0.0.1", Left = 10, Top = 30, Width = 310 };

            lblMsg = new Label { Text = "Введите сообщение:", Left = 10, Top = 70, Width = 200 };
            txtMessage = new TextBox { Left = 10, Top = 90, Width = 310, Height = 80, Multiline = true };

            btnSend = new Button { Text = "ОТПРАВИТЬ", Left = 10, Top = 180, Width = 310, Height = 45 };
            btnSend.Click += btnSend_Click;

            this.Controls.AddRange(new Control[] { lblIP, txtIP, lblMsg, txtMessage, btnSend });
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(IPAddress.Parse(txtIP.Text), 8888);

                // получение файлового потока (шаг 4 алгоритма)
                NetworkStream stm = client.GetStream();

                string myIP = "";
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    // "Подключаемся" к любому внешнему адресу (реального соединения не будет)
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    myIP = endPoint.Address.ToString();
                }
                string fullMsg = $"[IP {myIP}]: {txtMessage.Text}";

                byte[] buff = Encoding.UTF8.GetBytes(fullMsg);

                stm.Write(buff, 0, buff.Length);

                client.Close();

                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
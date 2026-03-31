using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Client2
{
    public partial class Form1 : Form
    {
        private TextBox txtIP, txtNick, txtMessage;
        private Button btnSend;
        private Label lblIP, lblNick, lblMsg;

        public Form1()
        {
            this.Text = "Клиент Чат v1.1 (с Ником)";
            this.Width = 350;
            this.Height = 320;

            lblIP = new Label { Text = "IP адрес сервера:", Left = 10, Top = 10, Width = 200 };
            txtIP = new TextBox { Text = "127.0.0.1", Left = 10, Top = 30, Width = 310 };

            // НОВОЕ: Поле для Никнейма
            lblNick = new Label { Text = "Ваш Ник/Логин:", Left = 10, Top = 60, Width = 200 };
            txtNick = new TextBox { Text = "User1", Left = 10, Top = 80, Width = 310 };

            lblMsg = new Label { Text = "Введите сообщение:", Left = 10, Top = 110, Width = 200 };
            txtMessage = new TextBox { Left = 10, Top = 130, Width = 310, Height = 80, Multiline = true };

            btnSend = new Button { Text = "ОТПРАВИТЬ", Left = 10, Top = 220, Width = 310, Height = 45 };
            btnSend.Click += btnSend_Click;

            this.Controls.AddRange(new Control[] { lblIP, txtIP, lblNick, txtNick, lblMsg, txtMessage, btnSend });
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNick.Text))
            {
                MessageBox.Show("Введите никнейм!");
                return;
            }

            try
            {
                TcpClient client = new TcpClient();
                client.Connect(IPAddress.Parse(txtIP.Text), 8888);

                NetworkStream stm = client.GetStream();

                // МОДИФИКАЦИЯ: Теперь формируем строку на основе ника
                string nickname = txtNick.Text.Trim();
                string messageBody = txtMessage.Text;

                // Формат сообщения: "Ник: Текст сообщения"
                string fullMsg = $"{nickname}: {messageBody}";

                byte[] buff = Encoding.UTF8.GetBytes(fullMsg);
                stm.Write(buff, 0, buff.Length);

                client.Close();
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }
    }
}
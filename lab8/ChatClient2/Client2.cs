using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class ClientForm : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private TextBox textBoxIP;
        private TextBox textBoxNickname;
        private TextBox textBoxMessage;
        private Button buttonConnect;
        private Button buttonSend;
        private Label labelStatus;
        private ListBox listBoxMessages;

        public ClientForm()
        {
            InitializeComponent();
            Text = "Клиент чата v3.0 (с никами)";
            Size = new System.Drawing.Size(500, 450);
        }

        private void InitializeComponent()
        {
            Label labelIP = new Label();
            Label labelNick = new Label();
            Label labelMsg = new Label();
            textBoxIP = new TextBox();
            textBoxNickname = new TextBox();
            textBoxMessage = new TextBox();
            buttonConnect = new Button();
            buttonSend = new Button();
            labelStatus = new Label();
            listBoxMessages = new ListBox();

            // Label IP
            labelIP.Text = "IP сервера:";
            labelIP.Location = new System.Drawing.Point(12, 15);
            labelIP.Size = new System.Drawing.Size(80, 20);

            // TextBox IP
            textBoxIP.Location = new System.Drawing.Point(95, 12);
            textBoxIP.Size = new System.Drawing.Size(150, 20);
            textBoxIP.Text = "127.0.0.1";

            // Button Connect
            buttonConnect.Text = "Подключиться";
            buttonConnect.Location = new System.Drawing.Point(255, 10);
            buttonConnect.Size = new System.Drawing.Size(100, 25);
            buttonConnect.Click += ButtonConnect_Click;

            // Label Status
            labelStatus.Text = "Не подключен";
            labelStatus.Location = new System.Drawing.Point(365, 13);
            labelStatus.Size = new System.Drawing.Size(120, 20);
            labelStatus.ForeColor = System.Drawing.Color.Red;

            // Label Nickname
            labelNick.Text = "Ваш ник:";
            labelNick.Location = new System.Drawing.Point(12, 50);
            labelNick.Size = new System.Drawing.Size(80, 20);

            // TextBox Nickname
            textBoxNickname.Location = new System.Drawing.Point(95, 47);
            textBoxNickname.Size = new System.Drawing.Size(150, 20);
            textBoxNickname.Text = "User" + new Random().Next(100, 999);

            // Label Message
            labelMsg.Text = "Сообщение:";
            labelMsg.Location = new System.Drawing.Point(12, 85);
            labelMsg.Size = new System.Drawing.Size(80, 20);

            // TextBox Message
            textBoxMessage.Location = new System.Drawing.Point(95, 82);
            textBoxMessage.Size = new System.Drawing.Size(260, 20);
            textBoxMessage.Enabled = false;

            // Button Send
            buttonSend.Text = "Отправить";
            buttonSend.Location = new System.Drawing.Point(365, 80);
            buttonSend.Size = new System.Drawing.Size(100, 25);
            buttonSend.Enabled = false;
            buttonSend.Click += ButtonSend_Click;

            // ListBox Messages
            listBoxMessages.Location = new System.Drawing.Point(12, 120);
            listBoxMessages.Size = new System.Drawing.Size(460, 280);
            listBoxMessages.Font = new System.Drawing.Font("Consolas", 10);

            // Add controls
            Controls.Add(labelIP);
            Controls.Add(textBoxIP);
            Controls.Add(buttonConnect);
            Controls.Add(labelStatus);
            Controls.Add(labelNick);
            Controls.Add(textBoxNickname);
            Controls.Add(labelMsg);
            Controls.Add(textBoxMessage);
            Controls.Add(buttonSend);
            Controls.Add(listBoxMessages);
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            if (buttonConnect.Text == "Подключиться")
            {
                ConnectToServer();
            }
            else
            {
                DisconnectFromServer();
            }
        }

        private void ConnectToServer()
        {
            try
            {
                string ipAddress = textBoxIP.Text;
                
                if (string.IsNullOrWhiteSpace(textBoxNickname.Text))
                {
                    MessageBox.Show("Введите никнейм");
                    return;
                }
                
                // Подключаемся к серверу
                client = new TcpClient();
                client.Connect(IPAddress.Parse(ipAddress), 8888);
                stream = client.GetStream();

                // Отправляем ник серверу
                string nickMessage = $"NICK:{textBoxNickname.Text}";
                byte[] nickData = Encoding.UTF8.GetBytes(nickMessage);
                stream.Write(nickData, 0, nickData.Length);

                buttonConnect.Text = "Отключиться";
                textBoxIP.Enabled = false;
                textBoxNickname.Enabled = false;
                textBoxMessage.Enabled = true;
                buttonSend.Enabled = true;
                labelStatus.Text = "Подключен";
                labelStatus.ForeColor = System.Drawing.Color.Green;
                
                AddMessage($"Подключен к серверу {ipAddress}:8888 как {textBoxNickname.Text}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private void DisconnectFromServer()
        {
            try
            {
                stream?.Close();
                client?.Close();

                buttonConnect.Text = "Подключиться";
                textBoxIP.Enabled = true;
                textBoxNickname.Enabled = true;
                textBoxMessage.Enabled = false;
                buttonSend.Enabled = false;
                labelStatus.Text = "Не подключен";
                labelStatus.ForeColor = System.Drawing.Color.Red;
                
                AddMessage("Отключен от сервера");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отключения: {ex.Message}");
            }
        }

        private void ButtonSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            try
            {
                string message = textBoxMessage.Text;
                if (string.IsNullOrWhiteSpace(message))
                {
                    MessageBox.Show("Введите сообщение");
                    return;
                }

                // Отправляем сообщение в формате "MSG:текст"
                string fullMessage = $"MSG:{message}";
                byte[] data = Encoding.UTF8.GetBytes(fullMessage);
                stream.Write(data, 0, data.Length);
                
                AddMessage($"Вы: {message}");
                textBoxMessage.Clear();
                textBoxMessage.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки: {ex.Message}");
                DisconnectFromServer();
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DisconnectFromServer();
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
            Application.Run(new ClientForm());
        }
    }
}
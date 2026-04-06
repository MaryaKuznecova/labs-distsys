using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Linq;

namespace udp_chat_WinFormsApp
{
    public partial class Form1 : Form
    {
        bool alive = false;
        UdpClient client;
        const int LOCALPORT = 8001;
        const int REMOTEPORT = 8001;
        const int TTL = 20;
        const string HOST = "235.5.5.1";
        IPAddress groupAddress;

        string userName;

        public Form1()
        {
            InitializeComponent();

            loginButton.Enabled = true;
            logoutButton.Enabled = false;
            sendButton.Enabled = false;
            chatTextBox.ReadOnly = true;

            groupAddress = IPAddress.Parse(HOST);

            usersListBox.Items.Add("Все");
            usersListBox.SelectedIndex = 0;
        }

        private void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = client.Receive(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);

                    string[] parts = message.Split('|');
                    if (parts.Length < 4) continue;

                    string type = parts[0];
                    string senderName = parts[1];
                    string targetName = parts[2];
                    string content = parts[3];

                    this.Invoke(new MethodInvoker(() =>
                    {
                        ProcessIncomingMessage(type, senderName, targetName, content);
                    }));
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ProcessIncomingMessage(string type, string sender, string target, string text)
        {
            string time = DateTime.Now.ToShortTimeString();

            switch (type)
            {
                case "LOGIN":
                    AddUserToList(sender);
                    chatTextBox.AppendText($"{time} {sender} вошел в чат\r\n");
                    SendMessage("INFO", sender, "");
                    break;

                case "INFO":
                    AddUserToList(sender);
                    break;

                case "LOGOUT":
                    RemoveUserFromList(sender);
                    chatTextBox.AppendText($"{time} {sender} покинул чат\r\n");
                    break;

                case "MSG":
                    if (target == "Все" || target == userName || sender == userName)
                    {
                        string prefix = (target == "Все") ? "" : "[Приватно] ";
                        chatTextBox.AppendText($"{time} {prefix}{sender}: {text}\r\n");
                    }
                    break;
            }
        }

        private void SendMessage(string type, string target, string text)
        {
            try
            {
                string fullMsg = $"{type}|{userName}|{target}|{text}";
                byte[] data = Encoding.Unicode.GetBytes(fullMsg);
                client.Send(data, data.Length, HOST, REMOTEPORT);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void AddUserToList(string name)
        {
            if (name != userName && !usersListBox.Items.Contains(name))
                usersListBox.Items.Add(name);
        }

        private void RemoveUserFromList(string name)
        {
            if (usersListBox.Items.Contains(name))
                usersListBox.Items.Remove(name);
        }

        private void ExitChat()
        {
            SendMessage("LOGOUT", "Все", "покидает чат");
            alive = false;
            client.DropMulticastGroup(groupAddress);
            client.Close();

            loginButton.Enabled = true;
            logoutButton.Enabled = false;
            sendButton.Enabled = false;
            usersListBox.Items.Clear();
            usersListBox.Items.Add("Все");
            usersListBox.SelectedIndex = 0;
            userNameTextBox.ReadOnly = false;
        }


        private void loginButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(userNameTextBox.Text))
            {
                MessageBox.Show("Введите имя!");
                return;
            }

            userName = userNameTextBox.Text;
            userNameTextBox.ReadOnly = true;

            try
            {
                client = new UdpClient();
                // для работы нескольких клиентов на одном ПК
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(IPAddress.Any, LOCALPORT));

                client.JoinMulticastGroup(groupAddress, TTL);

                Task.Run(() => ReceiveMessages());

                SendMessage("LOGIN", "Все", "");

                loginButton.Enabled = false;
                logoutButton.Enabled = true;
                sendButton.Enabled = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            string target = usersListBox.SelectedItem.ToString();

            SendMessage("MSG", target, messageTextBox.Text);
            messageTextBox.Clear();
        }

        private void logoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }

    }
}
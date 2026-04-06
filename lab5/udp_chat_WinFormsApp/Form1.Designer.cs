namespace udp_chat_WinFormsApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            loginButton = new Button();
            logoutButton = new Button();
            label1 = new Label();
            userNameTextBox = new TextBox();
            messageTextBox = new TextBox();
            sendButton = new Button();
            chatTextBox = new TextBox();
            usersListBox = new ListBox();
            SuspendLayout();
            // 
            // loginButton
            // 
            loginButton.FlatAppearance.BorderColor = Color.White;
            loginButton.FlatAppearance.BorderSize = 0;
            loginButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 192, 192);
            loginButton.Location = new Point(672, 11);
            loginButton.Name = "loginButton";
            loginButton.Size = new Size(150, 32);
            loginButton.TabIndex = 0;
            loginButton.Text = "Вход";
            loginButton.UseVisualStyleBackColor = true;
            loginButton.Click += loginButton_Click;
            // 
            // logoutButton
            // 
            logoutButton.Location = new Point(672, 49);
            logoutButton.Name = "logoutButton";
            logoutButton.Size = new Size(150, 32);
            logoutButton.TabIndex = 1;
            logoutButton.Text = "Выход";
            logoutButton.UseVisualStyleBackColor = true;
            logoutButton.Click += logoutButton_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 41);
            label1.Name = "label1";
            label1.Size = new Size(123, 14);
            label1.TabIndex = 2;
            label1.Text = "Регистрация в чате:";
            // 
            // userNameTextBox
            // 
            userNameTextBox.Location = new Point(145, 33);
            userNameTextBox.Name = "userNameTextBox";
            userNameTextBox.Size = new Size(351, 22);
            userNameTextBox.TabIndex = 3;
            // 
            // messageTextBox
            // 
            messageTextBox.BorderStyle = BorderStyle.FixedSingle;
            messageTextBox.ForeColor = SystemColors.WindowText;
            messageTextBox.Location = new Point(12, 465);
            messageTextBox.Multiline = true;
            messageTextBox.Name = "messageTextBox";
            messageTextBox.Size = new Size(810, 75);
            messageTextBox.TabIndex = 4;
            // 
            // sendButton
            // 
            sendButton.Location = new Point(672, 557);
            sendButton.Name = "sendButton";
            sendButton.Size = new Size(150, 32);
            sendButton.TabIndex = 5;
            sendButton.Text = "Отправить";
            sendButton.UseVisualStyleBackColor = true;
            sendButton.Click += sendButton_Click;
            // 
            // chatTextBox
            // 
            chatTextBox.Location = new Point(12, 174);
            chatTextBox.Multiline = true;
            chatTextBox.Name = "chatTextBox";
            chatTextBox.ReadOnly = true;
            chatTextBox.Size = new Size(810, 274);
            chatTextBox.TabIndex = 6;
            // 
            // usersListBox
            // 
            usersListBox.FormattingEnabled = true;
            usersListBox.Location = new Point(12, 85);
            usersListBox.Name = "usersListBox";
            usersListBox.Size = new Size(285, 74);
            usersListBox.TabIndex = 7;
            usersListBox.Tag = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.InactiveBorder;
            BackgroundImageLayout = ImageLayout.None;
            ClientSize = new Size(842, 605);
            Controls.Add(usersListBox);
            Controls.Add(chatTextBox);
            Controls.Add(sendButton);
            Controls.Add(messageTextBox);
            Controls.Add(userNameTextBox);
            Controls.Add(label1);
            Controls.Add(logoutButton);
            Controls.Add(loginButton);
            Font = new Font("Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button loginButton;
        private Button logoutButton;
        private Label label1;
        private TextBox userNameTextBox;
        private TextBox messageTextBox;
        private Button sendButton;
        private TextBox chatTextBox;
        private ListBox usersListBox;
    }
}

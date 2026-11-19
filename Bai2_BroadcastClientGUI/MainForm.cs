using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Bai2_BroadcastClientGUI
{
    // Client GUI: lang nghe broadcast va hien thi danh sach nhieu server
    // - Luong nen nhan du lieu (ReceiveFrom), khong chay tren luong giao dien
    // - Moi khi nhan goi tin, dung BeginInvoke de chuyen ve luong giao dien roi moi sua ListView
    public class MainForm : Form
    {
        // qua so giay nay khong nhan duoc heartbeat thi danh dau Offline
        private const int OfflineTimeoutSeconds = 10;

        // ----- cac control tren giao dien -----
        private readonly TextBox txtPort = new TextBox();
        private readonly Button btnStartListening = new Button();
        private readonly Button btnStopListening = new Button();
        private readonly Button btnClear = new Button();
        private readonly ListView lvServers = new ListView();
        private readonly Label lblState = new Label();

        // timer cua Windows Forms: chay tren luong giao dien, dung de kiem tra timeout moi giay
        // (viet day du ten vi System.Threading cung co lop Timer)
        private readonly System.Windows.Forms.Timer _checkTimer = new System.Windows.Forms.Timer();

        // danh sach server da phat hien, khoa = IP|ten
        private readonly Dictionary<string, ServerInfo> _servers = new Dictionary<string, ServerInfo>();

        // ----- tai nguyen mang -----
        private Socket _socket;
        private int _listenPort;

        public MainForm()
        {
            BuildUi();
            UpdateButtons(false);
            UpdateStateLabel();
        }

        // ================= XAY DUNG GIAO DIEN =================

        private void BuildUi()
        {
            SuspendLayout();

            Text = "Broadcast Client GUI - Bai 2";
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(760, 420);
            MinimumSize = new Size(640, 300);
            StartPosition = FormStartPosition.CenterScreen;

            // bo cuc 3 hang: thanh nut, danh sach server, nhan trang thai
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(8);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // hang 0: port + cac nut
            FlowLayoutPanel top = new FlowLayoutPanel();
            top.AutoSize = true;
            top.Dock = DockStyle.Fill;

            Label lblPort = new Label();
            lblPort.Text = "Port:";
            lblPort.AutoSize = true;
            lblPort.Margin = new Padding(3, 9, 3, 0);

            txtPort.Text = "12790";
            txtPort.Width = 70;
            txtPort.Margin = new Padding(3, 6, 12, 3);

            btnStartListening.Text = "Start Listening";
            btnStartListening.AutoSize = true;
            btnStopListening.Text = "Stop Listening";
            btnStopListening.AutoSize = true;
            btnClear.Text = "Clear";
            btnClear.AutoSize = true;

            top.Controls.Add(lblPort);
            top.Controls.Add(txtPort);
            top.Controls.Add(btnStartListening);
            top.Controls.Add(btnStopListening);
            top.Controls.Add(btnClear);
            root.Controls.Add(top, 0, 0);

            // hang 1: danh sach server dang ListView che do Details
            lvServers.View = View.Details;
            lvServers.FullRowSelect = true;
            lvServers.GridLines = true;
            lvServers.Dock = DockStyle.Fill;
            lvServers.Columns.Add("Ten server", 160);
            lvServers.Columns.Add("IP", 140);
            lvServers.Columns.Add("Port", 70);
            lvServers.Columns.Add("Trang thai", 160);
            lvServers.Columns.Add("Nhan gan nhat", 130);
            root.Controls.Add(lvServers, 0, 1);

            // hang 2: nhan trang thai
            lblState.AutoSize = true;
            lblState.Margin = new Padding(3, 8, 3, 3);
            root.Controls.Add(lblState, 0, 2);

            Controls.Add(root);

            // timer kiem tra timeout moi 1 giay
            _checkTimer.Interval = 1000;
            _checkTimer.Tick += CheckTimer_Tick;

            // gan su kien
            btnStartListening.Click += BtnStartListening_Click;
            btnStopListening.Click += BtnStopListening_Click;
            btnClear.Click += BtnClear_Click;
            FormClosing += MainForm_FormClosing;

            ResumeLayout(false);
            PerformLayout();
        }

        private void UpdateButtons(bool listening)
        {
            btnStartListening.Enabled = !listening;
            btnStopListening.Enabled = listening;
            txtPort.Enabled = !listening;
        }

        private void UpdateStateLabel()
        {
            int online = 0;
            foreach (ServerInfo info in _servers.Values)
            {
                if (info.IsOnline) online++;
            }

            string listening = _socket != null ? "Dang lang nghe tren port " + _listenPort : "Chua lang nghe";
            lblState.Text = listening + "  |  Server online: " + online + " / " + _servers.Count;
        }

        // ================= XU LY NUT BAM =================

        private void BtnStartListening_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Port phai la so tu 1 den 65535.", "Port khong hop le", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                // cho phep nhieu chuong trinh tren cung 1 may cung bind 1 port
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // bind vao moi card mang tai port da chon
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                MessageBox.Show("Khong bind duoc port " + port + ":\n" + ex.Message + "\n\nHay thu doi sang port khac (doi dong bo ca server va client).",
                    "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _socket = socket;
            _listenPort = port;

            // luong nen chuyen nhan du lieu, giao dien khong bi treo
            Thread receiveThread = new Thread(() => ReceiveLoop(socket));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            _checkTimer.Start();
            UpdateButtons(true);
            UpdateStateLabel();
        }

        private void BtnStopListening_Click(object sender, EventArgs e)
        {
            StopListening();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            _servers.Clear();
            lvServers.Items.Clear();
            UpdateStateLabel();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // giai phong socket va timer khi dong cua so
            StopListening();
            _checkTimer.Dispose();
        }

        // dung lang nghe: dong socket thi ReceiveFrom ben luong nen se nem loi va luong tu thoat
        private void StopListening()
        {
            _checkTimer.Stop();

            Socket socket = _socket;

            // dat null truoc de luong nhan biet day la do nguoi dung chu dong dung, khong phai loi mang
            _socket = null;

            if (socket != null)
            {
                socket.Close();
            }

            UpdateButtons(false);
            UpdateStateLabel();
        }

        // ================= LUONG NHAN DU LIEU (LUONG NEN) =================

        private void ReceiveLoop(Socket socket)
        {
            // mang nhan du lieu 1024 byte
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                    // cho den khi co goi tin (block luong nen, khong anh huong giao dien)
                    int length = socket.ReceiveFrom(buffer, ref sender);

                    string message = Encoding.UTF8.GetString(buffer, 0, length);
                    string senderIp = ((IPEndPoint)sender).Address.ToString();

                    // KHONG sua control truc tiep tu luong nay
                    // dung BeginInvoke de chuyen viec cap nhat ve luong giao dien
                    RunOnUiThread(() => HandleMessage(socket, senderIp, message));
                }
                catch (SocketException ex)
                {
                    // neu do nguoi dung bam Stop thi _socket da la null, bo qua
                    // nguoc lai la loi mang that su, bao len giao dien
                    string error = ex.Message;
                    RunOnUiThread(() => OnReceiveError(socket, error));
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        // chuyen mot hanh dong sang luong giao dien, co kiem tra form con song hay khong
        private void RunOnUiThread(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;

            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
                // form dang dong, bo qua
            }
        }

        // ================= XU LY TREN LUONG GIAO DIEN =================

        private void OnReceiveError(Socket socket, string error)
        {
            // socket khac voi socket hien tai nghia la nguoi dung da dung hoac da bat dau lai
            if (_socket != socket) return;

            StopListening();
            lblState.Text = "Loi khi nhan du lieu: " + error;
        }

        private void HandleMessage(Socket socket, string senderIp, string message)
        {
            // goi tin den muon sau khi nguoi dung da bam Stop thi bo qua
            if (_socket != socket) return;

            // dinh dang: SERVER|name=...|status=...|port=...|time=...
            string[] parts = message.Split('|');
            if (parts.Length < 2 || parts[0] != "SERVER") return;

            // tach cac cap "ten=gia tri"
            Dictionary<string, string> fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < parts.Length; i++)
            {
                int equalIndex = parts[i].IndexOf('=');
                if (equalIndex <= 0) continue;

                string key = parts[i].Substring(0, equalIndex).Trim();
                string value = parts[i].Substring(equalIndex + 1).Trim();
                fields[key] = value;
            }

            string name = fields.TryGetValue("name", out string nameValue) && nameValue.Length > 0 ? nameValue : senderIp;
            string status = fields.TryGetValue("status", out string statusValue) ? statusValue : "(khong co)";
            int port = fields.TryGetValue("port", out string portValue) && int.TryParse(portValue, out int portNumber) ? portNumber : 0;

            string serverKey = senderIp + "|" + name;
            DateTime now = DateTime.Now;

            if (!_servers.TryGetValue(serverKey, out ServerInfo info))
            {
                // server moi: tao du lieu va them 1 dong vao ListView
                info = new ServerInfo();
                info.Key = serverKey;
                _servers[serverKey] = info;

                ListViewItem item = new ListViewItem(name);
                item.Name = serverKey; // Name dung lam khoa de tim lai dong nay sau nay
                item.SubItems.Add(senderIp);
                item.SubItems.Add(port.ToString());
                item.SubItems.Add(status);
                item.SubItems.Add(now.ToString("HH:mm:ss"));
                lvServers.Items.Add(item);
            }

            // server da co: chi cap nhat thong tin, khong them dong moi nen khong bi trung
            info.Name = name;
            info.IpAddress = senderIp;
            info.Port = port;
            info.Status = status;
            info.LastSeen = now;
            info.IsOnline = true;

            RefreshRow(info);
            UpdateStateLabel();
        }

        // ghi lai du lieu cua 1 server len dong tuong ung trong ListView
        private void RefreshRow(ServerInfo info)
        {
            ListViewItem item = lvServers.Items[info.Key];
            if (item == null) return;

            item.Text = info.Name;
            item.SubItems[1].Text = info.IpAddress;
            item.SubItems[2].Text = info.Port.ToString();
            item.SubItems[3].Text = info.IsOnline ? info.Status : "Offline";
            item.SubItems[4].Text = info.LastSeen.ToString("HH:mm:ss");

            // server Offline hien mau xam
            item.ForeColor = info.IsOnline ? SystemColors.WindowText : Color.Gray;
        }

        // chay moi giay: server nao qua 10 giay khong gui thi danh dau Offline
        private void CheckTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            foreach (ServerInfo info in _servers.Values)
            {
                bool online = (now - info.LastSeen).TotalSeconds <= OfflineTimeoutSeconds;

                if (online != info.IsOnline)
                {
                    info.IsOnline = online;
                    RefreshRow(info);
                }
            }

            UpdateStateLabel();
        }
    }
}

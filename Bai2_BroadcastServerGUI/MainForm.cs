using System;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai2_BroadcastServerGUI
{
    // Server GUI: gui heartbeat broadcast dinh ky moi 3 giay
    // Giao dien duoc tao bang code (khong dung file Designer) de de doc va de nop bai
    public class MainForm : Form
    {
        // chu ky gui heartbeat (mili giay)
        private const int HeartbeatIntervalMs = 3000;

        // so dong toi da giu trong o nhat ky
        private const int MaxLogLines = 200;

        // ----- cac control tren giao dien -----
        private readonly TextBox txtServerName = new TextBox();
        private readonly TextBox txtStatus = new TextBox();
        private readonly TextBox txtPort = new TextBox();
        private readonly TextBox txtBroadcastIp = new TextBox();
        private readonly Button btnStart = new Button();
        private readonly Button btnStop = new Button();
        private readonly Label lblState = new Label();
        private readonly ListBox lstLog = new ListBox();

        // ----- tai nguyen mang -----
        private Socket _socket;
        private CancellationTokenSource _cts;
        private IPEndPoint _target;
        private string _serverName = "";
        private int _sentCount;

        public MainForm()
        {
            BuildUi();
            UpdateButtons(false);
        }

        // ================= XAY DUNG GIAO DIEN =================

        private void BuildUi()
        {
            SuspendLayout();

            Text = "Broadcast Server GUI - Bai 2";
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(560, 420);
            MinimumSize = new Size(480, 360);
            StartPosition = FormStartPosition.CenterScreen;

            // bang chia luoi: cot 0 la nhan, cot 1 la o nhap
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12);
            layout.ColumnCount = 2;
            layout.RowCount = 7;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 6; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // gia tri mac dinh
            txtServerName.Text = "Lab-01";
            txtStatus.Text = "Ready";
            txtPort.Text = "12790";
            txtBroadcastIp.Text = "255.255.255.255";

            AddRow(layout, 0, "Ten server:", txtServerName);
            AddRow(layout, 1, "Noi dung / trang thai:", txtStatus);
            AddRow(layout, 2, "Port:", txtPort);
            AddRow(layout, 3, "Dia chi broadcast:", txtBroadcastIp);

            // o port ngan hon cho dep
            txtPort.Anchor = AnchorStyles.Left;
            txtPort.Width = 100;

            // hang chua 2 nut Start / Stop
            FlowLayoutPanel buttonRow = new FlowLayoutPanel();
            buttonRow.AutoSize = true;
            buttonRow.Dock = DockStyle.Fill;
            btnStart.Text = "Start";
            btnStart.Size = new Size(100, 32);
            btnStop.Text = "Stop";
            btnStop.Size = new Size(100, 32);
            buttonRow.Controls.Add(btnStart);
            buttonRow.Controls.Add(btnStop);
            layout.Controls.Add(buttonRow, 0, 4);
            layout.SetColumnSpan(buttonRow, 2);

            // nhan trang thai
            lblState.Text = "Trang thai: chua chay";
            lblState.AutoSize = true;
            lblState.Anchor = AnchorStyles.Left;
            lblState.Margin = new Padding(3, 8, 3, 8);
            layout.Controls.Add(lblState, 0, 5);
            layout.SetColumnSpan(lblState, 2);

            // o nhat ky (log) chiem phan con lai
            lstLog.Dock = DockStyle.Fill;
            lstLog.IntegralHeight = false;
            lstLog.HorizontalScrollbar = true;
            layout.Controls.Add(lstLog, 0, 6);
            layout.SetColumnSpan(lstLog, 2);

            Controls.Add(layout);

            // gan su kien
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            FormClosing += MainForm_FormClosing;

            ResumeLayout(false);
            PerformLayout();
        }

        // them 1 dong gom nhan + o nhap vao bang chia luoi
        private static void AddRow(TableLayoutPanel layout, int row, string caption, Control input)
        {
            Label lbl = new Label();
            lbl.Text = caption;
            lbl.AutoSize = true;
            lbl.Anchor = AnchorStyles.Left;
            lbl.Margin = new Padding(3, 8, 10, 8);

            input.Anchor = AnchorStyles.Left | AnchorStyles.Right;

            layout.Controls.Add(lbl, 0, row);
            layout.Controls.Add(input, 1, row);
        }

        // bat/tat nut va cac o nhap tuy theo server dang chay hay khong
        private void UpdateButtons(bool running)
        {
            btnStart.Enabled = !running;
            btnStop.Enabled = running;

            // dang chay thi khong cho doi ten, port, dia chi broadcast
            // rieng o trang thai van cho sua de thay doi noi dung heartbeat
            txtServerName.Enabled = !running;
            txtPort.Enabled = !running;
            txtBroadcastIp.Enabled = !running;
        }

        // ================= XU LY NUT BAM =================

        private void BtnStart_Click(object sender, EventArgs e)
        {
            // kiem tra du lieu nhap vao
            string name = Clean(txtServerName.Text);
            if (name.Length == 0)
            {
                MessageBox.Show("Hay nhap ten server.", "Thieu thong tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Port phai la so tu 1 den 65535.", "Port khong hop le", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IPAddress.TryParse(txtBroadcastIp.Text.Trim(), out IPAddress broadcastIp))
            {
                MessageBox.Show("Dia chi broadcast khong hop le (vi du 255.255.255.255 hoac 192.168.1.255).",
                    "Dia chi khong hop le", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // tao socket UDP va bat tuy chon broadcast
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            }
            catch (SocketException ex)
            {
                StopSending();
                MessageBox.Show("Khong tao duoc socket: " + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _serverName = name;
            _target = new IPEndPoint(broadcastIp, port);
            _sentCount = 0;
            _cts = new CancellationTokenSource();

            UpdateButtons(true);
            AddLog("Bat dau gui heartbeat den " + _target + " moi " + HeartbeatIntervalMs / 1000 + " giay");

            // chay vong lap gui heartbeat (khong cho ket qua)
            _ = HeartbeatLoopAsync(_cts.Token);
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopSending();
            UpdateButtons(false);
            lblState.Text = "Trang thai: da dung (tong cong da gui " + _sentCount + " thong bao)";
            AddLog("Da dung server");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // dong cua so thi giai phong socket va dung vong lap gui
            StopSending();
        }

        // ================= GUI HEARTBEAT =================

        // vong lap gui dinh ky bang PeriodicTimer
        // ham duoc goi tu luong giao dien nen sau moi lan await se quay lai luong giao dien,
        // vi vay co the cap nhat Label / ListBox truc tiep ma khong can Invoke
        private async Task HeartbeatLoopAsync(CancellationToken token)
        {
            try
            {
                using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(HeartbeatIntervalMs));

                // gui ngay lan dau, khong doi het 3 giay
                SendHeartbeat();

                while (await timer.WaitForNextTickAsync(token))
                {
                    if (token.IsCancellationRequested) break;
                    SendHeartbeat();
                }
            }
            catch (OperationCanceledException)
            {
                // nguoi dung bam Stop hoac dong form, thoat vong lap binh thuong
            }
        }

        private void SendHeartbeat()
        {
            // socket da bi dong thi khong gui nua
            if (_socket == null || _target == null) return;

            string status = Clean(txtStatus.Text);
            string time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            string message = "SERVER|name=" + _serverName + "|status=" + status + "|port=" + _target.Port + "|time=" + time;
            byte[] data = Encoding.UTF8.GetBytes(message);

            try
            {
                _socket.SendTo(data, _target);
                _sentCount++;
                lblState.Text = "Trang thai: dang gui heartbeat (da gui " + _sentCount + " lan)";
                AddLog("Da gui lan " + _sentCount + ": " + message);
            }
            catch (SocketException ex)
            {
                AddLog("Loi khi gui: " + ex.Message);
            }
            catch (ObjectDisposedException)
            {
                // socket vua bi dong, bo qua
            }
        }

        // dung gui: huy vong lap va dong socket
        private void StopSending()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
        }

        // ================= HAM PHU =================

        // them 1 dong vao nhat ky, giu toi da MaxLogLines dong
        private void AddLog(string text)
        {
            lstLog.Items.Add("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text);

            while (lstLog.Items.Count > MaxLogLines)
            {
                lstLog.Items.RemoveAt(0);
            }

            // cuon xuong dong cuoi cung
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }

        // bo ky tu '|' (dung de phan tach truong) va xuong dong de khong lam hong dinh dang goi tin
        private static string Clean(string value)
        {
            return value.Replace('|', '/').Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}

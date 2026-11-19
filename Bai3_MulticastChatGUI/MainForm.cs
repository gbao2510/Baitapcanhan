using System;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai3_MulticastChatGUI
{
    // Multicast Chat: nhieu client cung tham gia nhom 224.100.0.1:15000 va nhan tin cho nhau
    // Phan mo rong da lam: (1) to mau tin nhan cua minh / cua nguoi khac, (2) nut Clear Chat
    public class MainForm : Form
    {
        // dia chi nhom multicast va port dung chung cho tat ca client
        private const string GroupAddress = "224.100.0.1";
        private const int GroupPort = 15000;

        // do dai toi da cua 1 tin nhan
        private const int MaxMessageLength = 500;

        // mau hien thi
        private static readonly Color OwnColor = Color.RoyalBlue;      // tin nhan cua minh
        private static readonly Color OtherColor = Color.DarkGreen;    // tin nhan cua nguoi khac
        private static readonly Color SystemColor = Color.Gray;        // thong bao he thong

        // ----- cac control tren giao dien -----
        private readonly TextBox txtNickname = new TextBox();
        private readonly Button btnJoin = new Button();
        private readonly Button btnLeave = new Button();
        private readonly Button btnClearChat = new Button();
        private readonly RichTextBox rtbChat = new RichTextBox();
        private readonly TextBox txtMessage = new TextBox();
        private readonly Button btnSend = new Button();
        private readonly Label lblState = new Label();

        // ----- tai nguyen mang -----
        private UdpClient _udp;
        private CancellationTokenSource _cts;
        private string _nickname = "";

        // diem den khi gui tin: dia chi nhom + port
        private readonly IPEndPoint _groupEndPoint = new IPEndPoint(IPAddress.Parse(GroupAddress), GroupPort);

        public MainForm()
        {
            BuildUi();
            SetJoinedState(false);
        }

        // ================= XAY DUNG GIAO DIEN =================

        private void BuildUi()
        {
            SuspendLayout();

            Text = "Multicast Chat - " + GroupAddress + ":" + GroupPort;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(720, 520);
            MinimumSize = new Size(560, 380);
            StartPosition = FormStartPosition.CenterScreen;

            // bo cuc 4 hang: thanh tren, khung chat, o nhap tin nhan, nhan trang thai
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(8);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // hang 0: nickname + cac nut
            FlowLayoutPanel top = new FlowLayoutPanel();
            top.AutoSize = true;
            top.Dock = DockStyle.Fill;

            Label lblNick = new Label();
            lblNick.Text = "Nickname:";
            lblNick.AutoSize = true;
            lblNick.Margin = new Padding(3, 9, 3, 0);

            // nickname mac dinh khac nhau giua cac cua so (dua vao ma tien trinh)
            txtNickname.Text = "User" + Environment.ProcessId;
            txtNickname.Width = 150;
            txtNickname.MaxLength = 20;
            txtNickname.Margin = new Padding(3, 6, 12, 3);

            btnJoin.Text = "Join";
            btnJoin.AutoSize = true;
            btnLeave.Text = "Leave / Close";
            btnLeave.AutoSize = true;
            btnClearChat.Text = "Clear Chat";
            btnClearChat.AutoSize = true;

            top.Controls.Add(lblNick);
            top.Controls.Add(txtNickname);
            top.Controls.Add(btnJoin);
            top.Controls.Add(btnLeave);
            top.Controls.Add(btnClearChat);
            root.Controls.Add(top, 0, 0);

            // hang 1: khung hien thi tin nhan (RichTextBox de to mau duoc)
            rtbChat.Dock = DockStyle.Fill;
            rtbChat.ReadOnly = true;
            rtbChat.BackColor = Color.White;
            rtbChat.BorderStyle = BorderStyle.FixedSingle;
            root.Controls.Add(rtbChat, 0, 1);

            // hang 2: o nhap tin nhan + nut Send
            TableLayoutPanel sendRow = new TableLayoutPanel();
            sendRow.AutoSize = true;
            sendRow.Dock = DockStyle.Fill;
            sendRow.ColumnCount = 2;
            sendRow.RowCount = 1;
            sendRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            sendRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            sendRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            txtMessage.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtMessage.MaxLength = MaxMessageLength;
            btnSend.Text = "Send";
            btnSend.AutoSize = true;

            sendRow.Controls.Add(txtMessage, 0, 0);
            sendRow.Controls.Add(btnSend, 1, 0);
            root.Controls.Add(sendRow, 0, 2);

            // hang 3: nhan trang thai
            lblState.AutoSize = true;
            lblState.Margin = new Padding(3, 8, 3, 3);
            root.Controls.Add(lblState, 0, 3);

            Controls.Add(root);

            // nhan Enter trong o nhap tin nhan se bam nut Send
            AcceptButton = btnSend;

            // gan su kien
            btnJoin.Click += BtnJoin_Click;
            btnLeave.Click += BtnLeave_Click;
            btnClearChat.Click += BtnClearChat_Click;
            btnSend.Click += BtnSend_Click;
            FormClosing += MainForm_FormClosing;

            ResumeLayout(false);
            PerformLayout();
        }

        // bat/tat control tuy theo da tham gia nhom hay chua
        private void SetJoinedState(bool joined)
        {
            txtNickname.Enabled = !joined;
            btnJoin.Enabled = !joined;
            btnLeave.Enabled = joined;
            txtMessage.Enabled = joined;
            btnSend.Enabled = joined;

            lblState.Text = joined
                ? "Da tham gia nhom " + GroupAddress + ":" + GroupPort + " voi nickname \"" + _nickname + "\""
                : "Chua tham gia nhom. Nhap nickname roi bam Join.";
        }

        // ================= XU LY NUT BAM =================

        private void BtnJoin_Click(object sender, EventArgs e)
        {
            string nickname = CleanNickname(txtNickname.Text);
            if (nickname.Length == 0)
            {
                MessageBox.Show("Hay nhap nickname truoc khi tham gia.", "Thieu nickname", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UdpClient udp = null;
            try
            {
                // buoc 1: tao UdpClient (chua bind)
                udp = new UdpClient(AddressFamily.InterNetwork);

                // cho phep nhieu client tren cung 1 may cung dung port 15000
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // buoc 2: bind vao port 15000
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, GroupPort));

                // buoc 3: tham gia nhom multicast
                udp.JoinMulticastGroup(IPAddress.Parse(GroupAddress));
            }
            catch (SocketException ex)
            {
                if (udp != null) udp.Close();
                MessageBox.Show("Khong tham gia duoc nhom multicast:\n" + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _nickname = nickname;
            _udp = udp;
            _cts = new CancellationTokenSource();

            // buoc 4: chay vong lap nhan tren luong nen (Task.Run)
            CancellationToken token = _cts.Token;
            Task.Run(() => ReceiveLoopAsync(udp, token));

            SetJoinedState(true);
            AppendLine("[" + Now() + "] * Ban da tham gia nhom chat", SystemColor);
            txtMessage.Focus();
        }

        private void BtnLeave_Click(object sender, EventArgs e)
        {
            LeaveGroup();
            SetJoinedState(false);
            AppendLine("[" + Now() + "] * Ban da roi nhom chat", SystemColor);
        }

        private void BtnClearChat_Click(object sender, EventArgs e)
        {
            rtbChat.Clear();
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            if (_udp == null) return;

            // kiem tra tin nhan rong
            string content = txtMessage.Text.Trim();
            if (content.Length == 0) return;

            // xuong dong se lam hong dinh dang goi tin nen doi thanh dau cach
            content = content.Replace("\r", " ").Replace("\n", " ");

            // dinh dang: CHAT|nickname=An|message=Hello|time=2026-11-19T10:45:00
            string time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            string packet = "CHAT|nickname=" + _nickname + "|message=" + content + "|time=" + time;
            byte[] data = Encoding.UTF8.GetBytes(packet);

            try
            {
                // gui den dia chi nhom, tat ca client trong nhom deu nhan duoc
                _udp.Send(data, data.Length, _groupEndPoint);

                txtMessage.Clear();
                txtMessage.Focus();
            }
            catch (SocketException ex)
            {
                AppendLine("[" + Now() + "] * Loi khi gui: " + ex.Message, Color.Red);
            }
            catch (ObjectDisposedException)
            {
                // socket da dong, bo qua
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // dong cua so thi roi nhom, dong socket va dung luong nhan
            LeaveGroup();
        }

        // roi nhom: dung luong nhan, DropMulticastGroup, dong UdpClient
        // client nay dong khong anh huong cac client khac vi moi client co socket rieng
        private void LeaveGroup()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            UdpClient udp = _udp;
            _udp = null; // dat null truoc de luong nhan biet la nguoi dung chu dong roi nhom

            if (udp != null)
            {
                try
                {
                    udp.DropMulticastGroup(IPAddress.Parse(GroupAddress));
                }
                catch (Exception)
                {
                    // roi nhom that bai (vi du mang vua mat) thi van tiep tuc dong socket
                }

                udp.Close();
            }
        }

        // ================= LUONG NHAN DU LIEU (CHAY TREN THREAD POOL) =================

        private async Task ReceiveLoopAsync(UdpClient udp, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // cho goi tin tu nhom multicast
                    UdpReceiveResult result = await udp.ReceiveAsync(token);
                    string raw = Encoding.UTF8.GetString(result.Buffer);

                    // KHONG sua control truc tiep tu luong nay, chuyen ve luong giao dien bang BeginInvoke
                    RunOnUiThread(() => ShowIncoming(udp, raw));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // loi ICMP thoang qua cua Windows tren UDP, bo qua va nhan tiep
                    continue;
                }
                catch (SocketException ex)
                {
                    // loi mang that su (neu khong phai do nguoi dung chu dong roi nhom)
                    string error = ex.Message;
                    RunOnUiThread(() => OnReceiveError(udp, error));
                    break;
                }
            }
        }

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
            catch (ObjectDisposedException)
            {
                // form da bi huy, bo qua
            }
        }

        // ================= XU LY TREN LUONG GIAO DIEN =================

        private void OnReceiveError(UdpClient udp, string error)
        {
            // udp khac voi _udp hien tai nghia la nguoi dung da roi nhom, khong phai loi
            if (_udp != udp) return;

            LeaveGroup();
            SetJoinedState(false);
            AppendLine("[" + Now() + "] * Loi khi nhan du lieu: " + error, Color.Red);
        }

        private void ShowIncoming(UdpClient udp, string raw)
        {
            // goi tin den muon sau khi da roi nhom thi bo qua
            if (_udp != udp) return;

            if (!TryParseChat(raw, out string nickname, out string content)) return;

            // hien thi: [gio nhan] nickname: noi dung
            // tin nhan cua minh mau xanh duong, cua nguoi khac mau xanh la
            bool isMine = nickname == _nickname;
            AppendLine("[" + Now() + "] " + nickname + ": " + content, isMine ? OwnColor : OtherColor);
        }

        // tach goi tin CHAT|nickname=...|message=...|time=...
        // khong dung Split('|') vi noi dung tin nhan co the chua ky tu '|'
        // thay vao do tim vi tri cac nhan "|nickname=", "|message=", "|time=" (time la truong cuoi)
        private static bool TryParseChat(string raw, out string nickname, out string content)
        {
            nickname = "";
            content = "";

            const string NickTag = "|nickname=";
            const string MessageTag = "|message=";
            const string TimeTag = "|time=";

            if (!raw.StartsWith("CHAT", StringComparison.Ordinal)) return false;

            int nickIndex = raw.IndexOf(NickTag, StringComparison.Ordinal);
            int messageIndex = raw.IndexOf(MessageTag, StringComparison.Ordinal);
            int timeIndex = raw.LastIndexOf(TimeTag, StringComparison.Ordinal);

            // cac truong phai xuat hien dung thu tu nickname -> message -> time
            if (nickIndex < 0 || messageIndex <= nickIndex || timeIndex <= messageIndex) return false;

            int nickStart = nickIndex + NickTag.Length;
            int messageStart = messageIndex + MessageTag.Length;

            nickname = raw.Substring(nickStart, messageIndex - nickStart);
            content = raw.Substring(messageStart, timeIndex - messageStart);
            return true;
        }

        // ================= HAM PHU =================

        // them 1 dong co mau vao khung chat va cuon xuong cuoi
        private void AppendLine(string text, Color color)
        {
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionLength = 0;
            rtbChat.SelectionColor = color;
            rtbChat.AppendText(text + Environment.NewLine);
            rtbChat.SelectionColor = rtbChat.ForeColor;
            rtbChat.ScrollToCaret();
        }

        private static string Now()
        {
            return DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        // bo ky tu '|' va xuong dong khoi nickname de khong lam hong dinh dang goi tin
        private static string CleanNickname(string value)
        {
            return value.Replace('|', ' ').Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}

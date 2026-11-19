using System;

namespace Bai2_BroadcastClientGUI
{
    // thong tin cua 1 server ma client phat hien duoc
    public class ServerInfo
    {
        // khoa dinh danh server: "IP|ten server"
        // dung them ten vi nhieu server co the chay tren cung 1 may (cung IP, cung port)
        public string Key { get; set; } = "";

        public string Name { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public int Port { get; set; }
        public string Status { get; set; } = "";

        // thoi diem nhan heartbeat gan nhat
        public DateTime LastSeen { get; set; }

        // false khi qua 10 giay khong nhan duoc heartbeat
        public bool IsOnline { get; set; } = true;
    }
}

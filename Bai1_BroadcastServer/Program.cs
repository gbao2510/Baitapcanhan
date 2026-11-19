using System;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bai1_BroadcastServer
{
    internal static class Program
    {
        // port broadcast, phai giong voi port ben client
        private const int DefaultPort = 12790;

        // khoang cach giua 2 lan gui (mili giay)
        private const int IntervalMs = 3000;

        // cach chay: Bai1_BroadcastServer [tenServer] [diaChiBroadcast] [port]
        // vi du    : Bai1_BroadcastServer Server-B 192.168.1.255 12790
        private static void Main(string[] args)
        {
            string serverName = args.Length > 0 ? args[0] : "Server-A";

            // mac dinh gui den 255.255.255.255
            // neu chay tren nhieu may, nen doi sang dia chi broadcast cua LAN (vi du 192.168.1.255)
            IPAddress broadcastAddress = IPAddress.Broadcast;
            if (args.Length > 1)
            {
                if (!IPAddress.TryParse(args[1], out IPAddress parsed))
                {
                    Console.WriteLine("Dia chi broadcast khong hop le: " + args[1]);
                    return;
                }
                broadcastAddress = parsed;
            }

            int port = DefaultPort;
            if (args.Length > 2 && (!int.TryParse(args[2], out port) || port < 1 || port > 65535))
            {
                Console.WriteLine("Port khong hop le: " + args[2]);
                return;
            }

            string localIp = GetLocalIp();

            Console.WriteLine("=== BROADCAST SERVER ===");
            Console.WriteLine("Ten server : " + serverName);
            Console.WriteLine("IP may nay : " + localIp);
            Console.WriteLine("Gui den    : " + broadcastAddress + ":" + port + " (moi " + IntervalMs / 1000 + " giay)");
            Console.WriteLine("Nhan Enter de dung server.");
            Console.WriteLine();

            // buoc 1: tao socket UDP
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // buoc 2: bat tuy chon broadcast, neu khong bat thi SendTo den dia chi broadcast se bi loi
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

            // buoc 3: tao diem den (dia chi broadcast + port)
            IPEndPoint target = new IPEndPoint(broadcastAddress, port);

            // luong phu cho nguoi dung nhan Enter, sau do bao cho vong lap chinh biet de dung
            using ManualResetEvent stopSignal = new ManualResetEvent(false);
            Thread waitEnterThread = new Thread(() =>
            {
                Console.ReadLine();
                stopSignal.Set();
            });
            waitEnterThread.IsBackground = true;
            waitEnterThread.Start();

            int sentCount = 0;

            // buoc 4: gui lien tuc, moi lan cach nhau 3 giay
            // WaitOne vua dong vai tro Thread.Sleep(3000), vua thoat ngay khi nhan Enter
            do
            {
                string time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                string message = "SERVER|name=" + serverName + "|ip=" + localIp + "|port=" + port + "|time=" + time;

                // doi chuoi sang mang byte theo UTF-8
                byte[] data = Encoding.UTF8.GetBytes(message);

                try
                {
                    socket.SendTo(data, target);
                    sentCount++;
                    Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Da gui lan " + sentCount + ": " + message);
                }
                catch (SocketException ex)
                {
                    // vi du: may khong co mang, dia chi broadcast sai...
                    Console.WriteLine("Loi khi gui: " + ex.Message);
                }
            }
            while (!stopSignal.WaitOne(IntervalMs));

            Console.WriteLine();
            Console.WriteLine("Server da dung. Tong so thong bao da gui: " + sentCount);
        }

        // lay dia chi IPv4 cua card mang dang hoat dong (khong tinh loopback)
        private static string GetLocalIp()
        {
            try
            {
                foreach (NetworkInterface card in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (card.OperationalStatus != OperationalStatus.Up) continue;
                    if (card.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    foreach (UnicastIPAddressInformation info in card.GetIPProperties().UnicastAddresses)
                    {
                        if (info.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return info.Address.ToString();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // khong lay duoc thi dung dia chi mac dinh ben duoi
            }

            return "127.0.0.1";
        }
    }
}

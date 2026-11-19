using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bai1_BroadcastClient
{
    internal static class Program
    {
        // port broadcast, phai giong voi port ben server
        private const int DefaultPort = 12790;

        // cach chay: Bai1_BroadcastClient [port]
        private static void Main(string[] args)
        {
            int port = DefaultPort;
            if (args.Length > 0 && (!int.TryParse(args[0], out port) || port < 1 || port > 65535))
            {
                Console.WriteLine("Port khong hop le: " + args[0]);
                return;
            }

            // buoc 1: tao socket UDP
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                // cho phep nhieu client tren cung 1 may cung bind 1 port
                // (can thiet de chay 2 client tren 1 may khi kiem thu)
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // buoc 2: bind vao moi card mang (IPAddress.Any) tai port broadcast
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Khong bind duoc port " + port + ": " + ex.Message);
                Console.WriteLine("Hay thu doi sang port khac (doi dong bo ca server va client).");
                return;
            }

            Console.WriteLine("=== BROADCAST CLIENT ===");
            Console.WriteLine("Dang lang nghe tren port " + port + ". Nhan Enter de dung.");
            Console.WriteLine();

            // buoc 3: chay vong lap nhan tren luong rieng de luong chinh con cho nhan Enter
            Thread receiveThread = new Thread(() => ReceiveLoop(socket));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Console.ReadLine();

            // dong socket se lam ReceiveFrom nem loi, luong nhan thoat ra an toan
            socket.Close();
            receiveThread.Join(1000);

            Console.WriteLine("Client da dung.");
        }

        private static void ReceiveLoop(Socket socket)
        {
            // mang nhan du lieu khoang 1024 byte
            byte[] buffer = new byte[1024];
            int count = 0;

            while (true)
            {
                try
                {
                    // sender se duoc ghi dia chi + port cua may gui
                    EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                    // ham nay se cho (block) den khi co goi tin den
                    int length = socket.ReceiveFrom(buffer, ref sender);

                    string message = Encoding.UTF8.GetString(buffer, 0, length);
                    IPEndPoint senderEndPoint = (IPEndPoint)sender;
                    count++;

                    Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Goi so " + count
                        + " tu " + senderEndPoint.Address + ":" + senderEndPoint.Port);
                    Console.WriteLine("    " + message);
                }
                catch (SocketException)
                {
                    // socket da bi dong (nguoi dung nhan Enter) hoac loi mang -> thoat vong lap
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
    }
}

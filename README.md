# Chương 07 — UDP Broadcast và IP Multicast (C# .NET 10)

Bài thực hành lập trình mạng: gửi/nhận UDP Broadcast, giao diện Windows Forms phát hiện nhiều server, và phòng chat dùng IP Multicast.

## Mục lục

1. [Cấu trúc thư mục](#1-cấu-trúc-thư-mục)
2. [Cài đặt công cụ](#2-cài-đặt-công-cụ)
3. [Tải mã nguồn và mở project](#3-tải-mã-nguồn-và-mở-project)
4. [Chạy và kiểm thử từng bài](#4-chạy-và-kiểm-thử-từng-bài)
5. [Lỗi thường gặp](#5-lỗi-thường-gặp)
6. [Ảnh chụp và phần giải thích cần nộp](#6-ảnh-chụp-và-phần-giải-thích-cần-nộp)
7. [Đưa project lên GitHub bằng GitHub Desktop](#7-đưa-project-lên-github-bằng-github-desktop)
8. [Đối chiếu với tiêu chí chấm điểm](#8-đối-chiếu-với-tiêu-chí-chấm-điểm)

---

## 1. Cấu trúc thư mục

```
Chuong07_Broadcast/
├── Chuong07_Broadcast.sln          <- mở file này bằng Visual Studio
├── .gitignore                      <- loại bin/obj/.vs khỏi Git
├── README.md
├── Bai1_BroadcastServer/           <- Bài 1: console server (gửi broadcast mỗi 3 giây)
├── Bai1_BroadcastClient/           <- Bài 1: console client (nhận và hiển thị)
├── Bai2_BroadcastServerGUI/        <- Bài 2: GUI server (heartbeat)
├── Bai2_BroadcastClientGUI/        <- Bài 2: GUI client (danh sách nhiều server)
└── Bai3_MulticastChatGUI/          <- Bài 3: chat multicast
```

Thông số dùng chung (đúng theo đề):

| Thành phần | Giá trị |
|---|---|
| Giao thức | UDP |
| Broadcast | `255.255.255.255`, port `12790` |
| Multicast group | `224.100.0.1`, port `15000` |
| Encoding | UTF-8 |

Ghi chú về mã nguồn:

- Toàn bộ chú thích `//` trong code viết bằng tiếng Việt không dấu.
- Giao diện Windows Forms được dựng hoàn toàn bằng code (không có file `.Designer.cs`), nên mở bằng Visual Studio vẫn chạy bình thường nhưng cửa sổ Designer sẽ trống. Đây là chủ ý để mã dễ đọc và dễ nộp bài.
- Mỗi project là một chương trình độc lập. Các project GUI nhắm tới `net10.0-windows`, nên chỉ build được trên Windows.

---

## 2. Cài đặt công cụ

### 2.1. Cài .NET 10 SDK

1. Mở trang https://dotnet.microsoft.com/download/dotnet/10.0
2. Ở mục **SDK**, chọn bản **Windows x64** (Installer) và tải về. .NET 10 là bản LTS.
3. Chạy file vừa tải, bấm **Install** cho đến khi báo thành công.
4. Mở **Command Prompt** hoặc **PowerShell**, gõ lệnh sau để kiểm tra:
   ```
   dotnet --version
   ```
   Kết quả phải bắt đầu bằng `10.` (ví dụ `10.0.403`).

### 2.2. Cài Visual Studio 2026

Trang tải của Microsoft ghi .NET 10 được hỗ trợ trên **Visual Studio 2026**, vì vậy hãy dùng bản này (không dùng Visual Studio 2022).

1. Mở https://visualstudio.microsoft.com/downloads/ và tải bản **Community** (miễn phí).
2. Chạy file cài đặt. Ở màn hình **Workloads**, tích chọn **.NET desktop development** (Phát triển ứng dụng trên máy tính .NET).
3. Bấm **Install** và chờ hoàn tất (có thể mất vài chục phút).

> Nếu chỉ muốn dùng dòng lệnh hoặc VS Code thì chỉ cần .NET 10 SDK, các lệnh `dotnet build` / `dotnet run` ở dưới vẫn chạy được trên Windows.

### 2.3. Cài GitHub Desktop và tạo tài khoản GitHub

1. Nếu chưa có tài khoản, đăng ký tại https://github.com/signup
2. Tải GitHub Desktop tại https://desktop.github.com và cài đặt (bấm đúp file cài, đợi tự mở).

---

## 3. Tải mã nguồn và mở project

1. Tải file **`Chuong07_Broadcast.zip`** từ khung chat về máy.
2. Chuột phải file zip → **Extract All...** → bấm **Browse...** và đặt thư mục đích là `D:\Code\Chuong07_Broadcast` → **Extract**.
   - File zip chứa sẵn các file ở cấp ngoài cùng, nên thư mục đích chính là thư mục gốc của project. Hãy đặt tên thư mục đích cuối cùng là `Chuong07_Broadcast`.
   - Nên chọn đường dẫn ngắn, không dấu, không khoảng trắng.
   - Sau khi giải nén, trong `D:\Code\Chuong07_Broadcast` phải thấy ngay file `Chuong07_Broadcast.sln` (không bị lồng thêm một thư mục `Chuong07_Broadcast` nữa).
3. Bấm đúp **`Chuong07_Broadcast.sln`** để mở bằng Visual Studio. Ở cửa sổ **Solution Explorer** sẽ thấy 5 project.
4. Menu **Build → Rebuild Solution**. Cửa sổ Output báo `Rebuild All: 5 succeeded` là thành công.

Cách làm bằng dòng lệnh (không cần mở Visual Studio):

```
cd D:\Code\Chuong07_Broadcast
dotnet build
```

---

## 4. Chạy và kiểm thử từng bài

Có 3 cách chạy một project. Chọn cách nào cũng được:

- **Cách A — Visual Studio:** chuột phải project trong Solution Explorer → **Debug → Start New Instance**. Làm lại nhiều lần để mở nhiều cửa sổ cùng lúc.
- **Cách B — Dòng lệnh:** mở một Terminal cho mỗi chương trình, `cd` vào thư mục chứa file `.sln` rồi gõ `dotnet run --project <tên project>`.
- **Cách C — Chạy file exe:** vào thư mục `<project>\bin\Debug\net10.0\` (console) hoặc `<project>\bin\Debug\net10.0-windows\` (GUI) rồi bấm đúp file `.exe`. Bấm đúp nhiều lần để mở nhiều cửa sổ. Phải build trước thì mới có thư mục `bin`.

### Bài 1 — Console Broadcast Server và Client

Chạy **client trước, server sau** (để client không bỏ lỡ thông báo đầu tiên). Mở 3 cửa sổ Terminal:

```
dotnet run --project Bai1_BroadcastClient
dotnet run --project Bai1_BroadcastClient
dotnet run --project Bai1_BroadcastServer
```

Tham số tùy chọn (khi chạy trên nhiều máy hoặc muốn đổi tên/port):

```
dotnet run --project Bai1_BroadcastServer -- Server-A 192.168.1.255 12790
dotnet run --project Bai1_BroadcastClient -- 12790
```

Thứ tự tham số của server: `tenServer  diaChiBroadcast  port`.

Kiểm thử tối thiểu:

- [ ] Server gửi ít nhất 5 thông báo (mỗi 3 giây một lần).
- [ ] Cả hai client cùng nhận được thông báo.
- [ ] Client hiển thị địa chỉ IP và port của server.
- [ ] Nhấn **Enter** ở cửa sổ server để dừng an toàn (client cũng dừng bằng Enter).

### Bài 2 — GUI Broadcast Server và Client

1. Mở **3 cửa sổ Server GUI** (`Bai2_BroadcastServerGUI`). Đặt tên khác nhau: `Lab-01`, `Lab-02`, `Lab-03`. Giữ port `12790`, bấm **Start** ở cả ba.
2. Mở **1 Client GUI** (`Bai2_BroadcastClientGUI`), bấm **Start Listening**.
3. Kiểm tra:
   - [ ] Client hiển thị đủ 3 server, không có dòng bị nhân bản. Cột "Nhan gan nhat" tự cập nhật sau mỗi 3 giây.
   - [ ] Đổi nội dung ở ô trạng thái của một server (ví dụ `Busy`) thì dòng tương ứng trên client đổi theo.
   - [ ] Bấm **Stop** ở một server, sau khoảng 10 giây dòng đó chuyển sang `Offline` (chữ xám). Bấm **Start** lại thì dòng trở lại bình thường.
   - [ ] Kéo cửa sổ, gõ chữ, bấm nút trong lúc đang nghe: giao diện vẫn mượt, không bị treo.
   - [ ] Bấm **Clear** xóa sạch danh sách; đóng cửa sổ không để lại tiến trình chạy ngầm.

Ghi chú: ô **Dia chi broadcast** ở Server GUI mặc định là `255.255.255.255`. Khi chạy trên nhiều máy, hãy đổi sang địa chỉ broadcast của LAN (xem mục 5).

### Bài 3 — Multicast Chat GUI

1. Mở **3 cửa sổ** `Bai3_MulticastChatGUI`. Mỗi cửa sổ có sẵn một nickname khác nhau (`User` + mã tiến trình), có thể đổi thành `An`, `Binh`, `Chi`.
2. Bấm **Join** ở cả ba cửa sổ.
3. Gõ tin nhắn rồi bấm **Send** (hoặc nhấn Enter). Cả ba cửa sổ đều thấy tin nhắn: tin của mình màu xanh dương, tin của người khác màu xanh lá, kèm giờ nhận.
4. Bấm **Leave / Close** ở một cửa sổ (hoặc đóng hẳn cửa sổ): hai cửa sổ còn lại vẫn chat bình thường.
5. Thử gửi tin rỗng: chương trình bỏ qua, không gửi.

Phần mở rộng đã làm: **tô màu tin nhắn của mình / của người khác** và **nút Clear Chat**.

---

## 5. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| Không nhận được gói tin nào | Windows Firewall có thể đang chặn UDP. Khi chạy lần đầu, hộp thoại *Windows Defender Firewall has blocked some features...* hiện ra: tích **Private networks** rồi bấm **Allow access**. Nếu đã lỡ bấm Cancel, vào **Windows Security → Firewall & network protection → Allow an app through firewall** và cho phép các ứng dụng của bài ở mạng Private. |
| Nhận được ở máy này nhưng không nhận ở máy khác | Đổi sang địa chỉ broadcast đúng của LAN: gõ `ipconfig`, xem **IPv4 Address** và **Subnet Mask**. Ví dụ IP `192.168.1.10`, mask `255.255.255.0` thì địa chỉ broadcast là `192.168.1.255`. Với Wi-Fi, một số router bật chế độ **AP isolation** sẽ chặn broadcast/multicast giữa các máy. |
| Máy có VPN, VirtualBox, Hyper-V, WSL, nhiều card mạng | Gói tin có thể đi ra nhầm card mạng. Tạm tắt các card mạng ảo (gõ `ncpa.cpl` trong hộp Run, chuột phải card → Disable) khi kiểm thử. |
| Báo lỗi không bind được port | Port đang bị chương trình khác dùng. Đổi sang port khác (ví dụ `12791`) và đổi **thống nhất ở cả server lẫn client**. |
| Multicast chat không thấy tin nào, kể cả tin của chính mình | Thường do Firewall hoặc chọn sai card mạng như hai dòng trên. Máy phải ở mạng **Private**. |
| Lỗi `The current .NET SDK does not support targeting .NET 10.0` | Chưa cài .NET 10 SDK, hoặc đang dùng Visual Studio cũ. Cài lại theo mục 2. |
| Build GUI báo lỗi trên macOS/Linux | Project Windows Forms chỉ build được trên Windows. |

---

## 6. Ảnh chụp và phần giải thích cần nộp

### Ảnh chụp

- **Bài 1:** một ảnh chụp cửa sổ server và ít nhất hai client đang nhận dữ liệu.
- **Bài 2:** một ảnh chụp client hiển thị ít nhất 3 server (nên chụp thêm một ảnh có dòng `Offline`).
- **Bài 3:** một ảnh chụp ít nhất hai client đang trao đổi tin nhắn.

Gợi ý: tạo thư mục `AnhChup` trong project, đặt ảnh vào đó rồi commit cùng mã nguồn (xem mục 7.7).

### Bài 2 — Cách xử lý cập nhật GUI từ luồng nhận dữ liệu

Hàm `ReceiveFrom` là hàm chặn (blocking): nếu gọi trực tiếp trên luồng giao diện thì cửa sổ sẽ bị treo. Vì vậy client tạo một luồng nền (`Thread` với `IsBackground = true`) chạy vòng lặp nhận dữ liệu. Control của Windows Forms chỉ được phép truy cập từ luồng đã tạo ra nó, nên luồng nền không được sửa `ListView` trực tiếp. Mỗi khi nhận được một gói tin, luồng nền gọi `BeginInvoke` để đưa đoạn xử lý `HandleMessage` vào hàng đợi của luồng giao diện. Luồng giao diện mới là nơi tách chuỗi, cập nhật `Dictionary<string, ServerInfo>` và sửa `ListView`. Dùng `BeginInvoke` (bất đồng bộ) thay vì `Invoke` giúp luồng nhận không phải chờ giao diện xử lý xong và tiếp tục nhận gói tiếp theo. Việc kiểm tra timeout 10 giây dùng `System.Windows.Forms.Timer`, vốn chạy sẵn trên luồng giao diện nên không cần `Invoke`. Khi bấm Stop hoặc đóng cửa sổ, socket được đóng nên `ReceiveFrom` ném ngoại lệ và luồng nền tự thoát; trước khi gọi `BeginInvoke` chương trình kiểm tra form còn tồn tại (`IsDisposed`, `IsHandleCreated`) để tránh lỗi.

Server dùng `PeriodicTimer` kết hợp `async/await` bắt đầu từ luồng giao diện, nên mỗi lần gửi heartbeat đều quay lại luồng giao diện và cập nhật nhãn trạng thái an toàn.

Mỗi server được nhận diện bằng khóa `IP|tên server`. Không dùng riêng IP và port vì nhiều server có thể chạy trên cùng một máy với cùng một port broadcast, khi đó chỉ khác nhau ở tên. Nhờ khóa này, hai heartbeat liên tiếp của cùng một server chỉ cập nhật dòng cũ chứ không tạo dòng mới.

### Bài 3 — Broadcast khác Multicast ở điểm nào?

| | UDP Broadcast | IP Multicast |
|---|---|---|
| Đích gửi | Địa chỉ broadcast (`255.255.255.255` hoặc `x.x.x.255`) | Địa chỉ nhóm thuộc dải `224.0.0.0` – `239.255.255.255` (bài này dùng `224.100.0.1`) |
| Ai nhận | Mọi máy trong mạng con, dù có cần hay không | Chỉ các máy đã **tham gia nhóm** (join group) |
| Cần đăng ký | Không | Có, bằng `JoinMulticastGroup` (dùng giao thức IGMP) |
| Hiệu quả | Máy không liên quan vẫn phải xử lý gói tin | Máy không tham gia nhóm gần như không bị ảnh hưởng |
| Phạm vi | Chỉ trong một mạng LAN, router thường chặn | Có thể vượt qua router nếu mạng hỗ trợ multicast routing và TTL đủ lớn |
| Bật tùy chọn | Cần `SocketOptionName.Broadcast` ở bên gửi | Bên nhận cần join group; bên gửi chỉ cần gửi đến địa chỉ nhóm |

Tóm lại: broadcast là "hét cho cả mạng nghe", còn multicast là "nhắn vào một nhóm, ai vào nhóm thì nghe". Vì vậy broadcast phù hợp để dò tìm dịch vụ trong LAN (như Bài 2), còn multicast phù hợp cho nhóm chat hoặc phát dữ liệu đến nhiều người nhận (như Bài 3).

---

## 7. Đưa project lên GitHub bằng GitHub Desktop

### 7.1. Đăng nhập GitHub Desktop

1. Mở GitHub Desktop → **Sign in to GitHub.com**.
2. Trình duyệt mở ra, đăng nhập tài khoản GitHub và bấm **Authorize**. Quay lại GitHub Desktop.
3. Màn hình **Configure Git**: nhập tên và email (nên dùng email của tài khoản GitHub) → **Finish**.

### 7.2. Thêm thư mục project vào GitHub Desktop

1. Menu **File → Add local repository...** (phím tắt `Ctrl + O`).
2. Bấm **Choose...** và chọn thư mục `Chuong07_Broadcast` (thư mục chứa file `.sln`).
3. Vì thư mục chưa có Git, sẽ hiện cảnh báo *"This directory does not appear to be a Git repository"*. Bấm dòng chữ xanh **create a repository here instead**.
4. Hộp thoại **Create a new repository** hiện ra:
   - **Name:** `Chuong07_Broadcast`
   - **Description:** (tùy chọn) `Bai thuc hanh Chuong 07 - UDP Broadcast va IP Multicast`
   - **Local path:** để nguyên (đã tự điền)
   - **Initialize this repository with a README:** **bỏ tích** (project đã có sẵn `README.md`)
   - **Git ignore:** chọn **None** (project đã có sẵn `.gitignore`)
   - **License:** None
5. Bấm **Create repository**.

### 7.3. Kiểm tra danh sách file sẽ được commit

Ở tab **Changes** bên trái, kiểm tra:

- Có `.gitignore`, `README.md`, `Chuong07_Broadcast.sln` và các file trong 5 thư mục project.
- **Không** có thư mục `bin`, `obj`, `.vs`. Nếu thấy các thư mục này, hãy kiểm tra lại file `.gitignore` có nằm đúng ở thư mục gốc hay không.

### 7.4. Commit

1. Ở góc dưới bên trái, ô **Summary** gõ: `Hoan thanh bai thuc hanh chuong 07`.
2. Bấm nút xanh **Commit to main**.

### 7.5. Publish lên GitHub

1. Ở thanh trên cùng bấm **Publish repository**.
2. Kiểm tra **Name** và **Description**.
3. Ô **Keep this code private**:
   - Nếu giảng viên nhận bài qua đường link, bỏ tích để repository là **Public**, hoặc giữ **Private** rồi thêm giảng viên làm collaborator (GitHub → Settings → Collaborators). Hãy hỏi giảng viên cách nhận bài.
4. Bấm **Publish Repository**.

### 7.6. Xem kết quả trên GitHub

1. Menu **Repository → View on GitHub** (hoặc `Ctrl + Shift + G`) để mở repository trên trình duyệt.
2. Sao chép đường link (dạng `https://github.com/<ten-ban>/Chuong07_Broadcast`) để nộp bài.

### 7.7. Bổ sung ảnh chụp hoặc sửa code sau này

1. Tạo thư mục `AnhChup` trong `Chuong07_Broadcast` và bỏ các ảnh chụp vào.
2. Quay lại GitHub Desktop, tab **Changes** sẽ tự hiện các file mới. Nhập Summary (ví dụ `Them anh chup`) → **Commit to main**.
3. Bấm **Push origin** ở thanh trên để đẩy lên GitHub.

Quy trình cho mọi lần sửa sau này luôn là: **sửa file → Commit to main → Push origin**.

### 7.8. Sự cố thường gặp với Git

| Hiện tượng | Cách xử lý |
|---|---|
| Thấy hàng nghìn file `bin`/`obj` ở tab Changes | `.gitignore` chưa nằm ở thư mục gốc, hoặc đã commit nhầm trước đó. Đảm bảo `.gitignore` ở cùng cấp với file `.sln`. Nếu đã commit nhầm, nhờ giảng viên hoặc bạn bè hướng dẫn xóa khỏi lịch sử. |
| Publish báo repository đã tồn tại | Trên GitHub bạn đã có repository trùng tên. Đổi **Name** khi Publish. |
| Đăng nhập thất bại | **File → Options → Accounts** → Sign out rồi Sign in lại. |

---

## 8. Đối chiếu với tiêu chí chấm điểm

| Tiêu chí | Điểm | Nơi thực hiện |
|---|---|---|
| Bài 1 hoạt động đúng: gửi, nhận, hiển thị endpoint | 3 | `Bai1_BroadcastServer`, `Bai1_BroadcastClient` |
| Bài 2 hiển thị nhiều server, không trùng dòng | 3 | `Bai2_BroadcastClientGUI` (`Dictionary` theo khóa `IP\|tên`) |
| Bài 2 không treo GUI, có xử lý dừng/timeout | 1 | Luồng nền + `BeginInvoke`, `Timer` 1 giây, Offline sau 10 giây |
| Bài 3 multicast chat tối thiểu giữa hai client | 2 | `Bai3_MulticastChatGUI` |
| Tổ chức mã, xử lý lỗi, giải thích Broadcast/Multicast | 1 | `try/catch`, giải phóng socket khi đóng form, mục 6 của README |
| **Tổng** | **10** | |

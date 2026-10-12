# Chạy website GymHung bằng Docker trên máy Windows

Gói gồm frontend Nginx (bản build hiện có trong frontend/dist) và API .NET 9.
Frontend source hiện thiếu src/main.tsx và scripts/run.mjs, vì vậy image dùng bản
build hiện có, không tuyên bố build lại được frontend từ source.

## Khởi động

Mở Docker Desktop ở chế độ Linux containers. Tại thư mục này chạy PowerShell:

```powershell
Copy-Item .env.docker.example .env.docker
notepad .env.docker
docker compose --env-file .env.docker up --build -d
docker compose ps
```

Mở http://localhost:18088. Cổng 18088 tránh xung đột website đang chạy cổng 3107.
API đi qua /api trên cùng website; không cần mở riêng cổng backend.

## Database bắt buộc để đăng nhập và sử dụng nghiệp vụ

Thư mục này không có SQL tạo schema gymhung_demo hoặc backup database.
Container không tự tạo bảng, seed hay sao chép database hiện có.
Chỉnh GYM_DATABASE_CONNECTION trong .env.docker để trỏ tới SQL Server có
database gymhung_demo và đủ các bảng của dự án.

SQL Server trên Windows phải bật TCP/IP, có cổng TCP cố định (ví dụ 1433), cho
phép kết nối từ Docker Desktop, và có tài khoản SQL được cấp quyền cho database.
Không sử dụng Trusted_Connection/Integrated Security của Windows trong gói này.
host.docker.internal trỏ về máy Windows; không dùng .\SQLEXPRESS hoặc localhost
trong connection string của container. Không gửi mật khẩu vào chat.

Máy hiện tại: đã xác nhận gymhung_demo tồn tại và SQL Server chỉ bật Windows
Authentication. Để hoàn tất kết nối container:

1. Trong SQL Server Management Studio, kết nối `.\SQLEXPRESS` bằng Windows
   Authentication. Chuột phải server > Properties > Security > chọn SQL Server
   and Windows Authentication mode.
2. Trong SQL Server Configuration Manager > SQL Server Network Configuration >
   Protocols for SQLEXPRESS, bật TCP/IP. Trong TCP/IP Properties > IP Addresses
   > IPAll, xóa TCP Dynamic Ports và đặt TCP Port thành cổng trống, ví dụ 1433.
3. Restart SQL Server (SQLEXPRESS) để áp dụng. Việc này tạm ngắt các ứng dụng
   đang sử dụng SQL Server, nên thực hiện lúc phù hợp.
4. Trong SSMS > Security > Logins > New Login, tạo login SQL `gymhung_docker`
   với mật khẩu riêng. Trong User Mapping chọn gymhung_demo và cấp quyền
   db_datareader, db_datawriter, quyền EXECUTE cho user nếu nghiệp vụ cần.
   Ví dụ quyền EXECUTE: `USE gymhung_demo; GRANT EXECUTE TO gymhung_docker;`.
5. Sửa .env.docker với cổng/tài khoản/mật khẩu thực tế rồi chạy:
   `docker compose --env-file .env.docker up -d`.

Không cần tạo database mới hoặc xóa dữ liệu hiện có.

Nếu chạy trên máy khác, cần mang theo bản backup/SQL database và restore riêng.
Không commit .env.docker vì có thể chứa mật khẩu.

## AI và PDF

Các dịch vụ AI cổng 2024 và xử lý PDF cổng 3000 vẫn chạy bên ngoài Docker.
Chỉnh GYM_AI_URL và GYM_PDF_URL nếu thay đổi địa chỉ dịch vụ. Gói này không bao
gồm Ollama/model hoặc toàn bộ dịch vụ chatbot; AI cần các dịch vụ này hoạt động.

## Kiểm tra và dừng

```powershell
Invoke-WebRequest http://localhost:18088 -UseBasicParsing
docker compose logs --tail 80 api
docker compose logs --tail 80 web
docker compose down
```

Giao diện tải được chưa có nghĩa database hoạt động. Kiểm tra đăng nhập bằng
tài khoản đã có trong database để xác nhận kết nối và dữ liệu.

Đây là gói chạy thử local HTTP (Development), không phải cấu hình triển khai
Internet. Khi triển khai cần cấu hình HTTPS và JWT key riêng.

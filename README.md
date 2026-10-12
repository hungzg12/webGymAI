# Website quản lý phòng Gym tích hợp AI — Nhóm 3

React, ASP.NET Core .NET 9, SQL Server và AI qua Ollama/LangGraph.

## Nội dung

- `gymhung-backend`: API, nghiệp vụ, mô hình dữ liệu và kiểm thử xUnit.
- `frontend/dist`: bản React chạy được; `frontend/readable` chứa phần giao diện check-in/điểm danh mới.
- `ai-pdf-chatbot-langchain`: dịch vụ AI và tài liệu cấu hình riêng.

## Chạy demo local

Cần .NET SDK 9, Node.js và SQL Server có database/schema của dự án. Repository không chứa dữ liệu cá nhân, bản backup SQL hoặc thông tin bí mật. Cấu hình connection string và khóa JWT bằng biến môi trường; khóa JWT phải có ít nhất 32 byte.

```powershell
$env:ConnectionStrings__GymDatabase = 'Server=.\SQLEXPRESS;Database=gymhung_demo;Trusted_Connection=True;TrustServerCertificate=True;'
$env:Jwt__Key = '<khóa riêng ít nhất 32 byte>'
cd gymhung-backend
dotnet run --project GymHung.Api --launch-profile http
```

Trong terminal khác, tại thư mục gốc:

```powershell
node frontend/serve-local.mjs
```

Mở http://localhost:3107; API dùng cổng 5042. AI cần dịch vụ cấu hình tại `Ai__BaseUrl` (mặc định localhost:2024), Ollama và model tương ứng. Xem README của từng dịch vụ. Docker: xem `DOCKER-README.md`; tự tạo file môi trường với giá trị riêng trước khi chạy.

## Các nội dung đã sửa

- Thanh toán theo đăng ký; sử dụng gói không thu tiền lại.
- Check-in chọn gói đã thanh toán, còn hạn; trừ một buổi với gói có hạn mức.
- Điểm danh PT kiểm tra booking, phân công PT và gói PT riêng; chống trừ lặp và lưu lịch sử.
- AI tra cứu lịch cá nhân có phân quyền; gợi ý gói và lộ trình qua model, lưu gợi ý để người dùng xem xét.

## Kiểm tra và giới hạn

```powershell
cd gymhung-backend
dotnet build
dotnet test GymHung.Tests
```

Bản sửa đã build thành công, qua 30 kiểm tra API trên SQL Server và hai câu trả lời Ollama thực. Lượt kiểm thử đơn vị ban đầu: 14 test thành công; lượt chạy lại trên máy phát triển bị Windows Application Control chặn test adapter.

Thư mục `frontend/src` và `frontend/scripts/run.mjs` bị thiếu trong bản dự án đầu vào. Repository giữ bản frontend đã biên dịch và mã bổ sung đọc được; chưa thể build lại toàn bộ frontend từ nguồn hiện có. Thanh toán VNPay/Momo đang là luồng demo, cần tích hợp cổng thực để triển khai thương mại.

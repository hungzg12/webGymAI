# Các tác nhân AI GYM HUNG

Trang AI có bộ chọn: Tự động, Tư vấn tập luyện, Dữ liệu phòng gym, Đọc PDF. Đây là ba node chuyên trách dùng chung model Ollama qwen3:4b, không phải ba model hay ba tiến trình độc lập.

- `fitnessAgent`: tư vấn vận động chung, không nhận JSON SQL hay lịch sử pha trộn.
- `databaseAgent`: nhận JSON do AiSqlContext đọc theo quyền tài khoản. Không truy cập SQL trực tiếp và không ghi dữ liệu.
- `pdfAgent`: truy xuất tài liệu bằng câu hỏi mới, không nhận SQL. Không tìm thấy nội dung thì trả thông báo rõ ràng.

AiService xác định tác nhân từ lựa chọn hoặc từ khóa câu hỏi mới. Tiền tố nội bộ `[agent:fitness|database|pdf]` được bỏ trước khi lưu tin nhắn. Graph định tuyến không cần lượt gọi model phân loại. Mỗi lượt chỉ chạy một tác nhân.

Chạy: giữ frontend webgym, API 5042, Ollama 11434 và LangGraph 2024 hoạt động. Để tải PDF, dịch vụ Next.js 3000 cũng phải chạy (`npm.cmd run dev --workspace frontend -- --hostname 127.0.0.1 --port 3000`).

Kiểm tra: .NET build đạt; 4 Jest test định tuyến đạt; TypeScript đạt khi bỏ noUnusedLocals (kiểm tra đầy đủ còn lỗi có sẵn demo.ts import graph không dùng). API đã kiểm tra cả ba lựa chọn: giá BASIC 590.000 đồng, hướng dẫn khởi động, PDF báo không tìm thấy tài liệu phù hợp.

Giới hạn: kho PDF local hiện dùng chung trên máy, chưa tách theo tài khoản. Chưa kiểm tra upload PDF mới trong lần thay đổi này. Routing tự động là luật từ khóa, câu hỏi mơ hồ nên chọn tác nhân bằng tay. Nội dung sinh từ model vẫn cần người dùng đánh giá.


# Chatbot Webgym chạy local

Không cần khóa Gemini, OpenAI hoặc Supabase. PDF và vector tìm kiếm được lưu trong `backend/data/documents.json`.

Ollama dùng `qwen3:4b` để trả lời và `nomic-embed-text` để tìm kiếm tài liệu. Máy cần tải mô hình lần đầu; sau đó hỏi đáp có thể chạy khi không có Internet.

Mở thư mục dự án này trong VS Code. Chạy `./start-local.ps1` để khởi động dịch vụ Ollama nếu chưa chạy.

Terminal backend:

```powershell
cd backend
node ../node_modules/@langchain/langgraph-cli/dist/cli/cli.mjs dev --no-browser --host 127.0.0.1 -n 1
```

Terminal giao diện:

```powershell
npm run dev --workspace frontend -- --hostname 127.0.0.1 --port 3000
```

Mở `http://127.0.0.1:3000`, hoặc nút Chat PDF trong webgym. Bấm kẹp giấy để tải PDF có văn bản, đợi thông báo hoàn tất, rồi hỏi về tài liệu. PDF chỉ chứa ảnh cần OCR trước.

Kho PDF dùng chung trên máy; lịch sử cuộc hội thoại của backend phát triển có thể mất khi khởi động lại.


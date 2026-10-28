---
description: # WORKFLOW: TEST CODE & VALIDATION
---

## 1. GIAI ĐOẠN THIẾT LẬP (Setup)
- Phân tích: Xác định các "cạnh" của logic (edge cases) trong module cần test (ví dụ: khi crawler gặp 404, khi memory vượt ngưỡng, khi input bị null).
- Hành động: Tạo Artifact `Test_Plan.md` liệt kê 3-5 kịch bản kiểm thử quan trọng nhất.

## 2. GIAI ĐOẠN VIẾT TEST (Execution)
- Hành động:
  + Viết file test (dùng Jest/Mocha hoặc file script chạy thử `test.js`).
  + Ưu tiên: "Happy path" (luồng chạy đúng) và "Error path" (luồng xử lý lỗi).
  + Code test: Phải sạch, có comment tiếng Việt giải thích mục đích test case đó.
- Output: Đưa code test vào Artifact.

## 3. GIAI ĐOẠN CHẠY & FIX (Run & Debug)
- Hành động:
  + Chạy file test trong terminal tích hợp.
  + Nếu có lỗi: Agent phải tự phân tích log lỗi (stderr) -> Tự đưa ra phương án sửa (Diff) -> Hỏi ý kiến tôi trước khi Apply.

## 4. GIAI ĐOẠN TỔNG KẾT (Summary)
- Hành động: 
  + Chỉ báo kết quả: "Pass/Fail".
  + Nếu Fail: Phân tích nguyên nhân gốc rễ (Root Cause).
  + Nếu Pass: Xóa các file test tạm (nếu cần) hoặc giữ lại trong folder `/tests`.
- Output: "Task Test hoàn tất - [Tên Module]".
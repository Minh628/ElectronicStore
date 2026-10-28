---
description: # WORKFLOW: REFACTORING & OPTIMIZATION
---

## 1. GIAI ĐOẠN ĐÁNH GIÁ (Assessment)
- Hành động:
    + Đọc file cần refactor (chỉ định file).
    + Tìm các "Code Smells": Hàm quá dài, các logic lặp lại, biến không dùng tới, hoặc đoạn code khó đọc.
- Output: Tạo Artifact `Refactor_Audit.md` liệt kê các vị trí cần cải thiện và lý do (Ví dụ: "Hàm này có độ phức tạp cao, cần tách nhỏ").

## 2. GIAI ĐOẠN THỰC THI (Refactor)
- Sau khi tôi "Accept" bản Audit:
    + Thực hiện refactor từng hàm/module trong các Artifact riêng biệt.
    + Ưu tiên: Tối ưu hiệu năng , dễ đọc, dễ bảo trì.
    + Nguyên tắc: KHÔNG thay đổi logic nghiệp vụ (Behavior-Preserving Refactoring). Chỉ thay đổi cấu trúc code.
    + Comment: Cập nhật comment tiếng Việt nếu thay đổi cấu trúc làm thay đổi luồng logic.

## 3. GIAI ĐOẠN KIỂM CHỨNG (Verification)
- Hành động:
    + So sánh: Dùng `git diff` để kiểm tra các thay đổi.
    + Kiểm tra chéo: Agent phải trả lời câu hỏi: "Sau khi refactor, có làm ảnh hưởng đến các file khác trong hệ thống không?"

## 4. GIAI ĐOẠN TỔNG KẾT (Final Review)
- Output: Chỉ báo "Task Refactor hoàn tất - [Tên Module]".
- Cập nhật tài liệu: Nếu việc refactor làm thay đổi tên hàm hoặc cách gọi API, hãy tự động cập nhật ngay vào `README.md`và ## Folder Structure trong README.(nếu có).
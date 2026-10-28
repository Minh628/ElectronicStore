---
description: # WORKFLOW: RECURSIVE DEEP THINKING & MULTI-ROUND ARCHITECTURE REVIEW
---

## 1. NGUYÊN TẮC SUY NGHĨ SÂU LIÊN HOÀN (Recursive Thinking Protocol)
- **Tập trung cao độ**: TUYỆT ĐỐI KHÔNG viết code ngay khi chưa hoàn thành chuỗi tư duy đa tầng.
- **Artifact trung tâm**: Mọi suy nghĩ, phản biện, sơ đồ, phân tích rủi ro phải được ghi và cập nhật liên tục vào Artifact `Deep_Thought.md`.
- **Cơ chế lặp (Loop)**: Tư duy không dừng ở 1 lần phân tích bề nổi, mà bắt buộc trải qua tối thiểu 3 vòng phản biện liên tiếp.
---
## 2. CÁC VÒNG TƯ DUY ĐA TẦNG (Multi-Round Iterations)
### VÒNG 1: QUAN SÁT & ĐÀO SÂU GỐC RỄ (Root-Cause Deep Dive)
- Thu thập toàn bộ ngữ cảnh, đọc mã nguồn các phân hệ liên quan (Backend, Frontend, CSDL, Data flow).
- Lập bảng đối chiếu chi tiết: Hiện trạng vs Chuẩn mực vs Kỳ vọng.
- Liệt kê 3 phương án giải quyết (Phương án tối thiểu, Phương án tối ưu, Phương án toàn diện).
### VÒNG 2: PHẢN BIỆN TÀN NHẪN & STRESS-TEST (Adversarial Critique)
- Tự đóng vai **Senior Architect / Devil's Advocate** khó tính nhất:
  + Tìm ít nhất 3-5 điểm mù, kịch bản lỗi biên (edge cases, race conditions, memory leaks, null references, broken state).
  + Kiểm tra tính đồng bộ giữa các platform/công nghệ (ví dụ: Blazor vs MVC vs Razor).
  + Đặt câu hỏi: "Nếu dữ liệu tăng gấp 10 lần thì kiến trúc này vỡ ở đâu? Người dùng bấm liên tục thì sao?"
- Bổ sung các phát hiện vào `Deep_Thought.md`.
### VÒNG 3: TINH CHẾ & TỐI THƯỢNG HÓA KIẾN TRÚC (Deep Distillation)
- Tổng hợp các phản biện từ Vòng 2 để loại bỏ điểm yếu.
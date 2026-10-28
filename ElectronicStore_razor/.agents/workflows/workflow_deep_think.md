---
description: # WORKFLOW: DEEP THINKING & ARCHITECTURE REVIEW
---

## 1. GIAI ĐOẠN ĐỘNG NÃO (Deep Think)
- Yêu cầu: Đừng viết code ngay. Hãy tạo Artifact `Deep_Thought.md`.
- Hành động: 
    + Liệt kê 3 cách tiếp cận vấn đề (Cách dễ, Cách tối ưu, Cách dự phòng).
    + Phân tích rủi ro: Nếu làm cách này ví dụ thì bộ nhớ RAM có tăng không? Có bị block IP không?
    + Chọn lựa: Giải thích tại sao chọn cách hiện tại.

## 2. GIAI ĐOẠN PHẢN BIỆN (Self-Correction)
- Hành động: Agent tự đóng vai "Người phản biện":
    + Tìm lỗi sai trong chính bản thiết kế của mình ở bước 1.
    + Đưa ra phương án khắc phục các lỗ hổng đó.
- Output: Cập nhật `Deep_Thought.md` với các điểm đã được phản biện.

## 3. GIAI ĐOẠN CHỐT GIẢI PHÁP (Commit)
- Hành động: Sau khi tôi "Accept" bản tư duy này, Agent mới được phép chuyển sang các Workflow khác (như Feature-Build hoặc Test).
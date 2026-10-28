---
description: # WORKFLOW: RESEARCH & DESIGN (NGHIÊN CỨU & THIẾT KẾ)
---

## 1. GIAI ĐOẠN PHÂN TÍCH (Analysis)
- Phân tích yêu cầu: Xác định file cần thay đổi, các dependencies liên quan.
- Tra cứu Rule: Đọc quy tắc chung đã được set từ trước (không bắt buộc Clean Architecture).
- Output: Tạo Artifact `Feature_Plan.md` trình bày: 
    + Danh sách file bị ảnh hưởng. 
    + Luồng logic thay đổi.

## 2. GIAI ĐOẠN THỰC THI (Execution)
- Sau khi tôi "Accept" Step 1:
    + Viết code: Luôn đặt trong Artifact. Sử dụng comment tiếng Việt cho các logic quan trọng.
    + Tối ưu: Nếu là file hiện có, chỉ trả về đoạn Diff code cần thay đổi.
    + Kiểm tra chéo: Agent tự kiểm tra tính đúng đắn của logic code.

## 3. GIAI ĐOẠN TỰ ĐỘNG HÓA TÀI LIỆU (Documentation)
- Sau khi tôi "Apply" code:
    + Tự động cập nhật Artifact `README.md`.
    + Định dạng: Markdown.
    + Nội dung:
        1. Mục [Thay đổi gần đây]: Tóm tắt kỹ thuật (tập trung vào tính năng, không liệt kê lan man).
        2. Mục [API/Function]: Cập nhật signature hoặc endpoint mới.
    + Kiểm tra: README phải đồng bộ hoàn toàn với code vừa thay đổi và cập nhật lại ## Folder Structure trong README.(nếu có).

## 4. GIAI ĐOẠN TỔNG KẾT (Final Review)
- Agent tự kiểm tra lần cuối: 
    + Đã có comment tiếng Việt ở các phần logic quan trọng chưa?
    + README đã khớp với code chưa?
    + Có đoạn code thừa nào cần xóa không?
- Output: Chỉ báo "Task Đã hoàn tất nha ANH MINH - [Tên Task]" và đợi xác nhận cuối cùng của tôi.
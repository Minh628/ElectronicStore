---
description: # WORKFLOW: GIT COMMIT AUTOMATION
---

## 1. GIAI ĐOẠN KIỂM TRA TRẠNG THÁI (Check Status)
- Hành động: Agent tự động chạy lệnh `git status` để lấy danh sách các file đã được staged, modified hoặc untracked.
- Phân tích: Đọc các thay đổi (diff) của các file đã staged để hiểu nội dung thay đổi là gì.

## 2. GIAI ĐOẠN GỢI Ý COMMIT (Suggest)
- Hành động: Dựa trên diff của các file, tạo 3 phương án đặt tên commit theo định dạng Conventional Commits:
    + Phương án 1 (Ngắn gọn): `type: message ngắn`
    + Phương án 2 (Chi tiết): `type(scope): message mô tả rõ thay đổi`
    + Phương án 3 (Sáng tạo): `type: message tập trung vào tác động kỹ thuật`
- Lưu ý: Dùng tiếng ANH cho phần mô tả trong commit message.

## 3. GIAI ĐOẠN XÁC NHẬN (Finalize)
- Hành động: Hiển thị 3 phương án trong một Artifact.
- Output: Chờ tôi chọn phương án hoặc yêu cầu chỉnh sửa. Nếu tôi chọn, agent sẽ chuẩn bị lệnh `git commit -m "message"` tương ứng.
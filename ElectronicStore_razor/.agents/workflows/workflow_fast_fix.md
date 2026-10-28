---
description: # WORKFLOW: FAST-FIX (SỬA LỖI NHANH)
---

# WORKFLOW: FAST-FIX (SỬA LỖI NHANH)

## 1. GIAI ĐOẠN XÁC ĐỊNH LỖI (Identify)
- Yêu cầu: Đọc log lỗi hoặc mô tả bug từ tôi.
- Hành động: Chỉ ra dòng code hoặc logic gây lỗi ngắn gọn trong 1-2 câu. KHÔNG giải thích dông dài.

## 2. GIAI ĐOẠN THỰC THI (Execute)
- Hành động:
  + Mở/Truy cập đúng file cần sửa thông qua Artifact.
  + Chỉ trả về đúng đoạn mã cần thay đổi (dưới dạng Diff/Patch), tuyệt đối không in lại toàn bộ file.
  + Nếu có rule về comment tiếng Việt trong dự án, đảm bảo giữ nguyên hoặc cập nhật phần comment quanh khu vực sửa lỗi.

## 3. GIAI ĐOẠN KIỂM TRA (Verify)
- Agent tự rà soát:
  + Cú pháp đã chuẩn xác chưa?
  + Lỗi đã được triệt tiêu hoàn toàn chưa?
  + Kiểm tra: README phải đồng bộ hoàn toàn với code vừa thay đổi và cập nhật lại ## Folder Structure trong README.(nếu có).
- Output: Thông báo "Đã fix xong lỗi tại file [Tên file]. Sẵn sàng để Apply và commit."
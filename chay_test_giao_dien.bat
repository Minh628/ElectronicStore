@echo off
chcp 65001 >nul
title [ElectroStore] Master Playwright E2E Test Suite - 3 Dự Án
color 0b
echo ======================================================================
echo 🎬 MASTER PLAYWRIGHT E2E SUITE: CHẠY ĐẦY ĐỦ CẢ 3 DỰ ÁN
echo    1. ElectronicStore_mvc    (Port 5010)
echo    2. ElectronicStore_razor  (Port 5011)
echo    3. ElectronicStore_blazor (Port 5012)
echo ======================================================================
echo Cửa sổ Google Chrome sẽ tự động bật lên và thực hiện các thao tác:
echo  - Đăng ký tài khoản thành viên mới
echo  - Lọc danh mục Sub-Nav, lọc thương hiệu Apple, tìm kiếm iPhone
echo  - Xem chi tiết sản phẩm, thêm giỏ hàng, tăng số lượng
echo  - Đặt hàng thanh toán COD, kiểm tra OrderSuccess
echo  - Quản lý đơn hàng, hủy đơn kiểm tra hoàn kho
echo  - Đăng nhập Admin, Toggle ẩn danh mục, quản trị đơn hàng & KPI
echo.
python "C:\Users\Raymond\.gemini\antigravity\brain\9b3667a9-5357-4e1b-9317-311247261baf\scratch\run_master_playwright_3projects.py"
echo.
echo ======================================================================
echo 🏆 ĐÃ HOÀN THÀNH KIỂM THỬ TRỰC QUAN CẢ 3 DỰ ÁN!
echo 🎥 3 Video ghi hình được lưu tại thư mục: test_recordings\
echo ======================================================================
pause

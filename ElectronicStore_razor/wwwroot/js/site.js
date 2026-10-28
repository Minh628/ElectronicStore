// ==========================================
// ELECTROSTORE - INTERACTION & CLIENT LOGIC
// ==========================================

// 1. Hàm hiển thị Toast thông báo hiện đại
function showToast(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = 'electro-toast';
    
    const icon = type === 'success' 
        ? '<svg width="20" height="20" fill="none" stroke="#10b981" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>'
        : '<svg width="20" height="20" fill="none" stroke="#f43f5e" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/></svg>';

    toast.innerHTML = `${icon} <span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100%)';
        toast.style.transition = 'all 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3200);
}

// 2. Hàm Thêm sản phẩm vào giỏ hàng bằng AJAX
async function handleAddToCart(productId, quantity = 1, event = null) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }

    try {
        const formData = new FormData();
        formData.append('productId', productId);
        formData.append('quantity', quantity);

        // Lấy token chống giả mạo (Antiforgery) nếu có
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput) {
            formData.append('__RequestVerificationToken', tokenInput.value);
        }

        const response = await fetch('/Index?handler=AddToCart', {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                showToast(result.message, 'success');
                // Cập nhật số lượng hiển thị trên Badge giỏ hàng
                const badge = document.getElementById('cart-count-badge');
                if (badge) {
                    badge.innerText = result.cartCount;
                    badge.classList.remove('d-none');
                }
            } else {
                showToast(result.message, 'error');
            }
        } else {
            showToast('Không thể kết nối đến máy chủ!', 'error');
        }
    } catch (err) {
        console.error(err);
        showToast('Đã có lỗi xảy ra khi thêm vào giỏ!', 'error');
    }
}

// 3. Hàm định dạng tiền tệ VND
function formatVND(amount) {
    return new Intl.NumberFormat('vi-VN').format(amount) + ' ₫';
}

// 4. Hàm Tăng/Giảm số lượng trong Giỏ Hàng bằng AJAX (KHÔNG F5 RELOAD TRANG)
async function changeCartQtyAjax(productId, delta, maxStock) {
    const qtySpan = document.getElementById(`item-qty-${productId}`);
    if (!qtySpan) return;

    let currentQty = parseInt(qtySpan.innerText) || 1;
    let newQty = currentQty + delta;

    if (newQty < 1) {
        // Nếu giảm dưới 1 thì hỏi xóa
        if (confirm('Bạn muốn xóa thiết bị này khỏi giỏ hàng?')) {
            removeCartItemAjax(productId);
        }
        return;
    }

    if (newQty > maxStock) {
        showToast(`Sản phẩm này chỉ còn tối đa ${maxStock} thiết bị trong kho!`, 'error');
        return;
    }

    try {
        const formData = new FormData();
        formData.append('productId', productId);
        formData.append('quantity', newQty);

        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput) {
            formData.append('__RequestVerificationToken', tokenInput.value);
        }

        const response = await fetch('/Cart?handler=UpdateQuantity', {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                // Cập nhật số lượng ngay trên giao diện
                qtySpan.innerText = result.itemQuantity;

                // Cập nhật thành tiền của món hàng
                const itemTotalEl = document.getElementById(`item-total-${productId}`);
                if (itemTotalEl) {
                    itemTotalEl.innerText = formatVND(result.itemTotal);
                }

                // Cập nhật tổng tiền đơn hàng
                updateTotalAmounts(result.totalAmount, result.cartCount);

                // Cập nhật nút tăng (+) nếu đã đạt tồn kho tối đa
                const plusBtn = document.getElementById(`btn-plus-${productId}`);
                if (plusBtn) {
                    plusBtn.disabled = (result.itemQuantity >= maxStock);
                }
            }
        } else {
            showToast('Không thể cập nhật số lượng!', 'error');
        }
    } catch (err) {
        console.error(err);
        showToast('Lỗi khi cập nhật giỏ hàng!', 'error');
    }
}

// 5. Hàm Xóa sản phẩm khỏi giỏ hàng bằng AJAX
async function removeCartItemAjax(productId) {
    try {
        const formData = new FormData();
        formData.append('productId', productId);

        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput) {
            formData.append('__RequestVerificationToken', tokenInput.value);
        }

        const response = await fetch('/Cart?handler=Remove', {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                showToast('Đã xóa thiết bị khỏi giỏ hàng!', 'success');

                // Hiệu ứng mờ dần và xóa dòng sản phẩm
                const row = document.getElementById(`cart-row-${productId}`);
                if (row) {
                    row.style.transition = 'all 0.3s ease';
                    row.style.opacity = '0';
                    row.style.transform = 'scale(0.95)';
                    setTimeout(() => {
                        row.remove();
                        // Nếu giỏ hàng trống thì tải lại trang để hiện trạng thái trống
                        if (result.isEmpty) {
                            window.location.reload();
                        }
                    }, 300);
                }

                // Cập nhật tổng tiền và badge
                updateTotalAmounts(result.totalAmount, result.cartCount);
            }
        }
    } catch (err) {
        console.error(err);
        showToast('Lỗi khi xóa sản phẩm!', 'error');
    }
}

// Hàm phụ trợ cập nhật các vị trí hiển thị tổng tiền
function updateTotalAmounts(totalAmount, cartCount) {
    // 1. Badge trên navbar (hỗ trợ cả id và class)
    const badge = document.getElementById('cart-count-badge') || document.querySelector('.nav-cart-badge');
    if (badge) {
        badge.innerText = cartCount;
        if (cartCount > 0) {
            badge.classList.remove('d-none');
        } else {
            badge.classList.add('d-none');
        }
    }

    // 2. Tạm tính & Tổng thanh toán (hỗ trợ cả cart-subtotal và summary-subtotal)
    const subTotalEl = document.getElementById('cart-subtotal') || document.getElementById('summary-subtotal');
    if (subTotalEl) {
        subTotalEl.innerText = formatVND(totalAmount);
    }

    const totalEl = document.getElementById('cart-total') || document.getElementById('summary-total');
    if (totalEl) {
        totalEl.innerText = formatVND(totalAmount);
    }
}

// 6. Hàm Thêm vào giỏ từ Trang Chi Tiết Sản Phẩm (ProductDetail) qua AJAX
async function handleDetailAddToCart(productId, event) {
    if (event) {
        event.preventDefault();
    }
    const qtyInput = document.querySelector('input[name="quantity"]');
    const qty = qtyInput ? parseInt(qtyInput.value) || 1 : 1;
    await handleAddToCart(productId, qty, event);
}

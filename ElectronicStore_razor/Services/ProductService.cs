using ElectronicStore_razor.Models;

namespace ElectronicStore_razor.Services;

/// <summary>
/// Quản lý dữ liệu và logic thao tác với sản phẩm
/// </summary>
public class ProductService : IProductService
{
    // Danh sách dữ liệu mẫu mô phỏng từ database
    private readonly List<Product> _products = new()
    {
        new Product
        {
            Id = 1,
            Name = "Laptop Dell XPS 13",
            Description = "Laptop mỏng nhẹ cao cấp, màn hình sắc nét InfinityEdge, vi xử lý Intel Core i7 thế hệ mới giúp xử lý mượt mà mọi tác vụ văn phòng và đồ họa.",
            ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?auto=format&fit=crop&w=600&q=80",
            Price = 25000000m,
            Quantity = 10
        },
        new Product
        {
            Id = 2,
            Name = "Bàn phím cơ Logi",
            Description = "Bàn phím cơ gõ êm, trang bị switch cơ học phản hồi nhanh, đèn LED RGB rực rỡ và hỗ trợ kết nối đa thiết bị không dây Bluetooth.",
            ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=600&q=80",
            Price = 1500000m,
            Quantity = 5
        },
        new Product
        {
            Id = 3,
            Name = "Chuột Wireless",
            Description = "Chuột ergonomic chống mỏi cổ tay, cảm biến quang học độ chính xác cao 4000 DPI, pin sử dụng liên tục lên đến 70 ngày.",
            ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?auto=format&fit=crop&w=600&q=80",
            Price = 500000m,
            Quantity = 0 // Số lượng = 0 để kiểm tra trạng thái "Hết hàng"
        },
        new Product
        {
            Id = 4,
            Name = "Màn hình Dell UltraSharp 27\"",
            Description = "Màn hình đồ họa chuẩn 4K IPS, độ phủ màu 99% sRGB, viền siêu mỏng phù hợp cho thiết kế đồ họa chuyên nghiệp và dựng phim.",
            ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?auto=format&fit=crop&w=600&q=80",
            Price = 11500000m,
            Quantity = 7
        },
        new Product
        {
            Id = 5,
            Name = "Tai nghe Chống ồn Sony",
            Description = "Tai nghe over-ear công nghệ chống ồn chủ động đỉnh cao Noise Canceling, chất âm chân thực Hi-Res Audio, thời lượng pin 30 giờ.",
            ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=600&q=80",
            Price = 6900000m,
            Quantity = 3
        },
        new Product
        {
            Id = 6,
            Name = "Ổ cứng di động SSD 1TB",
            Description = "Tốc độ đọc ghi cực nhanh lên đến 1050MB/s, chuẩn kết nối USB Type-C, thiết kế siêu nhỏ gọn và chống sốc bền bỉ.",
            ImageUrl = "https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?auto=format&fit=crop&w=600&q=80",
            Price = 2800000m,
            Quantity = 0 // Hết hàng
        }
    };

    // Khóa luồng đồng bộ để đảm bảo an toàn đa luồng (Thread-safety) khi có nhiều request đồng thời
    private readonly object _lock = new();

    public List<Product> GetAllProducts()
    {
        lock (_lock)
        {
            // Trả về bản sao danh sách để tránh xung đột luồng khi duyệt bên ngoài
            return new List<Product>(_products);
        }
    }

    public Product? GetProductById(int id)
    {
        lock (_lock)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }
    }

    /// <summary>
    /// Xử lý nghiệp vụ mua sản phẩm:
    /// Kiểm tra số lượng tồn kho, nếu còn hàng thì giảm số lượng và ghi nhận thành công.
    /// </summary>
    public bool BuyProduct(int id)
    {
        lock (_lock)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null || product.Quantity <= 0)
            {
                return false;
            }

            product.Quantity--;
            return true;
        }
    }

    /// <summary>
    /// Thêm sản phẩm mới vào danh sách, tự động sinh Id tăng dần
    /// </summary>
    public void AddProduct(Product product)
    {
        lock (_lock)
        {
            // Tự động tạo Id mới lớn hơn Id lớn nhất hiện tại
            product.Id = _products.Count > 0 ? _products.Max(p => p.Id) + 1 : 1;
            _products.Add(product);
        }
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết của sản phẩm theo Id
    /// </summary>
    public bool UpdateProduct(Product product)
    {
        lock (_lock)
        {
            var existing = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existing == null)
            {
                return false;
            }

            // Cập nhật lại các trường thông tin
            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.ImageUrl = product.ImageUrl;
            existing.Price = product.Price;
            existing.Quantity = product.Quantity;
            return true;
        }
    }

    /// <summary>
    /// Xóa sản phẩm khỏi danh sách theo Id
    /// </summary>
    public bool DeleteProduct(int id)
    {
        lock (_lock)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return false;
            }

            return _products.Remove(product);
        }
    }
}

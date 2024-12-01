using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using shopflowerproject.Filters;
using shopflowerproject.Models;

namespace shopflowerproject.Controllers;
public class PaymentController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;
    public PaymentController(ILogger<PaymentController> logger, IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = logger;
    }
    [HttpGet]
    public IActionResult HandleCartActions()
    {
        return View();
    }
    [HttpPost]
    public IActionResult HandleCartActions(string action, string maHoa, int soLuong)
    {
        string? maGioHang = HttpContext.Session.GetString("cartid");

        if (string.IsNullOrEmpty(maGioHang))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập để có giỏ hàng." });
        }

        if (action == "AddToCart")
        {
            string connectionString = _configuration.GetConnectionString("Default");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("addToCart", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@maGioHang", maGioHang);
                        command.Parameters.AddWithValue("@maHoa", maHoa);
                        command.Parameters.AddWithValue("@soLuong", soLuong);
                        command.ExecuteNonQuery();
                    }
                    return Json(new { success = true, message = "Đã thêm vào giỏ hàng!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
                }
            }
        }
        else if (action == "BuyNow")
        {
            return Json(new { success = true, message = "Đang xử lý mua hàng." });
        }

        return Json(new { success = false, message = "Không xác định được hành động." });
    }


    // public async Task<IActionResult> addToCart()
    // {
    //     string? connecString = _configuration.GetConnectionString("Default");
    //     string cartId = FindIdShoppingCart();
    // }
    // public async Task<ShoppingCart> ShowInfo()
    // {
    //     var shoppingCart = new ShoppingCart();
    //     string? connecString = _configuration.GetConnectionString("Default");
    //     try
    //     {
    //         using (var connec = new SqlConnection(connecString))
    //         {
    //             await connec.OpenAsync();
    //             string? username = HttpContext.Session.GetString("username");
    //             using (SqlCommand cmd = new SqlCommand("select gh.MaGioHang,HoTen,h.TenHoa,h.MoTa,h.GiaBan,h.HinhAnh from GioHang as gh join Users as u on gh.MaUser = u.MaUser join ChiTietGioHang as ctgh on ctgh.MaGioHang = gh.MaGioHang join Hoa as h on ctgh.MaHoa = h.MaHoa where u.TenTaiKhoan = @username", connec))
    //             {
    //                 cmd.Parameters.AddWithValue("username", username);
    //                 using (var reader = await cmd.ExecuteReaderAsync())
    //                 {
    //                     while (await reader.ReadAsync())
    //                     {
    //                         shoppingCart.Add(new Flowers
    //                         {
    //                             MaHoa = reader.GetString(0),
    //                             TenHoa = reader.GetString(3),
    //                             MoTa = reader.GetString(7),
    //                             HinhAnh = reader.GetString(8),
    //                             GiaBan = reader.GetDecimal(5)
    //                         });
    //                     }
    //                 }
    //             }
    //         }
    //         return shoppingCart;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex.Message);
    //     }
    // }
    public ShoppingCart showListFlowers()
    {
        ShoppingCart shoppingCart = new ShoppingCart();
        var connecString = _configuration.GetConnectionString("Default");
        var maGioHang = HttpContext.Session.GetString("cartid");

        if (string.IsNullOrEmpty(maGioHang))
        {
            return shoppingCart;
        }

        try
        {
            using (SqlConnection connection = new SqlConnection(connecString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT h.MaHoa, h.TenHoa,h.HinhAnh ,h.GiaBan, ctgh.SoLuong, ctgh.GiaSanPham FROM ChiTietGioHang AS ctgh JOIN Hoa AS h ON h.MaHoa = ctgh.MaHoa WHERE ctgh.MaGioHang = @cartid", connection))
                {
                    command.Parameters.AddWithValue("@cartid", maGioHang);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Flowers flower = new Flowers
                            {
                                MaHoa = reader["MaHoa"].ToString(),
                                TenHoa = reader["TenHoa"].ToString(),
                                GiaBan = Convert.ToDecimal(reader["GiaBan"]),
                                SoLuong = Convert.ToInt32(reader["SoLuong"]),
                                HinhAnh = reader["HinhAnh"].ToString(),
                            };
                            shoppingCart.Flowers.Add(flower);
                        }

                        shoppingCart.ThanhTien = shoppingCart.Flowers.Sum(f => f.GiaBan * f.SoLuong);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return shoppingCart;
    }

    public ShoppingCart showFlower(string MaHoa)
    {
        ShoppingCart shoppingCart = new ShoppingCart();
        var connecString = _configuration.GetConnectionString("Default");

        if (string.IsNullOrEmpty(MaHoa))
        {
            return shoppingCart;
        }

        try
        {
            using (SqlConnection connection = new SqlConnection(connecString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT Mahoa,TenHoa,HinhAnh ,GiaBan from hoa WHERE MaHoa = @MaHoa", connection))
                {
                    command.Parameters.AddWithValue("@MaHoa", MaHoa);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Flowers flower = new Flowers
                            {
                                MaHoa = reader["MaHoa"].ToString(),
                                TenHoa = reader["TenHoa"].ToString(),
                                GiaBan = Convert.ToDecimal(reader["GiaBan"]),
                                HinhAnh = reader["HinhAnh"].ToString()
                            };
                            shoppingCart.Flowers.Add(flower);
                        }

                        shoppingCart.ThanhTien = shoppingCart.Flowers.Sum(flower => flower.GiaBan);

                    }
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return shoppingCart;
    }
    public string AutoGenerateBill()
    {
        DateTime now = DateTime.Now;
        string username = HttpContext.Session.GetString("username");
        return "HD" + now.ToString("yyyyMMddHHmmss") + username;
    }
    public string TakeUserId()
    {
        string maUser = "";
        var connectionString = _configuration.GetConnectionString("Default");
        string username = HttpContext.Session.GetString("username");
        string query = "SELECT MaUser FROM users WHERE TenTaiKhoan = @username";
        using (var connection = new SqlConnection(connectionString)){
            connection.Open();
            using (SqlCommand command = new SqlCommand(query, connection)){
                                command.Parameters.AddWithValue("@username", username);

                var result = command.ExecuteScalar();
                if (result != null)
                {
                    maUser = result.ToString();
                }
            }
        }
        return maUser;
    }
    [HttpPost]
public async Task<IActionResult> InsertInvoiceAndDetailsFromCart(Invoice invoice)
{
    string cartId = HttpContext.Session.GetString("cartid");
    var connectionString = _configuration.GetConnectionString("Default");
    string insertedInvoiceId = string.Empty;
    string userId = TakeUserId();
    string mahoadon = AutoGenerateBill();
    decimal tongtien = 0;
    string PhuongThucThanhToan = "Tiền Mặt";
    DateTime currentDate = DateTime.Now; 
    try
    {
        using (SqlConnection sqlConnection = new SqlConnection(connectionString))
        {
            await sqlConnection.OpenAsync();

            using (SqlCommand command = new SqlCommand("INSERT INTO hoadon (mahoadon,mauser, tennguoinhan, diachinhan, ngaymua, tongtien, phuongthucchuyenkhoan, ghichu) VALUES (@mahoadon ,@mauser, @tennguoinhan, @diachinhan, @ngaymua, @tongtien, @phuongthucchuyenkhoan, @ghichu)", sqlConnection))
            {
                command.Parameters.AddWithValue("@mahoadon", mahoadon);
                command.Parameters.AddWithValue("@mauser", userId);
                command.Parameters.AddWithValue("@tennguoinhan", invoice.Name);
                command.Parameters.AddWithValue("@diachinhan", invoice.Address);
                command.Parameters.AddWithValue("@ngaymua", currentDate);
                command.Parameters.AddWithValue("@tongtien", tongtien);
                command.Parameters.AddWithValue("@phuongthucchuyenkhoan", PhuongThucThanhToan);
                command.Parameters.AddWithValue("@ghichu", invoice.Note);


                var result = await command.ExecuteScalarAsync();
                if (result != DBNull.Value && result != null)
                {
                    insertedInvoiceId = result.ToString();
                }
                else
                {

                    insertedInvoiceId = string.Empty;
                }
            }


            using (SqlCommand queryCommand = new SqlCommand("SELECT h.MaHoa, h.GiaBan, ctgh.SoLuong FROM ChiTietGioHang AS ctgh JOIN Hoa AS h ON h.MaHoa = ctgh.MaHoa WHERE ctgh.MaGioHang = @cartid", sqlConnection))
            {
                queryCommand.Parameters.AddWithValue("@cartid", cartId);

                using (SqlDataReader reader = await queryCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int maHoa = reader.GetInt32(0);          
                        decimal giaBan = reader.GetDecimal(1);   
                        int soLuong = reader.GetInt32(2);        
                        decimal tongtientoanhoa = giaBan * soLuong;


                        using (SqlCommand detailCommand = new SqlCommand("INSERT INTO chitiethoadon (mahoadon, mahoa, soluong, giaban) VALUES (@mahoadon, @mahoa, @soluong, @giaban)", sqlConnection))
                        {
                            detailCommand.Parameters.AddWithValue("@mahoadon", insertedInvoiceId);
                            detailCommand.Parameters.AddWithValue("@mahoa", maHoa);
                            detailCommand.Parameters.AddWithValue("@soluong", soLuong);
                            detailCommand.Parameters.AddWithValue("@giaban", tongtientoanhoa);

                            await detailCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        return RedirectToAction("Success"); 
    }
    catch (Exception ex)
    {

        return StatusCode(500, "Đã có lỗi xảy ra: " + ex.Message);
    }
}



    public IActionResult FillInfo()
    {
        var cartList = showListFlowers();
        return View(cartList);
    }
    public IActionResult Success()
    {
        return View();
    }
    public IActionResult BuyNow(string MaHoa)
    {
        var buyNow = showFlower(MaHoa);
        return View(buyNow);
    }

    public ActionResult ShoppingCart()
    {
        var cartList = showListFlowers();
        return View(cartList);
    }
    public ActionResult Payment()
    {
        return View();
    }
    [HttpPost]
    public IActionResult UpdateBuyNowPrice(string MaHoa, decimal shippingCost)
    {
        var model = showFlower(MaHoa);
        model.ThanhTien = model.ThanhTien ?? 0;
        model.ThanhToan = model.ThanhTien + shippingCost;
        if (model.Flowers.Count == 0)
        {
            return Json(new { success = false, message = "Không tìm thấy hoa." });
        }

        return Json(new { totalAmount = model.ThanhToan });
    }
    [HttpPost]
    public IActionResult UpdateTotalAmount(decimal shippingCost)
    {
        var model = showListFlowers();

        model.ThanhTien = model.ThanhTien ?? 0;
        model.ThanhToan = model.ThanhTien + shippingCost;

        return Json(new { totalAmount = model.ThanhToan });
    }

    [HttpPost]
    public IActionResult UpdateFlowerQuantity(string MaHoa, int SoLuong)
    {
        string maGioHang = HttpContext.Session.GetString("cartid");

        try
        {
            var connecString = _configuration.GetConnectionString("Default");

            using (SqlConnection connection = new SqlConnection(connecString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("UPDATE ChiTietGioHang SET SoLuong = @soluong, Giasanpham = @soluong * (SELECT GiaBan FROM Hoa WHERE Hoa.MaHoa = ChiTietGioHang.MaHoa) WHERE MaHoa = @mahoa AND MaGioHang = @magiohang;", connection))
                {
                    command.Parameters.AddWithValue("@soluong", SoLuong);
                    command.Parameters.AddWithValue("@mahoa", MaHoa);
                    command.Parameters.AddWithValue("@magiohang", maGioHang);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm để cập nhật." });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    [HttpPost]
    public IActionResult UpdateFlowerQuantityAddToCart(string MaHoa, int SoLuong)
    {
        string maGioHang = HttpContext.Session.GetString("cartid");

        try
        {
            var connecString = _configuration.GetConnectionString("Default");

            using (SqlConnection connection = new SqlConnection(connecString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("UPDATE ChiTietGioHang SET SoLuong = Soluong + @soluong where MaHoa = @mahoa and MaGioHang = @magiohang", connection))
                {
                    command.Parameters.AddWithValue("@soluong", SoLuong);
                    command.Parameters.AddWithValue("@mahoa", MaHoa);
                    command.Parameters.AddWithValue("@magiohang", maGioHang);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm để cập nhật." });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    [HttpPost]
    public ActionResult DeleteProduct(string MaHoa)
    {
        var cartid = HttpContext.Session.GetString("cartid");
        var connecString = _configuration.GetConnectionString("Default");
        try
        {
            string query = "DELETE FROM ChiTietGioHang WHERE MaHoa = @MaHoa AND MaGioHang = @MaGioHang";

            using (SqlConnection conn = new SqlConnection(connecString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@MaHoa", MaHoa);
                cmd.Parameters.AddWithValue("@MaGioHang", cartid);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            decimal totalAmount = GetTotalAmountForCart(cartid);

            return Json(new { success = true, totalAmount = totalAmount });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private decimal GetTotalAmountForCart(string maGioHang)
    {
        string query = "SELECT SUM(SoLuong * GiaBan) FROM ChiTietGioHang JOIN Hoa ON ChiTietGioHang.MaHoa = Hoa.MaHoa WHERE MaGioHang = @MaGioHang";
        decimal totalAmount = 0;
        var connecString = _configuration.GetConnectionString("Default");

        using (SqlConnection conn = new SqlConnection(connecString))
        {
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaGioHang", maGioHang);
            conn.Open();
            object result = cmd.ExecuteScalar();
            if (result != DBNull.Value)
            {
                totalAmount = Convert.ToDecimal(result);
            }
        }

        return totalAmount;
    }

}
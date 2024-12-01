using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using shopflowerproject.Models;
using System.Data.SqlClient;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
namespace shopflowerproject.Controllers;
public class AccountController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;
    public AccountController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = logger;
    }
    [HttpGet]
    public IActionResult SignUp()
    {
        return View();
    }
    public async Task<int> TotalFlowers()
    {
        int totalCount = 0;
        var connecString = _configuration.GetConnectionString("Default");
        var maGioHang = HttpContext.Session.GetString("cartid");

        if (string.IsNullOrEmpty(maGioHang))
        {
            return totalCount;
        }

        try
        {
            using (SqlConnection sql = new SqlConnection(connecString))
            {
                await sql.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("SELECT SUM(ctgh.soluong) FROM ChiTietGioHang AS ctgh JOIN Hoa AS h ON h.MaHoa = ctgh.MaHoa WHERE ctgh.MaGioHang = @cartId", sql))
                {
                    cmd.Parameters.AddWithValue("@cartId", maGioHang);


                    var result = await cmd.ExecuteScalarAsync();

                    totalCount = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return totalCount;
    }

    [HttpPost]
    public async Task<ActionResult> SignUp(AccountRegister register)
    {
        string? connecString = _configuration.GetConnectionString("Default");
        if (!ModelState.IsValid)
        {
            return View();
        }
        try
        {
            register.Id = Guid.NewGuid().ToString();
            using (SqlConnection connec = new SqlConnection(connecString))
            {
                await connec.OpenAsync();

                using (SqlCommand command = new SqlCommand("sp_DangKiNguoiDung", connec))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@MaUser", register.Id);
                    command.Parameters.AddWithValue("@TenTaiKhoan", register.Username);
                    command.Parameters.AddWithValue("@Password", register.Password);
                    command.Parameters.AddWithValue("@Email", register.Email);
                    command.Parameters.AddWithValue("@FullName", register.FullName);

                    await command.ExecuteNonQueryAsync();
                }
            }
            TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        }
        catch (SqlException ex)
        {
            if (ex.Number == 50000)
            {
                ModelState.AddModelError("", ex.Message);
            }
            else
            {
                ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình đăng ký.");
            }
            return View(register);
        }
        return RedirectToAction("Login");
    }
    public string FindIdShoppingCart(string username)
    {
        string cartId = string.Empty;
        string? connecString = _configuration.GetConnectionString("Default");
        try
        {
            using (SqlConnection connecConnection = new SqlConnection(connecString))
            {
                connecConnection.Open();
                using (SqlCommand cmd = new SqlCommand("select MaGioHang from GioHang as gh join Users as u on gh.MaUser = u.MaUser where TenTaiKhoan = @TenTaiKhoan ", connecConnection))
                {
                    cmd.Parameters.AddWithValue("@TenTaiKhoan", username);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            cartId = reader["MaGioHang"].ToString();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        return cartId;
    }
    [HttpGet]
    public IActionResult Login()
    {

        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Login(AccountLogin login, bool rememberMe)
    {
        string? connec = _configuration.GetConnectionString("Default");
        if (!ModelState.IsValid)
        {
            return View(login);
        }
        try
        {
            using (SqlConnection connection = new SqlConnection(connec))
            {
                await connection.OpenAsync();
                string vaitro = string.Empty;
                using (SqlCommand command = new SqlCommand("sp_DangNhapNguoiDung", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Username", login.Username);
                    command.Parameters.AddWithValue("@Password", login.Password);
                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT vaitro FROM users WHERE tentaikhoan = @username", connection))
                        {
                            cmd.Parameters.AddWithValue("@username", login.Username);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    vaitro = reader["vaitro"].ToString();
                                }
                            }
                        }
                        if (rememberMe)
                        {
                            var cookieOptions = new CookieOptions
                            {
                                Expires = DateTime.Now.AddDays(30),
                                IsEssential = true,
                                HttpOnly = true,
                            };
                            Response.Cookies.Append("username", login.Username, cookieOptions);
                            HttpContext.Session.SetString("username", login.Username);
                            HttpContext.Session.SetString("role", vaitro);
                            string cartIdByUser = FindIdShoppingCart(login.Username);
                            HttpContext.Session.SetString("cartid", cartIdByUser);
                            int totalFlowers = await TotalFlowers();
                            HttpContext.Session.SetInt32("totalFlowers", totalFlowers);
                        }
                        else
                        {
                            HttpContext.Session.SetString("username", login.Username);
                            FindIdShoppingCart(login.Username);
                            HttpContext.Session.SetString("role", vaitro);
                            string cartIdByUser = FindIdShoppingCart(login.Username);
                            HttpContext.Session.SetString("cartid", cartIdByUser);
                            int totalFlowers = await TotalFlowers();
                            HttpContext.Session.SetInt32("totalFlowers", totalFlowers);
                        }
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            if (ex.Number == 50000)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            else
            {
                ModelState.AddModelError(string.Empty + "Lỗi", ex.Message);
            }
        }

        return View("Login");
    }
    //Đăng nhập với Google
    [HttpGet]
    public IActionResult LoginWithGoogle()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleCallback")
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }
    [HttpGet]
    public async Task<IActionResult> GoogleCallback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            return RedirectToAction("Login");
        }

        // Lấy thông tin từ Google
        var claims = authenticateResult.Principal.Identities.FirstOrDefault()?.Claims;
        var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        if (email != null)
        {
            // Kiểm tra nếu email đã tồn tại trong hệ thống
            string connecString = _configuration.GetConnectionString("Default");
            using (SqlConnection connection = new SqlConnection(connecString))
            {
                await connection.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE Email = @Email", connection))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    var count = (int)await cmd.ExecuteScalarAsync();

                    if (count == 0)
                    {
                        // Tạo tài khoản mới nếu chưa tồn tại
                        using (SqlCommand insertCmd = new SqlCommand("INSERT INTO Users (MaUser, HoTen, Email, TenTaiKhoan, VaiTro) VALUES (@MaUser, @Name, @Email, @Username, @Role)", connection))
                        {
                            string maUser = Guid.NewGuid().ToString();
                            insertCmd.Parameters.AddWithValue("@MaUser", maUser);
                            insertCmd.Parameters.AddWithValue("@Name", name ?? "Unknown");
                            insertCmd.Parameters.AddWithValue("@Email", email);
                            insertCmd.Parameters.AddWithValue("@Username", email);  // Dùng email làm username
                            insertCmd.Parameters.AddWithValue("@Role", "User");

                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }

            // Đăng nhập người dùng
            HttpContext.Session.SetString("username", email);
            HttpContext.Session.SetString("role", "User");

            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Login");
    }
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Remove("username");
        HttpContext.Session.Remove("role");
        Response.Cookies.Delete("username");
        HttpContext.Session.Remove("totalcart");
        HttpContext.Session.Remove("cartid");
        return RedirectToAction("Index", "Home");
    }
}
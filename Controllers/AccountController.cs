using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using shopflowerproject.Models;


using System.Data.SqlClient;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
                using (SqlCommand command = new SqlCommand("sp_DangNhapNguoiDung", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Username", login.Username);
                    command.Parameters.AddWithValue("@Password", login.Password);
                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
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
                        }
                        else
                        {
                            HttpContext.Session.SetString("username", login.Username);
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
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Remove("username");
        Response.Cookies.Delete("username");
        return RedirectToAction("Index", "Home");
    }
}
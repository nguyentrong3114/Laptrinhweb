using Microsoft.AspNetCore.Mvc;
using shopflowerproject.Models;
using shopflowerproject.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Threading.Tasks;
using Newtonsoft.Json;
using shopflowerproject.Filters;

namespace shopflowerproject.Areas.Admin.Controllers
{
    // Tắt/bật comment dòng ở dưới để bật/tắt xác minh admin khi vào trang quản lý người dùng
    [AuthorizeAdmin]

    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsersController> _logger;
        public UsersController(IConfiguration configuration, ILogger<UsersController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        // Hàm lấy danh sách vai trò từ cơ sở dữ liệu
        private async Task<List<string>> GetRolesAsync()
        {
            var roles = new List<string>();
            var connectionString = _configuration.GetConnectionString("Default");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SELECT TenVaiTro FROM VaiTro", connection);
                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        roles.Add(reader["TenVaiTro"].ToString());
                    }
                }
            }

            return roles;
        }

        public async Task<IActionResult> Index(int page = 1, string keyword = "", string sortOrder = "tentk_asc")

        {
            // Phân trang
            int pageSize = 6; // Số người dùng hiển thị mỗi trang
            int skip = (page - 1) * pageSize;

            List<Users> dsnd = new List<Users>();
            var connectionString = _configuration.GetConnectionString("Default");

            // Truy vấn danh sách sản phẩm tìm kiếm và phân trang

            var commandText = $@"
				EXEC sp_FindUsers @Keyword = @Keyword, @SortOrder = @SortOrder;
			";

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(commandText, connection);
                command.Parameters.AddWithValue("@Keyword", $"%{keyword}%");
                command.Parameters.AddWithValue("@SortOrder", sortOrder);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var u = new Users
                        {
                            MaUser = reader.GetString("MaUser"),
                            HoTen = reader.GetString("HoTen"),
                            Email = reader.GetString("Email"),
                            TenTaiKhoan = reader.GetString("TenTaiKhoan"),
                            SoDienThoai = reader.IsDBNull(reader.GetOrdinal("SoDienThoai")) ? null : reader.GetString(reader.GetOrdinal("SoDienThoai")),
                            DiaChi = reader.IsDBNull(reader.GetOrdinal("DiaChi")) ? null : reader.GetString(reader.GetOrdinal("DiaChi")),
                            GioiTinh = reader.IsDBNull(reader.GetOrdinal("GioiTinh")) ? null : reader.GetString(reader.GetOrdinal("GioiTinh")),
                            NgaySinh = reader.IsDBNull(reader.GetOrdinal("NgaySinh")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("NgaySinh")),
                            VaiTro = reader.GetString("VaiTro"),
                            NgayDangKy = reader.GetDateTime("NgayDangKy"),
                            IdFacebook = reader.IsDBNull(reader.GetOrdinal("IdFacebook")) ? null : reader.GetString(reader.GetOrdinal("IdFacebook")),
                            IdGoogle = reader.IsDBNull(reader.GetOrdinal("IdGoogle")) ? null : reader.GetString(reader.GetOrdinal("IdGoogle"))
                        };
                        dsnd.Add(u);
                    }
                }
            }

            // Lấy tổng số sản phẩm tìm kiếm (chỉ tính tổng số sản phẩm phù hợp với từ khóa tìm kiếm)
            var totalItemsCommand = new SqlCommand(@"
				SELECT COUNT(DISTINCT u.MaUser)
				FROM Users u
				WHERE LOWER(u.TenTaiKhoan) LIKE LOWER('%' + @Keyword + '%')
			", new SqlConnection(connectionString));

            totalItemsCommand.Parameters.AddWithValue("@Keyword", $"%{keyword}%");

            await totalItemsCommand.Connection.OpenAsync();
            int totalItems = (int)await totalItemsCommand.ExecuteScalarAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            dsnd = dsnd.Skip(skip).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Keyword = keyword;  // Đảm bảo từ khóa tìm kiếm được lưu lại khi phân trang

            return View(dsnd);
        }
        // Action Create (GET)
        public ActionResult Create()
        {
            var roles = new List<string> { "Admin", "User", "Guest" }; // Danh sách các vai trò cố định
            ViewBag.VaiTro = roles;

            return View(new Users());
        }

        //Action Create(POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Users u)
        {
            var connectionString = _configuration.GetConnectionString("Default");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("INSERT INTO Users (MaUser, HoTen, Email, TenTaiKhoan, SoDienThoai, DiaChi, GioiTinh, NgaySinh, VaiTro, NgayDangKy, IdFacebook, IdGoogle) " +
                                             "VALUES (@MaUser, @HoTen, @Email, @TenTaiKhoan, @SoDienThoai, @DiaChi, @GioiTinh, @NgaySinh, @VaiTro, @NgayDangKy, @IdFacebook, @IdGoogle)", connection);
                command.Parameters.AddWithValue("@MaUser", u.MaUser);
                command.Parameters.AddWithValue("@HoTen", u.HoTen);
                command.Parameters.AddWithValue("@Email", u.Email);
                command.Parameters.AddWithValue("@TenTaiKhoan", u.TenTaiKhoan);
                command.Parameters.AddWithValue("@SoDienThoai", u.SoDienThoai);
                command.Parameters.AddWithValue("@DiaChi", u.DiaChi);
                command.Parameters.AddWithValue("@GioiTinh", u.GioiTinh);
                command.Parameters.AddWithValue("@NgaySinh", u.NgaySinh);
                command.Parameters.AddWithValue("@VaiTro", u.VaiTro);
                command.Parameters.AddWithValue("@NgayDangKy", u.NgayDangKy);
                command.Parameters.AddWithValue("@IdFacebook", u.IdFacebook);
                command.Parameters.AddWithValue("@MaUser", u.IdGoogle);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }

            return RedirectToAction("Index");
        }
        // Action Edit (GET)
        public async Task<IActionResult> Edit(string id)
        {
            var connectionString = _configuration.GetConnectionString("Default");
            var u = new Users();

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SELECT * FROM Users WHERE MaUser = @MaUser", connection);
                command.Parameters.AddWithValue("@MaUser", id);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        u.MaUser = reader["MaUser"].ToString();
                        u.HoTen = reader["HoTen"].ToString();
                        u.Email = reader["Email"].ToString();
                        u.TenTaiKhoan = reader["TenTaiKhoan"].ToString();
                        u.SoDienThoai = reader["SoDienThoai"].ToString();
                        u.DiaChi = reader["DiaChi"].ToString();
                        u.GioiTinh = reader["GioiTinh"].ToString();
                        u.NgaySinh = reader.IsDBNull(reader.GetOrdinal("NgaySinh")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("NgaySinh"));
                        u.VaiTro = reader["VaiTro"].ToString();
                        u.NgayDangKy = reader.IsDBNull(reader.GetOrdinal("NgayDangKy")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("NgayDangKy"));
                        u.IdFacebook = reader["IdFacebook"].ToString();
                        u.IdGoogle = reader["IdGoogle"].ToString();
                    }
                }
            }
            return Json(new
            {
                maUser = u.MaUser,
                hoTen = u.HoTen,
                email = u.Email,
                tenTaiKhoan = u.TenTaiKhoan,
                soDienThoai = u.SoDienThoai,
                diaChi = u.DiaChi,
                gioiTinh = u.GioiTinh,
                ngaySinh = u.NgaySinh?.ToString("yyyy-MM-dd"),
                vaiTro = u.VaiTro,
                ngayDangKy = u.NgayDangKy?.ToString("yyyy-MM-dd"),
                idFacebook = u.IdFacebook,
                idGoogle = u.IdGoogle
            });
        }
        // Action Edit (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Users u)
        {

            var connectionString = _configuration.GetConnectionString("Default");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("UPDATE Users SET HoTen = @HoTen, Email = @Email, TenTaiKhoan = @TenTaiKhoan, " +
                                             "SoDienThoai = @SoDienThoai, DiaChi = @DiaChi, GioiTinh = @GioiTinh, NgaySinh = @NgaySinh, " +
                                             "VaiTro = @VaiTro, NgayDangKy = @NgayDangKy, IdFacebook = @IdFacebook, IdGoogle = @IdGoogle " +
                                             "WHERE MaUser = @MaUser", connection);

                command.Parameters.AddWithValue("@MaUser", u.MaUser);
                command.Parameters.AddWithValue("@HoTen", u.HoTen);
                command.Parameters.AddWithValue("@Email", u.Email);
                command.Parameters.AddWithValue("@TenTaiKhoan", u.TenTaiKhoan);
                command.Parameters.AddWithValue("@SoDienThoai", u.SoDienThoai);
                command.Parameters.AddWithValue("@DiaChi", u.DiaChi);
                command.Parameters.AddWithValue("@GioiTinh", u.GioiTinh);
                command.Parameters.AddWithValue("@NgaySinh", u.NgaySinh);
                command.Parameters.AddWithValue("@VaiTro", u.VaiTro);
                command.Parameters.AddWithValue("@NgayDangKy", u.NgayDangKy);
                command.Parameters.AddWithValue("@IdFacebook", u.IdFacebook);
                command.Parameters.AddWithValue("@IdGoogle", u.IdGoogle);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }

            return RedirectToAction("Index"); // Quay lại Index sau khi cập nhật
        }

        //Action Delete (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("Default");

                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand("DELETE FROM Users WHERE MaUser = @MaUser", connection);
                    command.Parameters.AddWithValue("@MaUser", id);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
        //Action DeleteSelected (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(string selectedIds)
        {
            try
            {
                var ids = JsonConvert.DeserializeObject<List<string>>(selectedIds);
                var connectionString = _configuration.GetConnectionString("Default");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    foreach (var id in ids)
                    {
                        var command = new SqlCommand("DELETE FROM Users WHERE MaUser = @MaUser", connection);
                        command.Parameters.AddWithValue("@MaUser", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}

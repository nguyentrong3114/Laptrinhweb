using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using shopflowerproject.Filters;
using shopflowerproject.Models;
namespace shopflowerproject.Areas.Admin.Controllers
{
    // [AuthorizeAdmin]
    [Area("Admin")]
    public class HomeAdminController : Controller
    {
        private readonly ILogger<HomeAdminController> _logger;
        private readonly IConfiguration _configuration;
        public HomeAdminController(ILogger<HomeAdminController> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<string> TotalEarnInYear()
        {
            string? Total = string.Empty;
            var connecString = _configuration.GetConnectionString("Default");
            using (SqlConnection connec = new SqlConnection(connecString))
            {
                await connec.OpenAsync();
                SqlCommand cmd = new SqlCommand("Select dbo.TongTienHangNam()", connec);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null)
                {
                    Total = result.ToString();
                }
            }
            return Total;
        }
        public async Task<string> TotalEarnInMonth()
        {
            string Total = "0";
            var connecString = _configuration.GetConnectionString("Default");

            using (SqlConnection connec = new SqlConnection(connecString))
            {
                await connec.OpenAsync();
                SqlCommand cmd = new SqlCommand("SELECT dbo.TongTienHangThang()", connec);


                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            Total = reader.GetDouble(0).ToString();
                        }
                    }
                }
            }
            return Total;
        }

        public async Task<string> TotalCustomer()
        {
            string? Total = string.Empty;
            var connecString = _configuration.GetConnectionString("Default");
            using (SqlConnection connec = new SqlConnection(connecString))
            {
                await connec.OpenAsync();
                SqlCommand cmd = new SqlCommand("Select dbo.TongSoKhachHang()", connec);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null)
                {
                    Total = result.ToString();
                }
            }
            return Total;
        }
        public async Task<string> TotalInvoicePerMonth()
        {
            string? Total = string.Empty;
            var connecString = _configuration.GetConnectionString("Default");
            using (SqlConnection connec = new SqlConnection(connecString))
            {
                await connec.OpenAsync();
                SqlCommand cmd = new SqlCommand("Select dbo.TongHoaDonHangThang()", connec);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null)
                {
                    Total = result.ToString();
                }
            }
            return Total;
        }
        public async Task<IActionResult> Index()
        {
            OverviewAdmin overview = new OverviewAdmin();
            overview.TotalEarnInYear = await TotalEarnInYear();
            overview.TotalEarnInMonth = await TotalEarnInMonth();
            overview.TotalCustomer = await TotalCustomer();
            overview.TotalInvoicePerMonth = await TotalInvoicePerMonth();
            return View(overview);
        }
        public IActionResult Charts()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GetChartData()
        {
            var revenueData = new List<decimal>();
            var labels = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

            string connectionString = _configuration.GetConnectionString("Default");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("sp_BieuDoDoanhThuTungThang  ", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        decimal[] monthlyRevenue = new decimal[12];

                        while (reader.Read())
                        {
                            int month = reader.GetInt32(1);
                            decimal revenue = reader.GetDecimal(2);


                            monthlyRevenue[month - 1] = revenue;
                        }

                        revenueData.AddRange(monthlyRevenue);
                    }
                }
            }
            var data = new
            {
                labels = labels,
                datasets = new[]
                {
                new
                {
                    label = "Tổng Doanh Thu",
                    backgroundColor = "orange",
                    data = revenueData.ToArray()
                }
            }
            };
            return Json(data);
        }
        [HttpGet]
        public IActionResult GetChartFlowersBestSeller()
        {

            var labels = new List<string>();
            var revenueData = new List<int>();
            string connectionString = _configuration.GetConnectionString("Default");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("sp_TopFlowersBestSeller", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            labels.Add(reader["TenHoa"].ToString());
                            revenueData.Add((int)reader["SoLuong"]);
                        }
                    }
                }
            }
            var data = new
            {
                labels = labels,
                datasets = new[]
                {
            new
            {
                label = "Tổng Số",
                backgroundColor = "orange",
                data = revenueData.ToArray()
            }
        }
            };

            return Json(data);
        }
        [HttpGet]
        public IActionResult GetChartTopUser()
        {
            var labels = new List<string>();
            var revenueData = new List<decimal>();
            string connectionString = _configuration.GetConnectionString("Default");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("sp_TopUserBuyFlower", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            labels.Add(reader["HoTen"].ToString());
                            revenueData.Add((decimal)reader["Total"]);
                        }
                    }
                }
            }
            var data = new
            {
                labels = labels,
                datasets = new[]
                {
            new
            {
                label = "Tổng Số Tiền Mua",
                backgroundColor = "orange",
                data = revenueData.ToArray()
            }
        }
            };

            return Json(data);
        }

        public IActionResult ManageAccount()
        {
            return View();
        }
        public IActionResult ManageFlowers()
        {
            return View();
        }

        
    }
}
using System.Data;
using System.Data.SqlClient;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using shopflowerproject.Models;
public class FlowersController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FlowersController> _logger;
    public FlowersController(IConfiguration configuration, ILogger<FlowersController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    public async Task<Flowers> getFlowerById(string id)
    {
        var flower = new Flowers();
        var connecString = _configuration.GetConnectionString("Default");
        using (var connec = new SqlConnection(connecString))
        {
            await connec.OpenAsync();
            using (SqlCommand cmd = new SqlCommand("SELECT * FROM HienThiToanBoHoa where MaHoa = @MaHoa", connec))
            {
                cmd.Parameters.AddWithValue("@MaHoa", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        flower = new Flowers
                        {
                            MaHoa = reader.GetString(0),
                            TenHoa = reader.IsDBNull(3) ? null : reader.GetString(3),
                            MoTa = reader.IsDBNull(6) ? null : reader.GetString(6),
                            HinhAnh = reader.IsDBNull(7) ? null : reader.GetString(7),
                            GiaBan = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4)
                        };
                    }
                }
            }
        }
        return flower;
    }
    public async Task<List<Flowers>> getAllFlowers()
    {
        var flowers = new List<Flowers>();
        var connecString = _configuration.GetConnectionString("Default");

        using (var connec = new SqlConnection(connecString))
        {
            await connec.OpenAsync();

            using (SqlCommand cmd = new SqlCommand("SELECT * FROM HienThiToanBoHoa", connec))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        flowers.Add(new Flowers
                        {
                            MaHoa = reader.GetString(0),
                            TenHoa = reader.GetString(3),
                            MoTa = reader.GetString(6),
                            HinhAnh = reader.GetString(7),
                            GiaBan = reader.GetDecimal(4)
                        });
                    }
                }
            }
        }
        return flowers;
    }
    public async Task<List<Flowers>> getAllFlowersByCategory(string Category)
    {
        var flowers = new List<Flowers>();
        var connecString = _configuration.GetConnectionString("Default");

        using (var connec = new SqlConnection(connecString))
        {
            await connec.OpenAsync();

            using (SqlCommand cmd = new SqlCommand("SELECT * FROM HienThiToanBoHoa where MaHoaTuoi = @Category", connec))
            {
                cmd.Parameters.Add(new SqlParameter("@Category", Category));
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        flowers.Add(new Flowers
                        {
                            MaHoa = reader.GetString(0),
                            TenHoa = reader.GetString(3),
                            MoTa = reader.GetString(6),
                            HinhAnh = reader.GetString(7),
                            GiaBan = reader.GetDecimal(4)
                        });
                    }
                }
            }
        }
        return flowers;
    }
    public async Task<List<Flowers>> getAllFlowersByOccasion(string Occasion)
    {
        var flowers = new List<Flowers>();
        var connecString = _configuration.GetConnectionString("Default");

        using (var connec = new SqlConnection(connecString))
        {
            await connec.OpenAsync();

            using (SqlCommand cmd = new SqlCommand("SELECT * FROM HienThiToanBoHoa where MaDanhMuc = @Occasion", connec))
            {
                cmd.Parameters.Add(new SqlParameter("@Occasion", Occasion));
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        flowers.Add(new Flowers
                        {
                            MaHoa = reader.GetString(0),
                            TenHoa = reader.GetString(3),
                            MoTa = reader.GetString(6),
                            HinhAnh = reader.GetString(7),
                            GiaBan = reader.GetDecimal(4)
                        });
                    }
                }
            }
        }
        return flowers;
    }
    public async Task<IActionResult> FlowersByCategory(string Category)
    {
        var flowers = await getAllFlowersByCategory(Category);
        ViewData["CategoryName"] = Category;
        return View(flowers);
    }
    public async Task<IActionResult> FlowersByOccasion(string Occasion)
    {
        var flowers = await getAllFlowersByOccasion(Occasion);
        ViewData["OccasionName"] = Occasion;
        return View(flowers);
    }

    public async Task<IActionResult> Flowers()
    {
        var flowers = await getAllFlowers();
        return View(flowers);
    }

    public async Task<IActionResult> FlowerDetails(string id)
    {
        var flower = new Flowers();
        flower = await getFlowerById(id);
        return View(flower);
    }
    [HttpGet("/find")]
    public IActionResult Find(string query)
    {
        var flowers = GetFlowersByKeyword(query);
        if (!flowers.Any())
        {
            ViewBag.Message = "No products found.";
        }
        return View(flowers);
    }
    public List<Flowers> GetFlowersByKeyword(string keyword)
    {
        var flowers = new List<Flowers>();
        var connecString = _configuration.GetConnectionString("Default");
        if (keyword == null)
        {
            return flowers;
        }
        using (SqlConnection connection = new SqlConnection(connecString))
        {
            using (SqlCommand command = new SqlCommand("sp_FindFlowers", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Keyword", keyword);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var flower = new Flowers
                        {
                            MaHoa = reader.GetString(reader.GetOrdinal("MaHoa")),
                            TenHoa = reader.GetString(reader.GetOrdinal("TenHoa")),
                            MoTa = reader.GetString(reader.GetOrdinal("MoTa")),
                            GiaBan = reader.GetDecimal(reader.GetOrdinal("GiaBan")),
                            HinhAnh = reader.GetString(reader.GetOrdinal("HinhAnh"))
                        };
                        flowers.Add(flower);
                    }
                }
            }
        }

        return flowers;
    }
    [HttpPost]
    public async Task<IActionResult> SortFlowers(int priceSort)
    {
        var flowers = new List<Flowers>();
        var connecString = _configuration.GetConnectionString("Default");

        string orderBy = priceSort switch
        {
            1 => "ORDER BY GiaBan ASC",
            2 => "ORDER BY GiaBan DESC",
            _ => ""
        };

        string query = $"SELECT * FROM HienThiToanBoHoa {orderBy}";

        using (var connec = new SqlConnection(connecString))
        {
            await connec.OpenAsync();
            using (SqlCommand cmd = new SqlCommand(query, connec))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        flowers.Add(new Flowers
                        {
                            MaHoa = reader.GetString(0),
                            TenHoa = reader.GetString(3),
                            MoTa = reader.GetString(6),
                            HinhAnh = reader.GetString(7),
                            GiaBan = reader.GetDecimal(4)
                        });
                    }
                }
            }
        }
        return PartialView("_FlowersPartial", flowers);
    }


    [HttpPost]
    public async Task<IActionResult> SortFlowersByCategory(string Category, int priceSort)
    {
        var flowers = new List<Flowers>();
        var connecString = _configuration.GetConnectionString("Default");
        if (string.IsNullOrEmpty(Category))
        {
            ModelState.AddModelError("Category", "Category không được để trống.");
            return View("Error");
        }
        string orderBy = priceSort switch
        {
            1 => "ORDER BY GiaBan ASC",
            2 => "ORDER BY GiaBan DESC",
            _ => ""
        };

        string query = $"SELECT * FROM HienThiToanBoHoa WHERE Mahoatuoi = @Category {orderBy}";

        try
        {
            using (var connec = new SqlConnection(connecString))
            {
                await connec.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(query, connec))
                {
                    cmd.Parameters.AddWithValue("@Category", Category);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            flowers.Add(new Flowers
                            {
                                MaHoa = reader.GetString(0),
                                TenHoa = reader.GetString(3),
                                MoTa = reader.GetString(6),
                                HinhAnh = reader.GetString(7),
                                GiaBan = reader.GetDecimal(4)
                            });
                        }
                    }
                }
            }
            return PartialView("_FlowersPartial", flowers);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
            return View("Error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SortFlowersByOccasion(string Occasion, int priceSort)
    {
        var flowers = new List<Flowers>();
        var connecString = _configuration.GetConnectionString("Default");

        string orderBy = priceSort switch
        {
            1 => "ORDER BY GiaBan ASC",
            2 => "ORDER BY GiaBan DESC",
            _ => ""
        };

        string query = $"SELECT * FROM HienThiToanBoHoa WHERE Madanhmuc = @Occasion {orderBy}";

        try
        {
            using (var connec = new SqlConnection(connecString))
            {
                await connec.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(query, connec))
                {
                    cmd.Parameters.AddWithValue("@Occasion", Occasion);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            flowers.Add(new Flowers
                            {
                                MaHoa = reader.GetString(0),
                                TenHoa = reader.GetString(3),
                                MoTa = reader.GetString(6),
                                HinhAnh = reader.GetString(7),
                                GiaBan = reader.GetDecimal(4)
                            });
                        }
                    }
                }
            }
            return PartialView("_FlowersPartial", flowers);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
            return View("Error");
        }
    }
}

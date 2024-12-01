using Microsoft.AspNetCore.Mvc;
using shopflowerproject.Models;
using shopflowerproject.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Threading.Tasks;
using Newtonsoft.Json;
using shopflowerproject.Filters;
using shopflowerproject.Areas.Admin.Models;

namespace shopflowerproject.Areas.Admin.Controllers
{
    // Tắt/bật comment dòng ở dưới để bật/tắt xác minh admin khi vào trang quản lý hoa
	[AuthorizeAdmin]

	[Area("Admin")]
	public class FlowersController : Controller
	{
		private readonly IConfiguration _configuration;
        private readonly ILogger<FlowersController> _logger;
        public FlowersController(IConfiguration configuration, ILogger<FlowersController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        //Lấy danh sách danh mục và loại hoa
        private async Task<List<FlowerCategory>> GetFlowerCategoryAsync()
		{
			var category = new List<FlowerCategory>();

			var connectionString = _configuration.GetConnectionString("Default");

			using (var connection = new SqlConnection(connectionString))
			{
				await connection.OpenAsync();
				using (var command = new SqlCommand("SELECT MaDanhMuc, TenDanhMuc " +
													"FROM DanhMucHoa", connection))
				using (var reader = await command.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						category.Add(new FlowerCategory
						{
							MaDanhMuc = reader.GetString("MaDanhMuc"),
							TenDanhMuc = reader.GetString("TenDanhMuc")
						});
					}
				}
			}
			return category;
		}
		private async Task<List<FlowerType>> GetFlowerTypeAsync()
		{
			var type = new List<FlowerType>();

			var connectionString = _configuration.GetConnectionString("Default");

			using (var connection = new SqlConnection(connectionString))
			{
				await connection.OpenAsync();
				using (var command = new SqlCommand("SELECT MaHoaTuoi, TenHoaTuoi " +
													"FROM TheLoai", connection))
				using (var reader = await command.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						type.Add(new FlowerType
						{
							MaHoaTuoi = reader.GetString("MaHoaTuoi"),
							TenHoaTuoi = reader.GetString("TenHoaTuoi")
						});
					}
				}
			}
			return type;
		}

        private async Task<string> UploadImageToImgBB(IFormFile file)
        {
            var client = new HttpClient();
            var form = new MultipartFormDataContent();

            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            form.Add(fileContent, "image", file.FileName);

            // Thay "YOUR_API_KEY" bằng API key của bạn từ imgBB
            var response = await client.PostAsync("https://api.imgbb.com/1/upload?key=3fdf5f6f5d21a17846d947ee404e85b5", form);

            // Kiểm tra trạng thái phản hồi từ API
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(json);

                // In ra phản hồi để kiểm tra
                Console.WriteLine(jsonResponse);

                return jsonResponse.data.url;  // Trả về URL ảnh từ imgBB
            }
            else
            {
                // In lỗi nếu API trả về không thành công
                Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                return null; // Hoặc xử lý lỗi theo cách bạn muốn
            }
        }

        // Action Index để hiển thị danh sách hoa
        public async Task<IActionResult> Index(int page = 1, string keyword = "", string sortOrder = "mahoa_asc")
        {
            // Phân trang
            int pageSize = 6; // Số sản phẩm hiển thị mỗi trang
            int skip = (page - 1) * pageSize;

            List<Flowers> dshoa = new List<Flowers>();
            var connectionString = _configuration.GetConnectionString("Default");

            // Truy vấn danh sách sản phẩm tìm kiếm và phân trang
            var commandText = $@"
				EXEC sp_FindFlowersManagement @Keyword = @Keyword, @SortOrder = @SortOrder;
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
                        var flower = new Flowers
                        {
                            MaHoa = reader.GetString("MaHoa"),
                            TenHoa = reader.GetString("TenHoa"),
                            TenDanhMuc = reader.GetString("TenDanhMuc"),
                            TenHoaTuoi = reader.GetString("TenHoaTuoi"),
                            MoTa = reader.GetString("MoTa"),
                            GiaBan = reader.GetDecimal("GiaBan"),
                            HinhAnh = reader.GetString("HinhAnh")
                        };
                        dshoa.Add(flower);
                    }
                }
            }

            // Lấy tổng số sản phẩm tìm kiếm (chỉ tính tổng số sản phẩm phù hợp với từ khóa tìm kiếm)
            var totalItemsCommand = new SqlCommand(@"
				SELECT COUNT(DISTINCT h.MaHoa)
				FROM Hoa h
				INNER JOIN DanhMucHoa dm ON h.MaDanhMuc = dm.MaDanhMuc
				INNER JOIN TheLoai tl ON h.MaHoaTuoi = tl.MaHoaTuoi
				WHERE LOWER(h.TenHoa) LIKE LOWER('%' + @Keyword + '%')  
					  OR LOWER(dm.TenDanhMuc) LIKE LOWER('%' + @Keyword + '%')
					  OR LOWER(tl.TenHoaTuoi) LIKE LOWER('%' + @Keyword + '%')
			", new SqlConnection(connectionString));

            totalItemsCommand.Parameters.AddWithValue("@Keyword", $"%{keyword}%");

            await totalItemsCommand.Connection.OpenAsync();
            int totalItems = (int)await totalItemsCommand.ExecuteScalarAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            dshoa = dshoa.Skip(skip).Take(pageSize).ToList();
            ViewBag.Pagination = new PaginationModel
            {
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                MaxPagesToShow = 5 // Hiển thị tối đa 5 trang liền kề
            };
            //ViewBag.CurrentPage = page;
            //ViewBag.TotalPages = totalPages;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Keyword = keyword;  // Đảm bảo từ khóa tìm kiếm được lưu lại khi phân trang
            ViewBag.FlowerCategory = await GetFlowerCategoryAsync();
            ViewBag.FlowerType = await GetFlowerTypeAsync();

            var viewModel = new FlowerVM
            {
                FlowerList = dshoa,
                flw = new Flowers()
            };

            return View(viewModel);
        }

        // Action Create (GET)
        public async Task<IActionResult> Create()
		{
			ViewBag.FlowerCategory = await GetFlowerCategoryAsync();
			ViewBag.FlowerType = await GetFlowerTypeAsync();
			var viewModel = new FlowerVM
			{
				FlowerList = new List<Flowers>(),
				flw = new Flowers()
			};
			return View(viewModel);
		}

		//Action Create(POST)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(FlowerVM viewModel, IFormFile HinhAnh)
		{
            var f = viewModel.flw;

            var connectionString = _configuration.GetConnectionString("Default");

            if (HinhAnh != null && HinhAnh.Length > 0)
            {
                // Gửi ảnh lên imgBB và nhận URL ảnh
                var imageUrl = await UploadImageToImgBB(HinhAnh);
                f.HinhAnh = imageUrl;  // Lưu URL ảnh vào thuộc tính HinhAnh
            }

			using (var connection = new SqlConnection(connectionString))
			{
				var command = new SqlCommand("INSERT INTO Hoa (MaHoa, MaDanhMuc, MaHoaTuoi, TenHoa, GiaBan, MoTa, HinhAnh) " +
											 "VALUES (@MaHoa, @MaDanhMuc, @MaHoaTuoi, @TenHoa, @GiaBan, @MoTa, @HinhAnh)", connection);
				command.Parameters.AddWithValue("@MaHoa", f.MaHoa);
				command.Parameters.AddWithValue("@MaDanhMuc", f.MaDanhMuc);
				command.Parameters.AddWithValue("@MaHoaTuoi", f.MaHoaTuoi);
				command.Parameters.AddWithValue("@TenHoa", f.TenHoa);
				command.Parameters.AddWithValue("@MoTa", f.MoTa);
				command.Parameters.AddWithValue("@GiaBan", f.GiaBan);
				command.Parameters.AddWithValue("@HinhAnh", f.HinhAnh);

				await connection.OpenAsync();
				await command.ExecuteNonQueryAsync();
			}

			return RedirectToAction("Index");
		}

        // Action Edit (GET)
        public async Task<IActionResult> Edit(string id)
        {
            var connectionString = _configuration.GetConnectionString("Default");
            var flower = new Flowers();

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SELECT * FROM Hoa WHERE MaHoa = @MaHoa", connection);
                command.Parameters.AddWithValue("@MaHoa", id);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        flower.MaHoa = reader["MaHoa"].ToString();
                        flower.MaDanhMuc = reader["MaDanhMuc"].ToString();
                        flower.MaHoaTuoi = reader["MaHoaTuoi"].ToString();
                        flower.TenHoa = reader["TenHoa"].ToString();
                        flower.GiaBan = Convert.ToDecimal(reader["GiaBan"]);
                        flower.MoTa = reader["MoTa"].ToString();
                        flower.HinhAnh = reader["HinhAnh"].ToString();
                    }
                }
            }

            return Json(flower);
        }

        // Action Edit (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FlowerVM viewModel, IFormFile HinhAnhMoi)
        {
            var f = viewModel.flw;

            // Kiểm tra nếu có tệp hình ảnh được tải lên
            if (HinhAnhMoi != null && HinhAnhMoi.Length > 0)
            {
                // Gọi hàm tải ảnh lên imgBB hoặc dịch vụ tương tự để lấy URL ảnh
                var imageUrl = await UploadImageToImgBB(HinhAnhMoi); // Bạn có thể sử dụng phương thức UploadImageToImgBB như đã tạo trước đó
                f.HinhAnh = imageUrl; // Lưu lại URL của hình ảnh vào đối tượng Flower
            }
            else
            {
                f.HinhAnh = viewModel.CurrentHinhAnh;
            }

            var connectionString = _configuration.GetConnectionString("Default");

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("UPDATE Hoa SET TenHoa = @TenHoa, MaDanhMuc = @MaDanhMuc, " +
                                             "MaHoaTuoi = @MaHoaTuoi, GiaBan = @GiaBan, MoTa = @MoTa, HinhAnh = @HinhAnh " +
                                             "WHERE MaHoa = @MaHoa", connection);

                command.Parameters.AddWithValue("@MaHoa", f.MaHoa);
                command.Parameters.AddWithValue("@TenHoa", f.TenHoa);
                command.Parameters.AddWithValue("@MaDanhMuc", f.MaDanhMuc);
                command.Parameters.AddWithValue("@MaHoaTuoi", f.MaHoaTuoi);
                command.Parameters.AddWithValue("@GiaBan", f.GiaBan);
                command.Parameters.AddWithValue("@MoTa", f.MoTa);
                command.Parameters.AddWithValue("@HinhAnh", f.HinhAnh); // Gửi URL hình ảnh (cũ hoặc mới)

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
					var command = new SqlCommand("DELETE FROM Hoa WHERE MaHoa = @MaHoa", connection);
					command.Parameters.AddWithValue("@MaHoa", id);

					await connection.OpenAsync();
					await command.ExecuteNonQueryAsync();
				}
                //return Json(new { success = true, message = "Xóa thành công!" });
                return RedirectToAction("Index");
			}
			catch
			{
                return Json(new { success = false, message = "Error" });
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
						var command = new SqlCommand("DELETE FROM Hoa WHERE MaHoa = @MaHoa", connection);
						command.Parameters.AddWithValue("@MaHoa", id);
						await command.ExecuteNonQueryAsync();
					}
				}
                //return Json(new { success = true, message = "Xóa thành công!" });
                return RedirectToAction("Index");
			}
			catch
			{
                return Json(new { success = false, message = "Error" });
            }
		}
    }
}

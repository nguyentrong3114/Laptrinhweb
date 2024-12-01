using System.ComponentModel.DataAnnotations;

namespace shopflowerproject.Models;

public class Flowers
{
    [Required(ErrorMessage = "Vui lòng nhập mã hoa.")]
    [RegularExpression(@"^[A-za-z 0-9]*$", ErrorMessage = "Không được sử dụng ký tự đặc biệt.")]
    [MinLength(2, ErrorMessage = "Vui lòng nhập ít nhất 2 ký tự trở lên")]
    public string? MaHoa { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public string? MaDanhMuc { get; set; }
    public string? TenDanhMuc { get; set; }
    public string? TheLoai { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại hoa .")]
    public string? MaHoaTuoi { get; set; }
    public string? TenHoaTuoi { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập mô tả.")]
    public string? MoTa { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên hoa.")]
    [RegularExpression(@"^[A-za-z 0-9]*$", ErrorMessage = "Không được sử dụng ký tự đặc biệt.")]
    public string? TenHoa { get; set; }
    public int SoLuong { get; set; }
    [Range(0, 99999999.99, ErrorMessage = "Giá bán phải nằm trong khoảng từ 0 đến 99,999,999.99.")]
    public Decimal GiaBan { get; set; }
    [Required(ErrorMessage = "Vui lòng thêm ảnh sản phẩm.")]
    public string? HinhAnh { get; set; }
}

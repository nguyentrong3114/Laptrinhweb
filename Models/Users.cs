using System.ComponentModel.DataAnnotations;
namespace shopflowerproject.Models
{
    public class Users
    {
        public string? MaUser { get; set; }
        [Required]
        public string? HoTen { get; set; }
        [Required]
        public string? Email { get; set; }
        [Required]
        public string? TenTaiKhoan { get; set; }
        public string? SoDienThoai { get; set; }
        public string? DiaChi { get; set; }
        public string? GioiTinh { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? VaiTro { get; set; }
        public DateTime? NgayDangKy { get; set; }
        public string? IdFacebook { get; set; }
        public string? IdGoogle { get; set; }
    }
}

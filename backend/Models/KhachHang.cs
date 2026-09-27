namespace GymManagement.API.Models;

public class KhachHang
{
    public int Id { get; set; }
    public string MaThe { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? DienThoai { get; set; }
    public string? DiaChi { get; set; }
    public string? LoaiThe { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public string TrangThai { get; set; } = "Chưa kích hoạt";
    public int SoLanTap { get; set; } = 0;
    public int DaTap { get; set; } = 0;
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public string? NguoiTao { get; set; } = "Administrator";
    public DateTime? NgaySua { get; set; }
    public string? NguoiSua { get; set; }
}

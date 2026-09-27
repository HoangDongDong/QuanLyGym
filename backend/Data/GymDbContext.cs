using Microsoft.EntityFrameworkCore;
using GymManagement.API.Models;

namespace GymManagement.API.Data;

public class GymDbContext : DbContext
{
    public GymDbContext(DbContextOptions<GymDbContext> options) : base(options)
    {
    }

    public DbSet<KhachHang> KhachHangs => Set<KhachHang>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.ToTable("KHACH_HANG");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(e => e.MaThe).HasColumnName("MA_THE").HasMaxLength(50).IsRequired();
            entity.Property(e => e.HoTen).HasColumnName("HO_TEN").HasMaxLength(150).IsRequired();
            entity.Property(e => e.DienThoai).HasColumnName("DIEN_THOAI").HasMaxLength(20);
            entity.Property(e => e.DiaChi).HasColumnName("DIA_CHI").HasMaxLength(250);
            entity.Property(e => e.LoaiThe).HasColumnName("LOAI_THE").HasMaxLength(100);
            entity.Property(e => e.TuNgay).HasColumnName("TU_NGAY");
            entity.Property(e => e.DenNgay).HasColumnName("DEN_NGAY");
            entity.Property(e => e.TrangThai).HasColumnName("TRANG_THAI").HasMaxLength(50);
            entity.Property(e => e.SoLanTap).HasColumnName("SO_LAN_TAP");
            entity.Property(e => e.DaTap).HasColumnName("DA_TAP");
            entity.Property(e => e.NgayTao).HasColumnName("NGAY_TAO");
            entity.Property(e => e.NguoiTao).HasColumnName("NGUOI_TAO").HasMaxLength(50);
            entity.Property(e => e.NgaySua).HasColumnName("NGAY_SUA");
            entity.Property(e => e.NguoiSua).HasColumnName("NGUOI_SUA").HasMaxLength(50);
        });
    }
}

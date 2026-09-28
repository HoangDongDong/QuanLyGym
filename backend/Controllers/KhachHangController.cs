using Microsoft.AspNetCore.Mvc;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Text;
using System.Text.Json;

namespace GymManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KhachHangController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly string _adminUserId = "4f1466a0-0756-4ba9-afa8-053b96ca7569";

    public KhachHangController(IConfiguration config)
    {
        _config = config;
    }

    private string GetConnStr() => _config.GetConnectionString("FirebirdConnection") ?? "";

    /// <summary>
    /// 1. Danh sách khách hàng với bộ lọc trạng thái / nhóm khách hàng và tìm kiếm
    /// </summary>
    [HttpGet]
    public IActionResult GetAll(
        [FromQuery] string? search, 
        [FromQuery] string? trangThaiId, 
        [FromQuery] string? nhomKhachHangId)
    {
        var list = new List<object>();

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();
            using var cmd = conn.CreateCommand();

            var sql = @"
                SELECT 
                    k.ID,
                    k.MAKHACH,
                    k.NAME AS TEN_KHACH_HANG,
                    k.DIACHI,
                    k.DIENTHOAI,
                    k.EMAIL,
                    k.FACEBOOK,
                    k.DLOAITHEID,
                    lt.NAME AS TEN_LOAI_THE,
                    k.TUNGAY,
                    k.DENNGAY,
                    k.DTRANGTHAIID,
                    tt.NAME AS TEN_TRANG_THAI,
                    k.SOLAN,
                    k.DATAP,
                    k.CONLAI,
                    k.NOTE,
                    k.STATUS,
                    k.MAVANTAY,
                    k.DCATAPID,
                    ct.NAME AS TEN_CATAP,
                    k.DNHOMKHACHHANGID,
                    nk.NAME AS TEN_NHOM,
                    k.DNHANVIENID,
                    nv.NAME AS TEN_NHANVIEN,
                    k.DTINHTHANHID,
                    tinh.NAME AS TEN_TINHTHANH,
                    k.NGAYSINH,
                    k.MASOTHUE,
                    k.DIEMTICHLUYBANDAU,
                    k.ANH,
                    k.TIMECREATED,
                    k.TIMEMODIFIED,
                    k.USERCREATEDID,
                    k.USERMODIFIEDID,
                    u1.NAME AS NGUOI_TAO,
                    u2.NAME AS NGUOI_SUA
                FROM DKHACHHANG k
                LEFT JOIN DLOAITHE lt ON k.DLOAITHEID = lt.ID
                LEFT JOIN DTRANGTHAI tt ON k.DTRANGTHAIID = tt.ID
                LEFT JOIN DCATAP ct ON k.DCATAPID = ct.ID
                LEFT JOIN DNHOMKHACHHANG nk ON k.DNHOMKHACHHANGID = nk.ID
                LEFT JOIN DNHANVIEN nv ON k.DNHANVIENID = nv.ID
                LEFT JOIN DTINHTHANH tinh ON k.DTINHTHANHID = tinh.ID
                LEFT JOIN SUSER u1 ON k.USERCREATEDID = u1.ID
                LEFT JOIN SUSER u2 ON k.USERMODIFIEDID = u2.ID
                WHERE 1=1 ";

            bool isTrash = string.Equals(trangThaiId, "trash", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(nhomKhachHangId, "trash", StringComparison.OrdinalIgnoreCase);

            if (isTrash)
            {
                // Thùng rác: No1Lib lưu STATUS = 0 hoặc -1
                sql += " AND (k.STATUS = 0 OR k.STATUS = -1) ";
            }
            else
            {
                // Danh sách đang hoạt động: STATUS = 30 hoặc khác 0 / -1
                sql += " AND (k.STATUS <> 0 AND k.STATUS <> -1 OR k.STATUS IS NULL) ";

                // 1. Lọc theo trạng thái thẻ (DTRANGTHAIID)
                if (string.Equals(trangThaiId, "unset", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND (k.DTRANGTHAIID IS NULL OR TRIM(k.DTRANGTHAIID) = '') ";
                }
                else if (!string.IsNullOrWhiteSpace(trangThaiId) && !string.Equals(trangThaiId, "all", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND k.DTRANGTHAIID = @trangThaiId ";
                    cmd.Parameters.AddWithValue("@trangThaiId", trangThaiId.Trim());
                }

                // 2. Lọc theo nhóm khách hàng (DNHOMKHACHHANGID) - Như trong TreeGridMg
                if (string.Equals(nhomKhachHangId, "unset", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND (k.DNHOMKHACHHANGID IS NULL OR TRIM(k.DNHOMKHACHHANGID) = '') ";
                }
                else if (!string.IsNullOrWhiteSpace(nhomKhachHangId) && !string.Equals(nhomKhachHangId, "all", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND k.DNHOMKHACHHANGID = @nhomKhachHangId ";
                    cmd.Parameters.AddWithValue("@nhomKhachHangId", nhomKhachHangId.Trim());
                }
            }

            // Tìm kiếm nhanh (F3) - Đúng theo logic No1Lib: UPPER(NAME) LIKE ... OR UPPER(MAKHACH) LIKE ... OR UPPER(DIENTHOAI) LIKE ...
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToUpper();
                sql += " AND (UPPER(k.MAKHACH) LIKE @search OR UPPER(k.NAME) LIKE @search OR UPPER(k.DIENTHOAI) LIKE @search OR UPPER(k.DIACHI) LIKE @search) ";
                cmd.Parameters.AddWithValue("@search", $"%{s}%");
            }

            sql += " ORDER BY k.TIMECREATED DESC ROWS 1 TO 500";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            int index = 1;
            while (reader.Read())
            {
                var tuNgay = reader["TUNGAY"] is DBNull ? null : Convert.ToDateTime(reader["TUNGAY"]).ToString("dd/MM/yyyy");
                var denNgay = reader["DENNGAY"] is DBNull ? null : Convert.ToDateTime(reader["DENNGAY"]).ToString("dd/MM/yyyy");
                var timeCreated = reader["TIMECREATED"] is DBNull ? null : Convert.ToDateTime(reader["TIMECREATED"]).ToString("dd/MM/yyyy HH:mm:ss");
                var timeModified = reader["TIMEMODIFIED"] is DBNull ? null : Convert.ToDateTime(reader["TIMEMODIFIED"]).ToString("dd/MM/yyyy HH:mm:ss");

                var ngaySinh = reader["NGAYSINH"] is DBNull ? "" : Convert.ToDateTime(reader["NGAYSINH"]).ToString("yyyy-MM-dd");
                var dTinhThanhId = reader["DTINHTHANHID"]?.ToString()?.Trim() ?? "";

                string? anhBase64 = null;
                if (reader["ANH"] is not DBNull)
                {
                    try
                    {
                        var b = (byte[])reader["ANH"];
                        if (b != null && b.Length > 0)
                        {
                            anhBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(b);
                        }
                    }
                    catch { }
                }

                list.Add(new
                {
                    stt = index++,
                    id = reader["ID"]?.ToString()?.Trim(),
                    maThe = reader["MAKHACH"]?.ToString()?.Trim() ?? "",
                    tenKhachHang = reader["TEN_KHACH_HANG"]?.ToString()?.Trim() ?? "",
                    diaChi = reader["DIACHI"]?.ToString()?.Trim() ?? "",
                    dienThoai = reader["DIENTHOAI"]?.ToString()?.Trim() ?? "",
                    email = reader["EMAIL"]?.ToString()?.Trim() ?? "",
                    facebook = reader["FACEBOOK"]?.ToString()?.Trim() ?? "",
                    dLoaiTheId = reader["DLOAITHEID"]?.ToString()?.Trim() ?? "",
                    loaiThe = reader["TEN_LOAI_THE"]?.ToString()?.Trim() ?? "",
                    tuNgay = tuNgay,
                    denNgay = denNgay,
                    dTrangThaiId = reader["DTRANGTHAIID"]?.ToString()?.Trim() ?? "",
                    trangThai = reader["TEN_TRANG_THAI"]?.ToString()?.Trim() ?? "Chưa kích hoạt",
                    soLan = reader["SOLAN"] is DBNull ? 0 : Convert.ToInt32(reader["SOLAN"]),
                    daTap = reader["DATAP"] is DBNull ? 0 : Convert.ToInt32(reader["DATAP"]),
                    conLai = reader["CONLAI"] is DBNull ? 0 : Convert.ToInt32(reader["CONLAI"]),
                    note = reader["NOTE"]?.ToString()?.Trim() ?? "",
                    status = reader["STATUS"] is DBNull ? 30 : Convert.ToInt32(reader["STATUS"]),
                    maVanTay = reader["MAVANTAY"]?.ToString()?.Trim() ?? "",
                    dCaTapId = reader["DCATAPID"]?.ToString()?.Trim() ?? "",
                    caTap = reader["TEN_CATAP"]?.ToString()?.Trim() ?? "",
                    dNhomKhachHangId = reader["DNHOMKHACHHANGID"]?.ToString()?.Trim() ?? "",
                    nhomKhachHang = reader["TEN_NHOM"]?.ToString()?.Trim() ?? "",
                    dNhanVienId = reader["DNHANVIENID"]?.ToString()?.Trim() ?? "",
                    nhanVien = reader["TEN_NHANVIEN"]?.ToString()?.Trim() ?? "",
                    dTinhThanhId = dTinhThanhId,
                    tinhThanh = reader["TEN_TINHTHANH"]?.ToString()?.Trim() ?? "",
                    ngaySinh = ngaySinh,
                    maSoThue = reader["MASOTHUE"]?.ToString()?.Trim() ?? "",
                    diemTichLuyBanDau = reader["DIEMTICHLUYBANDAU"] is DBNull ? 0 : Convert.ToDecimal(reader["DIEMTICHLUYBANDAU"]),
                    anh = anhBase64,
                    timeCreated = timeCreated,
                    timeModified = timeModified,
                    userCreatedName = reader["NGUOI_TAO"]?.ToString()?.Trim() ?? "Administrator",
                    userModifiedName = reader["NGUOI_SUA"]?.ToString()?.Trim() ?? "Administrator"
                });
            }

            return Ok(new { success = true, count = list.Count, data = list });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi đọc danh mục khách hàng: " + ex.Message });
        }
    }

    /// <summary>
    /// 2. Số lượng khách hàng theo trạng thái thẻ và theo nhóm khách hàng bên cây danh mục trái
    /// </summary>
    [HttpGet("tree-counts")]
    public IActionResult GetTreeCounts()
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            var counts = new Dictionary<string, int>
            {
                ["all"] = 0,
                ["unset"] = 0,
                ["0"] = 0, // Chưa kích hoạt
                ["1"] = 0, // Đang sử dụng
                ["2"] = 0, // Bảo lưu
                ["3"] = 0, // Quá hạn
                ["4"] = 0, // Quá lần tập
                ["trash"] = 0 // Thùng rác
            };

            var groupCounts = new Dictionary<string, int>
            {
                ["all"] = 0,
                ["unset"] = 0,
                ["trash"] = 0
            };

            // 1. Đếm theo Trạng thái thẻ
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        STATUS, 
                        DTRANGTHAIID, 
                        COUNT(*) AS CNT
                    FROM DKHACHHANG
                    GROUP BY STATUS, DTRANGTHAIID";

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var status = r["STATUS"] is not DBNull ? Convert.ToInt32(r["STATUS"]) : 30;
                    var dTrangThaiId = r["DTRANGTHAIID"]?.ToString()?.Trim() ?? "";
                    var cnt = Convert.ToInt32(r["CNT"]);

                    if (status == 0 || status == -1)
                    {
                        counts["trash"] += cnt;
                    }
                    else
                    {
                        counts["all"] += cnt;
                        if (string.IsNullOrEmpty(dTrangThaiId))
                        {
                            counts["unset"] += cnt;
                        }
                        else
                        {
                            if (counts.ContainsKey(dTrangThaiId))
                                counts[dTrangThaiId] += cnt;
                            else
                                counts[dTrangThaiId] = cnt;
                        }
                    }
                }
            }

            // 2. Đếm theo Nhóm khách hàng
            var groupList = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        k.STATUS, 
                        k.DNHOMKHACHHANGID, 
                        COUNT(*) AS CNT
                    FROM DKHACHHANG k
                    GROUP BY k.STATUS, k.DNHOMKHACHHANGID";

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var status = r["STATUS"] is not DBNull ? Convert.ToInt32(r["STATUS"]) : 30;
                    var nhomId = r["DNHOMKHACHHANGID"]?.ToString()?.Trim() ?? "";
                    var cnt = Convert.ToInt32(r["CNT"]);

                    if (status == 0 || status == -1)
                    {
                        groupCounts["trash"] += cnt;
                    }
                    else
                    {
                        groupCounts["all"] += cnt;
                        if (string.IsNullOrEmpty(nhomId))
                        {
                            groupCounts["unset"] += cnt;
                        }
                        else
                        {
                            if (!groupCounts.ContainsKey(nhomId)) groupCounts[nhomId] = 0;
                            groupCounts[nhomId] += cnt;
                        }
                    }
                }
            }

            // Lấy danh sách nhóm khách hàng từ DNHOMKHACHHANG
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME FROM DNHOMKHACHHANG ORDER BY NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var gId = r["ID"]?.ToString()?.Trim() ?? "";
                    var gName = r["NAME"]?.ToString()?.Trim() ?? "";
                    var cnt = groupCounts.ContainsKey(gId) ? groupCounts[gId] : 0;
                    groupList.Add(new { id = gId, name = gName, count = cnt });
                }
            }

            return Ok(new { success = true, counts, groupCounts, groups = groupList });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi đếm trạng thái: " + ex.Message });
        }
    }

        /// <summary>
    /// 3. Lấy dữ liệu chi tiết cho 14 subtabs ở dưới (chuẩn theo database DATA.fdb)
    /// </summary>
    [HttpGet("{id}/subtabs")]
    public IActionResult GetCustomerSubtabs(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { success = false, message = "ID khách hàng không hợp lệ" });

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            // 1. Tab Thông tin (DKHACHHANG)
            object? auditInfo = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        k.ID, k.MAKHACH, k.NAME, k.DIACHI, k.DIENTHOAI, k.EMAIL, k.FACEBOOK,
                        k.TIMECREATED, k.TIMEMODIFIED, k.USERCREATEDID, k.USERMODIFIEDID,
                        k.MAVANTAY, k.NOTE, k.NGAYSINH, k.MASOTHUE, k.DIEMTICHLUYBANDAU,
                        k.TUNGAY, k.DENNGAY, k.SOLAN, k.DATAP, k.CONLAI,
                        u1.NAME AS USERCREATED_NAME, u1.USERNAME AS USERCREATED_USER,
                        u2.NAME AS USERMODIFIED_NAME, u2.USERNAME AS USERMODIFIED_USER,
                        lt.NAME AS TEN_LOAI_THE, ct.NAME AS TEN_CATAP,
                        nk.NAME AS TEN_NHOM, nv.NAME AS TEN_NHANVIEN,
                        tinh.NAME AS TEN_TINHTHANH, tt.NAME AS TEN_TRANGTHAI
                    FROM DKHACHHANG k
                    LEFT JOIN SUSER u1 ON k.USERCREATEDID = u1.ID
                    LEFT JOIN SUSER u2 ON k.USERMODIFIEDID = u2.ID
                    LEFT JOIN DLOAITHE lt ON k.DLOAITHEID = lt.ID
                    LEFT JOIN DCATAP ct ON k.DCATAPID = ct.ID
                    LEFT JOIN DNHOMKHACHHANG nk ON k.DNHOMKHACHHANGID = nk.ID
                    LEFT JOIN DNHANVIEN nv ON k.DNHANVIENID = nv.ID
                    LEFT JOIN DTINHTHANH tinh ON k.DTINHTHANHID = tinh.ID
                    LEFT JOIN DTRANGTHAI tt ON k.DTRANGTHAIID = tt.ID
                    WHERE k.ID = @id";
                cmd.Parameters.AddWithValue("@id", id.Trim());

                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    auditInfo = new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        maKhach = r["MAKHACH"]?.ToString()?.Trim() ?? "",
                        name = r["NAME"]?.ToString()?.Trim() ?? "",
                        diaChi = r["DIACHI"]?.ToString()?.Trim() ?? "",
                        dienThoai = r["DIENTHOAI"]?.ToString()?.Trim() ?? "",
                        email = r["EMAIL"]?.ToString()?.Trim() ?? "",
                        facebook = r["FACEBOOK"]?.ToString()?.Trim() ?? "",
                        maVanTay = r["MAVANTAY"]?.ToString()?.Trim() ?? "",
                        note = r["NOTE"]?.ToString()?.Trim() ?? "",
                        ngaySinh = r["NGAYSINH"] is not DBNull ? Convert.ToDateTime(r["NGAYSINH"]).ToString("dd/MM/yyyy") : "",
                        maSoThue = r["MASOTHUE"]?.ToString()?.Trim() ?? "",
                        diemTichLuyBanDau = r["DIEMTICHLUYBANDAU"] is not DBNull ? Convert.ToDecimal(r["DIEMTICHLUYBANDAU"]) : 0,
                        tuNgay = r["TUNGAY"] is not DBNull ? Convert.ToDateTime(r["TUNGAY"]).ToString("dd/MM/yyyy") : "",
                        denNgay = r["DENNGAY"] is not DBNull ? Convert.ToDateTime(r["DENNGAY"]).ToString("dd/MM/yyyy") : "",
                        soLan = r["SOLAN"] is not DBNull ? Convert.ToInt32(r["SOLAN"]) : 0,
                        daTap = r["DATAP"] is not DBNull ? Convert.ToInt32(r["DATAP"]) : 0,
                        conLai = r["CONLAI"] is not DBNull ? Convert.ToInt32(r["CONLAI"]) : 0,
                        tenLoaiThe = r["TEN_LOAI_THE"]?.ToString()?.Trim() ?? "",
                        tenCaTap = r["TEN_CATAP"]?.ToString()?.Trim() ?? "",
                        tenNhom = r["TEN_NHOM"]?.ToString()?.Trim() ?? "",
                        tenNhanVien = r["TEN_NHANVIEN"]?.ToString()?.Trim() ?? "",
                        tenTinhThanh = r["TEN_TINHTHANH"]?.ToString()?.Trim() ?? "",
                        tenTrangThai = r["TEN_TRANGTHAI"]?.ToString()?.Trim() ?? "",
                        timeCreated = r["TIMECREATED"] is not DBNull ? Convert.ToDateTime(r["TIMECREATED"]).ToString("dd/MM/yyyy hh:mm tt") : "---",
                        timeModified = r["TIMEMODIFIED"] is not DBNull ? Convert.ToDateTime(r["TIMEMODIFIED"]).ToString("dd/MM/yyyy hh:mm tt") : "---",
                        userCreated = r["USERCREATED_NAME"]?.ToString()?.Trim() ?? r["USERCREATED_USER"]?.ToString()?.Trim() ?? "Administrator",
                        userModified = r["USERMODIFIED_NAME"]?.ToString()?.Trim() ?? r["USERMODIFIED_USER"]?.ToString()?.Trim() ?? "Administrator"
                    };
                }
            }

            // 2. Tab Báo giá (TBAOGIA)
            var baoGia = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        b.ID, b.NAME AS SO_PHIEU, b.NGAY, b.TENKHACH, b.DIENTHOAI, b.DIACHI, b.EMAIL,
                        b.TIENHANG, b.TILEGIAMGIA, b.TIENGIAMGIA, b.TILETHUE, b.TIENTHUE, b.PHIVANCHUYEN,
                        b.TONGCONG, b.NOTE
                    FROM TBAOGIA b
                    WHERE b.DKHACHHANGID = @id
                    ORDER BY b.NGAY DESC, b.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    baoGia.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        tenKhach = r["TENKHACH"]?.ToString()?.Trim() ?? "",
                        dienThoai = r["DIENTHOAI"]?.ToString()?.Trim() ?? "",
                        diaChi = r["DIACHI"]?.ToString()?.Trim() ?? "",
                        email = r["EMAIL"]?.ToString()?.Trim() ?? "",
                        tienHang = r["TIENHANG"] is not DBNull ? Convert.ToDecimal(r["TIENHANG"]) : 0,
                        tiLeGiamGia = r["TILEGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TILEGIAMGIA"]) : 0,
                        tienGiamGia = r["TIENGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TIENGIAMGIA"]) : 0,
                        tiLeThue = r["TILETHUE"] is not DBNull ? Convert.ToDecimal(r["TILETHUE"]) : 0,
                        tienThue = r["TIENTHUE"] is not DBNull ? Convert.ToDecimal(r["TIENTHUE"]) : 0,
                        phiVanChuyen = r["PHIVANCHUYEN"] is not DBNull ? Convert.ToDecimal(r["PHIVANCHUYEN"]) : 0,
                        tongCong = r["TONGCONG"] is not DBNull ? Convert.ToDecimal(r["TONGCONG"]) : 0,
                        note = r["NOTE"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 3. Tab Đơn hàng (TDONHANG)
            var donHang = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        d.ID, d.NAME AS SO_PHIEU, d.NGAY, k.NAME AS TEN_KHACH, d.TONGCONG,
                        nvx.NAME AS TEN_NV_BAN, d.GIOTHANHTOAN, u.NAME AS TEN_THUNGAN, vc.NAME AS TEN_VOUCHER,
                        d.TIENMAT, d.CHUYENKHOAN, d.THETRATRUOC, d.TIENHANG, d.TIENGIAMGIA, d.TIENTHUE,
                        d.PHIVANCHUYEN, d.THANHTOAN, d.CONLAI, nvg.NAME AS TEN_NV_GIAO, d.TRICHNHANVIEN,
                        ch.NAME AS TEN_CUAHANG, d.NOTE
                    FROM TDONHANG d
                    LEFT JOIN DKHACHHANG k ON d.DKHACHHANGID = k.ID
                    LEFT JOIN DNHANVIEN nvx ON d.DNHANVIENXUATID = nvx.ID
                    LEFT JOIN DNHANVIEN nvg ON d.DNHANVIENGIAOID = nvg.ID
                    LEFT JOIN SUSER u ON d.USERTHANHTOANID = u.ID
                    LEFT JOIN DVOUCHER vc ON d.DVOUCHERID = vc.ID
                    LEFT JOIN DCUAHANG ch ON d.DCUAHANGID = ch.ID
                    WHERE d.DKHACHHANGID = @id
                    ORDER BY d.NGAY DESC, d.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    donHang.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        khachHang = r["TEN_KHACH"]?.ToString()?.Trim() ?? "",
                        tongCong = r["TONGCONG"] is not DBNull ? Convert.ToDecimal(r["TONGCONG"]) : 0,
                        nhanVienBan = r["TEN_NV_BAN"]?.ToString()?.Trim() ?? "",
                        gioThanhToan = r["GIOTHANHTOAN"] is not DBNull ? Convert.ToDateTime(r["GIOTHANHTOAN"]).ToString("HH:mm:ss") : "",
                        thuNgan = r["TEN_THUNGAN"]?.ToString()?.Trim() ?? "",
                        voucher = r["TEN_VOUCHER"]?.ToString()?.Trim() ?? "",
                        tienMat = r["TIENMAT"] is not DBNull ? Convert.ToDecimal(r["TIENMAT"]) : 0,
                        chuyenKhoan = r["CHUYENKHOAN"] is not DBNull ? Convert.ToDecimal(r["CHUYENKHOAN"]) : 0,
                        theTraTruoc = r["THETRATRUOC"] is not DBNull ? Convert.ToDecimal(r["THETRATRUOC"]) : 0,
                        tienHang = r["TIENHANG"] is not DBNull ? Convert.ToDecimal(r["TIENHANG"]) : 0,
                        tienGiamGia = r["TIENGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TIENGIAMGIA"]) : 0,
                        tienThue = r["TIENTHUE"] is not DBNull ? Convert.ToDecimal(r["TIENTHUE"]) : 0,
                        phiVanChuyen = r["PHIVANCHUYEN"] is not DBNull ? Convert.ToDecimal(r["PHIVANCHUYEN"]) : 0,
                        thanhToan = r["THANHTOAN"] is not DBNull ? Convert.ToDecimal(r["THANHTOAN"]) : 0,
                        conLai = r["CONLAI"] is not DBNull ? Convert.ToDecimal(r["CONLAI"]) : 0,
                        nhanVienGiaoHang = r["TEN_NV_GIAO"]?.ToString()?.Trim() ?? "",
                        trichNhanVien = r["TRICHNHANVIEN"] is not DBNull ? Convert.ToDecimal(r["TRICHNHANVIEN"]) : 0,
                        cuaHang = r["TEN_CUAHANG"]?.ToString()?.Trim() ?? "",
                        note = r["NOTE"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 4. Tab Đặt hàng (TDATHANG)
            var datHang = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        dh.ID, dh.NAME AS SO_PHIEU, dh.NGAY, dh.TENKHACH, dh.DIENTHOAI, dh.DIACHI, dh.EMAIL,
                        dh.TIENHANG, dh.TILEGIAMGIA, dh.TIENGIAMGIA, dh.TILETHUE, dh.TIENTHUE, dh.PHIVANCHUYEN,
                        dh.TONGCONG, dh.NOTE
                    FROM TDATHANG dh
                    WHERE dh.DKHACHHANGID = @id
                    ORDER BY dh.NGAY DESC, dh.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    datHang.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        tenKhach = r["TENKHACH"]?.ToString()?.Trim() ?? "",
                        dienThoai = r["DIENTHOAI"]?.ToString()?.Trim() ?? "",
                        diaChi = r["DIACHI"]?.ToString()?.Trim() ?? "",
                        email = r["EMAIL"]?.ToString()?.Trim() ?? "",
                        tienHang = r["TIENHANG"] is not DBNull ? Convert.ToDecimal(r["TIENHANG"]) : 0,
                        tiLeGiamGia = r["TILEGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TILEGIAMGIA"]) : 0,
                        tienGiamGia = r["TIENGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TIENGIAMGIA"]) : 0,
                        tiLeThue = r["TILETHUE"] is not DBNull ? Convert.ToDecimal(r["TILETHUE"]) : 0,
                        tienThue = r["TIENTHUE"] is not DBNull ? Convert.ToDecimal(r["TIENTHUE"]) : 0,
                        phiVanChuyen = r["PHIVANCHUYEN"] is not DBNull ? Convert.ToDecimal(r["PHIVANCHUYEN"]) : 0,
                        tongCong = r["TONGCONG"] is not DBNull ? Convert.ToDecimal(r["TONGCONG"]) : 0,
                        note = r["NOTE"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 5. Tab Gia hạn thẻ (TGIAHANTHE: DLOAIGIAODICHID IN ('0', '1', '5') hoặc NULL)
            var giaHanThe = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        g.ID, g.SOTIEN, g.NOTE, g.NAME AS SO_PHIEU, g.NGAY, k.NAME AS TEN_KHACH,
                        g.SOLAN, lt.NAME AS TEN_LOAI_THE, g.TUNGAY, g.DENNGAY, g.DATAP,
                        g.TILEGIAMGIA, g.TIENGIAMGIA, g.TONGCONG, g.THANHTOAN, g.KHUYENMAI,
                        g.SONGAY, g.SOTHANG, g.LANTANGTHEM, g.NGAYTANGTHEM, g.DOANHSO, g.CHUAKICHHOAT
                    FROM TGIAHANTHE g
                    LEFT JOIN DKHACHHANG k ON g.DKHACHHANGID = k.ID
                    LEFT JOIN DLOAITHE lt ON g.DLOAITHEID = lt.ID
                    WHERE g.DKHACHHANGID = @id 
                      AND (g.DLOAIGIAODICHID IS NULL OR g.DLOAIGIAODICHID IN ('0', '1', '5'))
                    ORDER BY g.NGAY DESC, g.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    giaHanThe.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soTien = r["SOTIEN"] is not DBNull ? Convert.ToDecimal(r["SOTIEN"]) : 0,
                        note = r["NOTE"]?.ToString()?.Trim() ?? "",
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        khachHang = r["TEN_KHACH"]?.ToString()?.Trim() ?? "",
                        soLan = r["SOLAN"] is not DBNull ? Convert.ToInt32(r["SOLAN"]) : 0,
                        loaiThe = r["TEN_LOAI_THE"]?.ToString()?.Trim() ?? "",
                        tuNgay = r["TUNGAY"] is not DBNull ? Convert.ToDateTime(r["TUNGAY"]).ToString("dd/MM/yyyy") : "",
                        denNgay = r["DENNGAY"] is not DBNull ? Convert.ToDateTime(r["DENNGAY"]).ToString("dd/MM/yyyy") : "",
                        daTap = r["DATAP"] is not DBNull && (Convert.ToInt32(r["DATAP"]) == 1 || Convert.ToInt32(r["DATAP"]) == 30),
                        tiLeGiamGia = r["TILEGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TILEGIAMGIA"]) : 0,
                        tienGiamGia = r["TIENGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TIENGIAMGIA"]) : 0,
                        tongCong = r["TONGCONG"] is not DBNull ? Convert.ToDecimal(r["TONGCONG"]) : 0,
                        thanhToan = r["THANHTOAN"] is not DBNull ? Convert.ToDecimal(r["THANHTOAN"]) : 0,
                        khuyenMai = r["KHUYENMAI"]?.ToString()?.Trim() ?? "",
                        soNgay = r["SONGAY"] is not DBNull ? Convert.ToInt32(r["SONGAY"]) : 0,
                        soThang = r["SOTHANG"] is not DBNull ? Convert.ToInt32(r["SOTHANG"]) : 0,
                        lanTangThem = r["LANTANGTHEM"] is not DBNull ? Convert.ToInt32(r["LANTANGTHEM"]) : 0,
                        ngayTangThem = r["NGAYTANGTHEM"] is not DBNull ? Convert.ToInt32(r["NGAYTANGTHEM"]) : 0,
                        doanhSo = r["DOANHSO"] is not DBNull ? Convert.ToDecimal(r["DOANHSO"]) : 0,
                        chuaKichHoat = r["CHUAKICHHOAT"] is not DBNull && (Convert.ToInt32(r["CHUAKICHHOAT"]) == 1 || Convert.ToInt32(r["CHUAKICHHOAT"]) == 30)
                    });
                }
            }

            // 6. Tab Bảo lưu thẻ (TGIAHANTHE: DLOAIGIAODICHID = '9')
            var baoLuuThe = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        g.ID, g.SOTIEN, g.NOTE, g.NAME AS SO_PHIEU, g.NGAY, k.NAME AS TEN_KHACH,
                        lt.NAME AS TEN_LOAI_THE, g.TUNGAY, g.DENNGAY, g.SONGAY, g.DENNGAYTHUC
                    FROM TGIAHANTHE g
                    LEFT JOIN DKHACHHANG k ON g.DKHACHHANGID = k.ID
                    LEFT JOIN DLOAITHE lt ON g.DLOAITHEID = lt.ID
                    WHERE g.DKHACHHANGID = @id AND g.DLOAIGIAODICHID = '9'
                    ORDER BY g.NGAY DESC, g.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    baoLuuThe.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soTien = r["SOTIEN"] is not DBNull ? Convert.ToDecimal(r["SOTIEN"]) : 0,
                        note = r["NOTE"]?.ToString()?.Trim() ?? "",
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        khachHang = r["TEN_KHACH"]?.ToString()?.Trim() ?? "",
                        loaiThe = r["TEN_LOAI_THE"]?.ToString()?.Trim() ?? "",
                        tuNgay = r["TUNGAY"] is not DBNull ? Convert.ToDateTime(r["TUNGAY"]).ToString("dd/MM/yyyy") : "",
                        denNgay = r["DENNGAY"] is not DBNull ? Convert.ToDateTime(r["DENNGAY"]).ToString("dd/MM/yyyy") : "",
                        soNgay = r["SONGAY"] is not DBNull ? Convert.ToInt32(r["SONGAY"]) : 0,
                        denNgayThuc = r["DENNGAYTHUC"] is not DBNull ? Convert.ToDateTime(r["DENNGAYTHUC"]).ToString("dd/MM/yyyy") : ""
                    });
                }
            }

            // 7. Tab Đổi loại thẻ (TGIAHANTHE: DLOAIGIAODICHID = '2')
            var doiLoaiThe = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        g.ID, g.SOTIEN, g.NOTE, g.NAME AS SO_PHIEU, g.NGAY, k.NAME AS TEN_KHACH,
                        lt.NAME AS TEN_LOAI_THE, g.DLOAITHEID, g.DCATAPID, g.TUNGAY, g.DENNGAY, g.SOLAN,
                        g.NGAYTANGTHEM, g.LANTANGTHEM, g.TILEGIAMGIA, g.TIENGIAMGIA, g.TONGCONG, g.THANHTOAN,
                        g.REFID, ltRef.NAME AS TEN_LOAI_THE_CU
                    FROM TGIAHANTHE g
                    LEFT JOIN DKHACHHANG k ON g.DKHACHHANGID = k.ID
                    LEFT JOIN DLOAITHE lt ON g.DLOAITHEID = lt.ID
                    LEFT JOIN TGIAHANTHE gRef ON g.REFID = gRef.ID
                    LEFT JOIN DLOAITHE ltRef ON gRef.DLOAITHEID = ltRef.ID
                    WHERE g.DKHACHHANGID = @id AND g.DLOAIGIAODICHID = '2'
                    ORDER BY g.NGAY DESC, g.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    doiLoaiThe.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soTien = r["SOTIEN"] is not DBNull ? Convert.ToDecimal(r["SOTIEN"]) : 0,
                        note = r["NOTE"]?.ToString()?.Trim() ?? "",
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        khachHang = r["TEN_KHACH"]?.ToString()?.Trim() ?? "",
                        loaiThe = r["TEN_LOAI_THE"]?.ToString()?.Trim() ?? "",
                        loaiTheCu = r["TEN_LOAI_THE_CU"]?.ToString()?.Trim() ?? "",
                        dloaiTheId = r["DLOAITHEID"]?.ToString()?.Trim() ?? "",
                        dcatapId = r["DCATAPID"]?.ToString()?.Trim() ?? "",
                        tuNgay = r["TUNGAY"] is not DBNull ? Convert.ToDateTime(r["TUNGAY"]).ToString("dd/MM/yyyy") : "",
                        denNgay = r["DENNGAY"] is not DBNull ? Convert.ToDateTime(r["DENNGAY"]).ToString("dd/MM/yyyy") : "",
                        soLan = r["SOLAN"] is not DBNull ? Convert.ToInt32(r["SOLAN"]) : 0,
                        ngayTangThem = r["NGAYTANGTHEM"] is not DBNull ? Convert.ToInt32(r["NGAYTANGTHEM"]) : 0,
                        lanTangThem = r["LANTANGTHEM"] is not DBNull ? Convert.ToInt32(r["LANTANGTHEM"]) : 0,
                        tiLeGiamGia = r["TILEGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TILEGIAMGIA"]) : 0,
                        tienGiamGia = r["TIENGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TIENGIAMGIA"]) : 0,
                        tongCong = r["TONGCONG"] is not DBNull ? Convert.ToDecimal(r["TONGCONG"]) : 0,
                        thanhToan = r["THANHTOAN"] is not DBNull ? Convert.ToDecimal(r["THANHTOAN"]) : 0,
                        refId = r["REFID"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 8. Tab Tăng giảm điểm (TTANGGIAMDIEM)
            var tangGiamDiem = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        tg.ID, tg.NAME AS SO_PHIEU, tg.NGAY, k.NAME AS TEN_KHACH,
                        tg.DIEMTANG, tg.DIEMGIAM, tg.LYDO, tg.NOTE
                    FROM TTANGGIAMDIEM tg
                    LEFT JOIN DKHACHHANG k ON tg.DKHACHHANGID = k.ID
                    WHERE tg.DKHACHHANGID = @id
                    ORDER BY tg.NGAY DESC, tg.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    tangGiamDiem.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        khachHang = r["TEN_KHACH"]?.ToString()?.Trim() ?? "",
                        diemTang = r["DIEMTANG"] is not DBNull ? Convert.ToDecimal(r["DIEMTANG"]) : 0,
                        diemGiam = r["DIEMGIAM"] is not DBNull ? Convert.ToDecimal(r["DIEMGIAM"]) : 0,
                        lyDo = r["LYDO"]?.ToString()?.Trim() ?? "",
                        note = r["NOTE"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 9. Tab Thể trạng (DTHETRANG)
            var theTrang = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        t.ID, t.NGAY, k.NAME AS TEN_KHACH, t.CHIEUCAO, t.CANNANG, t.BMI,
                        t.VONGNGUC, t.VONGBUNG, t.VONGMONG, t.NOTE
                    FROM DTHETRANG t
                    LEFT JOIN DKHACHHANG k ON t.DKHACHHANGID = k.ID
                    WHERE t.DKHACHHANGID = @id
                    ORDER BY t.NGAY DESC, t.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    theTrang.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        khachHang = r["TEN_KHACH"]?.ToString()?.Trim() ?? "",
                        chieuCao = r["CHIEUCAO"] is not DBNull ? Convert.ToDecimal(r["CHIEUCAO"]) : 0,
                        canNang = r["CANNANG"] is not DBNull ? Convert.ToDecimal(r["CANNANG"]) : 0,
                        bmi = r["BMI"] is not DBNull ? Convert.ToDecimal(r["BMI"]) : 0,
                        vongNguc = r["VONGNGUC"] is not DBNull ? Convert.ToDecimal(r["VONGNGUC"]) : 0,
                        vongBung = r["VONGBUNG"] is not DBNull ? Convert.ToDecimal(r["VONGBUNG"]) : 0,
                        vongMong = r["VONGMONG"] is not DBNull ? Convert.ToDecimal(r["VONGMONG"]) : 0,
                        note = r["NOTE"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 10. Tab Phiếu thu (TTHUCHI: THU > 0, LAPHIEUTHUCONGNO = 0, DATCOCID IS NULL)
            var phieuThu = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        tc.ID, tc.NAME AS SO_PHIEU, tc.NGAY, tc.THU, tc.TENDOITUONG,
                        ld.NAME AS TEN_LYDO, tc.DIENGIAI, tc.CHUNGTUGOC, nv.NAME AS TEN_NHANVIEN,
                        tc.CHUYENKHOAN, ch.NAME AS TEN_CUAHANG, tc.NOTE
                    FROM TTHUCHI tc
                    LEFT JOIN DLYDOTHUCHI ld ON tc.DLYDOTHUCHIID = ld.ID
                    LEFT JOIN DNHANVIEN nv ON tc.DNHANVIENID = nv.ID
                    LEFT JOIN DCUAHANG ch ON tc.DCUAHANGID = ch.ID
                    WHERE tc.DKHACHHANGID = @id 
                      AND tc.THU > 0 
                      AND (tc.LAPHIEUTHUCONGNO = 0 OR tc.LAPHIEUTHUCONGNO IS NULL)
                      AND (tc.DATCOCID IS NULL OR tc.DATCOCID = '')
                    ORDER BY tc.NGAY DESC, tc.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    phieuThu.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        thu = r["THU"] is not DBNull ? Convert.ToDecimal(r["THU"]) : 0,
                        tenDoiTuong = r["TENDOITUONG"]?.ToString()?.Trim() ?? "",
                        lyDoThuChi = r["TEN_LYDO"]?.ToString()?.Trim() ?? "",
                        dienGiai = r["DIENGIAI"]?.ToString()?.Trim() ?? "",
                        chungTuGoc = r["CHUNGTUGOC"]?.ToString()?.Trim() ?? "",
                        nhanVien = r["TEN_NHANVIEN"]?.ToString()?.Trim() ?? "",
                        chuyenKhoan = r["CHUYENKHOAN"] is not DBNull && (Convert.ToInt32(r["CHUYENKHOAN"]) == 1 || Convert.ToInt32(r["CHUYENKHOAN"]) == 30),
                        cuaHang = r["TEN_CUAHANG"]?.ToString()?.Trim() ?? "",
                        note = r["NOTE"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 11. Tab Phiếu chi (TTHUCHI: CHI > 0)
            var phieuChi = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        tc.ID, tc.NAME AS SO_PHIEU, tc.NGAY, tc.CHI, tc.TENDOITUONG,
                        ld.NAME AS TEN_LYDO, tc.DIENGIAI, tc.CHUNGTUGOC, nv.NAME AS TEN_NHANVIEN,
                        tc.CHUYENKHOAN, ch.NAME AS TEN_CUAHANG, tc.NOTE
                    FROM TTHUCHI tc
                    LEFT JOIN DLYDOTHUCHI ld ON tc.DLYDOTHUCHIID = ld.ID
                    LEFT JOIN DNHANVIEN nv ON tc.DNHANVIENID = nv.ID
                    LEFT JOIN DCUAHANG ch ON tc.DCUAHANGID = ch.ID
                    WHERE tc.DKHACHHANGID = @id AND tc.CHI > 0
                    ORDER BY tc.NGAY DESC, tc.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    phieuChi.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        chi = r["CHI"] is not DBNull ? Convert.ToDecimal(r["CHI"]) : 0,
                        tenDoiTuong = r["TENDOITUONG"]?.ToString()?.Trim() ?? "",
                        lyDoThuChi = r["TEN_LYDO"]?.ToString()?.Trim() ?? "",
                        dienGiai = r["DIENGIAI"]?.ToString()?.Trim() ?? "",
                        chungTuGoc = r["CHUNGTUGOC"]?.ToString()?.Trim() ?? "",
                        nhanVien = r["TEN_NHANVIEN"]?.ToString()?.Trim() ?? "",
                        chuyenKhoan = r["CHUYENKHOAN"] is not DBNull && (Convert.ToInt32(r["CHUYENKHOAN"]) == 1 || Convert.ToInt32(r["CHUYENKHOAN"]) == 30),
                        cuaHang = r["TEN_CUAHANG"]?.ToString()?.Trim() ?? "",
                        note = r["NOTE"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 12. Tab Phiếu thu công nợ (TTHUCHI: LAPHIEUTHUCONGNO = 1 - KHỚP 100% SCOLUMN VÀ WINFORMS)
            var thuCongNo = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        tc.ID, tc.NOTE, tc.NAME AS SO_PHIEU, tc.NGAY, tc.TENDOITUONG, tc.DIACHI,
                        nv.NAME AS TEN_NHANVIEN, k.NAME AS TEN_KHACH, tc.LOAIDOITUONG, ld.NAME AS TEN_LYDO,
                        tc.DIENGIAI, tc.CHUNGTUGOC, tc.THU, tc.CHI, ncc.NAME AS TEN_NHACUNGCAP,
                        tc.CHUYENKHOAN, tc.TDATHANGID
                    FROM TTHUCHI tc
                    LEFT JOIN DNHANVIEN nv ON tc.DNHANVIENID = nv.ID
                    LEFT JOIN DKHACHHANG k ON tc.DKHACHHANGID = k.ID
                    LEFT JOIN DLYDOTHUCHI ld ON tc.DLYDOTHUCHIID = ld.ID
                    LEFT JOIN DNHACUNGCAP ncc ON tc.DNHACUNGCAPID = ncc.ID
                    WHERE tc.DKHACHHANGID = @id AND tc.LAPHIEUTHUCONGNO = 1
                    ORDER BY tc.NGAY DESC, tc.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    thuCongNo.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        note = r["NOTE"]?.ToString()?.Trim() ?? "",
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        tenDoiTuong = r["TENDOITUONG"]?.ToString()?.Trim() ?? "",
                        diaChi = r["DIACHI"]?.ToString()?.Trim() ?? "",
                        nhanVien = r["TEN_NHANVIEN"]?.ToString()?.Trim() ?? "",
                        khachHang = r["TEN_KHACH"]?.ToString()?.Trim() ?? "",
                        loaiDoiTuong = r["LOAIDOITUONG"]?.ToString()?.Trim() ?? "",
                        lyDoThuChi = r["TEN_LYDO"]?.ToString()?.Trim() ?? "",
                        dienGiai = r["DIENGIAI"]?.ToString()?.Trim() ?? "",
                        chungTuGoc = r["CHUNGTUGOC"]?.ToString()?.Trim() ?? "",
                        thu = r["THU"] is not DBNull ? Convert.ToDecimal(r["THU"]) : 0,
                        chi = r["CHI"] is not DBNull ? Convert.ToDecimal(r["CHI"]) : 0,
                        nhaCungCap = r["TEN_NHACUNGCAP"]?.ToString()?.Trim() ?? "",
                        chuyenKhoan = r["CHUYENKHOAN"] is not DBNull && (Convert.ToInt32(r["CHUYENKHOAN"]) == 1 || Convert.ToInt32(r["CHUYENKHOAN"]) == 30),
                        datCoc = r["TDATHANGID"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 13. Tab Đặt cọc (TTHUCHI: DATCOCID IS NOT NULL AND DATCOCID <> '')
            var datCoc = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        tc.ID, tc.NAME AS SO_PHIEU, tc.NGAY, tc.THU, tc.TENDOITUONG,
                        tc.DIENGIAI, tc.CHUNGTUGOC, nv.NAME AS TEN_NHANVIEN, tc.NOTE
                    FROM TTHUCHI tc
                    LEFT JOIN DNHANVIEN nv ON tc.DNHANVIENID = nv.ID
                    WHERE tc.DKHACHHANGID = @id AND tc.DATCOCID IS NOT NULL AND tc.DATCOCID <> ''
                    ORDER BY tc.NGAY DESC, tc.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    datCoc.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        thu = r["THU"] is not DBNull ? Convert.ToDecimal(r["THU"]) : 0,
                        tenDoiTuong = r["TENDOITUONG"]?.ToString()?.Trim() ?? "",
                        dienGiai = r["DIENGIAI"]?.ToString()?.Trim() ?? "",
                        chungTuGoc = r["CHUNGTUGOC"]?.ToString()?.Trim() ?? "",
                        nhanVien = r["TEN_NHANVIEN"]?.ToString()?.Trim() ?? "",
                        note = r["NOTE"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 14. Tab Vào ra (TVAORA)
            var vaoRa = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        v.ID, v.NAME AS MA_THE, v.NGAY, v.GIO, k.NAME AS TEN_KHACH,
                        m.NAME AS TEN_MAY, g.NAME AS SO_PHIEU_GIAHAN, v.NOTE
                    FROM TVAORA v
                    LEFT JOIN DKHACHHANG k ON v.DKHACHHANGID = k.ID
                    LEFT JOIN DMAYVANTAY m ON v.DMAYVANTAYID = m.ID
                    LEFT JOIN TGIAHANTHE g ON v.TGIAHANTHEID = g.ID
                    WHERE v.DKHACHHANGID = @id
                    ORDER BY v.NGAY DESC, v.GIO DESC
                    ROWS 1 TO 100";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    vaoRa.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        maThe = r["MA_THE"]?.ToString()?.Trim() ?? "",
                        ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                        gio = r["GIO"] is not DBNull ? Convert.ToDateTime(r["GIO"]).ToString("HH:mm:ss") : "",
                        khachHang = r["TEN_KHACH"]?.ToString()?.Trim() ?? "",
                        may = r["TEN_MAY"]?.ToString()?.Trim() ?? "Máy quẹt vân tay",
                        giaHanThe = r["SO_PHIEU_GIAHAN"]?.ToString()?.Trim() ?? "",
                        note = r["NOTE"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    thongTin = auditInfo,
                    baoGia,
                    donHang,
                    datHang,
                    giaHanThe,
                    baoLuuThe,
                    doiLoaiThe,
                    tangGiamDiem,
                    theTrang,
                    phieuThu,
                    phieuChi,
                    phieuThuCongNo = thuCongNo,
                    datCoc,
                    vaoRa
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy subtabs: " + ex.Message });
        }
    }

    /// <summary>
    /// Thêm mới bản ghi vào subtab tương ứng
    /// </summary>
    [HttpPost("subtabs/{tabId}")]
    public IActionResult CreateSubtabItem(string tabId, [FromBody] System.Text.Json.JsonElement body)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            string newId = Guid.NewGuid().ToString();
            string khId = body.TryGetProperty("khachHangId", out var pKh) ? pKh.GetString() ?? "" : "";
            string soPhiu = body.TryGetProperty("soPhiu", out var pSp) ? pSp.GetString() ?? "" : "";
            string note = body.TryGetProperty("note", out var pNote) ? pNote.GetString() ?? "" : "";

            DateTime ngayChungTu = DateTime.Now;
            if (body.TryGetProperty("ngay", out var pNg) && !string.IsNullOrWhiteSpace(pNg.GetString()) && DateTime.TryParse(pNg.GetString(), out var parsedNgay))
            {
                ngayChungTu = parsedNgay;
            }

            if (string.IsNullOrWhiteSpace(soPhiu))
            {
                string norm = tabId.Trim().ToLower().Replace("_", "").Replace("-", "");
                var def = SlipDefinitions.FirstOrDefault(d => 
                    string.Equals(d.Key, norm, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.TableName, norm, StringComparison.OrdinalIgnoreCase));
                if (def != null)
                {
                    string template = "";
                    using (var tCmd = conn.CreateCommand())
                    {
                        tCmd.CommandText = $"SELECT NOTEMPLATE FROM {def.Source} WHERE ID = @id";
                        tCmd.Parameters.AddWithValue("@id", def.SourceId);
                        var tplObj = tCmd.ExecuteScalar();
                        if (tplObj != null && tplObj != DBNull.Value)
                            template = tplObj.ToString()?.Trim() ?? "";
                    }
                    soPhiu = GenerateSlipNumberCore(conn, template, def.TableName, def.ColumnName);
                }
            }

            using var cmd = conn.CreateCommand();

            switch (tabId.ToLower())
            {
                case "dathang":
                    cmd.CommandText = @"
                        INSERT INTO TDATHANG (ID, NAME, NGAY, DKHACHHANGID, TENKHACH, DIACHI, DIENTHOAI, EMAIL, TIENHANG, TILEGIAMGIA, TIENGIAMGIA, TILETHUE, TIENTHUE, PHIVANCHUYEN, TONGCONG, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, @tenKhach, @diaChi, @dienThoai, @email, @tienHang, @tiLeGiam, @tienGiam, @tiLeThue, @tienThue, @phiVc, @tongCong, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@tenKhach", body.TryGetProperty("tenKhach", out var pDhTk) ? (object)pDhTk.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pDhDc) ? (object)pDhDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienThoai", body.TryGetProperty("dienThoai", out var pDhDt) ? (object)pDhDt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", body.TryGetProperty("email", out var pDhEm) ? (object)pDhEm.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@tienHang", body.TryGetProperty("tienHang", out var pDhTh) ? pDhTh.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeGiam", body.TryGetProperty("tiLeGiamGia", out var pDhTlg) ? pDhTlg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiamGia", out var pDhTg) ? pDhTg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeThue", body.TryGetProperty("tiLeThue", out var pDhTlth) ? pDhTlth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienThue", body.TryGetProperty("tienThue", out var pDhTth) ? pDhTth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@phiVc", body.TryGetProperty("phiVanChuyen", out var pDhPvc) ? pDhPvc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tongCong", body.TryGetProperty("tongCong", out var pDhTc) ? pDhTc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "baogia":
                    cmd.CommandText = @"
                        INSERT INTO TBAOGIA (ID, NAME, NGAY, DKHACHHANGID, TENKHACH, DIACHI, DIENTHOAI, EMAIL, TIENHANG, TILEGIAMGIA, TIENGIAMGIA, TILETHUE, TIENTHUE, PHIVANCHUYEN, TONGCONG, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, @tenKhach, @diaChi, @dienThoai, @email, @tienHang, @tiLeGiam, @tienGiam, @tiLeThue, @tienThue, @phiVc, @tongCong, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@tenKhach", body.TryGetProperty("tenKhach", out var pBgTk) ? (object)pBgTk.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pBgDc) ? (object)pBgDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienThoai", body.TryGetProperty("dienThoai", out var pBgDt) ? (object)pBgDt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", body.TryGetProperty("email", out var pBgEm) ? (object)pBgEm.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@tienHang", body.TryGetProperty("tienHang", out var pBth) ? pBth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeGiam", body.TryGetProperty("tiLeGiamGia", out var pBgTlg) ? pBgTlg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiamGia", out var pBgTg) ? pBgTg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeThue", body.TryGetProperty("tiLeThue", out var pBgTlth) ? pBgTlth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienThue", body.TryGetProperty("tienThue", out var pBgTth) ? pBgTth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@phiVc", body.TryGetProperty("phiVanChuyen", out var pBgPvc) ? pBgPvc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tongCong", body.TryGetProperty("tongCong", out var pBtc) ? pBtc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "donhang":
                    cmd.CommandText = @"
                        INSERT INTO TDONHANG (ID, NAME, NGAY, DKHACHHANGID, TIENHANG, TILEGIAMGIA, TIENGIAMGIA, TILETHUE, TIENTHUE, PHIVANCHUYEN, TONGCONG, THANHTOAN, DKHOXUATID, DNHANVIENXUATID, DIENGIAI, GIAOHANG, DOITRA, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, @tienHang, @tiLeGiam, @tienGiam, @tiLeThue, @tienThue, @phiVc, @tongCong, @thanhToan, @khoId, @nvXuatId, @dienGiai, @giaoHang, @doiTra, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@tienHang", body.TryGetProperty("tienHang", out var pDth) ? pDth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeGiam", body.TryGetProperty("tiLeGiamGia", out var pDtlg) ? pDtlg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiamGia", out var pDtg) ? pDtg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeThue", body.TryGetProperty("tiLeThue", out var pDtlth) ? pDtlth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienThue", body.TryGetProperty("tienThue", out var pDtth) ? pDtth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@phiVc", body.TryGetProperty("phiVanChuyen", out var pDpvc) ? pDpvc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tongCong", body.TryGetProperty("tongCong", out var pDtc) ? pDtc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@thanhToan", body.TryGetProperty("thanhToan", out var pDtt) ? pDtt.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@khoId", body.TryGetProperty("dkhoXuatId", out var pDkx) ? (object)pDkx.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@nvXuatId", body.TryGetProperty("dnhanVienXuatId", out var pDnvx) ? (object)pDnvx.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienGiai", body.TryGetProperty("dienGiai", out var pDdg) ? (object)pDdg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@giaoHang", body.TryGetProperty("giaoHang", out var pDgh) ? (object)pDgh.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@doiTra", body.TryGetProperty("doiTra", out var pDdt) ? pDdt.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "giahanthe":
                    cmd.CommandText = @"
                        INSERT INTO TGIAHANTHE (ID, NAME, NGAY, DKHACHHANGID, DLOAITHEID, DCATAPID, SOTIEN, TILEGIAMGIA, TIENGIAMGIA, TONGCONG, THANHTOAN, SOLAN, SOTHANG, SONGAY, NGAYTANGTHEM, LANTANGTHEM, KHUYENMAI, TUNGAY, DENNGAY, DLOAIGIAODICHID, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, @loaiTheId, @catapId, @soTien, @tiLeGiam, @tienGiam, @tongCong, @thanhToan, @soLan, @soThang, @soNgay, @ngayTang, @lanTang, @khuyenMai, @tuNgay, @denNgay, '1', @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@loaiTheId", body.TryGetProperty("dloaiTheId", out var pLt) ? (object)pLt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@catapId", body.TryGetProperty("dcatapId", out var pCt) ? (object)pCt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@soTien", body.TryGetProperty("soTien", out var pSt) ? pSt.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeGiam", body.TryGetProperty("tiLeGiamGia", out var pGtl) ? pGtl.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiamGia", out var pGtg) ? pGtg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tongCong", body.TryGetProperty("tongCong", out var pGtc) ? pGtc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@thanhToan", body.TryGetProperty("thanhToan", out var pGtt) ? pGtt.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@soLan", body.TryGetProperty("soLan", out var pSl) ? pSl.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@soThang", body.TryGetProperty("soThang", out var pSth) ? pSth.GetInt32() : 1);
                    cmd.Parameters.AddWithValue("@soNgay", body.TryGetProperty("soNgay", out var pSng) ? pSng.GetInt32() : 30);
                    cmd.Parameters.AddWithValue("@ngayTang", body.TryGetProperty("ngayTangThem", out var pNt) ? pNt.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@lanTang", body.TryGetProperty("lanTangThem", out var pLtng) ? pLtng.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@khuyenMai", body.TryGetProperty("khuyenMai", out var pKm) ? (object)pKm.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@tuNgay", body.TryGetProperty("tuNgay", out var pTn) && DateTime.TryParse(pTn.GetString(), out var dTn) ? dTn : DateTime.Now);
                    cmd.Parameters.AddWithValue("@denNgay", body.TryGetProperty("denNgay", out var pDn) && DateTime.TryParse(pDn.GetString(), out var dDn) ? dDn : DateTime.Now.AddMonths(1));
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "baoluuthe":
                    cmd.CommandText = @"
                        INSERT INTO TGIAHANTHE (ID, NAME, NGAY, DKHACHHANGID, DLOAITHEID, TUNGAY, DENNGAY, SONGAY, SOTIEN, DLOAIGIAODICHID, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, @loaiTheId, @tuNgay, @denNgay, @soNgay, @soTien, '9', @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@loaiTheId", body.TryGetProperty("dloaiTheId", out var pBllt) ? (object)pBllt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@tuNgay", body.TryGetProperty("tuNgay", out var pBltn) && DateTime.TryParse(pBltn.GetString(), out var dBltn) ? dBltn : DateTime.Now);
                    cmd.Parameters.AddWithValue("@denNgay", body.TryGetProperty("denNgay", out var pBldn) && DateTime.TryParse(pBldn.GetString(), out var dBldn) ? dBldn : DateTime.Now.AddDays(30));
                    cmd.Parameters.AddWithValue("@soNgay", body.TryGetProperty("soNgay", out var pSn) ? pSn.GetInt32() : 30);
                    cmd.Parameters.AddWithValue("@soTien", body.TryGetProperty("soTien", out var pBlst) ? pBlst.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "doiloaithe":
                    cmd.CommandText = @"
                        INSERT INTO TGIAHANTHE (ID, NAME, NGAY, DKHACHHANGID, DLOAITHEID, DCATAPID, TUNGAY, DENNGAY, SOLAN, SOTHANG, SONGAY, NGAYTANGTHEM, LANTANGTHEM, SOTIEN, TILEGIAMGIA, TIENGIAMGIA, TONGCONG, THANHTOAN, REFID, DLOAIGIAODICHID, LOAI, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, @loaiTheId, @catapId, @tuNgay, @denNgay, @soLan, @soThang, @soNgay, @ngayTang, @lanTang, @soTien, @tiLeGiam, @tienGiam, @tongCong, @thanhToan, @refId, '2', 2, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    string doiLoaiTheId = body.TryGetProperty("dloaiTheId", out var pDlt) ? pDlt.GetString() : (body.TryGetProperty("dloaiTheIdMoi", out var pDltM) ? pDltM.GetString() : null);
                    cmd.Parameters.AddWithValue("@loaiTheId", (object)doiLoaiTheId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@catapId", body.TryGetProperty("dcatapId", out var pDct) ? (object)pDct.GetString() : DBNull.Value);
                    DateTime doiTuNgay = body.TryGetProperty("tuNgay", out var pDtn) && DateTime.TryParse(pDtn.GetString(), out var dDtn) ? dDtn : DateTime.Now;
                    DateTime doiDenNgay = body.TryGetProperty("denNgay", out var pDdn) && DateTime.TryParse(pDdn.GetString(), out var dDdn) ? dDdn : DateTime.Now.AddMonths(1);
                    cmd.Parameters.AddWithValue("@tuNgay", doiTuNgay);
                    cmd.Parameters.AddWithValue("@denNgay", doiDenNgay);
                    cmd.Parameters.AddWithValue("@soLan", body.TryGetProperty("soLan", out var pDsl) ? pDsl.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@soThang", body.TryGetProperty("soThang", out var pDsth) ? pDsth.GetInt32() : 1);
                    cmd.Parameters.AddWithValue("@soNgay", body.TryGetProperty("soNgay", out var pDsng) ? pDsng.GetInt32() : 30);
                    cmd.Parameters.AddWithValue("@ngayTang", body.TryGetProperty("ngayTangThem", out var pDnt) ? pDnt.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@lanTang", body.TryGetProperty("lanTangThem", out var pDltng) ? pDltng.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@soTien", body.TryGetProperty("soTien", out var pDlst) ? pDlst.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeGiam", body.TryGetProperty("tiLeGiamGia", out var pDgtl) ? pDgtl.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiamGia", out var pDgtg) ? pDgtg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tongCong", body.TryGetProperty("tongCong", out var pDgtc) ? pDgtc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@thanhToan", body.TryGetProperty("thanhToan", out var pDgtt) ? pDgtt.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@refId", body.TryGetProperty("refId", out var pDref) ? (object)pDref.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "tanggiamdiem":
                    cmd.CommandText = @"
                        INSERT INTO TTANGGIAMDIEM (ID, NAME, NGAY, DKHACHHANGID, DIEMTANG, DIEMGIAM, LYDO, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, @diemTang, @diemGiam, @lyDo, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@diemTang", body.TryGetProperty("diemTang", out var pDt) ? pDt.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@diemGiam", body.TryGetProperty("diemGiam", out var pDg) ? pDg.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@lyDo", body.TryGetProperty("lyDo", out var pLd) ? (object)pLd.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "thetrang":
                    cmd.CommandText = @"
                        INSERT INTO DTHETRANG (ID, NGAY, DKHACHHANGID, CHIEUCAO, CANNANG, BMI, VONGNGUC, VONGBUNG, VONGMONG, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @ngay, @khId, @chieuCao, @canNang, @bmi, @vongNguc, @vongBung, @vongMong, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@chieuCao", body.TryGetProperty("chieuCao", out var pCc) ? pCc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@canNang", body.TryGetProperty("canNang", out var pCn) ? pCn.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@bmi", body.TryGetProperty("bmi", out var pBmi) ? pBmi.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@vongNguc", body.TryGetProperty("vongNguc", out var pVn) ? pVn.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@vongBung", body.TryGetProperty("vongBung", out var pVb) ? pVb.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@vongMong", body.TryGetProperty("vongMong", out var pVm) ? pVm.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "phieuthu":
                    cmd.CommandText = @"
                        INSERT INTO TTHUCHI (ID, NAME, NGAY, DKHACHHANGID, LOAI, LOAIDOITUONG, TENDOITUONG, DIACHI, DNHANVIENID, DLYDOTHUCHIID, DIENGIAI, CHUNGTUGOC, THU, CHI, CHUYENKHOAN, DCUAHANGID, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, 0, @loaiDoiTuong, @tenDoiTuong, @diaChi, @nhanVienId, @lyDoId, @dienGiai, @chungTuGoc, @thu, 0, @chuyenKhoan, @cuaHangId, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@loaiDoiTuong", body.TryGetProperty("loaiDoiTuong", out var pLdt) ? pLdt.GetInt32() : 2);
                    cmd.Parameters.AddWithValue("@tenDoiTuong", body.TryGetProperty("tenDoiTuong", out var pTdt) ? (object)pTdt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pDc) ? (object)pDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@nhanVienId", body.TryGetProperty("dnhanVienId", out var pNvid) ? (object)pNvid.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@lyDoId", body.TryGetProperty("dlyDoThuChiId", out var pLdid) ? (object)pLdid.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienGiai", body.TryGetProperty("dienGiai", out var pPtDg) ? (object)pPtDg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@chungTuGoc", body.TryGetProperty("chungTuGoc", out var pPtCtg) ? (object)pPtCtg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@thu", body.TryGetProperty("thu", out var pPtThu) ? pPtThu.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@chuyenKhoan", body.TryGetProperty("chuyenKhoan", out var pPtCk) && pPtCk.GetBoolean() ? 1 : 0);
                    cmd.Parameters.AddWithValue("@cuaHangId", body.TryGetProperty("dcuaHangId", out var pChid) ? (object)pChid.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "phieuchi":
                    cmd.CommandText = @"
                        INSERT INTO TTHUCHI (ID, NAME, NGAY, DKHACHHANGID, LOAI, LOAIDOITUONG, TENDOITUONG, DIACHI, DNHANVIENID, DLYDOTHUCHIID, DIENGIAI, CHUNGTUGOC, THU, CHI, CHUYENKHOAN, DCUAHANGID, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, 1, @loaiDoiTuong, @tenDoiTuong, @diaChi, @nhanVienId, @lyDoId, @dienGiai, @chungTuGoc, 0, @chi, @chuyenKhoan, @cuaHangId, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@loaiDoiTuong", body.TryGetProperty("loaiDoiTuong", out var pLdtC) ? pLdtC.GetInt32() : 2);
                    cmd.Parameters.AddWithValue("@tenDoiTuong", body.TryGetProperty("tenDoiTuong", out var pTdtC) ? (object)pTdtC.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pDcC) ? (object)pDcC.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@nhanVienId", body.TryGetProperty("dnhanVienId", out var pNvidC) ? (object)pNvidC.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@lyDoId", body.TryGetProperty("dlyDoThuChiId", out var pLdidC) ? (object)pLdidC.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienGiai", body.TryGetProperty("dienGiai", out var pPcDg) ? (object)pPcDg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@chungTuGoc", body.TryGetProperty("chungTuGoc", out var pPcCtg) ? (object)pPcCtg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@chi", body.TryGetProperty("chi", out var pPcChi) ? pPcChi.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@chuyenKhoan", body.TryGetProperty("chuyenKhoan", out var pPcCk) && pPcCk.GetBoolean() ? 1 : 0);
                    cmd.Parameters.AddWithValue("@cuaHangId", body.TryGetProperty("dcuaHangId", out var pChidC) ? (object)pChidC.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "datcoc":
                    cmd.CommandText = @"
                        INSERT INTO TTHUCHI (ID, NAME, NGAY, DKHACHHANGID, TENDOITUONG, DIENTHOAI, DIACHI, DLOAITHEID, GIATRIGOI, GIAMGIA, TIENGIAM, THU, CHI, DLYDOTHUCHIID, CHUYENKHOAN, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, @tenDoiTuong, @dienThoai, @diaChi, @loaiTheId, @giaTriGoi, @giamGia, @tienGiam, @thu, 0, @lyDoId, @chuyenKhoan, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@tenDoiTuong", body.TryGetProperty("tenDoiTuong", out var pDcTdt) ? (object)pDcTdt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienThoai", body.TryGetProperty("dienThoai", out var pDcDt) ? (object)pDcDt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pDcDc) ? (object)pDcDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@loaiTheId", body.TryGetProperty("dloaiTheId", out var pDcLt) ? (object)pDcLt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@giaTriGoi", body.TryGetProperty("giaTriGoi", out var pDcg) ? pDcg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@giamGia", body.TryGetProperty("giamGia", out var pDcGg) ? pDcGg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiam", out var pDcTg) ? pDcTg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@thu", body.TryGetProperty("thu", out var pDcThu) ? pDcThu.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@lyDoId", body.TryGetProperty("dlyDoThuChiId", out var pDcLyDo) ? (object)pDcLyDo.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@chuyenKhoan", body.TryGetProperty("chuyenKhoan", out var pDcCk) && pDcCk.GetBoolean() ? 1 : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "thucongno":
                    cmd.CommandText = @"
                        INSERT INTO TTHUCHI (ID, NAME, NGAY, DKHACHHANGID, LOAI, LAPHIEUTHUCONGNO, TENDOITUONG, DIACHI, DNHANVIENID, DLYDOTHUCHIID, DIENGIAI, CHUNGTUGOC, THU, CHI, CHUYENKHOAN, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @khId, 0, 1, @tenDoiTuong, @diaChi, @nhanVienId, @lyDoId, @dienGiai, @chungTuGoc, @thu, 0, @chuyenKhoan, @note, 30, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@tenDoiTuong", body.TryGetProperty("tenDoiTuong", out var pCnTdt) ? (object)pCnTdt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pCnDc) ? (object)pCnDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@nhanVienId", body.TryGetProperty("dnhanVienId", out var pCnNv) ? (object)pCnNv.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@lyDoId", body.TryGetProperty("dlyDoThuChiId", out var pCnLyDo) ? (object)pCnLyDo.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienGiai", body.TryGetProperty("dienGiai", out var pCnDg) ? (object)pCnDg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@chungTuGoc", body.TryGetProperty("chungTuGoc", out var pCnCtg) ? (object)pCnCtg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@thu", body.TryGetProperty("thu", out var pCnThu) ? pCnThu.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@chuyenKhoan", body.TryGetProperty("chuyenKhoan", out var pCnCk) && pCnCk.GetBoolean() ? 1 : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                case "vaora":
                    cmd.CommandText = @"
                        INSERT INTO TVAORA (ID, NAME, NGAY, GIO, DKHACHHANGID, DMAYVANTAYID, NOTE, STATUS, TIMECREATED, USERCREATEDID)
                        VALUES (@id, @name, @ngay, @gio, @khId, @mayVanTayId, @note, 1, @timeCreated, @userCreated)";
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", soPhiu);
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@gio", body.TryGetProperty("gio", out var pVrG) ? (object)pVrG.GetString() : DateTime.Now.ToString("HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@khId", khId);
                    cmd.Parameters.AddWithValue("@mayVanTayId", body.TryGetProperty("dmayVanTayId", out var pMvt) ? (object)pMvt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@timeCreated", DateTime.Now);
                    cmd.Parameters.AddWithValue("@userCreated", _adminUserId);
                    break;

                default:
                    return BadRequest(new { success = false, message = $"Subtab '{tabId}' không được hỗ trợ thêm trực tiếp." });
            }

            cmd.ExecuteNonQuery();

            return Ok(new { success = true, id = newId, message = $"Đã thêm thành công vào tab {tabId}!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi thêm bản ghi subtab: " + ex.Message });
        }
    }

    /// <summary>
    /// Chỉnh sửa bản ghi trong subtab
    /// </summary>
    [HttpPut("subtabs/{tabId}/{itemId}")]
    public IActionResult UpdateSubtabItem(string tabId, string itemId, [FromBody] JsonElement body)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            using var cmd = conn.CreateCommand();
            string note = body.TryGetProperty("note", out var pNote) ? pNote.GetString() ?? "" : "";
            DateTime ngayChungTu = body.TryGetProperty("ngay", out var pNgay) && DateTime.TryParse(pNgay.GetString(), out var dNgay) ? dNgay : DateTime.Now;

            switch (tabId.ToLower())
            {
                case "giahanthe":
                    cmd.CommandText = @"
                        UPDATE TGIAHANTHE SET 
                            NGAY = @ngay, DLOAITHEID = @loaiTheId, DCATAPID = @catapId,
                            SOTIEN = @soTien, TILEGIAMGIA = @tiLeGiam, TIENGIAMGIA = @tienGiam,
                            TONGCONG = @tongCong, THANHTOAN = @thanhToan, SOLAN = @soLan,
                            SOTHANG = @soThang, SONGAY = @soNgay, NGAYTANGTHEM = @ngayTang,
                            LANTANGTHEM = @lanTang, KHUYENMAI = @khuyenMai, TUNGAY = @tuNgay,
                            DENNGAY = @denNgay, NOTE = @note, TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@loaiTheId", body.TryGetProperty("dloaiTheId", out var pLt) ? (object)pLt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@catapId", body.TryGetProperty("dcatapId", out var pCt) ? (object)pCt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@soTien", body.TryGetProperty("soTien", out var pSt) ? pSt.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeGiam", body.TryGetProperty("tiLeGiamGia", out var pGtl) ? pGtl.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiamGia", out var pGtg) ? pGtg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tongCong", body.TryGetProperty("tongCong", out var pGtc) ? pGtc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@thanhToan", body.TryGetProperty("thanhToan", out var pGtt) ? pGtt.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@soLan", body.TryGetProperty("soLan", out var pSl) ? pSl.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@soThang", body.TryGetProperty("soThang", out var pSth) ? pSth.GetInt32() : 1);
                    cmd.Parameters.AddWithValue("@soNgay", body.TryGetProperty("soNgay", out var pSng) ? pSng.GetInt32() : 30);
                    cmd.Parameters.AddWithValue("@ngayTang", body.TryGetProperty("ngayTangThem", out var pNt) ? pNt.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@lanTang", body.TryGetProperty("lanTangThem", out var pLtng) ? pLtng.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@khuyenMai", body.TryGetProperty("khuyenMai", out var pKm) ? (object)pKm.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@tuNgay", body.TryGetProperty("tuNgay", out var pTn) && DateTime.TryParse(pTn.GetString(), out var dTn) ? dTn : DateTime.Now);
                    cmd.Parameters.AddWithValue("@denNgay", body.TryGetProperty("denNgay", out var pDn) && DateTime.TryParse(pDn.GetString(), out var dDn) ? dDn : DateTime.Now.AddMonths(1));
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                case "baoluuthe":
                    cmd.CommandText = @"
                        UPDATE TGIAHANTHE SET 
                            NGAY = @ngay, DLOAITHEID = @loaiTheId, TUNGAY = @tuNgay, DENNGAY = @denNgay,
                            SONGAY = @soNgay, SOTIEN = @soTien, NOTE = @note, TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@loaiTheId", body.TryGetProperty("dloaiTheId", out var pBllt) ? (object)pBllt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@tuNgay", body.TryGetProperty("tuNgay", out var pBltn) && DateTime.TryParse(pBltn.GetString(), out var dBltn) ? dBltn : DateTime.Now);
                    cmd.Parameters.AddWithValue("@denNgay", body.TryGetProperty("denNgay", out var pBldn) && DateTime.TryParse(pBldn.GetString(), out var dBldn) ? dBldn : DateTime.Now.AddDays(30));
                    cmd.Parameters.AddWithValue("@soNgay", body.TryGetProperty("soNgay", out var pSn) ? pSn.GetInt32() : 30);
                    cmd.Parameters.AddWithValue("@soTien", body.TryGetProperty("soTien", out var pBlst) ? pBlst.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                case "doiloaithe":
                    cmd.CommandText = @"
                        UPDATE TGIAHANTHE SET 
                            NGAY = @ngay, DLOAITHEID = @loaiTheId, DCATAPID = @catapId, TUNGAY = @tuNgay, DENNGAY = @denNgay,
                            SOLAN = @soLan, SOTHANG = @soThang, SONGAY = @soNgay, NGAYTANGTHEM = @ngayTang, LANTANGTHEM = @lanTang,
                            SOTIEN = @soTien, TILEGIAMGIA = @tiLeGiam, TIENGIAMGIA = @tienGiam, TONGCONG = @tongCong, THANHTOAN = @thanhToan,
                            REFID = @refId, NOTE = @note, TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@loaiTheId", body.TryGetProperty("dloaiTheId", out var pDlt) ? (object)pDlt.GetString() : (body.TryGetProperty("dloaiTheIdMoi", out var pDltM) ? (object)pDltM.GetString() : DBNull.Value));
                    cmd.Parameters.AddWithValue("@catapId", body.TryGetProperty("dcatapId", out var pDct) ? (object)pDct.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@tuNgay", body.TryGetProperty("tuNgay", out var pDtn) && DateTime.TryParse(pDtn.GetString(), out var dDtn) ? dDtn : DateTime.Now);
                    cmd.Parameters.AddWithValue("@denNgay", body.TryGetProperty("denNgay", out var pDdn) && DateTime.TryParse(pDdn.GetString(), out var dDdn) ? dDdn : DateTime.Now.AddMonths(1));
                    cmd.Parameters.AddWithValue("@soLan", body.TryGetProperty("soLan", out var pDsl) ? pDsl.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@soThang", body.TryGetProperty("soThang", out var pDsth) ? pDsth.GetInt32() : 1);
                    cmd.Parameters.AddWithValue("@soNgay", body.TryGetProperty("soNgay", out var pDsng) ? pDsng.GetInt32() : 30);
                    cmd.Parameters.AddWithValue("@ngayTang", body.TryGetProperty("ngayTangThem", out var pDnt) ? pDnt.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@lanTang", body.TryGetProperty("lanTangThem", out var pDltng) ? pDltng.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@soTien", body.TryGetProperty("soTien", out var pDlst) ? pDlst.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeGiam", body.TryGetProperty("tiLeGiamGia", out var pDgtl) ? pDgtl.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiamGia", out var pDgtg) ? pDgtg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tongCong", body.TryGetProperty("tongCong", out var pDgtc) ? pDgtc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@thanhToan", body.TryGetProperty("thanhToan", out var pDgtt) ? pDgtt.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@refId", body.TryGetProperty("refId", out var pDref) ? (object)pDref.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                case "baogia":
                    cmd.CommandText = @"
                        UPDATE TBAOGIA SET 
                            NGAY = @ngay, TENKHACH = @tenKhach, DIACHI = @diaChi, DIENTHOAI = @dienThoai, EMAIL = @email,
                            TIENHANG = @tienHang, TILEGIAMGIA = @tiLeGiam, TIENGIAMGIA = @tienGiam, TILETHUE = @tiLeThue,
                            TIENTHUE = @tienThue, PHIVANCHUYEN = @phiVc, TONGCONG = @tongCong, NOTE = @note,
                            TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@tenKhach", body.TryGetProperty("tenKhach", out var pBgTk) ? (object)pBgTk.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pBgDc) ? (object)pBgDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienThoai", body.TryGetProperty("dienThoai", out var pBgDt) ? (object)pBgDt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", body.TryGetProperty("email", out var pBgEm) ? (object)pBgEm.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@tienHang", body.TryGetProperty("tienHang", out var pBth) ? pBth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeGiam", body.TryGetProperty("tiLeGiamGia", out var pBgTlg) ? pBgTlg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiamGia", out var pBgTg) ? pBgTg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeThue", body.TryGetProperty("tiLeThue", out var pBgTlth) ? pBgTlth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienThue", body.TryGetProperty("tienThue", out var pBgTth) ? pBgTth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@phiVc", body.TryGetProperty("phiVanChuyen", out var pBgPvc) ? pBgPvc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tongCong", body.TryGetProperty("tongCong", out var pBtc) ? pBtc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                case "dathang":
                    cmd.CommandText = @"
                        UPDATE TDATHANG SET 
                            NGAY = @ngay, TENKHACH = @tenKhach, DIACHI = @diaChi, DIENTHOAI = @dienThoai, EMAIL = @email,
                            TIENHANG = @tienHang, TILEGIAMGIA = @tiLeGiam, TIENGIAMGIA = @tienGiam, TILETHUE = @tiLeThue,
                            TIENTHUE = @tienThue, PHIVANCHUYEN = @phiVc, TONGCONG = @tongCong, NOTE = @note,
                            TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@tenKhach", body.TryGetProperty("tenKhach", out var pDhTk) ? (object)pDhTk.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pDhDc) ? (object)pDhDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienThoai", body.TryGetProperty("dienThoai", out var pDhDt) ? (object)pDhDt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", body.TryGetProperty("email", out var pDhEm) ? (object)pDhEm.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@tienHang", body.TryGetProperty("tienHang", out var pDth) ? pDth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeGiam", body.TryGetProperty("tiLeGiamGia", out var pDhTlg) ? pDhTlg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiamGia", out var pDhTg) ? pDhTg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tiLeThue", body.TryGetProperty("tiLeThue", out var pDhTlth) ? pDhTlth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienThue", body.TryGetProperty("tienThue", out var pDhTth) ? pDhTth.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@phiVc", body.TryGetProperty("phiVanChuyen", out var pDhPvc) ? pDhPvc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tongCong", body.TryGetProperty("tongCong", out var pDtc) ? pDtc.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                case "tanggiamdiem":
                    cmd.CommandText = @"
                        UPDATE TTANGGIAMDIEM SET 
                            NGAY = @ngay, DIEMTANG = @diemTang, DIEMGIAM = @diemGiam, LYDO = @lyDo, NOTE = @note,
                            TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@diemTang", body.TryGetProperty("diemTang", out var pDt) ? pDt.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@diemGiam", body.TryGetProperty("diemGiam", out var pDg) ? pDg.GetInt32() : 0);
                    cmd.Parameters.AddWithValue("@lyDo", body.TryGetProperty("lyDo", out var pLd) ? (object)pLd.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                case "vaora":
                    cmd.CommandText = @"
                        UPDATE TVAORA SET 
                            NGAY = @ngay, GIO = @gio, DMAYVANTAYID = @mayVanTayId, NOTE = @note,
                            TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@gio", body.TryGetProperty("gio", out var pVrG) ? (object)pVrG.GetString() : DateTime.Now.ToString("HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@mayVanTayId", body.TryGetProperty("dmayVanTayId", out var pMvt) ? (object)pMvt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                case "phieuthu":
                case "phieuchi":
                    int loaiThuChi = tabId.ToLower() == "phieuthu" ? 0 : 1;
                    cmd.CommandText = @"
                        UPDATE TTHUCHI SET 
                            NGAY = @ngay, LOAIDOITUONG = @loaiDoiTuong, TENDOITUONG = @tenDoiTuong, DIACHI = @diaChi,
                            DNHANVIENID = @nhanVienId, DLYDOTHUCHIID = @lyDoId, DIENGIAI = @dienGiai, CHUNGTUGOC = @chungTuGoc,
                            THU = @thu, CHI = @chi, CHUYENKHOAN = @chuyenKhoan, DCUAHANGID = @cuaHangId, NOTE = @note,
                            TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@loaiDoiTuong", body.TryGetProperty("loaiDoiTuong", out var pLdt) ? pLdt.GetInt32() : 2);
                    cmd.Parameters.AddWithValue("@tenDoiTuong", body.TryGetProperty("tenDoiTuong", out var pTdt) ? (object)pTdt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pDc) ? (object)pDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@nhanVienId", body.TryGetProperty("dnhanVienId", out var pNvid) ? (object)pNvid.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@lyDoId", body.TryGetProperty("dlyDoThuChiId", out var pLdid) ? (object)pLdid.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienGiai", body.TryGetProperty("dienGiai", out var pTcDg) ? (object)pTcDg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@chungTuGoc", body.TryGetProperty("chungTuGoc", out var pCtg) ? (object)pCtg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@thu", loaiThuChi == 0 ? (body.TryGetProperty("thu", out var pThu) ? pThu.GetDecimal() : 0) : 0);
                    cmd.Parameters.AddWithValue("@chi", loaiThuChi == 1 ? (body.TryGetProperty("chi", out var pChi) ? pChi.GetDecimal() : 0) : 0);
                    cmd.Parameters.AddWithValue("@chuyenKhoan", body.TryGetProperty("chuyenKhoan", out var pCk) && pCk.GetBoolean() ? 1 : 0);
                    cmd.Parameters.AddWithValue("@cuaHangId", body.TryGetProperty("dcuaHangId", out var pChid) ? (object)pChid.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                case "datcoc":
                    cmd.CommandText = @"
                        UPDATE TTHUCHI SET 
                            NGAY = @ngay, TENDOITUONG = @tenDoiTuong, DIENTHOAI = @dienThoai, DIACHI = @diaChi,
                            DLOAITHEID = @loaiTheId, GIATRIGOI = @giaTriGoi, GIAMGIA = @giamGia, TIENGIAM = @tienGiam,
                            THU = @thu, DLYDOTHUCHIID = @lyDoId, CHUYENKHOAN = @chuyenKhoan, NOTE = @note,
                            TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@tenDoiTuong", body.TryGetProperty("tenDoiTuong", out var pDcTdt) ? (object)pDcTdt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienThoai", body.TryGetProperty("dienThoai", out var pDcDt) ? (object)pDcDt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pDcDc) ? (object)pDcDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@loaiTheId", body.TryGetProperty("dloaiTheId", out var pDcLt) ? (object)pDcLt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@giaTriGoi", body.TryGetProperty("giaTriGoi", out var pDcg) ? pDcg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@giamGia", body.TryGetProperty("giamGia", out var pDcGg) ? pDcGg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@tienGiam", body.TryGetProperty("tienGiam", out var pDcTg) ? pDcTg.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@thu", body.TryGetProperty("thu", out var pDcThu) ? pDcThu.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@lyDoId", body.TryGetProperty("dlyDoThuChiId", out var pDcLyDo) ? (object)pDcLyDo.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@chuyenKhoan", body.TryGetProperty("chuyenKhoan", out var pDcCk) && pDcCk.GetBoolean() ? 1 : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                case "thucongno":
                    cmd.CommandText = @"
                        UPDATE TTHUCHI SET 
                            NGAY = @ngay, TENDOITUONG = @tenDoiTuong, DIACHI = @diaChi, DNHANVIENID = @nhanVienId,
                            DLYDOTHUCHIID = @lyDoId, DIENGIAI = @dienGiai, CHUNGTUGOC = @chungTuGoc, THU = @thu,
                            CHUYENKHOAN = @chuyenKhoan, NOTE = @note, TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userModified
                        WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", itemId.Trim());
                    cmd.Parameters.AddWithValue("@ngay", ngayChungTu);
                    cmd.Parameters.AddWithValue("@tenDoiTuong", body.TryGetProperty("tenDoiTuong", out var pCnTdt) ? (object)pCnTdt.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@diaChi", body.TryGetProperty("diaChi", out var pCnDc) ? (object)pCnDc.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@nhanVienId", body.TryGetProperty("dnhanVienId", out var pCnNv) ? (object)pCnNv.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@lyDoId", body.TryGetProperty("dlyDoThuChiId", out var pCnLyDo) ? (object)pCnLyDo.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@dienGiai", body.TryGetProperty("dienGiai", out var pCnDg) ? (object)pCnDg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@chungTuGoc", body.TryGetProperty("chungTuGoc", out var pCnCtg) ? (object)pCnCtg.GetString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("@thu", body.TryGetProperty("thu", out var pCnThu) ? pCnThu.GetDecimal() : 0);
                    cmd.Parameters.AddWithValue("@chuyenKhoan", body.TryGetProperty("chuyenKhoan", out var pCnCk) && pCnCk.GetBoolean() ? 1 : 0);
                    cmd.Parameters.AddWithValue("@note", note);
                    cmd.Parameters.AddWithValue("@userModified", _adminUserId);
                    break;

                default:
                    return BadRequest(new { success = false, message = $"Subtab '{tabId}' không được hỗ trợ cập nhật." });
            }

            int rows = cmd.ExecuteNonQuery();
            return Ok(new { success = true, rowsAffected = rows, message = "Đã cập nhật bản ghi thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi cập nhật bản ghi subtab: " + ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách giao dịch cho Quản lý gia hạn thẻ (TGIAHANTHE)
    /// </summary>
    [HttpGet("giahanthe/list")]
    public IActionResult GetGiaHanTheList(
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] string? loaiGiaoDich = null,
        [FromQuery] string? loaiTheId = null,
        [FromQuery] string? search = null)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            using var cmd = conn.CreateCommand();
            var sb = new StringBuilder(@"
                SELECT 
                    g.ID, g.NAME AS SO_PHIEU, g.NGAY, g.DKHACHHANGID, k.NAME AS TEN_KHACH, k.MAKHACH,
                    g.DLOAITHEID, lt.NAME AS TEN_LOAI_THE, g.TUNGAY, g.DENNGAY,
                    g.NGAYTANGTHEM, g.LANTANGTHEM, g.SOTIEN, g.TILEGIAMGIA, g.TIENGIAMGIA,
                    g.TONGCONG, g.THANHTOAN, g.KHUYENMAI, g.SOLAN, g.SOTHANG, g.SONGAY,
                    g.DATAP, g.NOTE, g.DLOAIGIAODICHID, g.TIMECREATED, g.USERCREATEDID,
                    g.TIMEMODIFIED, g.USERMODIFIEDID,
                    u.NAME AS NGUOI_TAO, um.NAME AS NGUOI_SUA
                FROM TGIAHANTHE g
                LEFT JOIN DKHACHHANG k ON g.DKHACHHANGID = k.ID
                LEFT JOIN DLOAITHE lt ON g.DLOAITHEID = lt.ID
                LEFT JOIN SUSER u ON g.USERCREATEDID = u.ID
                LEFT JOIN SUSER um ON g.USERMODIFIEDID = um.ID
                WHERE (g.STATUS <> -1 OR g.STATUS IS NULL)
            ");

            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var dFrom))
            {
                sb.Append(" AND g.NGAY >= @fromDate");
                cmd.Parameters.AddWithValue("@fromDate", dFrom.Date);
            }
            if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var dTo))
            {
                sb.Append(" AND g.NGAY <= @toDate");
                cmd.Parameters.AddWithValue("@toDate", dTo.Date.AddDays(1).AddSeconds(-1));
            }
            if (!string.IsNullOrEmpty(loaiGiaoDich) && loaiGiaoDich != "all")
            {
                sb.Append(" AND g.DLOAIGIAODICHID = @lgd");
                cmd.Parameters.AddWithValue("@lgd", loaiGiaoDich);
            }
            if (!string.IsNullOrEmpty(loaiTheId) && loaiTheId != "all" && loaiTheId != "chuaThietLap" && loaiTheId != "trash")
            {
                sb.Append(" AND g.DLOAITHEID = @loaiTheId");
                cmd.Parameters.AddWithValue("@loaiTheId", loaiTheId);
            }
            else if (loaiTheId == "chuaThietLap")
            {
                sb.Append(" AND (g.DLOAITHEID IS NULL OR g.DLOAITHEID = '')");
            }

            if (!string.IsNullOrEmpty(search))
            {
                sb.Append(" AND (LOWER(g.NAME) LIKE @kw OR LOWER(k.NAME) LIKE @kw OR LOWER(k.MAKHACH) LIKE @kw)");
                cmd.Parameters.AddWithValue("@kw", $"%{search.ToLower()}%");
            }

            sb.Append(" ORDER BY g.NGAY DESC, g.TIMECREATED DESC");
            cmd.CommandText = sb.ToString();

            var list = new List<object>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new
                {
                    id = r["ID"]?.ToString()?.Trim(),
                    soPhiu = r["SO_PHIEU"]?.ToString()?.Trim() ?? "",
                    ngay = r["NGAY"] is not DBNull ? Convert.ToDateTime(r["NGAY"]).ToString("dd/MM/yyyy") : "",
                    khachHangId = r["DKHACHHANGID"]?.ToString()?.Trim() ?? "",
                    khachHang = r["TEN_KHACH"]?.ToString()?.Trim() ?? "",
                    maKhach = r["MAKHACH"]?.ToString()?.Trim() ?? "",
                    loaiTheId = r["DLOAITHEID"]?.ToString()?.Trim() ?? "",
                    loaiThe = r["TEN_LOAI_THE"]?.ToString()?.Trim() ?? "",
                    tuNgay = r["TUNGAY"] is not DBNull ? Convert.ToDateTime(r["TUNGAY"]).ToString("dd/MM/yyyy") : "",
                    denNgay = r["DENNGAY"] is not DBNull ? Convert.ToDateTime(r["DENNGAY"]).ToString("dd/MM/yyyy") : "",
                    ngayTangThem = r["NGAYTANGTHEM"] is not DBNull ? Convert.ToInt32(r["NGAYTANGTHEM"]) : 0,
                    lanTangThem = r["LANTANGTHEM"] is not DBNull ? Convert.ToInt32(r["LANTANGTHEM"]) : 0,
                    soTien = r["SOTIEN"] is not DBNull ? Convert.ToDecimal(r["SOTIEN"]) : 0,
                    tiLeGiamGia = r["TILEGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TILEGIAMGIA"]) : 0,
                    tienGiamGia = r["TIENGIAMGIA"] is not DBNull ? Convert.ToDecimal(r["TIENGIAMGIA"]) : 0,
                    tongCong = r["TONGCONG"] is not DBNull ? Convert.ToDecimal(r["TONGCONG"]) : 0,
                    thanhToan = r["THANHTOAN"] is not DBNull ? Convert.ToDecimal(r["THANHTOAN"]) : 0,
                    khuyenMai = r["KHUYENMAI"]?.ToString()?.Trim() ?? "",
                    soLan = r["SOLAN"] is not DBNull ? Convert.ToInt32(r["SOLAN"]) : 0,
                    soThang = r["SOTHANG"] is not DBNull ? Convert.ToInt32(r["SOTHANG"]) : 0,
                    soNgay = r["SONGAY"] is not DBNull ? Convert.ToInt32(r["SONGAY"]) : 0,
                    daTap = r["DATAP"] is not DBNull && (Convert.ToInt32(r["DATAP"]) == 1 || Convert.ToInt32(r["DATAP"]) == 30),
                    note = r["NOTE"]?.ToString()?.Trim() ?? "",
                    loaiGiaoDich = r["DLOAIGIAODICHID"]?.ToString()?.Trim() ?? "1",
                    timeCreated = r["TIMECREATED"] is not DBNull ? Convert.ToDateTime(r["TIMECREATED"]).ToString("dd/MM/yyyy HH:mm") : "",
                    userCreated = r["NGUOI_TAO"]?.ToString()?.Trim() ?? "Administrator",
                    timeModified = r["TIMEMODIFIED"] is not DBNull ? Convert.ToDateTime(r["TIMEMODIFIED"]).ToString("dd/MM/yyyy HH:mm") : "",
                    userModified = r["NGUOI_SUA"]?.ToString()?.Trim() ?? ""
                });
            }

            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi lấy danh sách gia hạn thẻ: " + ex.Message });
        }
    }

    /// <summary>
    /// Xóa bản ghi trong subtab
    /// </summary>
    [HttpDelete("subtabs/{tabId}/{itemId}")]
    public IActionResult DeleteSubtabItem(string tabId, string itemId)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            string tableName = tabId.ToLower() switch
            {
                "dathang" => "TDATHANG",
                "baogia" => "TBAOGIA",
                "donhang" => "TDONHANG",
                "giahanthe" or "baoluuthe" or "doiloaithe" => "TGIAHANTHE",
                "tanggiamdiem" => "TTANGGIAMDIEM",
                "thetrang" => "DTHETRANG",
                "phieuthu" or "phieuchi" or "thucongno" or "datcoc" => "TTHUCHI",
                "vaora" => "TVAORA",
                _ => ""
            };

            if (string.IsNullOrEmpty(tableName))
                return BadRequest(new { success = false, message = $"Tab '{tabId}' không hợp lệ." });

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM {tableName} WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", itemId.Trim());
            int rows = cmd.ExecuteNonQuery();

            return Ok(new { success = true, rowsAffected = rows, message = "Đã xóa bản ghi thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi xóa bản ghi subtab: " + ex.Message });
        }
    }

    /// <summary>
    /// 4. Danh mục tham chiếu phục vụ thêm/sửa khách hàng
    /// </summary>
    [HttpGet("metadata")]
    public IActionResult GetMetadata()
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            var loaiThe = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME, SOTHANG, SONGAY, SOLAN, GIABAN FROM DLOAITHE WHERE (STATUS <> -1 OR STATUS IS NULL) ORDER BY SORTORDER, NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    loaiThe.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? "",
                        soThang = r["SOTHANG"] is not DBNull ? Convert.ToInt32(r["SOTHANG"]) : 0,
                        soNgay = r["SONGAY"] is not DBNull ? Convert.ToInt32(r["SONGAY"]) : 0,
                        soLan = r["SOLAN"] is not DBNull ? Convert.ToInt32(r["SOLAN"]) : 0,
                        giaBan = r["GIABAN"] is not DBNull ? Convert.ToDecimal(r["GIABAN"]) : 0
                    });
                }
            }

            var nhomKhach = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME FROM DNHOMKHACHHANG WHERE (STATUS <> -1 OR STATUS IS NULL) ORDER BY SORTORDER, NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    nhomKhach.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            var caTap = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME, TUGIO, DENGIO FROM DCATAP WHERE (STATUS <> -1 OR STATUS IS NULL) ORDER BY SORTORDER, NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    caTap.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? "",
                        tuGio = r["TUGIO"]?.ToString()?.Trim() ?? "",
                        denGio = r["DENGIO"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            var nhanVien = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME FROM DNHANVIEN WHERE (STATUS <> -1 OR STATUS IS NULL) ORDER BY NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    nhanVien.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            var trangThai = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME FROM DTRANGTHAI ORDER BY SORTORDER";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    trangThai.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            var tinhThanh = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME FROM DTINHTHANH ORDER BY NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    tinhThanh.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            var lyDoThuChi = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME, LALYDOTHU FROM DLYDOTHUCHI WHERE (STATUS <> -1 OR STATUS IS NULL) ORDER BY SORTORDER, NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int ldt = r["LALYDOTHU"] is not DBNull ? Convert.ToInt32(r["LALYDOTHU"]) : 0;
                    lyDoThuChi.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? "",
                        laThu = ldt == 30,
                        laChi = ldt == 0
                    });
                }
            }

            var khoHang = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME FROM DKHOHANG WHERE (STATUS <> -1 OR STATUS IS NULL) ORDER BY SORTORDER, NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    khoHang.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            var cuaHang = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME FROM DCUAHANG WHERE (STATUS <> -1 OR STATUS IS NULL) ORDER BY SORTORDER, NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    cuaHang.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            var mayVanTay = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID, NAME FROM DMAYVANTAY WHERE (STATUS <> -1 OR STATUS IS NULL) ORDER BY NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    mayVanTay.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    loaiThe,
                    nhomKhach,
                    caTap,
                    nhanVien,
                    trangThai,
                    tinhThanh,
                    lyDoThuChi,
                    khoHang,
                    cuaHang,
                    mayVanTay
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi đọc metadata: " + ex.Message });
        }
    }

    public class SaveCustomerRequest
    {
        public string? MaThe { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DiaChi { get; set; }
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public string? Facebook { get; set; }
        public string? DLoaiTheId { get; set; }
        public string? DTrangThaiId { get; set; }
        public string? DNhomKhachHangId { get; set; }
        public string? DTinhThanhId { get; set; }
        public string? DCaTapId { get; set; }
        public string? DNhanVienId { get; set; }
        public string? TuNgay { get; set; }
        public string? DenNgay { get; set; }
        public int SoLan { get; set; }
        public int DaTap { get; set; }
        public int ConLai { get; set; }
        public string? MaVanTay { get; set; }
        public string? Note { get; set; }
        public string? NgaySinh { get; set; }
        public string? Anh { get; set; }
    }

    /// <summary>
    /// 5. Thêm mới khách hàng (luôn lưu TIMECREATED, TIMEMODIFIED = CURRENT_TIMESTAMP)
    /// </summary>
    [HttpPost]
    public IActionResult CreateCustomer([FromBody] SaveCustomerRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(new { success = false, message = "Tên khách hàng không được để trống!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            var newId = Guid.NewGuid().ToString();
            var maKhach = string.IsNullOrWhiteSpace(req.MaThe) ? new Random().Next(100000, 999999).ToString() : req.MaThe.Trim();

            byte[]? imgBytes = null;
            if (!string.IsNullOrWhiteSpace(req.Anh) && req.Anh.Contains("base64,"))
            {
                try
                {
                    var raw = req.Anh.Substring(req.Anh.IndexOf("base64,") + 7);
                    imgBytes = Convert.FromBase64String(raw);
                }
                catch { }
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO DKHACHHANG (
                    ID, MAKHACH, NAME, DIACHI, DIENTHOAI, EMAIL, FACEBOOK,
                    DLOAITHEID, DTRANGTHAIID, DNHOMKHACHHANGID, DTINHTHANHID, DCATAPID, DNHANVIENID,
                    TUNGAY, DENNGAY, SOLAN, DATAP, CONLAI, MAVANTAY, NOTE, NGAYSINH, ANH,
                    STATUS, TIMECREATED, USERCREATEDID, TIMEMODIFIED, USERMODIFIEDID
                ) VALUES (
                    @id, @maKhach, @name, @diaChi, @dienThoai, @email, @facebook,
                    @loaiTheId, @trangThaiId, @nhomId, @tinhThanhId, @caTapId, @nhanVienId,
                    @tuNgay, @denNgay, @soLan, @daTap, @conLai, @maVanTay, @note, @ngaySinh, @anh,
                    30, CURRENT_TIMESTAMP, @userCreatedId, CURRENT_TIMESTAMP, @userModifiedId
                )";

            cmd.Parameters.AddWithValue("@id", newId);
            cmd.Parameters.AddWithValue("@maKhach", maKhach);
            cmd.Parameters.AddWithValue("@name", req.Name.Trim());
            cmd.Parameters.AddWithValue("@diaChi", (object?)req.DiaChi?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dienThoai", (object?)req.DienThoai?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)req.Email?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@facebook", (object?)req.Facebook?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@loaiTheId", (object?)req.DLoaiTheId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@trangThaiId", string.IsNullOrWhiteSpace(req.DTrangThaiId) ? "0" : req.DTrangThaiId.Trim());
            cmd.Parameters.AddWithValue("@nhomId", (object?)req.DNhomKhachHangId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tinhThanhId", (object?)req.DTinhThanhId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caTapId", (object?)req.DCaTapId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nhanVienId", (object?)req.DNhanVienId?.Trim() ?? DBNull.Value);

            if (!string.IsNullOrWhiteSpace(req.TuNgay) && DateTime.TryParse(req.TuNgay, out var d1))
                cmd.Parameters.AddWithValue("@tuNgay", d1.Date);
            else
                cmd.Parameters.AddWithValue("@tuNgay", DateTime.Today);

            if (!string.IsNullOrWhiteSpace(req.DenNgay) && DateTime.TryParse(req.DenNgay, out var d2))
                cmd.Parameters.AddWithValue("@denNgay", d2.Date);
            else
                cmd.Parameters.AddWithValue("@denNgay", DateTime.Today.AddMonths(1));

            cmd.Parameters.AddWithValue("@soLan", req.SoLan);
            cmd.Parameters.AddWithValue("@daTap", req.DaTap);
            cmd.Parameters.AddWithValue("@conLai", req.ConLai);
            cmd.Parameters.AddWithValue("@maVanTay", (object?)req.MaVanTay?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@note", (object?)req.Note?.Trim() ?? DBNull.Value);

            if (!string.IsNullOrWhiteSpace(req.NgaySinh) && DateTime.TryParse(req.NgaySinh, out var d3))
                cmd.Parameters.AddWithValue("@ngaySinh", d3.Date);
            else
                cmd.Parameters.AddWithValue("@ngaySinh", DBNull.Value);

            cmd.Parameters.AddWithValue("@anh", (object?)imgBytes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@userCreatedId", _adminUserId);
            cmd.Parameters.AddWithValue("@userModifiedId", _adminUserId);

            cmd.ExecuteNonQuery();

            return Ok(new { success = true, message = $"Đã thêm thành công khách hàng {req.Name}!", id = newId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi thêm khách hàng: " + ex.Message });
        }
    }

    /// <summary>
    /// 6. Cập nhật khách hàng (luôn lưu TIMEMODIFIED = CURRENT_TIMESTAMP)
    /// </summary>
    [HttpPut("{id}")]
    public IActionResult UpdateCustomer(string id, [FromBody] SaveCustomerRequest req)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { success = false, message = "Thiếu ID khách hàng!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            byte[]? imgBytes = null;
            bool updateImg = false;
            if (!string.IsNullOrWhiteSpace(req.Anh))
            {
                if (req.Anh.Contains("base64,"))
                {
                    try
                    {
                        var raw = req.Anh.Substring(req.Anh.IndexOf("base64,") + 7);
                        imgBytes = Convert.FromBase64String(raw);
                        updateImg = true;
                    }
                    catch { }
                }
            }

            using var cmd = conn.CreateCommand();
            var sql = @"
                UPDATE DKHACHHANG 
                SET MAKHACH = @maKhach,
                    NAME = @name,
                    DIACHI = @diaChi,
                    DIENTHOAI = @dienThoai,
                    EMAIL = @email,
                    FACEBOOK = @facebook,
                    DLOAITHEID = @loaiTheId,
                    DTRANGTHAIID = @trangThaiId,
                    DNHOMKHACHHANGID = @nhomId,
                    DTINHTHANHID = @tinhThanhId,
                    DCATAPID = @caTapId,
                    DNHANVIENID = @nhanVienId,
                    TUNGAY = @tuNgay,
                    DENNGAY = @denNgay,
                    SOLAN = @soLan,
                    DATAP = @daTap,
                    CONLAI = @conLai,
                    MAVANTAY = @maVanTay,
                    NOTE = @note,
                    NGAYSINH = @ngaySinh,
                    TIMEMODIFIED = CURRENT_TIMESTAMP,
                    USERMODIFIEDID = @userModifiedId ";

            if (updateImg)
            {
                sql += ", ANH = @anh ";
                cmd.Parameters.AddWithValue("@anh", (object?)imgBytes ?? DBNull.Value);
            }

            sql += " WHERE ID = @id";
            cmd.CommandText = sql;

            cmd.Parameters.AddWithValue("@id", id.Trim());
            cmd.Parameters.AddWithValue("@maKhach", req.MaThe?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@name", req.Name.Trim());
            cmd.Parameters.AddWithValue("@diaChi", (object?)req.DiaChi?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dienThoai", (object?)req.DienThoai?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)req.Email?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@facebook", (object?)req.Facebook?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@loaiTheId", (object?)req.DLoaiTheId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@trangThaiId", (object?)req.DTrangThaiId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nhomId", (object?)req.DNhomKhachHangId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tinhThanhId", (object?)req.DTinhThanhId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caTapId", (object?)req.DCaTapId?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nhanVienId", (object?)req.DNhanVienId?.Trim() ?? DBNull.Value);

            if (!string.IsNullOrWhiteSpace(req.TuNgay) && DateTime.TryParse(req.TuNgay, out var d1))
                cmd.Parameters.AddWithValue("@tuNgay", d1.Date);
            else
                cmd.Parameters.AddWithValue("@tuNgay", DBNull.Value);

            if (!string.IsNullOrWhiteSpace(req.DenNgay) && DateTime.TryParse(req.DenNgay, out var d2))
                cmd.Parameters.AddWithValue("@denNgay", d2.Date);
            else
                cmd.Parameters.AddWithValue("@denNgay", DBNull.Value);

            cmd.Parameters.AddWithValue("@soLan", req.SoLan);
            cmd.Parameters.AddWithValue("@daTap", req.DaTap);
            cmd.Parameters.AddWithValue("@conLai", req.ConLai);
            cmd.Parameters.AddWithValue("@maVanTay", (object?)req.MaVanTay?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@note", (object?)req.Note?.Trim() ?? DBNull.Value);

            if (!string.IsNullOrWhiteSpace(req.NgaySinh) && DateTime.TryParse(req.NgaySinh, out var d3))
                cmd.Parameters.AddWithValue("@ngaySinh", d3.Date);
            else
                cmd.Parameters.AddWithValue("@ngaySinh", DBNull.Value);

            cmd.Parameters.AddWithValue("@userModifiedId", _adminUserId);

            cmd.ExecuteNonQuery();

            return Ok(new { success = true, message = $"Đã cập nhật thông tin khách hàng {req.Name} thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi cập nhật khách hàng: " + ex.Message });
        }
    }

    /// <summary>
    /// 7. Xóa khách hàng (vào thùng rác STATUS = -1, lưu TIMEMODIFIED)
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult DeleteCustomer(string id)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE DKHACHHANG 
                SET STATUS = 0, 
                    TIMEMODIFIED = CURRENT_TIMESTAMP, 
                    USERMODIFIEDID = @uid 
                WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", id.Trim());
            cmd.Parameters.AddWithValue("@uid", _adminUserId);
            cmd.ExecuteNonQuery();

            return Ok(new { success = true, message = "Đã chuyển khách hàng vào thùng rác thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi xóa khách hàng: " + ex.Message });
        }
    }

    /// <summary>
    /// 8. Phục hồi khách hàng từ thùng rác (STATUS = 30)
    /// </summary>
    [HttpPost("{id}/restore")]
    public IActionResult RestoreCustomer(string id)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE DKHACHHANG 
                SET STATUS = 30, 
                    TIMEMODIFIED = CURRENT_TIMESTAMP, 
                    USERMODIFIEDID = @uid 
                WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", id.Trim());
            cmd.Parameters.AddWithValue("@uid", _adminUserId);
            cmd.ExecuteNonQuery();

            return Ok(new { success = true, message = "Đã phục hồi khách hàng thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi phục hồi khách hàng: " + ex.Message });
        }
    }

    /// <summary>
    /// 9. Nháº­p hÃ ng loáº¡t khÃ¡ch hÃ ng tá»« Excel
    /// </summary>
    [HttpPost("batch-import")]
    public IActionResult BatchImportCustomers([FromBody] List<SaveCustomerRequest> list)
    {
        if (list == null || list.Count == 0)
        {
            return BadRequest(new { success = false, message = "Danh sách nhập trống!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();
            using var trans = conn.BeginTransaction();

            int imported = 0;
            var rand = new Random();

            foreach (var req in list)
            {
                if (string.IsNullOrWhiteSpace(req.Name)) continue;

                var newId = Guid.NewGuid().ToString();
                var maKhach = string.IsNullOrWhiteSpace(req.MaThe) ? rand.Next(100000, 999999).ToString() : req.MaThe.Trim();

                using var cmd = conn.CreateCommand();
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT INTO DKHACHHANG (
                        ID, MAKHACH, NAME, DIACHI, DIENTHOAI, EMAIL, FACEBOOK,
                        DLOAITHEID, DTRANGTHAIID, DNHOMKHACHHANGID, DTINHTHANHID, DCATAPID, DNHANVIENID,
                        TUNGAY, DENNGAY, SOLAN, DATAP, CONLAI, MAVANTAY, NOTE, NGAYSINH,
                        STATUS, TIMECREATED, USERCREATEDID, TIMEMODIFIED, USERMODIFIEDID
                    ) VALUES (
                        @id, @maKhach, @name, @diaChi, @dienThoai, @email, @facebook,
                        @loaiTheId, @trangThaiId, @nhomId, @tinhThanhId, @caTapId, @nhanVienId,
                        @tuNgay, @denNgay, @soLan, @daTap, @conLai, @maVanTay, @note, @ngaySinh,
                        30, CURRENT_TIMESTAMP, @userCreatedId, CURRENT_TIMESTAMP, @userModifiedId
                    )";

                cmd.Parameters.AddWithValue("@id", newId);
                cmd.Parameters.AddWithValue("@maKhach", maKhach);
                cmd.Parameters.AddWithValue("@name", req.Name.Trim());
                cmd.Parameters.AddWithValue("@diaChi", (object?)req.DiaChi?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dienThoai", (object?)req.DienThoai?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@email", (object?)req.Email?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@facebook", (object?)req.Facebook?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@loaiTheId", (object?)req.DLoaiTheId?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@trangThaiId", string.IsNullOrWhiteSpace(req.DTrangThaiId) ? "0" : req.DTrangThaiId.Trim());
                cmd.Parameters.AddWithValue("@nhomId", (object?)req.DNhomKhachHangId?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tinhThanhId", (object?)req.DTinhThanhId?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@caTapId", (object?)req.DCaTapId?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nhanVienId", (object?)req.DNhanVienId?.Trim() ?? DBNull.Value);

                if (!string.IsNullOrWhiteSpace(req.TuNgay) && DateTime.TryParse(req.TuNgay, out var d1))
                    cmd.Parameters.AddWithValue("@tuNgay", d1.Date);
                else
                    cmd.Parameters.AddWithValue("@tuNgay", DateTime.Today);

                if (!string.IsNullOrWhiteSpace(req.DenNgay) && DateTime.TryParse(req.DenNgay, out var d2))
                    cmd.Parameters.AddWithValue("@denNgay", d2.Date);
                else
                    cmd.Parameters.AddWithValue("@denNgay", DateTime.Today.AddMonths(1));

                cmd.Parameters.AddWithValue("@soLan", req.SoLan);
                cmd.Parameters.AddWithValue("@daTap", req.DaTap);
                cmd.Parameters.AddWithValue("@conLai", req.ConLai > 0 ? req.ConLai : req.SoLan);
                cmd.Parameters.AddWithValue("@maVanTay", (object?)req.MaVanTay?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@note", (object?)req.Note?.Trim() ?? DBNull.Value);

                if (!string.IsNullOrWhiteSpace(req.NgaySinh) && DateTime.TryParse(req.NgaySinh, out var d3))
                    cmd.Parameters.AddWithValue("@ngaySinh", d3.Date);
                else
                    cmd.Parameters.AddWithValue("@ngaySinh", DBNull.Value);

                cmd.Parameters.AddWithValue("@userCreatedId", _adminUserId);
                cmd.Parameters.AddWithValue("@userModifiedId", _adminUserId);

                cmd.ExecuteNonQuery();
                imported++;
            }

            trans.Commit();
            return Ok(new { success = true, importedCount = imported, message = $"Nhập thành công {imported} khách hàng!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi nhập hàng loạt khách hàng: " + ex.Message });
        }
    }

    [HttpDelete("{id}/permanent")]
    public IActionResult PermanentDeleteCustomer(string id)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM DKHACHHANG WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", id.Trim());
            int rows = cmd.ExecuteNonQuery();
            return Ok(new { success = true, rowsAffected = rows, message = "ÄÃ£ xÃ³a vÄ©nh viá»…n khÃ¡ch hÃ ng thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lá»—i khi xÃ³a vÄ©nh viá»…n: " + ex.Message });
        }
    }

    // =========================================================================
    // API QUẢN LÝ CÂY TRẠNG THÁI & NHÓM KHÁCH HÀNG (DTRANGTHAI, DNHOMKHACHHANG, SIMAGE)
    // =========================================================================

    [HttpGet("icons")]
    public IActionResult GetIcons()
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();
            var list = new List<object>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT FIRST 120 ID, NAME, IMAGE FROM SIMAGE WHERE (STATUS <> -1 OR STATUS IS NULL) AND IMAGE IS NOT NULL ORDER BY NAME";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var imgVal = r["IMAGE"];
                string imgStr = "";
                if (imgVal is byte[] b) imgStr = Convert.ToBase64String(b);
                else if (imgVal is string s) imgStr = s;

                list.Add(new
                {
                    id = r["ID"]?.ToString()?.Trim(),
                    name = r["NAME"]?.ToString()?.Trim() ?? "",
                    image = imgStr
                });
            }
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi nạp danh sách icon: " + ex.Message });
        }
    }

    [HttpGet("tree-items")]
    public IActionResult GetTreeItems([FromQuery] string mode = "trangThai")
    {
        try
        {
            var table = mode == "trangThai" ? "DTRANGTHAI" : "DNHOMKHACHHANG";
            using var conn = new FbConnection(GetConnStr());
            conn.Open();
            var list = new List<object>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT t.ID, t.NAME, t.NOTE, t.STATUS, t.SORTORDER, t.PARENTID, t.ITEMTYPE, t.SIMAGEID, img.IMAGE AS SIMAGE_DATA FROM {table} t LEFT JOIN SIMAGE img ON t.SIMAGEID = img.ID WHERE (t.STATUS <> -1 OR t.STATUS IS NULL) ORDER BY t.SORTORDER, t.NAME";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var imgVal = r["SIMAGE_DATA"];
                string imgStr = "";
                if (imgVal is byte[] b) imgStr = Convert.ToBase64String(b);
                else if (imgVal is string s) imgStr = s;

                list.Add(new
                {
                    id = r["ID"]?.ToString()?.Trim(),
                    name = r["NAME"]?.ToString()?.Trim() ?? "",
                    note = r["NOTE"]?.ToString()?.Trim() ?? "",
                    status = r["STATUS"] is not DBNull ? Convert.ToInt32(r["STATUS"]) : 30,
                    sortOrder = r["SORTORDER"]?.ToString()?.Trim() ?? "",
                    parentId = r["PARENTID"]?.ToString()?.Trim() ?? "",
                    itemType = r["ITEMTYPE"] is not DBNull ? Convert.ToInt32(r["ITEMTYPE"]) : 0,
                    simageId = r["SIMAGEID"]?.ToString()?.Trim() ?? "",
                    simage = imgStr
                });
            }
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi tải cây danh mục: " + ex.Message });
        }
    }

    [HttpPost("tree-item")]
    public IActionResult CreateTreeItem([FromBody] TreeItemRequest req)
    {
        try
        {
            var table = req.Mode == "trangThai" ? "DTRANGTHAI" : "DNHOMKHACHHANG";
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            var newId = string.IsNullOrWhiteSpace(req.Id) ? Guid.NewGuid().ToString() : req.Id.Trim();
            var name = req.ItemType == 2 ? "—" : (req.Name?.Trim() ?? "");

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO {table} (
                    ID, NAME, NOTE, STATUS, SORTORDER, PARENTID, ITEMTYPE, SIMAGEID, TIMECREATED, USERCREATEDID
                ) VALUES (
                    @id, @name, @note, 30, @sortOrder, @parentId, @itemType, @simageId, CURRENT_TIMESTAMP, @userId
                )";

            cmd.Parameters.AddWithValue("@id", newId);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@note", (object?)req.Note?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sortOrder", "ZZZ" + DateTime.Now.ToString("HHmmss"));
            cmd.Parameters.AddWithValue("@parentId", string.IsNullOrWhiteSpace(req.ParentId) ? (object)DBNull.Value : req.ParentId.Trim());
            cmd.Parameters.AddWithValue("@itemType", req.ItemType);
            cmd.Parameters.AddWithValue("@simageId", string.IsNullOrWhiteSpace(req.SimageId) ? (object)DBNull.Value : req.SimageId.Trim());
            cmd.Parameters.AddWithValue("@userId", _adminUserId);

            cmd.ExecuteNonQuery();
            return Ok(new { success = true, id = newId, message = "Đã thêm thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi thêm: " + ex.Message });
        }
    }

    [HttpPut("tree-item/{id}")]
    public IActionResult UpdateTreeItem(string id, [FromBody] TreeItemRequest req)
    {
        try
        {
            var table = req.Mode == "trangThai" ? "DTRANGTHAI" : "DNHOMKHACHHANG";
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                UPDATE {table} SET
                    NAME = @name,
                    NOTE = @note,
                    SIMAGEID = @simageId,
                    TIMEMODIFIED = CURRENT_TIMESTAMP,
                    USERMODIFIEDID = @userId
                WHERE ID = @id";

            cmd.Parameters.AddWithValue("@id", id.Trim());
            cmd.Parameters.AddWithValue("@name", req.Name?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@note", (object?)req.Note?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@simageId", string.IsNullOrWhiteSpace(req.SimageId) ? (object)DBNull.Value : req.SimageId.Trim());
            cmd.Parameters.AddWithValue("@userId", _adminUserId);

            int rows = cmd.ExecuteNonQuery();
            return Ok(new { success = true, rowsAffected = rows, message = "Cập nhật thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi cập nhật: " + ex.Message });
        }
    }

    [HttpDelete("tree-item/{id}")]
    public IActionResult DeleteTreeItem(string id, [FromQuery] string mode = "trangThai")
    {
        try
        {
            var table = mode == "trangThai" ? "DTRANGTHAI" : "DNHOMKHACHHANG";
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE {table} SET STATUS = -1, TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = @userId WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", id.Trim());
            cmd.Parameters.AddWithValue("@userId", _adminUserId);

            int rows = cmd.ExecuteNonQuery();
            return Ok(new { success = true, rowsAffected = rows, message = "Đã chuyển vào thùng rác thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi xóa: " + ex.Message });
        }
    }

    [HttpPost("tree-items/batch")]
    public IActionResult BatchCreateTreeItems([FromBody] BatchTreeItemRequest req)
    {
        if (req.Names == null || req.Names.Count == 0)
        {
            return BadRequest(new { success = false, message = "Danh sách tên không được rỗng!" });
        }

        try
        {
            var table = req.Mode == "trangThai" ? "DTRANGTHAI" : "DNHOMKHACHHANG";
            using var conn = new FbConnection(GetConnStr());
            conn.Open();
            using var trans = conn.BeginTransaction();

            int count = 0;
            int idx = 0;
            foreach (var rawName in req.Names)
            {
                var name = rawName?.Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                using var cmd = conn.CreateCommand();
                cmd.Transaction = trans;
                cmd.CommandText = $@"
                    INSERT INTO {table} (
                        ID, NAME, STATUS, SORTORDER, PARENTID, ITEMTYPE, TIMECREATED, USERCREATEDID
                    ) VALUES (
                        @id, @name, 30, @sortOrder, @parentId, 0, CURRENT_TIMESTAMP, @userId
                    )";

                cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@sortOrder", "ZZZ" + DateTime.Now.ToString("HHmmss") + idx.ToString("D2"));
                cmd.Parameters.AddWithValue("@parentId", string.IsNullOrWhiteSpace(req.ParentId) ? (object)DBNull.Value : req.ParentId.Trim());
                cmd.Parameters.AddWithValue("@userId", _adminUserId);

                cmd.ExecuteNonQuery();
                count++;
                idx++;
            }

            trans.Commit();
            return Ok(new { success = true, createdCount = count, message = $"Đã thêm nhanh {count} mục thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi thêm nhanh: " + ex.Message });
        }
    }

    // =========================================================================
    // 7. PHÂN QUYỀN NGƯỜI DÙNG & CẤU HÌNH HỆ THỐNG DANH MỤC KHÁCH HÀNG
    // =========================================================================

    [HttpGet("permissions")]
    public IActionResult GetUserPermissions([FromQuery] string? username, [FromQuery] string? userId)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            bool isAdmin = false;
            string? sgroupUserId = null;
            string groupName = "Quản trị";

            // 1. Kiểm tra tài khoản người dùng
            if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(userId))
            {
                using var userCmd = conn.CreateCommand();
                userCmd.CommandText = @"
                    SELECT FIRST 1 u.ID, u.USERNAME, u.ISADMIN, u.SGROUPUSERID, g.NAME AS GROUPNAME
                    FROM SUSER u
                    LEFT JOIN SGROUPUSER g ON u.SGROUPUSERID = g.ID
                    WHERE (u.STATUS IS NULL OR u.STATUS <> -1)
                      AND (UPPER(u.USERNAME) = @u OR u.ID = @uid)";
                userCmd.Parameters.AddWithValue("@u", username?.Trim().ToUpper() ?? "");
                userCmd.Parameters.AddWithValue("@uid", userId?.Trim() ?? "");

                using var r = userCmd.ExecuteReader();
                if (r.Read())
                {
                    var uname = r["USERNAME"]?.ToString()?.Trim() ?? "";
                    var isAdmVal = r["ISADMIN"] is not DBNull && Convert.ToInt32(r["ISADMIN"]) == 1;
                    if (isAdmVal || uname.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
                    {
                        isAdmin = true;
                    }
                    sgroupUserId = r["SGROUPUSERID"]?.ToString()?.Trim();
                    groupName = r["GROUPNAME"]?.ToString()?.Trim() ?? (isAdmin ? "Quản trị hệ thống" : "Nhân viên");
                }
            }
            else
            {
                isAdmin = true;
            }

            // Nếu là Admin thì toàn quyền
            if (isAdmin)
            {
                return Ok(new
                {
                    success = true,
                    isAdmin = true,
                    groupName = "Quản trị hệ thống (Toàn quyền)",
                    canView = true,
                    canAdd = true,
                    canEdit = true,
                    canDelete = true,
                    canExport = true,
                    canSyncDevice = true,
                    canManageConfig = true,
                    subtabs = new
                    {
                        baoGia = new { canView = true, canAdd = true, canEdit = true, canDelete = true },
                        donHang = new { canView = true, canAdd = true, canEdit = true, canDelete = true },
                        datHang = new { canView = true, canAdd = true, canEdit = true, canDelete = true },
                        giaHanThe = new { canView = true, canAdd = true, canEdit = true, canDelete = true },
                        baoLuuThe = new { canView = true, canAdd = true, canEdit = true, canDelete = true },
                        doiLoaiThe = new { canView = true, canAdd = true, canEdit = true, canDelete = true },
                        datCoc = new { canView = true, canAdd = true, canEdit = true, canDelete = true },
                        thuCongNo = new { canView = true, canAdd = true, canEdit = true, canDelete = true }
                    }
                });
            }

            // Nếu không phải Admin, tra cứu quyền hạn trong SGROUPROLE theo SGROUPUSERID
            var roleModes = new Dictionary<string, int>();
            if (!string.IsNullOrWhiteSpace(sgroupUserId))
            {
                using var roleCmd = conn.CreateCommand();
                roleCmd.CommandText = "SELECT SFUNCTIONID, MODE FROM SGROUPROLE WHERE SGROUPUSERID = @gid AND (STATUS IS NULL OR STATUS <> -1)";
                roleCmd.Parameters.AddWithValue("@gid", sgroupUserId);
                using var rRole = roleCmd.ExecuteReader();
                while (rRole.Read())
                {
                    var fid = rRole["SFUNCTIONID"]?.ToString()?.Trim() ?? "";
                    var m = rRole["MODE"] is not DBNull ? Convert.ToInt32(rRole["MODE"]) : 0;
                    roleModes[fid] = m;
                }
            }

            // Function ID Danh mục khách hàng: 36c3bad1-9d2e-4916-98e0-efb6c0e681e2
            int custMode = roleModes.TryGetValue("36c3bad1-9d2e-4916-98e0-efb6c0e681e2", out var cm) ? cm : 0;
            // Cho phép sao chép dữ liệu (Export / In): c320a6bd-7aaf-49c3-9015-2e7532f0a669
            int copyMode = roleModes.TryGetValue("c320a6bd-7aaf-49c3-9015-2e7532f0a669", out var cpm) ? cpm : 0;
            // Đẩy thông tin thẻ lên thiết bị: 0b994c59-2301-46cc-8f43-015b7b55f0e8
            int syncMode = roleModes.TryGetValue("0b994c59-2301-46cc-8f43-015b7b55f0e8", out var sm) ? sm : 0;
            // Cấu hình toàn hệ thống: 7
            int cfgMode = roleModes.TryGetValue("7", out var cfm) ? cfm : 0;

            bool canView = (custMode & 16) != 0 || custMode == 240;
            bool canAdd = (custMode & 32) != 0 || custMode == 240;
            bool canEdit = (custMode & 64) != 0 || custMode == 240;
            bool canDelete = (custMode & 128) != 0 || custMode == 240;
            bool canExport = (copyMode & 16) != 0 || copyMode == 240 || canView;
            bool canSyncDevice = (syncMode & 16) != 0 || syncMode == 240;
            bool canManageConfig = (cfgMode & 16) != 0 || cfgMode == 240;

            Func<string, object> getSubtabPerm = (funcId) =>
            {
                int m = roleModes.TryGetValue(funcId, out var subM) ? subM : 0;
                return new
                {
                    canView = (m & 16) != 0 || m == 240,
                    canAdd = (m & 32) != 0 || m == 240,
                    canEdit = (m & 64) != 0 || m == 240,
                    canDelete = (m & 128) != 0 || m == 240
                };
            };

            return Ok(new
            {
                success = true,
                isAdmin = false,
                groupName = groupName,
                canView = canView,
                canAdd = canAdd,
                canEdit = canEdit,
                canDelete = canDelete,
                canExport = canExport,
                canSyncDevice = canSyncDevice,
                canManageConfig = canManageConfig,
                subtabs = new
                {
                    baoGia = getSubtabPerm("7be6722c-aeed-486a-85ef-607b8b4c7407"),
                    donHang = getSubtabPerm("db591d0e-d671-42d0-b3b1-f705345cc641"),
                    datHang = getSubtabPerm("97487cfa-66f6-415e-9046-9a245c61f2ae"),
                    giaHanThe = getSubtabPerm("f91ac379-3afd-455d-8d67-25c5d220c869"),
                    baoLuuThe = getSubtabPerm("7e5dedef-7c46-473e-b7d2-64f980defc65"),
                    doiLoaiThe = getSubtabPerm("42129259-1ac5-4600-bbaf-03b8e63bd2ec"),
                    datCoc = getSubtabPerm("e3501b22-4374-4e7a-af19-b57a15a49fb5"),
                    thuCongNo = getSubtabPerm("d5fd5371-a3a7-4247-ae44-68fd88812f90")
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi kiểm tra phân quyền: " + ex.Message });
        }
    }

    [HttpGet("system-config")]
    public IActionResult GetSystemConfig()
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            var configList = new List<object>();
            var configDict = new Dictionary<string, object?>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT ID, NAME, CAPTION, CONTROLTYPE, DATATYPE, MOREDETAIL, NOTE
                    FROM SCONFIG
                    WHERE NAME IN (
                        'ChoPhepTrungTenKhachHang',
                        'ChoPhepNhapBangBanPhim',
                        'GiaHanTheKhiThemKhachHang',
                        'TuDongTaoXoaThe',
                        'ChoPhepKhachNoGym',
                        'CoPhanCaTap',
                        'CoSuDungTheTheoLan',
                        'SoNgayCanhBaoSapHetHan',
                        'SoLanCanhBaoSapHet',
                        'ThongBaoKhachDenNgaySinhNhat'
                    )";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var name = r["NAME"]?.ToString()?.Trim() ?? "";
                    var caption = r["CAPTION"]?.ToString()?.Trim() ?? "";
                    var ctype = r["CONTROLTYPE"] is not DBNull ? Convert.ToInt32(r["CONTROLTYPE"]) : 9;
                    var val = r["MOREDETAIL"]?.ToString()?.Trim() ?? "";

                    object parsedVal = val;
                    if (ctype == 9) // Boolean
                    {
                        parsedVal = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (ctype == 3) // Integer
                    {
                        parsedVal = int.TryParse(val, out var intV) ? intV : 0;
                    }

                    configDict[name] = parsedVal;
                    configList.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = name,
                        caption = caption,
                        controlType = ctype,
                        value = parsedVal,
                        rawValue = val
                    });
                }
            }

            return Ok(new
            {
                success = true,
                configs = configDict,
                items = configList
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi đọc cấu hình hệ thống: " + ex.Message });
        }
    }

    [HttpPost("system-config")]
    public IActionResult UpdateSystemConfig([FromBody] UpdateSystemConfigRequest req)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            foreach (var kvp in req.Configs)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE SCONFIG 
                    SET MOREDETAIL = @val, 
                        TIMEMODIFIED = CURRENT_TIMESTAMP,
                        USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569'
                    WHERE NAME = @name";
                cmd.Parameters.AddWithValue("@val", kvp.Value ?? "");
                cmd.Parameters.AddWithValue("@name", kvp.Key);
                cmd.ExecuteNonQuery();
            }

            return Ok(new { success = true, message = "Lưu cấu hình hệ thống thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi lưu cấu hình hệ thống: " + ex.Message });
        }
    }

    private static readonly List<SlipConfigDef> SlipDefinitions = new()
    {
        new() { Key = "khachhang", Name = "Khách hàng", Source = "STABLEDESC", SourceId = "5fcc571a-662d-4953-a83f-6004c732f439", TableName = "DKHACHHANG", ColumnName = "MAKHACH" },
        new() { Key = "baogia", Name = "Báo giá", Source = "STABLEDESC", SourceId = "b2d3ffd4-9037-4dd1-b255-ec16dd122e66", TableName = "TBAOGIA", ColumnName = "NAME" },
        new() { Key = "donhang", Name = "Đơn hàng", Source = "SFORM", SourceId = "f3f7bb77-f4ba-4111-9066-014f52be79a0", TableName = "TDONHANG", ColumnName = "NAME" },
        new() { Key = "phieunhap", Name = "Phiếu nhập kho", Source = "SFORM", SourceId = "24399cd6-11fa-4eb8-98bc-9e23e66aab14", TableName = "TDONHANG", ColumnName = "NAME" },
        new() { Key = "phieuxuat", Name = "Phiếu xuất kho", Source = "SFORM", SourceId = "1f13b546-6197-4d51-9af1-b45fff263df0", TableName = "TDONHANG", ColumnName = "NAME" },
        new() { Key = "phieuchuyenkho", Name = "Phiếu chuyển kho", Source = "SFORM", SourceId = "cc92bb29-b8a5-41c1-976a-174ca77fb542", TableName = "TDONHANG", ColumnName = "NAME" },
        new() { Key = "phieukiemke", Name = "Phiếu kiểm kê", Source = "SFORM", SourceId = "93d9d7d4-dc0d-4fb4-a3e8-ee5d4f808346", TableName = "TDONHANG", ColumnName = "NAME" },
        new() { Key = "dathang", Name = "Đặt hàng", Source = "STABLEDESC", SourceId = "780a9daf-b2b7-417c-8ff3-21700fab6990", TableName = "TDATHANG", ColumnName = "NAME" },
        new() { Key = "phieuthu", Name = "Phiếu thu", Source = "SFORM", SourceId = "6a447203-3a1b-4622-b248-b6a84a29d3e3", TableName = "TTHUCHI", ColumnName = "NAME" },
        new() { Key = "phieuchi", Name = "Phiếu chi", Source = "SFORM", SourceId = "9f094553-7b79-4427-9acd-dedf3c2a0eda", TableName = "TTHUCHI", ColumnName = "NAME" },
        new() { Key = "thucongno", Name = "Phiếu thu công nợ", Source = "SFORM", SourceId = "f34aa294-898a-4674-8a92-54d07994d159", TableName = "TTHUCHI", ColumnName = "NAME" },
        new() { Key = "datcoc", Name = "Đặt cọc", Source = "SFORM", SourceId = "18b008ef-9054-4908-bb3e-69aff123eebf", TableName = "TTHUCHI", ColumnName = "NAME" },
        new() { Key = "bangluong", Name = "Bảng lương", Source = "STABLEDESC", SourceId = "bd89ba4e-9d23-4b22-9541-0e4385fe28f8", TableName = "TBANGLUONG", ColumnName = "NAME" },
        new() { Key = "giahanthe", Name = "Gia hạn thẻ", Source = "SFORM", SourceId = "522dea56-9af0-4f94-9f0e-7107b1f9702e", TableName = "TGIAHANTHE", ColumnName = "NAME" },
        new() { Key = "baoluuthe", Name = "Bảo lưu thẻ", Source = "SFORM", SourceId = "1ad99fb9-ba15-451c-9a36-d62dbb95fda0", TableName = "TGIAHANTHE", ColumnName = "NAME" },
        new() { Key = "doiloaithe", Name = "Đổi loại thẻ", Source = "SFORM", SourceId = "beda35b4-e4bc-479b-b892-1b35e00de712", TableName = "TGIAHANTHE", ColumnName = "NAME" },
        new() { Key = "tanggiamdiem", Name = "Tăng giảm điểm", Source = "STABLEDESC", SourceId = "34ec0bca-7b21-4d67-9b92-b1f7800139c5", TableName = "TTANGGIAMDIEM", ColumnName = "NAME" }
    };

    private string GenerateSlipNumberCore(FbConnection conn, string template, string tableName, string colName = "NAME", DateTime? date = null)
    {
        if (string.IsNullOrWhiteSpace(template)) return "";
        var d = date ?? DateTime.Now;

        string res = template;
        res = res.Replace("(yyyy)", d.ToString("yyyy"));
        res = res.Replace("(yy)", d.ToString("yy"));
        res = res.Replace("(MM)", d.ToString("MM"));
        res = res.Replace("(dd)", d.ToString("dd"));

        var match = System.Text.RegularExpressions.Regex.Match(res, @"\(\*+\)");
        if (!match.Success) return res;

        int digitCount = match.Value.Length - 2;
        string prefix = res.Substring(0, match.Index);
        string suffix = res.Substring(match.Index + match.Length);

        long maxNum = 0;
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {colName} FROM {tableName} WHERE {colName} LIKE @pattern";
            cmd.Parameters.AddWithValue("@pattern", $"{prefix}%{suffix}");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0)) continue;
                string val = reader.GetString(0).Trim();
                if (val.StartsWith(prefix) && (string.IsNullOrEmpty(suffix) || val.EndsWith(suffix)))
                {
                    int len = val.Length - prefix.Length - suffix.Length;
                    if (len > 0)
                    {
                        string numPart = val.Substring(prefix.Length, len);
                        if (long.TryParse(numPart, out long parsed))
                        {
                            if (parsed > maxNum) maxNum = parsed;
                        }
                    }
                }
            }
        }
        catch
        {
            // Table or column may not exist or error
        }

        long nextNum = maxNum + 1;
        string padded = nextNum.ToString().PadLeft(digitCount, '0');
        return $"{prefix}{padded}{suffix}";
    }

    [HttpGet("slip-configs")]
    public IActionResult GetSlipConfigs()
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            var result = new List<SlipConfigDef>();

            foreach (var def in SlipDefinitions)
            {
                var item = new SlipConfigDef
                {
                    Key = def.Key,
                    Name = def.Name,
                    Source = def.Source,
                    SourceId = def.SourceId,
                    TableName = def.TableName,
                    ColumnName = def.ColumnName
                };

                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT NOTEMPLATE FROM {def.Source} WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", def.SourceId);
                    var tplObj = cmd.ExecuteScalar();
                    item.Template = tplObj != null && tplObj != DBNull.Value ? tplObj.ToString()?.Trim() ?? "" : "";
                }
                catch
                {
                    item.Template = "";
                }

                if (!string.IsNullOrEmpty(item.Template))
                {
                    item.Sample = GenerateSlipNumberCore(conn, item.Template, def.TableName, def.ColumnName);
                }
                else
                {
                    item.Sample = "";
                }

                result.Add(item);
            }

            return Ok(new { success = true, items = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi tải cấu hình số phiếu: " + ex.Message });
        }
    }

    [HttpPost("slip-configs")]
    public IActionResult UpdateSlipConfigs([FromBody] UpdateSlipConfigsRequest req)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            foreach (var item in req.Items)
            {
                var def = SlipDefinitions.FirstOrDefault(d => 
                    string.Equals(d.Key, item.Key, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(item.SourceId) && string.Equals(d.SourceId, item.SourceId, StringComparison.OrdinalIgnoreCase)));

                if (def == null) continue;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    UPDATE {def.Source} 
                    SET NOTEMPLATE = @tpl,
                        TIMEMODIFIED = CURRENT_TIMESTAMP,
                        USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569'
                    WHERE ID = @id";
                cmd.Parameters.AddWithValue("@tpl", item.Template ?? "");
                cmd.Parameters.AddWithValue("@id", def.SourceId);
                cmd.ExecuteNonQuery();
            }

            return Ok(new { success = true, message = "Cập nhật cấu hình số phiếu thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi lưu cấu hình số phiếu: " + ex.Message });
        }
    }

    [HttpGet("generate-slip-number")]
    public IActionResult GenerateSlipNumber([FromQuery] string tabId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tabId))
                return BadRequest(new { success = false, message = "tabId không hợp lệ" });

            // Normalize tabId: e.g. "baoGia" -> "baogia", "datHang" -> "dathang", "giaHanThe" -> "giahanthe"
            string normalized = tabId.Trim().ToLower().Replace("_", "").Replace("-", "");

            var def = SlipDefinitions.FirstOrDefault(d => 
                string.Equals(d.Key, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.TableName, normalized, StringComparison.OrdinalIgnoreCase));

            if (def == null)
            {
                // Fallback prefix
                return Ok(new { success = true, tabId, soPhiu = "", template = "" });
            }

            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            string template = "";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT NOTEMPLATE FROM {def.Source} WHERE ID = @id";
                cmd.Parameters.AddWithValue("@id", def.SourceId);
                var tplObj = cmd.ExecuteScalar();
                if (tplObj != null && tplObj != DBNull.Value)
                    template = tplObj.ToString()?.Trim() ?? "";
            }

            string soPhiu = GenerateSlipNumberCore(conn, template, def.TableName, def.ColumnName);

            return Ok(new { success = true, tabId, template, soPhiu });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi sinh số phiếu: " + ex.Message });
        }
    }
}

public class SlipConfigDef
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Source { get; set; } = "SFORM";
    public string SourceId { get; set; } = "";
    public string TableName { get; set; } = "";
    public string ColumnName { get; set; } = "NAME";
    public string? Template { get; set; }
    public string? Sample { get; set; }
}

public class UpdateSlipConfigsRequest
{
    public List<UpdateSlipConfigItem> Items { get; set; } = new();
}

public class UpdateSlipConfigItem
{
    public string Key { get; set; } = "";
    public string? Source { get; set; }
    public string? SourceId { get; set; }
    public string Template { get; set; } = "";
}

public class UpdateSystemConfigRequest
{
    public Dictionary<string, string> Configs { get; set; } = new();
}

public class TreeItemRequest
{
    public string? Mode { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Note { get; set; }
    public string? ParentId { get; set; }
    public int ItemType { get; set; } = 0;
    public string? SimageId { get; set; }
}

public class BatchTreeItemRequest
{
    public string? Mode { get; set; }
    public string? ParentId { get; set; }
    public List<string> Names { get; set; } = new();
}

using Microsoft.AspNetCore.Mvc;
using FirebirdSql.Data.FirebirdClient;
using System.Data;

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
                        else if (counts.ContainsKey(dTrangThaiId))
                        {
                            counts[dTrangThaiId] += cnt;
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
                        lt.NAME AS TEN_LOAI_THE
                    FROM TGIAHANTHE g
                    LEFT JOIN DKHACHHANG k ON g.DKHACHHANGID = k.ID
                    LEFT JOIN DLOAITHE lt ON g.DLOAITHEID = lt.ID
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
                        loaiThe = r["TEN_LOAI_THE"]?.ToString()?.Trim() ?? ""
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

            // 12. Tab Phiếu thu công nợ (TTHUCHI: LAPHIEUTHUCONGNO = 1)
            var thuCongNo = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        tc.ID, tc.NAME AS SO_PHIEU, tc.NGAY, tc.THU, tc.TENDOITUONG,
                        tc.DIENGIAI, tc.CHUNGTUGOC, nv.NAME AS TEN_NHANVIEN, tc.NOTE
                    FROM TTHUCHI tc
                    LEFT JOIN DNHANVIEN nv ON tc.DNHANVIENID = nv.ID
                    WHERE tc.DKHACHHANGID = @id AND tc.LAPHIEUTHUCONGNO = 1
                    ORDER BY tc.NGAY DESC, tc.TIMECREATED DESC";
                cmd.Parameters.AddWithValue("@id", id.Trim());
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    thuCongNo.Add(new
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
                    tinhThanh
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
}
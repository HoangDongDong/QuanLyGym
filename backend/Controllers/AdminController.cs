using Microsoft.AspNetCore.Mvc;
using FirebirdSql.Data.FirebirdClient;

namespace GymManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IConfiguration _config;

    public AdminController(IConfiguration config)
    {
        _config = config;
    }

    private string GetConnStr() => _config.GetConnectionString("FirebirdConnection") ?? "";

    // ==========================================
    // 1. LỊCH SỬ TƯƠNG TÁC HỆ THỐNG (STRACKING)
    // ==========================================
    [HttpGet("audit-logs")]
    public IActionResult GetAuditLogs([FromQuery] string? search = "", [FromQuery] int limit = 100)
    {
        var logs = new List<object>();
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();
            using var cmd = conn.CreateCommand();

            var sql = $"SELECT FIRST {limit} ID, TAIKHOAN, DOITUONG, NAME, NOTE, NGAY, GIO, IP, TIMECREATED FROM STRACKING";
            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " WHERE UPPER(TAIKHOAN) LIKE @search OR UPPER(DOITUONG) LIKE @search OR UPPER(NAME) LIKE @search OR UPPER(NOTE) LIKE @search";
                cmd.Parameters.AddWithValue("@search", $"%{search.Trim().ToUpper()}%");
            }
            sql += " ORDER BY TIMECREATED DESC";

            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var ngay = reader["NGAY"] is not DBNull ? Convert.ToDateTime(reader["NGAY"]).ToString("dd/MM/yyyy") : "";
                var gio = reader["GIO"]?.ToString()?.Trim() ?? "";
                var timeCreated = reader["TIMECREATED"] is not DBNull ? Convert.ToDateTime(reader["TIMECREATED"]).ToString("dd/MM/yyyy HH:mm:ss") : "";

                logs.Add(new
                {
                    id = reader["ID"]?.ToString()?.Trim(),
                    taiKhoan = reader["TAIKHOAN"]?.ToString()?.Trim() ?? "Hệ thống",
                    doiTuong = reader["DOITUONG"]?.ToString()?.Trim() ?? "",
                    hanhDong = reader["NAME"]?.ToString()?.Trim() ?? "",
                    ghiChu = reader["NOTE"]?.ToString()?.Trim() ?? "",
                    ngay = ngay,
                    gio = gio,
                    ip = reader["IP"]?.ToString()?.Trim() ?? "127.0.0.1",
                    thoiGian = !string.IsNullOrEmpty(timeCreated) ? timeCreated : $"{ngay} {gio}".Trim()
                });
            }

            return Ok(new { success = true, count = logs.Count, data = logs });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi đọc lịch sử tương tác: " + ex.Message });
        }
    }

    // ==========================================
    // 2. NGƯỜI DÙNG VÀ PHÂN QUYỀN (SUSER & SGROUPUSER)
    // ==========================================
    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        var users = new List<object>();
        var groups = new List<object>();

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            // Lấy danh sách nhóm quyền (SGROUPUSER)
            using (var cmdGroup = conn.CreateCommand())
            {
                cmdGroup.CommandText = "SELECT ID, NAME, NOTE FROM SGROUPUSER WHERE STATUS <> -1 OR STATUS IS NULL ORDER BY NAME";
                using var grpReader = cmdGroup.ExecuteReader();
                while (grpReader.Read())
                {
                    groups.Add(new
                    {
                        id = grpReader["ID"]?.ToString()?.Trim(),
                        name = grpReader["NAME"]?.ToString()?.Trim(),
                        note = grpReader["NOTE"]?.ToString()?.Trim()
                    });
                }
            }

            // Lấy danh sách tài khoản (SUSER)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT u.ID, u.USERNAME, u.NAME, u.EMAIL, u.ISADMIN, u.STATUS, 
                           u.TIMECREATED, g.NAME AS GROUPNAME, u.SGROUPUSERID
                    FROM SUSER u
                    LEFT JOIN SGROUPUSER g ON u.SGROUPUSERID = g.ID
                    WHERE u.STATUS <> -1 OR u.STATUS IS NULL
                    ORDER BY u.ISADMIN DESC, u.USERNAME ASC";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var isAdmin = reader["ISADMIN"] is not DBNull && Convert.ToInt32(reader["ISADMIN"]) == 1;
                    var status = reader["STATUS"] is not DBNull ? Convert.ToInt32(reader["STATUS"]) : 1;

                    users.Add(new
                    {
                        id = reader["ID"]?.ToString()?.Trim(),
                        username = reader["USERNAME"]?.ToString()?.Trim(),
                        fullName = reader["NAME"]?.ToString()?.Trim() ?? "Chưa đặt tên",
                        email = reader["EMAIL"]?.ToString()?.Trim() ?? "---",
                        isAdmin = isAdmin,
                        role = isAdmin ? "Admin (Quản trị tối cao)" : "Staff (Nhân viên vận hành)",
                        status = status,
                        groupName = reader["GROUPNAME"]?.ToString()?.Trim() ?? (isAdmin ? "Ban Quản Trị" : "Nhân viên"),
                        groupId = reader["SGROUPUSERID"]?.ToString()?.Trim(),
                        timeCreated = reader["TIMECREATED"] is not DBNull ? Convert.ToDateTime(reader["TIMECREATED"]).ToString("dd/MM/yyyy HH:mm") : "---"
                    });
                }
            }

            return Ok(new { success = true, users, groups });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi đọc danh sách người dùng: " + ex.Message });
        }
    }

    public class SaveUserFullRequest
    {
        public string? Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
        public string? GroupId { get; set; }
        public string? EmployeeId { get; set; }
        public List<string>? StoreIds { get; set; }
    }

    public class CreateEmployeeRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? DiaChi { get; set; }
        public string? DienThoai { get; set; }
        public string? Note { get; set; }
        public string? SImageId { get; set; }
        public int CachTinhLuong { get; set; } = 30; // 30 = Lương theo ca, 60 = Lương tháng theo ca, 0 = Lương tháng theo ngày
        public decimal LuongCa { get; set; }
        public decimal LuongThang { get; set; }
        public bool NghiThu7 { get; set; }
        public bool NghiChuNhat { get; set; }
    }

    [HttpGet("users/metadata")]
    public IActionResult GetUserMetadata()
    {
        var employees = new List<object>();
        var stores = new List<object>();
        var groups = new List<object>();
        var images = new List<object>();

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            // 1. DNHANVIEN
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM DNHANVIEN WHERE STATUS <> -1 OR STATUS IS NULL ORDER BY NAME";
                using var r = cmd.ExecuteReader();
                var cols = Enumerable.Range(0, r.FieldCount).Select(i => r.GetName(i)).ToList();
                while (r.Read())
                {
                    employees.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim() ?? "",
                        name = cols.Contains("NAME") ? (r["NAME"]?.ToString()?.Trim() ?? "") : "",
                        diaChi = cols.Contains("DIACHI") ? (r["DIACHI"]?.ToString()?.Trim() ?? "") : "",
                        dienThoai = cols.Contains("DIENTHOAI") ? (r["DIENTHOAI"]?.ToString()?.Trim() ?? "") : "",
                        note = cols.Contains("NOTE") ? (r["NOTE"]?.ToString()?.Trim() ?? "") : "",
                        sImageId = cols.Contains("SIMAGEID") ? (r["SIMAGEID"]?.ToString()?.Trim() ?? "") : "",
                        cachTinhLuong = cols.Contains("CACHTINHLUONG") && r["CACHTINHLUONG"] is not DBNull ? Convert.ToInt32(r["CACHTINHLUONG"]) : 30,
                        luongCa = cols.Contains("LUONGCA") && r["LUONGCA"] is not DBNull ? Convert.ToDecimal(r["LUONGCA"]) : 0,
                        luongThang = cols.Contains("LUONGTHANG") && r["LUONGTHANG"] is not DBNull ? Convert.ToDecimal(r["LUONGTHANG"]) : 0,
                        nghiThu7 = cols.Contains("NGHITHU7") && r["NGHITHU7"] is not DBNull && Convert.ToInt32(r["NGHITHU7"]) > 0,
                        nghiChuNhat = cols.Contains("NGHICHUNHAT") && r["NGHICHUNHAT"] is not DBNull && Convert.ToInt32(r["NGHICHUNHAT"]) > 0
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> Lỗi đọc DNHANVIEN: " + ex.Message);
            }

            // 2. DCUAHANG
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM DCUAHANG WHERE STATUS <> -1 OR STATUS IS NULL ORDER BY NAME";
                using var r = cmd.ExecuteReader();
                var cols = Enumerable.Range(0, r.FieldCount).Select(i => r.GetName(i)).ToList();
                while (r.Read())
                {
                    stores.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim() ?? "",
                        name = cols.Contains("NAME") ? (r["NAME"]?.ToString()?.Trim() ?? "") : "",
                        code = cols.Contains("MA") ? (r["MA"]?.ToString()?.Trim() ?? "") : (cols.Contains("CODE") ? (r["CODE"]?.ToString()?.Trim() ?? "") : "")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> Lỗi đọc DCUAHANG: " + ex.Message);
            }

            // 3. SGROUPUSER
            try
            {
                using var cmdGroup = conn.CreateCommand();
                cmdGroup.CommandText = "SELECT ID, NAME, NOTE, SIMAGEID FROM SGROUPUSER WHERE STATUS <> -1 OR STATUS IS NULL ORDER BY NAME";
                using var grpReader = cmdGroup.ExecuteReader();
                while (grpReader.Read())
                {
                    groups.Add(new
                    {
                        id = grpReader["ID"]?.ToString()?.Trim() ?? "",
                        name = grpReader["NAME"]?.ToString()?.Trim() ?? "",
                        note = grpReader["NOTE"]?.ToString()?.Trim() ?? "",
                        sImageId = grpReader["SIMAGEID"]?.ToString()?.Trim() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> Lỗi đọc SGROUPUSER: " + ex.Message);
            }

            // 4. SIMAGE
            try
            {
                using var cmdImg = conn.CreateCommand();
                cmdImg.CommandText = "SELECT * FROM SIMAGE WHERE STATUS <> -1 OR STATUS IS NULL";
                using var rImg = cmdImg.ExecuteReader();
                var imgCols = Enumerable.Range(0, rImg.FieldCount).Select(i => rImg.GetName(i)).ToList();
                while (rImg.Read())
                {
                    string id = rImg["ID"]?.ToString()?.Trim() ?? "";
                    string name = imgCols.Contains("NAME") ? (rImg["NAME"]?.ToString()?.Trim() ?? "") : "";
                    string base64Data = "";
                    foreach (var col in new[] { "PHOTO", "IMAGE", "DATA", "PICTURE", "BLOBVALUE" })
                    {
                        if (imgCols.Contains(col) && rImg[col] is byte[] b && b.Length > 0)
                        {
                            base64Data = Convert.ToBase64String(b);
                            break;
                        }
                    }
                    images.Add(new { id, name, data = base64Data });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> Lỗi đọc SIMAGE: " + ex.Message);
            }

            return Ok(new { success = true, employees, stores, groups, images });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi nạp danh mục: " + ex.Message });
        }
    }

    [HttpGet("users/{id}/detail")]
    public IActionResult GetUserDetail(string id)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            object? user = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM SUSER WHERE ID = @id";
                cmd.Parameters.AddWithValue("@id", id);
                using var r = cmd.ExecuteReader();
                var cols = Enumerable.Range(0, r.FieldCount).Select(i => r.GetName(i)).ToList();
                if (r.Read())
                {
                    user = new
                    {
                        id = r["ID"]?.ToString()?.Trim() ?? "",
                        username = r["USERNAME"]?.ToString()?.Trim() ?? "",
                        fullName = cols.Contains("NAME") ? (r["NAME"]?.ToString()?.Trim() ?? "") : "",
                        email = cols.Contains("EMAIL") ? (r["EMAIL"]?.ToString()?.Trim() ?? "") : "",
                        groupId = cols.Contains("SGROUPUSERID") ? (r["SGROUPUSERID"]?.ToString()?.Trim() ?? "") : "",
                        employeeId = cols.Contains("DNHANVIENID") ? (r["DNHANVIENID"]?.ToString()?.Trim() ?? "") : "",
                        isAdmin = cols.Contains("ISADMIN") && r["ISADMIN"] is not DBNull && Convert.ToInt32(r["ISADMIN"]) == 1
                    };
                }
            }

            var storeAccess = new List<object>();
            try
            {
                using var cmdStore = conn.CreateCommand();
                cmdStore.CommandText = @"
                    SELECT c.ID, c.NAME, 
                           CASE WHEN t.ID IS NULL THEN 0 ELSE 30 END AS TRUYCAP
                    FROM DCUAHANG c
                    LEFT JOIN TNGUOIDUNGTHEOCUAHANG t ON c.ID = t.DCUAHANGID AND t.SUSERID = @id
                    WHERE c.STATUS <> -1 OR c.STATUS IS NULL
                    ORDER BY c.NAME";
                cmdStore.Parameters.AddWithValue("@id", id);
                using var rStore = cmdStore.ExecuteReader();
                while (rStore.Read())
                {
                    storeAccess.Add(new
                    {
                        id = rStore["ID"]?.ToString()?.Trim() ?? "",
                        name = rStore["NAME"]?.ToString()?.Trim() ?? "",
                        truyCap = Convert.ToInt32(rStore["TRUYCAP"]) == 30
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> Lỗi đọc store access: " + ex.Message);
            }

            return Ok(new { success = true, user, storeAccess });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi đọc chi tiết người dùng: " + ex.Message });
        }
    }

    [HttpPost("users")]
    public IActionResult CreateUser([FromBody] SaveUserFullRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
        {
            return BadRequest(new { success = false, message = "Tên tài khoản không được để trống!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            // Kiểm tra trùng username
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM SUSER WHERE UPPER(USERNAME) = @u AND (STATUS <> -1 OR STATUS IS NULL)";
                checkCmd.Parameters.AddWithValue("@u", req.Username.Trim().ToUpper());
                var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    return BadRequest(new { success = false, message = $"Tài khoản '{req.Username}' đã tồn tại trong hệ thống!" });
                }
            }

            // Lấy danh sách cột của SUSER để insert chính xác
            var suserCols = new List<string>();
            using (var colCmd = conn.CreateCommand())
            {
                colCmd.CommandText = "SELECT FIRST 1 * FROM SUSER";
                using var cr = colCmd.ExecuteReader();
                suserCols = Enumerable.Range(0, cr.FieldCount).Select(i => cr.GetName(i).ToUpper()).ToList();
            }

            var newUserId = Guid.NewGuid().ToString();
            using (var insertCmd = conn.CreateCommand())
            {
                var fields = new List<string> { "ID", "USERNAME", "STATUS", "TIMECREATED", "USERCREATEDID" };
                var vals = new List<string> { "@id", "@username", "1", "CURRENT_TIMESTAMP", "'4f1466a0-0756-4ba9-afa8-053b96ca7569'" };

                if (suserCols.Contains("TIMEMODIFIED"))
                {
                    fields.Add("TIMEMODIFIED");
                    vals.Add("CURRENT_TIMESTAMP");
                }
                if (suserCols.Contains("USERMODIFIEDID"))
                {
                    fields.Add("USERMODIFIEDID");
                    vals.Add("'4f1466a0-0756-4ba9-afa8-053b96ca7569'");
                }

                insertCmd.Parameters.AddWithValue("@id", newUserId);
                insertCmd.Parameters.AddWithValue("@username", req.Username.Trim());

                if (suserCols.Contains("NAME"))
                {
                    fields.Add("NAME");
                    vals.Add("@name");
                    insertCmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(req.FullName) ? req.Username.Trim() : req.FullName.Trim());
                }
                if (suserCols.Contains("PASSWORD"))
                {
                    fields.Add("PASSWORD");
                    vals.Add("@password");
                    insertCmd.Parameters.AddWithValue("@password", req.Password?.Trim() ?? "");
                }
                if (suserCols.Contains("EMAIL"))
                {
                    fields.Add("EMAIL");
                    vals.Add("@email");
                    insertCmd.Parameters.AddWithValue("@email", (object?)req.Email?.Trim() ?? DBNull.Value);
                }
                if (suserCols.Contains("ISADMIN"))
                {
                    fields.Add("ISADMIN");
                    vals.Add("@isAdmin");
                    insertCmd.Parameters.AddWithValue("@isAdmin", req.IsAdmin ? 1 : 0);
                }
                if (suserCols.Contains("SGROUPUSERID"))
                {
                    fields.Add("SGROUPUSERID");
                    vals.Add("@groupId");
                    insertCmd.Parameters.AddWithValue("@groupId", (object?)req.GroupId ?? DBNull.Value);
                }
                if (suserCols.Contains("DNHANVIENID"))
                {
                    fields.Add("DNHANVIENID");
                    vals.Add("@nhanVienId");
                    insertCmd.Parameters.AddWithValue("@nhanVienId", (object?)req.EmployeeId ?? DBNull.Value);
                }

                insertCmd.CommandText = $"INSERT INTO SUSER ({string.Join(", ", fields)}) VALUES ({string.Join(", ", vals)})";
                insertCmd.ExecuteNonQuery();
            }

            // Lưu danh sách phân quyền cửa hàng (TNGUOIDUNGTHEOCUAHANG)
            SaveStoreAccess(conn, newUserId, req.StoreIds);

            // Ghi nhật ký tương tác
            GhiNhatKy(conn, "Người dùng", $"Tạo mới tài khoản '{req.Username}' ({ (req.IsAdmin ? "Admin" : "Nhân viên") })");

            return Ok(new { success = true, message = $"Đã tạo thành công tài khoản {req.Username}!", id = newUserId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi tạo người dùng: " + ex.Message });
        }
    }

    [HttpPut("users/{id}")]
    public IActionResult UpdateUser(string id, [FromBody] SaveUserFullRequest req)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            var suserCols = new List<string>();
            using (var colCmd = conn.CreateCommand())
            {
                colCmd.CommandText = "SELECT FIRST 1 * FROM SUSER";
                using var cr = colCmd.ExecuteReader();
                suserCols = Enumerable.Range(0, cr.FieldCount).Select(i => cr.GetName(i).ToUpper()).ToList();
            }

            using (var updateCmd = conn.CreateCommand())
            {
                var sets = new List<string>();
                if (suserCols.Contains("NAME"))
                {
                    sets.Add("NAME = @name");
                    updateCmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(req.FullName) ? req.Username.Trim() : req.FullName.Trim());
                }
                if (suserCols.Contains("EMAIL"))
                {
                    sets.Add("EMAIL = @email");
                    updateCmd.Parameters.AddWithValue("@email", (object?)req.Email?.Trim() ?? DBNull.Value);
                }
                if (suserCols.Contains("SGROUPUSERID"))
                {
                    sets.Add("SGROUPUSERID = @groupId");
                    updateCmd.Parameters.AddWithValue("@groupId", (object?)req.GroupId ?? DBNull.Value);
                }
                if (suserCols.Contains("DNHANVIENID"))
                {
                    sets.Add("DNHANVIENID = @nhanVienId");
                    updateCmd.Parameters.AddWithValue("@nhanVienId", (object?)req.EmployeeId ?? DBNull.Value);
                }
                if (suserCols.Contains("ISADMIN"))
                {
                    sets.Add("ISADMIN = @isAdmin");
                    updateCmd.Parameters.AddWithValue("@isAdmin", req.IsAdmin ? 1 : 0);
                }
                if (!string.IsNullOrWhiteSpace(req.Password) && suserCols.Contains("PASSWORD"))
                {
                    sets.Add("PASSWORD = @password");
                    updateCmd.Parameters.AddWithValue("@password", req.Password.Trim());
                }
                if (suserCols.Contains("TIMEMODIFIED"))
                {
                    sets.Add("TIMEMODIFIED = CURRENT_TIMESTAMP");
                }
                if (suserCols.Contains("USERMODIFIEDID"))
                {
                    sets.Add("USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569'");
                }

                if (sets.Count > 0)
                {
                    updateCmd.CommandText = $"UPDATE SUSER SET {string.Join(", ", sets)} WHERE ID = @id";
                    updateCmd.Parameters.AddWithValue("@id", id);
                    updateCmd.ExecuteNonQuery();
                }
            }

            // Cập nhật phân quyền cửa hàng
            SaveStoreAccess(conn, id, req.StoreIds);

            GhiNhatKy(conn, "Người dùng", $"Cập nhật thông tin tài khoản '{req.Username}'");

            return Ok(new { success = true, message = $"Đã cập nhật thành công tài khoản {req.Username}!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi cập nhật người dùng: " + ex.Message });
        }
    }

    private void SaveStoreAccess(FbConnection conn, string userId, List<string>? storeIds)
    {
        if (storeIds == null) return;

        try
        {
            // Kiểm tra bảng TNGUOIDUNGTHEOCUAHANG
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT FIRST 1 * FROM TNGUOIDUNGTHEOCUAHANG";
            using var cr = checkCmd.ExecuteReader();
            var cols = Enumerable.Range(0, cr.FieldCount).Select(i => cr.GetName(i).ToUpper()).ToList();

            // Xóa quyền cũ
            using var delCmd = conn.CreateCommand();
            delCmd.CommandText = "DELETE FROM TNGUOIDUNGTHEOCUAHANG WHERE SUSERID = @uid";
            delCmd.Parameters.AddWithValue("@uid", userId);
            delCmd.ExecuteNonQuery();

            // Thêm quyền các cửa hàng được chọn
            foreach (var storeId in storeIds)
            {
                if (string.IsNullOrWhiteSpace(storeId)) continue;
                using var insCmd = conn.CreateCommand();
                var fields = new List<string> { "ID", "SUSERID", "DCUAHANGID" };
                var vals = new List<string> { "@id", "@uid", "@cid" };

                insCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                insCmd.Parameters.AddWithValue("@uid", userId);
                insCmd.Parameters.AddWithValue("@cid", storeId.Trim());

                if (cols.Contains("STATUS"))
                {
                    fields.Add("STATUS");
                    vals.Add("30");
                }
                if (cols.Contains("TIMECREATED"))
                {
                    fields.Add("TIMECREATED");
                    vals.Add("CURRENT_TIMESTAMP");
                }
                if (cols.Contains("TIMEMODIFIED"))
                {
                    fields.Add("TIMEMODIFIED");
                    vals.Add("CURRENT_TIMESTAMP");
                }
                if (cols.Contains("USERCREATEDID"))
                {
                    fields.Add("USERCREATEDID");
                    vals.Add("'4f1466a0-0756-4ba9-afa8-053b96ca7569'");
                }
                if (cols.Contains("USERMODIFIEDID"))
                {
                    fields.Add("USERMODIFIEDID");
                    vals.Add("'4f1466a0-0756-4ba9-afa8-053b96ca7569'");
                }

                insCmd.CommandText = $"INSERT INTO TNGUOIDUNGTHEOCUAHANG ({string.Join(", ", fields)}) VALUES ({string.Join(", ", vals)})";
                insCmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(">>> Lỗi lưu TNGUOIDUNGTHEOCUAHANG: " + ex.Message);
        }
    }

    [HttpPost("employees")]
    public IActionResult CreateEmployee([FromBody] CreateEmployeeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(new { success = false, message = "Tên nhân viên không được để trống!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            // Lấy danh sách cột của DNHANVIEN
            var empCols = new List<string>();
            using (var colCmd = conn.CreateCommand())
            {
                colCmd.CommandText = "SELECT FIRST 1 * FROM DNHANVIEN";
                using var cr = colCmd.ExecuteReader();
                empCols = Enumerable.Range(0, cr.FieldCount).Select(i => cr.GetName(i).ToUpper()).ToList();
            }

            var newEmpId = Guid.NewGuid().ToString();
            using (var insertCmd = conn.CreateCommand())
            {
                var fields = new List<string> { "ID", "STATUS", "TIMECREATED", "USERCREATEDID" };
                var vals = new List<string> { "@id", "1", "CURRENT_TIMESTAMP", "'4f1466a0-0756-4ba9-afa8-053b96ca7569'" };

                if (empCols.Contains("TIMEMODIFIED"))
                {
                    fields.Add("TIMEMODIFIED");
                    vals.Add("CURRENT_TIMESTAMP");
                }
                if (empCols.Contains("USERMODIFIEDID"))
                {
                    fields.Add("USERMODIFIEDID");
                    vals.Add("'4f1466a0-0756-4ba9-afa8-053b96ca7569'");
                }

                insertCmd.Parameters.AddWithValue("@id", newEmpId);

                if (empCols.Contains("NAME"))
                {
                    fields.Add("NAME");
                    vals.Add("@name");
                    insertCmd.Parameters.AddWithValue("@name", req.Name.Trim());
                }
                if (empCols.Contains("DIACHI"))
                {
                    fields.Add("DIACHI");
                    vals.Add("@diachi");
                    insertCmd.Parameters.AddWithValue("@diachi", (object?)req.DiaChi?.Trim() ?? DBNull.Value);
                }
                if (empCols.Contains("DIENTHOAI"))
                {
                    fields.Add("DIENTHOAI");
                    vals.Add("@dienthoai");
                    insertCmd.Parameters.AddWithValue("@dienthoai", (object?)req.DienThoai?.Trim() ?? DBNull.Value);
                }
                if (empCols.Contains("NOTE"))
                {
                    fields.Add("NOTE");
                    vals.Add("@note");
                    insertCmd.Parameters.AddWithValue("@note", (object?)req.Note?.Trim() ?? DBNull.Value);
                }
                if (empCols.Contains("SIMAGEID"))
                {
                    fields.Add("SIMAGEID");
                    vals.Add("@sImageId");
                    insertCmd.Parameters.AddWithValue("@sImageId", (object?)req.SImageId ?? DBNull.Value);
                }
                if (empCols.Contains("CACHTINHLUONG"))
                {
                    fields.Add("CACHTINHLUONG");
                    vals.Add("@cachTinhLuong");
                    insertCmd.Parameters.AddWithValue("@cachTinhLuong", req.CachTinhLuong);
                }
                if (empCols.Contains("LUONGCA"))
                {
                    fields.Add("LUONGCA");
                    vals.Add("@luongCa");
                    insertCmd.Parameters.AddWithValue("@luongCa", req.LuongCa);
                }
                if (empCols.Contains("LUONGTHANG"))
                {
                    fields.Add("LUONGTHANG");
                    vals.Add("@luongThang");
                    insertCmd.Parameters.AddWithValue("@luongThang", req.LuongThang);
                }
                if (empCols.Contains("NGHITHU7"))
                {
                    fields.Add("NGHITHU7");
                    vals.Add("@nghiThu7");
                    insertCmd.Parameters.AddWithValue("@nghiThu7", req.NghiThu7 ? 30 : 0);
                }
                if (empCols.Contains("NGHICHUNHAT"))
                {
                    fields.Add("NGHICHUNHAT");
                    vals.Add("@nghiChuNhat");
                    insertCmd.Parameters.AddWithValue("@nghiChuNhat", req.NghiChuNhat ? 30 : 0);
                }

                insertCmd.CommandText = $"INSERT INTO DNHANVIEN ({string.Join(", ", fields)}) VALUES ({string.Join(", ", vals)})";
                insertCmd.ExecuteNonQuery();
            }

            GhiNhatKy(conn, "Nhân viên", $"Thêm mới nhân viên '{req.Name}' vào hệ thống");

            return Ok(new
            {
                success = true,
                message = $"Đã thêm nhân viên {req.Name} thành công!",
                employee = new
                {
                    id = newEmpId,
                    name = req.Name.Trim(),
                    diaChi = req.DiaChi?.Trim() ?? "",
                    dienThoai = req.DienThoai?.Trim() ?? "",
                    note = req.Note?.Trim() ?? "",
                    sImageId = req.SImageId ?? "",
                    cachTinhLuong = req.CachTinhLuong,
                    luongCa = req.LuongCa,
                    luongThang = req.LuongThang,
                    nghiThu7 = req.NghiThu7,
                    nghiChuNhat = req.NghiChuNhat
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi thêm nhân viên: " + ex.Message });
        }
    }

    [HttpDelete("users/{id}")]
    public IActionResult DeleteUser(string id)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            // Không cho xóa tài khoản Admin gốc
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT USERNAME FROM SUSER WHERE ID = @id";
                checkCmd.Parameters.AddWithValue("@id", id);
                var username = checkCmd.ExecuteScalar()?.ToString()?.Trim();
                if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "Không được phép xóa tài khoản Administrator hệ thống!" });
                }

                var suserCols = new List<string>();
                using (var colCmd = conn.CreateCommand())
                {
                    colCmd.CommandText = "SELECT FIRST 1 * FROM SUSER";
                    using var cr = colCmd.ExecuteReader();
                    suserCols = Enumerable.Range(0, cr.FieldCount).Select(i => cr.GetName(i).ToUpper()).ToList();
                }

                var setClause = "STATUS = -1";
                if (suserCols.Contains("TIMEMODIFIED")) setClause += ", TIMEMODIFIED = CURRENT_TIMESTAMP";
                if (suserCols.Contains("USERMODIFIEDID")) setClause += ", USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569'";

                using var delCmd = conn.CreateCommand();
                delCmd.CommandText = $"UPDATE SUSER SET {setClause} WHERE ID = @id";
                delCmd.Parameters.AddWithValue("@id", id);
                delCmd.ExecuteNonQuery();

                GhiNhatKy(conn, "Người dùng", $"Xóa tài khoản '{username}' khỏi hệ thống");
            }

            return Ok(new { success = true, message = "Đã vô hiệu hóa tài khoản thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi xóa người dùng: " + ex.Message });
        }
    }

    // ==========================================
    // 3. CẤU HÌNH TOÀN HỆ THỐNG (SCONFIG & SCONFIGGROUP)
    // ==========================================
    [HttpGet("configs")]
    public IActionResult GetConfigs()
    {
        var result = new List<object>();

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            // 1. Đọc tất cả các nhóm cấu hình (SCONFIGGROUP), loại bỏ nhóm 'Không hiển thị'
            var groups = new List<(string id, string name, string sortOrder)>();
            using (var cmdGrp = conn.CreateCommand())
            {
                cmdGrp.CommandText = @"
                    SELECT ID, NAME, SORTORDER 
                    FROM SCONFIGGROUP 
                    WHERE (STATUS <> -1 OR STATUS IS NULL) 
                      AND (NAME <> 'Không hiển thị')
                    ORDER BY SORTORDER, NAME";
                using var r = cmdGrp.ExecuteReader();
                while (r.Read())
                {
                    groups.Add((
                        r["ID"]?.ToString()?.Trim() ?? "",
                        r["NAME"]?.ToString()?.Trim() ?? "",
                        r["SORTORDER"]?.ToString()?.Trim() ?? ""
                    ));
                }
            }

            // 2. Đọc tất cả tham số SCONFIG
            var configMap = new Dictionary<string, List<object>>();
            foreach (var g in groups)
            {
                configMap[g.id] = new List<object>();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT ID, NAME, CAPTION, CONTROLTYPE, DATATYPE, OTHERCONFIG, 
                           TEXTVALUE, INTVALUE, DECIMALVALUE, DATETIMEVALUE, BLOBVALUE,
                           MOREDETAIL, NOTE, SORTORDER, SCONFIGGROUPID, SOCOT, TAB
                    FROM SCONFIG
                    WHERE (STATUS <> -1 OR STATUS IS NULL)
                    ORDER BY SORTORDER, NAME";

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var gId = r["SCONFIGGROUPID"]?.ToString()?.Trim() ?? "";
                    if (!configMap.ContainsKey(gId)) continue;

                    var cType = r["CONTROLTYPE"] is not DBNull ? Convert.ToInt32(r["CONTROLTYPE"]) : 5;
                    var dType = r["DATATYPE"] is not DBNull ? Convert.ToInt32(r["DATATYPE"]) : 1;
                    var intVal = r["INTVALUE"] is not DBNull ? Convert.ToInt32(r["INTVALUE"]) : (int?)null;
                    var decVal = r["DECIMALVALUE"] is not DBNull ? Convert.ToDecimal(r["DECIMALVALUE"]) : (decimal?)null;
                    var dateVal = r["DATETIMEVALUE"] is not DBNull ? Convert.ToDateTime(r["DATETIMEVALUE"]).ToString("yyyy-MM-ddTHH:mm") : null;
                    var otherCfg = r["OTHERCONFIG"]?.ToString() ?? "";

                    string? blobBase64 = null;
                    if (r["BLOBVALUE"] is not DBNull)
                    {
                        try
                        {
                            var bytes = (byte[])r["BLOBVALUE"];
                            if (bytes != null && bytes.Length > 0)
                            {
                                blobBase64 = Convert.ToBase64String(bytes);
                            }
                        }
                        catch { }
                    }

                    configMap[gId].Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim(),
                        name = r["NAME"]?.ToString()?.Trim(),
                        caption = r["CAPTION"]?.ToString()?.Trim() ?? "",
                        controlType = cType,
                        dataType = dType,
                        otherConfig = otherCfg,
                        textValue = r["TEXTVALUE"]?.ToString() ?? "",
                        intValue = intVal,
                        decimalValue = decVal,
                        dateTimeValue = dateVal,
                        blobValue = blobBase64,
                        moreDetail = r["MOREDETAIL"]?.ToString()?.Trim() ?? "",
                        note = r["NOTE"]?.ToString()?.Trim() ?? "",
                        sortOrder = r["SORTORDER"]?.ToString()?.Trim() ?? "",
                        soCot = r["SOCOT"] is not DBNull ? Convert.ToInt32(r["SOCOT"]) : 1
                    });
                }
            }

            foreach (var g in groups)
            {
                result.Add(new
                {
                    groupId = g.id,
                    groupName = g.name,
                    sortOrder = g.sortOrder,
                    items = configMap[g.id]
                });
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi đọc cấu hình: " + ex.Message });
        }
    }

    public class ConfigUpdateItem
    {
        public string Id { get; set; } = string.Empty;
        public string? TextValue { get; set; }
        public int? IntValue { get; set; }
        public decimal? DecimalValue { get; set; }
        public string? DateTimeValue { get; set; }
        public string? BlobValue { get; set; }
    }

    public class UpdateConfigsRequest
    {
        public List<ConfigUpdateItem> Items { get; set; } = new();
    }

    [HttpPost("configs/sync-theme-options")]
    public IActionResult SyncThemeOptions()
    {
        try
        {
            string otherConfigSidebar = string.Join("\r\n", new[]
            {
                "Mẫu 1 (Xanh dương)",
                "Mẫu 2 (Tím)",
                "Mẫu 3 (Cam)",
                "Mẫu 4 (Xanh lá)",
                "Mẫu 5 (Đỏ/Hồng)",
                "Mẫu 6 (Xám/Trung tính)",
                "Mẫu 7 (Cyan)",
                "Mẫu 8 (Midnight)",
                "Office 2010 Blue",
                "Office 2010 Silver",
                "Office 2010 Black"
            });

            string otherConfigContent = string.Join("\r\n", new[]
            {
                "Mẫu 1 (Xanh dương)",
                "Mẫu 2 (Xanh mềm)",
                "Mẫu 3 (Tím)",
                "Mẫu 4 (Cam)",
                "Mẫu 5 (Xanh bạc hà)",
                "Mẫu 6 (Hồng phấn)",
                "Mẫu 7 (Cyan)",
                "Mẫu 8 (Trắng)",
                "Trắng",
                "Bạc",
                "Xám",
                "Xanh lá",
                "Xanh dương",
                "Đỏ",
                "Cam",
                "Hồng",
                "Tím",
                "Vàng"
            });

            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE SCONFIG 
                    SET OTHERCONFIG = @otherConfig
                    WHERE UPPER(NAME) = 'GIAODIENCHUONGTRINH'";
                cmd.Parameters.AddWithValue("@otherConfig", otherConfigSidebar);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE SCONFIG 
                    SET OTHERCONFIG = @otherConfig
                    WHERE UPPER(NAME) = 'MAUNENGIAODIENCHINH'";
                cmd.Parameters.AddWithValue("@otherConfig", otherConfigContent);
                cmd.ExecuteNonQuery();
            }

            // Đồng thời cập nhật vào database mẫu TEMPLATE.FDB nếu tồn tại
            string templateDbPath = @"D:\QuanLyPhongGym\backend\Data\TEMPLATE.FDB";
            if (System.IO.File.Exists(templateDbPath))
            {
                try
                {
                    string templateConnStr = $"User=SYSDBA;Password=masterkey;Database={templateDbPath};DataSource=localhost;Port=3050;Dialect=3;Charset=UTF8;";
                    using var tConn = new FbConnection(templateConnStr);
                    tConn.Open();

                    using (var tCmd = tConn.CreateCommand())
                    {
                        tCmd.CommandText = @"
                            UPDATE SCONFIG 
                            SET OTHERCONFIG = @otherConfig
                            WHERE UPPER(NAME) = 'GIAODIENCHUONGTRINH'";
                        tCmd.Parameters.AddWithValue("@otherConfig", otherConfigSidebar);
                        tCmd.ExecuteNonQuery();
                    }

                    using (var tCmd = tConn.CreateCommand())
                    {
                        tCmd.CommandText = @"
                            UPDATE SCONFIG 
                            SET OTHERCONFIG = @otherConfig
                            WHERE UPPER(NAME) = 'MAUNENGIAODIENCHINH'";
                        tCmd.Parameters.AddWithValue("@otherConfig", otherConfigContent);
                        tCmd.ExecuteNonQuery();
                    }
                }
                catch (Exception exTpl)
                {
                    Console.WriteLine("Cảnh báo cập nhật TEMPLATE.FDB: " + exTpl.Message);
                }
            }

            return Ok(new { success = true, message = "Đã cập nhật các mẫu nền vào bảng SCONFIG trong DATA.fdb và TEMPLATE.FDB!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi cập nhật SCONFIG: " + ex.Message });
        }
    }

    [HttpPost("configs/update")]
    public IActionResult UpdateConfigs([FromBody] UpdateConfigsRequest req)
    {
        if (req.Items == null || req.Items.Count == 0)
        {
            return BadRequest(new { success = false, message = "Không có giá trị cấu hình nào để cập nhật!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            foreach (var item in req.Items)
            {
                using var cmd = conn.CreateCommand();
                var updateBlobSql = item.BlobValue != null ? ", BLOBVALUE = @blob" : "";
                cmd.CommandText = $@"
                    UPDATE SCONFIG 
                    SET TEXTVALUE = @text,
                        INTVALUE = @int,
                        DECIMALVALUE = @dec,
                        DATETIMEVALUE = @date
                        {updateBlobSql},
                        TIMEMODIFIED = CURRENT_TIMESTAMP,
                        USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569'
                    WHERE ID = @id";

                cmd.Parameters.AddWithValue("@id", item.Id);
                cmd.Parameters.AddWithValue("@text", (object?)item.TextValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@int", (object?)item.IntValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dec", (object?)item.DecimalValue ?? DBNull.Value);

                if (!string.IsNullOrEmpty(item.DateTimeValue) && DateTime.TryParse(item.DateTimeValue, out var dt))
                {
                    cmd.Parameters.AddWithValue("@date", dt);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@date", DBNull.Value);
                }

                if (item.BlobValue != null)
                {
                    if (string.IsNullOrEmpty(item.BlobValue))
                    {
                        cmd.Parameters.AddWithValue("@blob", DBNull.Value);
                    }
                    else
                    {
                        try
                        {
                            var bytes = Convert.FromBase64String(item.BlobValue);
                            cmd.Parameters.AddWithValue("@blob", bytes);
                        }
                        catch
                        {
                            cmd.Parameters.AddWithValue("@blob", DBNull.Value);
                        }
                    }
                }

                cmd.ExecuteNonQuery();
            }

            GhiNhatKy(conn, "Cấu hình hệ thống", $"Lưu thiết lập {req.Items.Count} tham số cấu hình toàn bộ hệ thống (SCONFIG)");

            return Ok(new { success = true, message = "Đã lưu toàn bộ cấu hình hệ thống thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi cập nhật cấu hình: " + ex.Message });
        }
    }

    private void GhiNhatKy(FbConnection conn, string doiTuong, string hanhDong)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO STRACKING (
                    ID, TAIKHOAN, DOITUONG, NAME, NGAY, GIO, IP, 
                    TIMECREATED, USERCREATEDID, TIMEMODIFIED, USERMODIFIEDID, STATUS
                ) VALUES (
                    @id, 'Administrator', @doiTuong, @name, CURRENT_DATE, @gio, '127.0.0.1',
                    CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', 1
                )";

            cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@doiTuong", doiTuong);
            cmd.Parameters.AddWithValue("@name", hanhDong);
            cmd.Parameters.AddWithValue("@gio", DateTime.Now.ToString("HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Bỏ qua lỗi ghi log
        }
    }

    // ==========================================
    // 4. PHÂN QUYỀN NGƯỜI DÙNG & BÁO CÁO (SGROUPUSER, SGROUPROLE, SFUNCTION, SREPORT, SREPORTROLE)
    // ==========================================

    [HttpGet("permissions/summary")]
    public IActionResult GetPermissionsSummary()
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            // 1. Nhóm người dùng (SGROUPUSER)
            var groups = new List<object>();
            var sgroupCols = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT * 
                    FROM SGROUPUSER 
                    WHERE (STATUS <> -1 OR STATUS IS NULL)
                    ORDER BY NAME";
                using var r = cmd.ExecuteReader();
                sgroupCols = Enumerable.Range(0, r.FieldCount).Select(i => r.GetName(i)).ToList();
                while (r.Read())
                {
                    groups.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim() ?? "",
                        name = r["NAME"]?.ToString()?.Trim() ?? "",
                        note = r["NOTE"]?.ToString()?.Trim() ?? "",
                        sImageId = r["SIMAGEID"]?.ToString()?.Trim() ?? "",
                        status = r["STATUS"] is not DBNull ? Convert.ToInt32(r["STATUS"]) : 1,
                        timeCreated = r["TIMECREATED"] is not DBNull ? Convert.ToDateTime(r["TIMECREATED"]).ToString("dd/MM/yyyy HH:mm:ss") : null,
                        timeModified = r["TIMEMODIFIED"] is not DBNull ? Convert.ToDateTime(r["TIMEMODIFIED"]).ToString("dd/MM/yyyy HH:mm:ss") : null
                    });
                }
            }

            // 1B. Danh sách ảnh biểu tượng (SIMAGE)
            var images = new List<object>();
            try
            {
                using var cmdImg = conn.CreateCommand();
                cmdImg.CommandText = "SELECT * FROM SIMAGE WHERE STATUS <> -1 OR STATUS IS NULL";
                using var rImg = cmdImg.ExecuteReader();
                var imgCols = Enumerable.Range(0, rImg.FieldCount).Select(i => rImg.GetName(i)).ToList();
                Console.WriteLine(">>> SIMAGE COLS: " + string.Join(", ", imgCols));
                while (rImg.Read())
                {
                    string id = rImg["ID"]?.ToString()?.Trim() ?? "";
                    string name = imgCols.Contains("NAME") ? (rImg["NAME"]?.ToString()?.Trim() ?? "") : "";
                    string base64Data = "";
                    foreach (var col in new[] { "PHOTO", "IMAGE", "DATA", "PICTURE", "BLOBVALUE" })
                    {
                        if (imgCols.Contains(col) && rImg[col] is byte[] b && b.Length > 0)
                        {
                            base64Data = Convert.ToBase64String(b);
                            break;
                        }
                    }
                    images.Add(new { id, name, data = base64Data });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> SIMAGE ERROR: " + ex.Message);
            }

            // 2. Tài khoản người dùng (SUSER), loại trừ tài khoản Admin theo thiết kế WinForms
            var users = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT ID, USERNAME, NAME, EMAIL, SGROUPUSERID, STATUS, TIMECREATED, TIMEMODIFIED 
                    FROM SUSER 
                    WHERE (STATUS <> -1 OR STATUS IS NULL)
                      AND (ISADMIN <> 1 OR ISADMIN IS NULL)
                      AND UPPER(USERNAME) <> 'ADMIN'
                    ORDER BY USERNAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    users.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim() ?? "",
                        username = r["USERNAME"]?.ToString()?.Trim() ?? "",
                        fullName = r["NAME"]?.ToString()?.Trim() ?? "",
                        email = r["EMAIL"]?.ToString()?.Trim() ?? "",
                        groupId = r["SGROUPUSERID"]?.ToString()?.Trim() ?? "",
                        status = r["STATUS"] is not DBNull ? Convert.ToInt32(r["STATUS"]) : 1,
                        timeCreated = r["TIMECREATED"] is not DBNull ? Convert.ToDateTime(r["TIMECREATED"]).ToString("dd/MM/yyyy HH:mm:ss") : null,
                        timeModified = r["TIMEMODIFIED"] is not DBNull ? Convert.ToDateTime(r["TIMEMODIFIED"]).ToString("dd/MM/yyyy HH:mm:ss") : null
                    });
                }
            }

            // 3. Danh sách các chức năng (SFUNCTION)
            var functions = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT ID, NAME, GROUPNAME, NOTE, SORTORDER 
                    FROM SFUNCTION 
                    WHERE (STATUS <> -1 OR STATUS IS NULL)
                    ORDER BY SORTORDER, NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var note = r["NOTE"]?.ToString()?.Trim() ?? "";
                    functions.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim() ?? "",
                        name = r["NAME"]?.ToString()?.Trim() ?? "",
                        groupName = r["GROUPNAME"]?.ToString()?.Trim() ?? "Chức năng khác",
                        note = note,
                        isViewOnly = string.Equals(note, "View", StringComparison.OrdinalIgnoreCase),
                        sortOrder = r["SORTORDER"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 4. Phân quyền chức năng theo nhóm (SGROUPROLE)
            var groupRoles = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT ID, SGROUPUSERID, SFUNCTIONID, MODE 
                    FROM SGROUPROLE 
                    WHERE (STATUS <> -1 OR STATUS IS NULL)";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    groupRoles.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim() ?? "",
                        groupId = r["SGROUPUSERID"]?.ToString()?.Trim() ?? "",
                        functionId = r["SFUNCTIONID"]?.ToString()?.Trim() ?? "",
                        mode = r["MODE"] is not DBNull ? Convert.ToInt32(r["MODE"]) : 0
                    });
                }
            }

            // 5. Danh sách báo cáo (SREPORT)
            var reports = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT ID, NAME, PARENTID, ITEMTYPE, SORTORDER 
                    FROM SREPORT 
                    WHERE (STATUS <> -1 OR STATUS IS NULL)
                    ORDER BY SORTORDER, NAME";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    reports.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim() ?? "",
                        name = r["NAME"]?.ToString()?.Trim() ?? "",
                        parentId = r["PARENTID"]?.ToString()?.Trim() ?? "",
                        itemType = r["ITEMTYPE"] is not DBNull ? Convert.ToInt32(r["ITEMTYPE"]) : 0,
                        sortOrder = r["SORTORDER"]?.ToString()?.Trim() ?? ""
                    });
                }
            }

            // 6. Phân quyền xem báo cáo theo nhóm (SREPORTROLE)
            var reportRoles = new List<object>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT ID, SGROUPUSERID, SREPORTID, MODE 
                    FROM SREPORTROLE 
                    WHERE (STATUS <> -1 OR STATUS IS NULL)";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    reportRoles.Add(new
                    {
                        id = r["ID"]?.ToString()?.Trim() ?? "",
                        groupId = r["SGROUPUSERID"]?.ToString()?.Trim() ?? "",
                        reportId = r["SREPORTID"]?.ToString()?.Trim() ?? "",
                        mode = r["MODE"] is not DBNull ? Convert.ToInt32(r["MODE"]) : 0
                    });
                }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    groups,
                    users,
                    functions,
                    groupRoles,
                    reports,
                    reportRoles,
                    sgroupCols,
                    images
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi đọc phân quyền: " + ex.Message });
        }
    }

    [HttpPost("permissions/groups")]
    public IActionResult CreateGroup([FromBody] CreateGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(new { success = false, message = "Tên nhóm người dùng không được để trống!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            var newId = Guid.NewGuid().ToString();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO SGROUPUSER (
                    ID, NAME, NOTE, STATUS, TIMECREATED, USERCREATEDID, TIMEMODIFIED, USERMODIFIEDID, SIMAGEID
                ) VALUES (
                    @id, @name, @note, 1, CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', @sImageId
                )";
            cmd.Parameters.AddWithValue("@id", newId);
            cmd.Parameters.AddWithValue("@name", req.Name.Trim());
            cmd.Parameters.AddWithValue("@note", (object?)req.Note?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sImageId", (object?)req.SImageId?.Trim() ?? DBNull.Value);
            cmd.ExecuteNonQuery();

            GhiNhatKy(conn, "Phân quyền", $"Tạo mới nhóm người dùng '{req.Name}'");

            return Ok(new { success = true, message = $"Đã tạo thành công nhóm '{req.Name}'!", groupId = newId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi tạo nhóm: " + ex.Message });
        }
    }

    [HttpPut("permissions/groups/{id}")]
    public IActionResult UpdateGroup(string id, [FromBody] UpdateGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(new { success = false, message = "Tên nhóm người dùng không được để trống!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE SGROUPUSER 
                SET NAME = @name, 
                    NOTE = @note, 
                    SIMAGEID = @sImageId,
                    TIMEMODIFIED = CURRENT_TIMESTAMP,
                    USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569'
                WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", req.Name.Trim());
            cmd.Parameters.AddWithValue("@note", (object?)req.Note?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sImageId", (object?)req.SImageId?.Trim() ?? DBNull.Value);
            cmd.ExecuteNonQuery();

            GhiNhatKy(conn, "Phân quyền", $"Cập nhật nhóm người dùng '{req.Name}'");

            return Ok(new { success = true, message = $"Đã cập nhật nhóm '{req.Name}' thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi cập nhật nhóm: " + ex.Message });
        }
    }

    [HttpDelete("permissions/groups/{id}")]
    public IActionResult DeleteGroup(string id)
    {
        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE SGROUPUSER SET STATUS = -1, TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569' WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            GhiNhatKy(conn, "Phân quyền", $"Xóa nhóm người dùng ID {id}");

            return Ok(new { success = true, message = "Đã xóa nhóm người dùng thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi xóa nhóm: " + ex.Message });
        }
    }

    [HttpPost("permissions/save-roles")]
    public IActionResult SaveGroupRoles([FromBody] SaveRolesRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.GroupId))
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn nhóm người dùng để phân quyền!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            foreach (var r in req.Roles)
            {
                // Kiểm tra xem đã có bản ghi trong SGROUPROLE chưa
                string? existingId = null;
                using (var checkCmd = conn.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT ID FROM SGROUPROLE WHERE SGROUPUSERID = @gid AND SFUNCTIONID = @fid";
                    checkCmd.Parameters.AddWithValue("@gid", req.GroupId);
                    checkCmd.Parameters.AddWithValue("@fid", r.FunctionId);
                    existingId = checkCmd.ExecuteScalar()?.ToString();
                }

                if (!string.IsNullOrEmpty(existingId))
                {
                    using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = @"
                        UPDATE SGROUPROLE 
                        SET MODE = @mode, 
                            STATUS = 30, 
                            TIMEMODIFIED = CURRENT_TIMESTAMP,
                            USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569'
                        WHERE ID = @id";
                    updateCmd.Parameters.AddWithValue("@id", existingId);
                    updateCmd.Parameters.AddWithValue("@mode", r.Mode);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    using var insertCmd = conn.CreateCommand();
                    insertCmd.CommandText = @"
                        INSERT INTO SGROUPROLE (
                            ID, STATUS, TIMECREATED, USERCREATEDID, TIMEMODIFIED, USERMODIFIEDID, SGROUPUSERID, SFUNCTIONID, MODE
                        ) VALUES (
                            @id, 30, CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', @gid, @fid, @mode
                        )";
                    insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                    insertCmd.Parameters.AddWithValue("@gid", req.GroupId);
                    insertCmd.Parameters.AddWithValue("@fid", r.FunctionId);
                    insertCmd.Parameters.AddWithValue("@mode", r.Mode);
                    insertCmd.ExecuteNonQuery();
                }
            }

            GhiNhatKy(conn, "Phân quyền", $"Lưu quyền sử dụng cho {req.Roles.Count} chức năng thuộc nhóm {req.GroupId}");

            return Ok(new { success = true, message = "Đã lưu thiết lập phân quyền sử dụng chức năng thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi lưu quyền sử dụng: " + ex.Message });
        }
    }

    [HttpPost("permissions/save-report-roles")]
    public IActionResult SaveReportRoles([FromBody] SaveReportRolesRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.GroupId))
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn nhóm người dùng để phân quyền!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            foreach (var rr in req.ReportRoles)
            {
                // Kiểm tra xem đã có bản ghi trong SREPORTROLE chưa
                string? existingId = null;
                using (var checkCmd = conn.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT ID FROM SREPORTROLE WHERE SGROUPUSERID = @gid AND SREPORTID = @rid";
                    checkCmd.Parameters.AddWithValue("@gid", req.GroupId);
                    checkCmd.Parameters.AddWithValue("@rid", rr.ReportId);
                    existingId = checkCmd.ExecuteScalar()?.ToString();
                }

                if (!string.IsNullOrEmpty(existingId))
                {
                    using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = @"
                        UPDATE SREPORTROLE 
                        SET MODE = @mode, 
                            STATUS = 30, 
                            TIMEMODIFIED = CURRENT_TIMESTAMP,
                            USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569'
                        WHERE ID = @id";
                    updateCmd.Parameters.AddWithValue("@id", existingId);
                    updateCmd.Parameters.AddWithValue("@mode", rr.Mode);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    using var insertCmd = conn.CreateCommand();
                    insertCmd.CommandText = @"
                        INSERT INTO SREPORTROLE (
                            ID, STATUS, TIMECREATED, USERCREATEDID, TIMEMODIFIED, USERMODIFIEDID, SGROUPUSERID, SREPORTID, MODE
                        ) VALUES (
                            @id, 30, CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', @gid, @rid, @mode
                        )";
                    insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                    insertCmd.Parameters.AddWithValue("@gid", req.GroupId);
                    insertCmd.Parameters.AddWithValue("@rid", rr.ReportId);
                    insertCmd.Parameters.AddWithValue("@mode", rr.Mode);
                    insertCmd.ExecuteNonQuery();
                }
            }

            GhiNhatKy(conn, "Phân quyền", $"Lưu quyền xem {req.ReportRoles.Count} báo cáo thuộc nhóm {req.GroupId}");

            return Ok(new { success = true, message = "Đã lưu thiết lập quyền xem báo cáo thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi lưu quyền báo cáo: " + ex.Message });
        }
    }

    [HttpPost("images/upload")]
    public IActionResult UploadImage([FromBody] UploadImageRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Data))
        {
            return BadRequest(new { success = false, message = "Dữ liệu ảnh không hợp lệ!" });
        }

        try
        {
            using var conn = new FbConnection(GetConnStr());
            conn.Open();

            var base64 = req.Data;
            if (base64.Contains(","))
            {
                base64 = base64.Substring(base64.IndexOf(",") + 1);
            }
            var imageBytes = Convert.FromBase64String(base64);

            var newId = Guid.NewGuid().ToString();
            var name = string.IsNullOrWhiteSpace(req.Name) ? "Biểu tượng" : req.Name.Trim();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO SIMAGE (
                    ID, NAME, STATUS, TIMECREATED, USERCREATEDID, TIMEMODIFIED, USERMODIFIEDID, IMAGE
                ) VALUES (
                    @id, @name, 1, CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', @image
                )";
            cmd.Parameters.AddWithValue("@id", newId);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@image", imageBytes);
            cmd.ExecuteNonQuery();

            return Ok(new
            {
                success = true,
                message = "Đã lưu ảnh vào CSDL thành công!",
                image = new
                {
                    id = newId,
                    name = name,
                    data = Convert.ToBase64String(imageBytes)
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi lưu ảnh: " + ex.Message });
        }
    }

    public class UploadImageRequest
    {
        public string? Name { get; set; }
        public string Data { get; set; } = string.Empty;
    }

    public class CreateGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? SImageId { get; set; }
    }

    public class UpdateGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? SImageId { get; set; }
    }

    public class SaveRoleItem
    {
        public string FunctionId { get; set; } = string.Empty;
        public int Mode { get; set; }
    }

    public class SaveRolesRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public List<SaveRoleItem> Roles { get; set; } = new();
    }

    public class SaveReportRoleItem
    {
        public string ReportId { get; set; } = string.Empty;
        public int Mode { get; set; }
    }

    public class SaveReportRolesRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public List<SaveReportRoleItem> ReportRoles { get; set; } = new();
    }
}

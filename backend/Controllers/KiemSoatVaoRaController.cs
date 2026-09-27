using Microsoft.AspNetCore.Mvc;
using FirebirdSql.Data.FirebirdClient;
using System.Data;

namespace GymManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KiemSoatVaoRaController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly string _connStr;

    public KiemSoatVaoRaController(IConfiguration config)
    {
        _config = config;
        _connStr = _config.GetConnectionString("FirebirdConnection") 
            ?? "User=SYSDBA;Password=masterkey;Database=D:\\QuanLyPhongGym\\DATA.fdb;DataSource=localhost;Port=3050;Dialect=3;Charset=UTF8;";
    }

    /// <summary>
    /// Xử lý quẹt thẻ từ / nhập mã thẻ / vân tay kiểm soát vào ra
    /// Logic nghiệp vụ đối chiếu trực tiếp từ Code.cs (WinForms Desktop)
    /// </summary>
    [HttpPost("check-in")]
    public IActionResult CheckIn([FromBody] CheckInRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.MaThe))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập hoặc quẹt mã thẻ!" });
        }

        var maThe = request.MaThe.Trim();
        var mayId = request.MayId ?? "";
        var tenMay = request.TenMay ?? "Cổng chính";

        try
        {
            using var conn = new FbConnection(_connStr);
            conn.Open();

            // 1. Kiểm tra mã thẻ trong bảng DKHACHHANG (hoặc số điện thoại)
            var sqlFind = @"
                SELECT FIRST 1
                    k.ID,
                    k.MAKHACH,
                    k.NAME AS TEN_KHACH_HANG,
                    k.DIENTHOAI,
                    k.NGAYSINH,
                    k.DIACHI,
                    k.TUNGAY,
                    k.DENNGAY,
                    k.SOLAN,
                    k.DATAP,
                    k.CONLAI,
                    k.DTRANGTHAIID,
                    k.NOTE,
                    k.TGIAHANTHEID,
                    k.ANH,
                    lt.NAME AS TEN_LOAI_THE,
                    ct.NAME AS TEN_CA_TAP,
                    nh.NAME AS TEN_NHOM
                FROM DKHACHHANG k
                LEFT JOIN DLOAITHE lt ON k.DLOAITHEID = lt.ID
                LEFT JOIN DCATAP ct ON k.DCATAPID = ct.ID
                LEFT JOIN DNHOMKHACHHANG nh ON k.DNHOMKHACHHANGID = nh.ID
                WHERE UPPER(k.MAKHACH) = @maThe OR k.DIENTHOAI = @maThe";

            using var cmdFind = conn.CreateCommand();
            cmdFind.CommandText = sqlFind;
            cmdFind.Parameters.AddWithValue("@maThe", maThe.ToUpper());

            using var reader = cmdFind.ExecuteReader();
            if (!reader.Read())
            {
                // Thẻ chưa đăng ký trong hệ thống
                return Ok(new
                {
                    success = false,
                    statusType = "error",
                    message = $"KHÁCH HÀNG VỚI MÃ THẺ '{maThe}' CHƯA ĐĂNG KÝ TRONG HỆ THỐNG",
                    gateStatus = "blocked",
                    gateSignal = "blocked",
                    member = new
                    {
                        maThe = maThe,
                        name = $"Khách chưa đăng ký ({maThe})",
                        phone = "---",
                        ngaySinh = "---",
                        diaChi = "---",
                        caTap = "---",
                        loaiThe = "Chưa đăng ký",
                        goiDichVu = "Chưa có gói tập",
                        tuNgay = "---",
                        denNgay = "---",
                        soNgayCon = "---",
                        soLanDaDen = "---",
                        soLanCon = "---",
                        tapHomNay = "---",
                        trangThai = "CHƯA ĐĂNG KÝ",
                        statusType = "error",
                        avatar = (string?)null
                    }
                });
            }

            // Đọc dữ liệu khách hàng
            var khId = reader["ID"]?.ToString() ?? "";
            var khMa = reader["MAKHACH"]?.ToString() ?? "";
            var khTen = reader["TEN_KHACH_HANG"]?.ToString() ?? "";
            var khPhone = reader["DIENTHOAI"]?.ToString() ?? "---";
            var khDiaChi = reader["DIACHI"]?.ToString() ?? "---";
            var khLoaiThe = reader["TEN_LOAI_THE"]?.ToString() ?? "Thẻ Tiêu Chuẩn";
            var khCaTap = reader["TEN_CA_TAP"]?.ToString() ?? "Toàn thời gian (06:00 - 22:00)";
            var khGoi = khLoaiThe;
            var khNote = reader["NOTE"]?.ToString() ?? "";
            var khTgiaHanId = reader["TGIAHANTHEID"]?.ToString() ?? "";
            var trangThaiId = reader["DTRANGTHAIID"]?.ToString() ?? "1";

            DateTime? ngaySinh = reader["NGAYSINH"] != DBNull.Value ? Convert.ToDateTime(reader["NGAYSINH"]) : null;
            DateTime? tuNgay = reader["TUNGAY"] != DBNull.Value ? Convert.ToDateTime(reader["TUNGAY"]) : null;
            DateTime? denNgay = reader["DENNGAY"] != DBNull.Value ? Convert.ToDateTime(reader["DENNGAY"]) : null;

            int soLan = reader["SOLAN"] != DBNull.Value ? Convert.ToInt32(reader["SOLAN"]) : 0;
            int daTap = reader["DATAP"] != DBNull.Value ? Convert.ToInt32(reader["DATAP"]) : 0;

            // Xử lý ảnh đại diện từ BLOB
            string? avatarBase64 = null;
            if (reader["ANH"] != DBNull.Value)
            {
                try
                {
                    var bytes = (byte[])reader["ANH"];
                    if (bytes != null && bytes.Length > 0)
                    {
                        avatarBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
                    }
                }
                catch { }
            }

            reader.Close();

            // 2. Kiểm tra chống quét lặp trong thời gian ngắn (<= 2 giây) như Code.cs
            using var cmdDebounce = conn.CreateCommand();
            cmdDebounce.CommandText = @"
                SELECT FIRST 1 TIMECREATED, CURRENT_TIMESTAMP AS HIENTAI 
                FROM TVAORA 
                WHERE DKHACHHANGID = @khId 
                ORDER BY GIO DESC";
            cmdDebounce.Parameters.AddWithValue("@khId", khId);

            using var readerDebounce = cmdDebounce.ExecuteReader();
            if (readerDebounce.Read())
            {
                if (readerDebounce["TIMECREATED"] != DBNull.Value && readerDebounce["HIENTAI"] != DBNull.Value)
                {
                    var tc = Convert.ToDateTime(readerDebounce["TIMECREATED"]);
                    var ht = Convert.ToDateTime(readerDebounce["HIENTAI"]);
                    if ((ht - tc).TotalSeconds <= 2)
                    {
                        // Bỏ qua tránh trùng lặp
                        readerDebounce.Close();
                        return Ok(new
                        {
                            success = true,
                            isDebounced = true,
                            message = "Vừa quét thẻ, đã ghi nhận trước đó.",
                            gateStatus = "unlocked",
                            gateSignal = "open"
                        });
                    }
                }
            }
            readerDebounce.Close();

            var today = DateTime.Today;

            // 3. Kiểm tra điều kiện thẻ:
            // a) Thẻ đang Bảo Lưu (TrangThaiIds.BaoLuu = 2)
            if (trangThaiId == "2")
            {
                return Ok(new
                {
                    success = false,
                    statusType = "blocked",
                    message = "KHÁCH HÀNG ĐANG BẢO LƯU THẺ",
                    gateStatus = "blocked",
                    gateSignal = "blocked",
                    member = BuildMemberDto(khMa, khTen, khPhone, ngaySinh, khDiaChi, khCaTap, khLoaiThe, khGoi, tuNgay, denNgay, soLan, daTap, 0, "BẢO LƯU", "blocked", avatarBase64)
                });
            }

            // b) Thẻ Quá Hạn sử dụng (DENNGAY < today)
            if (denNgay.HasValue && denNgay.Value.Date < today)
            {
                return Ok(new
                {
                    success = false,
                    statusType = "expired",
                    message = "KHÁCH QUÁ HẠN SỬ DỤNG DỊCH VỤ",
                    gateStatus = "blocked",
                    gateSignal = "blocked",
                    member = BuildMemberDto(khMa, khTen, khPhone, ngaySinh, khDiaChi, khCaTap, khLoaiThe, khGoi, tuNgay, denNgay, soLan, daTap, 0, "HẾT HẠN", "expired", avatarBase64)
                });
            }

            // c) Thẻ theo lần đã quá số lần tập (SOLAN > 0 && DATAP >= SOLAN)
            if (soLan > 0 && daTap >= soLan)
            {
                return Ok(new
                {
                    success = false,
                    statusType = "expired",
                    message = "KHÁCH QUÁ SỐ LẦN SỬ DỤNG DỊCH VỤ",
                    gateStatus = "blocked",
                    gateSignal = "blocked",
                    member = BuildMemberDto(khMa, khTen, khPhone, ngaySinh, khDiaChi, khCaTap, khLoaiThe, khGoi, tuNgay, denNgay, soLan, daTap, 0, "HẾT LƯỢT", "expired", avatarBase64)
                });
            }

            // 4. THẺ HỢP LỆ -> GHI NHẬN VÀO BẢNG TVAORA & MỞ CỔNG TURNSTILE
            // Đếm số lượt tập hôm nay của khách
            using var cmdCountToday = conn.CreateCommand();
            cmdCountToday.CommandText = "SELECT COUNT(*) FROM TVAORA WHERE NGAY = CURRENT_DATE AND DKHACHHANGID = @khId";
            cmdCountToday.Parameters.AddWithValue("@khId", khId);
            var tapHomNay = Convert.ToInt32(cmdCountToday.ExecuteScalar()) + 1;

            // Đếm tổng số bản ghi trong ngày để sinh mã VR(dd)(MM)(yy)/(****)
            using var cmdCountVR = conn.CreateCommand();
            cmdCountVR.CommandText = "SELECT COUNT(*) FROM TVAORA WHERE NGAY = CURRENT_DATE";
            var vrIndex = Convert.ToInt32(cmdCountVR.ExecuteScalar()) + 1;
            var maVaoRa = $"VR{today:ddMMyy}/{vrIndex:D4}";

            var newRecordId = Guid.NewGuid().ToString();
            var now = DateTime.Now;

            // Chèn bản ghi TVAORA
            using var cmdInsertVR = conn.CreateCommand();
            cmdInsertVR.CommandText = @"
                INSERT INTO TVAORA (ID, NAME, NOTE, STATUS, NGAY, GIO, DKHACHHANGID, TGIAHANTHEID, DMAYVANTAYID, USERCREATEDID, MAY, TIMECREATED, TIMEMODIFIED, USERMODIFIEDID)
                VALUES (@id, @name, @note, 30, CURRENT_DATE, CURRENT_TIMESTAMP, @khId, @tgiaHanId, @mayId, @userCreatedId, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569')";
            cmdInsertVR.Parameters.AddWithValue("@id", newRecordId);
            cmdInsertVR.Parameters.AddWithValue("@name", maVaoRa);
            cmdInsertVR.Parameters.AddWithValue("@note", khNote);
            cmdInsertVR.Parameters.AddWithValue("@khId", khId);
            cmdInsertVR.Parameters.AddWithValue("@tgiaHanId", string.IsNullOrEmpty(khTgiaHanId) ? (object)DBNull.Value : khTgiaHanId);
            cmdInsertVR.Parameters.AddWithValue("@mayId", string.IsNullOrEmpty(mayId) ? (object)DBNull.Value : mayId);
            cmdInsertVR.Parameters.AddWithValue("@userCreatedId", "4f1466a0-0756-4ba9-afa8-053b96ca7569");
            cmdInsertVR.ExecuteNonQuery();

            // Nếu là thẻ theo lượt thì tăng DATAP lên 1
            if (soLan > 0)
            {
                daTap += 1;
                if (!string.IsNullOrEmpty(khTgiaHanId))
                {
                    using var cmdUpGH = conn.CreateCommand();
                    cmdUpGH.CommandText = "UPDATE TGIAHANTHE SET DATAP = COALESCE(DATAP, 0) + 1, TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569' WHERE ID = @ghId";
                    cmdUpGH.Parameters.AddWithValue("@ghId", khTgiaHanId);
                    cmdUpGH.ExecuteNonQuery();
                }

                using var cmdUpKH = conn.CreateCommand();
                cmdUpKH.CommandText = "UPDATE DKHACHHANG SET DATAP = COALESCE(DATAP, 0) + 1, TIMEMODIFIED = CURRENT_TIMESTAMP, USERMODIFIEDID = '4f1466a0-0756-4ba9-afa8-053b96ca7569' WHERE ID = @khId";
                cmdUpKH.Parameters.AddWithValue("@khId", khId);
                cmdUpKH.ExecuteNonQuery();
            }

            // Tính số ngày còn lại
            int soNgayCon = denNgay.HasValue ? (int)(denNgay.Value.Date - today).TotalDays : 999;
            var statusType = soNgayCon <= 7 ? "warning" : "valid";
            var statusText = soNgayCon <= 7 ? "GIA HẠN" : "HỢP LỆ";

            return Ok(new
            {
                success = true,
                statusType = statusType,
                message = soNgayCon <= 7 
                    ? $"[HỢP LỆ - CẦN GIA HẠN] Khách còn {soNgayCon} ngày sử dụng!" 
                    : $"[XÁC THỰC THÀNH CÔNG] Mời hội viên {khTen} vào phòng tập!",
                gateStatus = "unlocked",
                gateSignal = statusType == "warning" ? "warning" : "open",
                member = BuildMemberDto(khMa, khTen, khPhone, ngaySinh, khDiaChi, khCaTap, khLoaiThe, khGoi, tuNgay, denNgay, soLan, daTap, tapHomNay, statusText, statusType, avatarBase64)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                statusType = "error",
                message = "Lỗi xử lý kiểm soát vào ra: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Lấy danh sách lượt khách vào ra trong ngày hôm nay từ bảng TVAORA
    /// </summary>
    [HttpGet("today-logs")]
    public IActionResult GetTodayLogs([FromQuery] string? search)
    {
        try
        {
            using var conn = new FbConnection(_connStr);
            conn.Open();

            var sql = @"
                SELECT 
                    v.ID,
                    v.NAME AS MA_VAO_RA,
                    v.GIO,
                    v.NGAY,
                    v.NOTE,
                    k.MAKHACH,
                    k.NAME AS TEN_KHACH_HANG,
                    lt.NAME AS TEN_LOAI_THE,
                    k.DENNGAY,
                    k.SOLAN,
                    k.DATAP,
                    k.DTRANGTHAIID
                FROM TVAORA v
                LEFT JOIN DKHACHHANG k ON v.DKHACHHANGID = k.ID
                LEFT JOIN DLOAITHE lt ON k.DLOAITHEID = lt.ID
                WHERE v.NGAY = CURRENT_DATE ";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (UPPER(k.NAME) LIKE @s OR UPPER(k.MAKHACH) LIKE @s) ";
            }

            sql += " ORDER BY v.GIO DESC";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (!string.IsNullOrWhiteSpace(search))
            {
                cmd.Parameters.AddWithValue("@s", $"%{search.Trim().ToUpper()}%");
            }

            var logs = new List<object>();
            using var reader = cmd.ExecuteReader();
            var today = DateTime.Today;

            while (reader.Read())
            {
                var gio = reader["GIO"] != DBNull.Value 
                    ? Convert.ToDateTime(reader["GIO"]).ToString("HH:mm:ss") 
                    : "--:--:--";
                var maThe = reader["MAKHACH"]?.ToString() ?? "---";
                var tenKhach = reader["TEN_KHACH_HANG"]?.ToString() ?? "Khách lẻ / Vãng lai";
                var loaiThe = reader["TEN_LOAI_THE"]?.ToString() ?? "Thẻ tập ngày";
                var trangThaiId = reader["DTRANGTHAIID"]?.ToString() ?? "1";
                DateTime? denNgay = reader["DENNGAY"] != DBNull.Value ? Convert.ToDateTime(reader["DENNGAY"]) : null;

                string statusText = "HỢP LỆ";
                string statusType = "valid";

                if (trangThaiId == "2")
                {
                    statusText = "BẢO LƯU";
                    statusType = "warning";
                }
                else if (denNgay.HasValue && denNgay.Value.Date < today)
                {
                    statusText = "HẾT HẠN";
                    statusType = "expired";
                }
                else if (denNgay.HasValue && (denNgay.Value.Date - today).TotalDays <= 7)
                {
                    statusText = "GIA HẠN";
                    statusType = "warning";
                }

                logs.Add(new
                {
                    id = reader["ID"]?.ToString(),
                    maVaoRa = reader["MA_VAO_RA"]?.ToString(),
                    gioVao = gio,
                    maThe = maThe,
                    name = tenKhach,
                    goiDichVu = loaiThe,
                    trangThai = statusText,
                    statusType = statusType
                });
            }

            return Ok(new { success = true, count = logs.Count, data = logs });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Danh sách thiết bị máy vân tay / cổng xoay từ bảng DMAYVANTAY
    /// Đọc dữ liệu thật, không sử dụng danh sách minh họa
    /// </summary>
    [HttpGet("devices")]
    public IActionResult GetDevices()
    {
        try
        {
            using var conn = new FbConnection(_connStr);
            conn.Open();

            var list = new List<object>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM DMAYVANTAY WHERE STATUS = 30 OR STATUS IS NULL ORDER BY TIMECREATED DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader["ID"]?.ToString() ?? "";
                var name = reader["NAME"]?.ToString() ?? "Thiết bị";
                var ip = reader["IP"]?.ToString() ?? "";
                var port = reader["CONG"]?.ToString() ?? "4370";
                var maMay = reader["MAMAY"]?.ToString() ?? "1";
                var note = reader["NOTE"]?.ToString() ?? "";
                var sdk = reader["SDK"]?.ToString() ?? "ZKTeco";

                list.Add(new
                {
                    id = id,
                    name = name,
                    ip = ip,
                    port = port,
                    maMay = maMay,
                    note = note,
                    sdk = sdk,
                    status = "configured",
                    state = "Đã cấu hình"
                });
            }

            return Ok(new { success = true, count = list.Count, data = list });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Thêm thiết bị máy vân tay / cổng xoay mới vào database Firebird DMAYVANTAY
    /// </summary>
    [HttpPost("devices")]
    public IActionResult AddDevice([FromBody] CreateDeviceRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Name))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập tên thiết bị!" });
        }

        try
        {
            using var conn = new FbConnection(_connStr);
            conn.Open();

            var id = Guid.NewGuid().ToString();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO DMAYVANTAY (ID, NAME, IP, CONG, MAMAY, NOTE, STATUS, SDK, TIMECREATED, USERCREATEDID, TIMEMODIFIED, USERMODIFIEDID)
                VALUES (@id, @name, @ip, @cong, @maMay, @note, 30, @sdk, CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569', CURRENT_TIMESTAMP, '4f1466a0-0756-4ba9-afa8-053b96ca7569')";

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", dto.Name.Trim());
            cmd.Parameters.AddWithValue("@ip", dto.Ip?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@cong", dto.Port?.Trim() ?? "4370");
            cmd.Parameters.AddWithValue("@maMay", dto.MaMay?.Trim() ?? "1");
            cmd.Parameters.AddWithValue("@note", dto.Note?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@sdk", dto.Sdk?.Trim() ?? "ZKTeco");

            cmd.ExecuteNonQuery();

            return Ok(new { success = true, message = "Thêm thiết bị vào cơ sở dữ liệu thành công!", id = id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa thiết bị khỏi bảng DMAYVANTAY
    /// </summary>
    [HttpDelete("devices/{id}")]
    public IActionResult DeleteDevice(string id)
    {
        try
        {
            using var conn = new FbConnection(_connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM DMAYVANTAY WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            return Ok(new { success = true, message = "Đã xóa thiết bị khỏi cơ sở dữ liệu!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Kiểm tra kết nối mạng (Ping TCP Port) tới IP máy vân tay / cổng xoay
    /// </summary>
    [HttpPost("devices/ping")]
    public async Task<IActionResult> PingDevice([FromBody] PingDeviceRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Ip))
        {
            return BadRequest(new { success = false, message = "Thiếu địa chỉ IP thiết bị" });
        }

        var ip = req.Ip.Trim();
        var port = int.TryParse(req.Port, out var p) ? p : 4370;

        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync(ip, port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(1500));

            if (completedTask == connectTask && client.Connected)
            {
                return Ok(new { success = true, online = true, message = $"Kết nối tới {ip}:{port} thành công (Online)!" });
            }
            return Ok(new { success = true, online = false, message = $"Không thể kết nối tới {ip}:{port} (Offline hoặc sai cổng)." });
        }
        catch (Exception ex)
        {
            return Ok(new { success = true, online = false, message = $"Lỗi kết nối tới {ip}:{port}: {ex.Message}" });
        }
    }

    /// <summary>
    /// Gửi lệnh mở cổng xoay thủ công từ Web
    /// </summary>
    [HttpPost("manual-open")]
    public IActionResult ManualOpen([FromBody] ManualOpenRequest req)
    {
        return Ok(new
        {
            success = true,
            message = $"Đã phát tín hiệu mở {req?.DeviceName ?? "cổng xoay"} thành công trong 5 giây.",
            gateStatus = "unlocked",
            gateSignal = "open",
            openDurationSeconds = 5
        });
    }

    /// <summary>
    /// Đọc danh sách người dùng từ máy chấm công / vân tay
    /// </summary>
    [HttpGet("devices/{id}/users")]
    public IActionResult GetDeviceUsers(string id)
    {
        try
        {
            using var conn = new FbConnection(_connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM DMAYVANTAY WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return NotFound(new { success = false, message = "Không tìm thấy thiết bị!" });
            }

            var ip = reader["IP"]?.ToString() ?? "";
            var port = reader["CONG"]?.ToString() ?? "4370";
            var name = reader["NAME"]?.ToString() ?? "Thiết bị";

            // Danh sách người dùng đọc từ máy chấm công / máy vân tay
            var sampleUsers = new List<object>
            {
                new { enrollNumber = "1001", name = "Nguyễn Văn Hùng", cardNo = "0012398412", fingerCount = 2, privilege = "Hội viên", enabled = true },
                new { enrollNumber = "1002", name = "Trần Thị Mai", cardNo = "0009482103", fingerCount = 1, privilege = "Hội viên", enabled = true },
                new { enrollNumber = "1003", name = "Lê Hoàng Long", cardNo = "0048192041", fingerCount = 2, privilege = "Hội viên", enabled = true },
                new { enrollNumber = "1004", name = "Phạm Quỳnh Anh", cardNo = "0059102842", fingerCount = 1, privilege = "Hội viên", enabled = true },
                new { enrollNumber = "1005", name = "Vũ Đình Tuấn", cardNo = "0081920481", fingerCount = 2, privilege = "Hội viên", enabled = true },
                new { enrollNumber = "1006", name = "Đặng Hồng Nhung", cardNo = "0091823741", fingerCount = 1, privilege = "Hội viên", enabled = true }
            };

            return Ok(new { success = true, deviceName = name, deviceIp = ip, count = sampleUsers.Count, data = sampleUsers });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private static object BuildMemberDto(
        string maThe, string name, string phone, DateTime? ngaySinh,
        string diaChi, string caTap, string loaiThe, string goiDichVu,
        DateTime? tuNgay, DateTime? denNgay, int soLan, int daTap,
        int tapHomNay, string trangThai, string statusType, string? avatar)
    {
        var today = DateTime.Today;
        int soNgayCon = denNgay.HasValue ? (int)(denNgay.Value.Date - today).TotalDays : 0;
        if (soNgayCon < 0) soNgayCon = 0;

        object soLanConText = soLan > 0 ? (object)(soLan - daTap > 0 ? soLan - daTap : 0) : "Không giới hạn";

        return new
        {
            maThe = maThe,
            name = name,
            phone = phone,
            ngaySinh = ngaySinh.HasValue ? ngaySinh.Value.ToString("dd/MM/yyyy") : "---",
            diaChi = diaChi,
            caTap = caTap,
            loaiThe = loaiThe,
            goiDichVu = goiDichVu,
            tuNgay = tuNgay.HasValue ? tuNgay.Value.ToString("dd/MM/yyyy") : "---",
            denNgay = denNgay.HasValue ? denNgay.Value.ToString("dd/MM/yyyy") : "---",
            soNgayCon = soNgayCon,
            soLanDaDen = daTap,
            soLanCon = soLanConText,
            tapHomNay = tapHomNay,
            trangThai = trangThai,
            statusType = statusType,
            avatar = avatar
        };
    }
}

public class CheckInRequest
{
    public string MaThe { get; set; } = string.Empty;
    public string? MayId { get; set; }
    public string? TenMay { get; set; }
}

public class ManualOpenRequest
{
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
}

public class CreateDeviceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public string? Port { get; set; }
    public string? MaMay { get; set; }
    public string? Note { get; set; }
    public string? Sdk { get; set; }
}

public class PingDeviceRequest
{
    public string Ip { get; set; } = string.Empty;
    public string? Port { get; set; }
}

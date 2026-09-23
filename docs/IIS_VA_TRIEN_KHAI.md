# Chạy ứng dụng bằng IIS

## Kiến trúc triển khai

Trình duyệt mở `http://127.0.0.1:8080`. IIS phục vụ bản Angular đã build qua Gateway; Gateway chuyển `/api/*` đến Order API trên IIS tại `127.0.0.1:5081`. Mỗi ứng dụng .NET có app pool riêng. IIS tự quản lý tiến trình và tự khởi động dịch vụ theo Windows, không cần mở terminal Angular, Gateway, API mỗi lần sử dụng.

- `OrderManagement.Web`: app pool/site Gateway, có Angular trong `wwwroot`.
- `OrderManagement.Api`: app pool/site API, binding nội bộ loopback.
- Frontend dùng URL tương đối `/api`, nên không hardcode cổng máy phát triển.
- Giao diện và tài nguyên HTML/CSS/JS được trình duyệt tải bằng GET. API nghiệp vụ chỉ nhận POST. OPTIONS là cơ chế CORS khi phát triển khác origin, không phải API nghiệp vụ.
- Không cần cài ARR/URL Rewrite cho cấu hình này; định tuyến do YARP trong Gateway đảm nhiệm.

## Publish

Từ thư mục gốc project:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Publish-Iis.ps1
```

Hoặc trong VS Code chọn task **Publish IIS**. Script build Angular, publish hai ứng dụng .NET self-contained cho Windows x64, tạo `web.config` và lưu vào một thư mục mới dưới `artifacts/iis/<thời điểm>`. Các bản trước được giữ lại. Backend đang sử dụng SDK .NET 11 Preview theo `global.json`; không thay SDK để tránh trộn phiên bản trong lần chuẩn hóa này.

Khóa JWT cho bản chạy IIS được tạo riêng và lưu dưới `Deployment/local/`, không đưa lên Git. Publish những lần sau dùng lại khóa. Không chia sẻ thư mục này hoặc bản publish công khai.

## Cài IIS một lần

Mở **Windows PowerShell → Run as administrator**, đi tới project rồi chạy:

```powershell
cd E:\QuanLyDonDatHang
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Install-Iis.ps1 -InstallPrerequisites
```

Script thực hiện:

1. Bật IIS và các thành phần cần thiết.
2. Nếu thiếu module ASP.NET Core, tải Hosting Bundle đúng phiên bản từ Microsoft; kiểm tra SHA512 và chữ ký Microsoft trước khi cài.
3. Tạo hai app pool `No Managed Code` và hai site của project; chỉ bind `127.0.0.1`.
4. Cấp quyền đọc thư mục publish cho app pool.
5. Tạo Windows login SQL `IIS APPPOOL\OrderManagement.Api`, chỉ cấp quyền đọc/ghi dữ liệu và thực thi trong database của project; không cấp sysadmin hay db_owner.
6. Khởi động dịch vụ IIS và kiểm tra POST `/api/health` qua Gateway.

Khi Windows/Hosting Bundle yêu cầu khởi động lại, script dừng và báo rõ; restart máy rồi chạy lại. Script phải chạy trên máy có SQL instance `HUANPHAM\MSSQLSERVER01` và bằng tài khoản được phép tạo SQL login. Không dùng mật khẩu tài khoản Windows cá nhân cho app pool.

Tình trạng máy khi chuẩn bị: chưa bật IIS đầy đủ, chưa có Hosting Bundle, phiên Codex không có quyền administrator. Bản publish có thể tạo và kiểm thử ở quyền thường; việc cài service và xác nhận chạy thực tế bằng IIS cần hoàn tất bước quản trị ở trên. Không coi việc publish thành công là IIS đã hoạt động.

## Cập nhật và khôi phục

Sau khi sửa code, chạy lại `Publish-Iis.ps1`, sau đó chạy `Install-Iis.ps1` với quyền administrator, không cần `-InstallPrerequisites`. Script chuyển physical path của hai site sang bản mới. Không xóa bản cũ.

Để quay lại bản đã publish trước, truyền đường dẫn cụ thể:

```powershell
.\scripts\Install-Iis.ps1 -PublishDirectory 'E:\QuanLyDonDatHang\artifacts\iis\<ban-cu>'
```

Khôi phục code không tự khôi phục database. Nếu schema thay đổi không tương thích, cần kế hoạch khôi phục CSDL bằng bản sao lưu tương ứng.

## Kiểm tra và xử lý lỗi

- `/orders` hoặc `/permissions` trả Angular khi mở trực tiếp, tránh lỗi refresh trang.
- `/api/...` không tồn tại trả JSON 404, không trả nhầm HTML Angular.
- Gửi GET/PUT/DELETE vào `/api` trả JSON 405.
- API dừng hoặc mất kết nối: Gateway trả thông báo 502/503/504 trong cùng cấu trúc `status/value/message`.
- Lỗi SQL sau khi chạy IIS thường liên quan quyền của app pool; kiểm tra Windows login SQL trong bước cài đặt.
- Lỗi 500.30/500.31 xảy ra trước khi ứng dụng khởi động: xem Windows Event Viewer và phiên bản Hosting Bundle. Lỗi ở tầng IIS trước khi vào ASP.NET Core không thể do middleware của ứng dụng chuyển thành JSON.

Tài liệu Microsoft: [Host ASP.NET Core on Windows with IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/), [In-process hosting](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/in-process-hosting).

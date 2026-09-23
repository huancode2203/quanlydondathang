# Quản lý đơn đặt hàng

Ứng dụng Angular + ASP.NET Core API cho đơn hàng, khách hàng, hàng hóa và phân quyền; dữ liệu lưu trong SQL Server `QuanLyDonDatHangDB`.

## Kiến trúc

```text
Angular → API Gateway (YARP) → Order API Controller
                                  ↓
                         Repository Interface
                                  ↓
                         Repository Implementation
                           ├─ Dapper: đọc/lọc
                           └─ EF Core: ghi và transaction
                                  ↓
                         SQL Server
```

Các API nghiệp vụ dùng POST và JSON body. Response có dạng `{ "status": 200, "value": ..., "message": "..." }`; lỗi có thể thêm `errors` và `traceId`. Trang Angular và các file tĩnh vẫn được tải theo HTTP GET.

## Tính năng chính

- Tìm, lọc, sắp xếp, phân trang và quản lý đơn đặt hàng.
- Quản lý khách hàng, hàng hóa, tồn kho và quyền theo nhóm/tài khoản.
- Nhiều nhóm quyền trên một tài khoản; quyền thao tác yêu cầu quyền xem cùng phân hệ.
- Đơn chờ/đang xử lý giữ lượng hàng khả dụng; đơn hủy nhả lượng giữ; đơn giao thành công trừ tồn thực tế một lần.
- CSDL có các ràng buộc miền dữ liệu cho ngày, trạng thái, tiền và số lượng.

## Cấu hình phát triển

SQL Server local được cấu hình trong [appsettings.json](Backend/Services/Order/Order.Api/appsettings.json). Máy phát triển hiện dùng instance `HUANPHAM\MSSQLSERVER01`, database `QuanLyDonDatHangDB`, và .NET 11 Preview SDK theo `global.json`.

Để phát triển bằng ba tiến trình riêng, chọn task **Run full project** trong VS Code. Để triển khai dùng IIS, chạy [scripts/Publish-Iis.ps1](scripts/Publish-Iis.ps1), sau đó chạy [scripts/Install-Iis.ps1](scripts/Install-Iis.ps1) trong Windows PowerShell với quyền Administrator. IIS được cấu hình để chạy giao diện và API qua một địa chỉ web; hướng dẫn và giới hạn máy hiện tại ở [docs/IIS_VA_TRIEN_KHAI.md](docs/IIS_VA_TRIEN_KHAI.md).

Tạo database mới bằng `sqlcmd -S 'HUANPHAM\MSSQLSERVER01' -E -C -b -i Database/Initialize.sql`. Với database đã có dữ liệu, sao lưu trước, chạy `Database/007_NormalizeDomains.sql` với `ApplyChanges=0` để xem trước; chỉ chạy `ApplyChanges=1` sau khi preflight báo PASS. Migration 007 không tính lại tồn kho lịch sử.

Để chạy kiểm thử contract HTTP độc lập: `dotnet run --project Backend/Tests/Order.Api.ContractTests -c Release`. Tùy chọn `--database` dựng database thử nghiệm mới, áp dụng schema hiện hành, chạy CRUD/tồn kho qua HTTP rồi xóa database thử nghiệm.

Giải thích Entity/DTO, hợp đồng POST và lỗi request nằm trong [docs/API_DTO_VA_XU_LY_LOI.md](docs/API_DTO_VA_XU_LY_LOI.md). Sơ đồ folder và công dụng file nằm trong [docs/KIEN_TRUC_VA_CONG_DUNG_FILE.md](docs/KIEN_TRUC_VA_CONG_DUNG_FILE.md).

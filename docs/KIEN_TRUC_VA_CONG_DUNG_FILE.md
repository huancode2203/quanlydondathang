# Kiến trúc và công dụng các folder, file

## 1. Luồng tổng thể

```text
Angular → API Gateway (5000) → Controller (5001)
        → Repository Interface → Repository Implementation
        → EF Core hoặc Dapper → SQL Server
```

- Angular hiển thị giao diện, kiểm tra dữ liệu sớm và gọi HTTP API.
- API Gateway là cửa vào duy nhất của frontend, chuyển tiếp `/api/*` sang service.
- Controller nhận request, xác thực JWT, kiểm tra quyền và trả HTTP status; không viết SQL.
- Interface định nghĩa contract để Controller không phụ thuộc lớp cài đặt.
- Repository Implementation chứa nghiệp vụ dữ liệu và giao tiếp SQL Server.
- EF Core dùng cho thêm/sửa/xóa và transaction. Dapper dùng cho danh sách, lọc, join và đăng nhập.

## 2. Folder gốc

| Folder/file | Công dụng |
|---|---|
| `Backend/` | API Gateway, service và thư viện repository ASP.NET Core. |
| `Frontend/` | Ứng dụng Angular cho đăng nhập, đơn hàng, khách hàng và hàng hóa. |
| `Database/` | Script tạo database và script nâng cấp. |
| `docs/` | Tài liệu kỹ thuật. |
| `.vscode/tasks.json` | Task `Run full project` chạy Order API, Gateway và Angular. |
| `.gitignore` | Loại `node_modules`, `bin`, `obj`, `dist` và file tạm khỏi Git. |
| `global.json` | Cố định .NET SDK dùng trên máy hiện tại. |
| `README.md` | Hướng dẫn chạy và danh sách API. |

## 3. Backend

### `Backend/APIGateway`

| File | Công dụng |
|---|---|
| `APIGateway.csproj` | Project Gateway và package YARP Reverse Proxy. |
| `Program.cs` | Khởi tạo YARP, CORS và health check. |
| `appsettings.json` | Chuyển `/api/{**catch-all}` đến `http://localhost:5001`. |
| `Properties/launchSettings.json` | Chạy Gateway tại cổng `5000`. |

Gateway không xử lý nghiệp vụ hoặc truy cập database.

### `Backend/Services/Order/Order.Api`

| File | Công dụng |
|---|---|
| `Order.Api.csproj` | Project API, tham chiếu `sv.Order` và JWT Bearer. |
| `Program.cs` | Đăng ký Controller, DbContext, Repository, JWT, CORS và DI. |
| `appsettings.json` | Chuỗi kết nối SQL Server và cấu hình ký JWT local. |
| `Controllers/AuthController.cs` | Đăng nhập và phát JWT chứa nhân viên/quyền. |
| `Controllers/OrdersController.cs` | CRUD, tìm kiếm đơn, kiểm tra `ORDER_*`, lấy người tạo từ JWT. |
| `Controllers/CustomersController.cs` | CRUD khách hàng, kiểm tra quyền khách hàng. |
| `Controllers/ProductsController.cs` | CRUD hàng hóa/tồn kho, kiểm tra quyền hàng hóa. |
| `Controllers/PermissionsController.cs` | Đọc/cập nhật quyền theo nhóm và từng tài khoản, chỉ cho phép `PERMISSION_MANAGE`. |
| `Properties/launchSettings.json` | Chạy Order API tại cổng `5001`. |

### `Backend/Common/sv.Order`

Đây là tầng dùng chung đúng cấu trúc `Interface → Implement`.

#### `Data`

- `OrderDbContext.cs`: ánh xạ class C# với bảng/cột SQL Server, precision, computed column, quan hệ và trigger.

#### `Entities`

| File | Bảng tương ứng |
|---|---|
| `OrderEntity.cs` | `tbl_DonDatHang` |
| `OrderItemEntity.cs` | `tbl_ChiTietDonDatHang` |
| `CustomerEntity.cs` | `tbl_KhachHang` |
| `ProductEntity.cs` | `tbl_HangHoa`, gồm `SoLuongTon` |
| `RoleEntity.cs` | `tbl_NhomQuyen` |
| `PermissionEntity.cs` | `tbl_Quyen` |
| `RolePermissionEntity.cs` | `tbl_CapQuyen` |
| `AccountEntity.cs` | `tbl_TaiKhoan`, gồm nhóm và chế độ dùng quyền riêng |
| `AccountPermissionEntity.cs` | `tbl_CapQuyenTaiKhoan` |

#### `DTOs`

| File | Công dụng |
|---|---|
| `OrderDtos.cs` | Request tìm kiếm nâng cao, danh sách/chi tiết đơn, request lưu và phân trang. |
| `AuthDtos.cs` | Request đăng nhập, user xác thực và response JWT. |
| `MasterDataDtos.cs` | Request/response khách hàng và hàng hóa. |
| `PermissionDtos.cs` | Danh sách tài khoản/nhóm/quyền và request cập nhật quyền. |

DTO là dữ liệu trao đổi giữa API và frontend, tách khỏi Entity để client không sửa cột hệ thống.

#### `Repository/Interface`

| File | Contract |
|---|---|
| `IOrderRepository.cs` | Truy vấn, tạo, sửa, xóa đơn và tải danh mục cho form. |
| `IAuthRepository.cs` | Xác thực tài khoản và tải quyền. |
| `IMasterDataRepository.cs` | CRUD khách hàng và hàng hóa. |
| `IPermissionRepository.cs` | Tải cấu hình quyền, cập nhật quyền nhóm và quyền từng tài khoản. |

Interface không chứa SQL hoặc phần thân hàm.

#### `Repository/Implement`

| File | Công dụng |
|---|---|
| `OrderRepository.cs` | Dapper cho tìm kiếm/chi tiết; EF Core cho CRUD/transaction; validation ngày, trùng hàng và tổng âm. |
| `AuthRepository.cs` | Dapper đọc tài khoản, nhân viên, nhóm quyền, quyền và cập nhật lần đăng nhập. |
| `MasterDataRepository.cs` | Dapper tải danh sách; EF Core thêm/sửa/xóa mềm khách hàng/hàng hóa. |
| `PermissionRepository.cs` | Dapper tải ma trận quyền; EF Core cập nhật nhóm/quyền tài khoản và quyền nhóm. |

## 4. Frontend Angular

### File cấu hình

| File | Công dụng |
|---|---|
| `angular.json` | Cấu hình build, serve, test và budget. |
| `package.json` | Dependency và lệnh `start`, `build`, `test`. |
| `src/main.ts` | Điểm khởi động Angular. |
| `src/index.html` | HTML gốc và tiêu đề. |
| `src/styles.scss` | Điểm nạp duy nhất cho hệ thống CSS dùng chung. |

### `src/app`

| File/folder | Công dụng |
|---|---|
| `app.ts` | Component gốc, phiên đăng nhập và đăng xuất. |
| `app.html` | Sidebar, menu và vùng `router-outlet`. |
| `app.config.ts` | Router, HttpClient và interceptor JWT. |
| `app.routes.ts` | Route login/orders/customers/products/permissions và các guard. |
| `app.spec.ts` | Unit test component gốc. |

### `core`

- `models/`: kiểu TypeScript cho đăng nhập, đơn, khách hàng và hàng hóa.
- `services/auth.service.ts`: đăng nhập, lưu phiên, kiểm tra quyền và chọn màn hình mặc định được phép truy cập.
- `services/order-api.service.ts`: API đơn và query string bộ lọc.
- `services/master-data-api.service.ts`: CRUD khách hàng/hàng hóa.
- `services/permission-api.service.ts`: tải và lưu cấu hình quyền theo nhóm.
- `interceptors/auth.interceptor.ts`: gắn JWT và xử lý HTTP 401.
- `guards/auth.guard.ts`: chặn route khi chưa đăng nhập.
- `guards/permission.guard.ts`: chặn từng route nếu thiếu quyền xem tương ứng và chuyển đến phân hệ được phép.

### `features`

| Folder | Công dụng |
|---|---|
| `login/` | Form đăng nhập tối giản. |
| `orders/` | Nghiệp vụ chính: lọc, CRUD đơn, tạo nhanh khách hàng, tồn kho, validation. |
| `customers/` | Danh sách và CRUD khách hàng đơn giản. |
| `products/` | Danh sách và CRUD hàng hóa/tồn kho đơn giản. |
| `permissions/` | Chọn nhóm tài khoản và cấp/bỏ quyền; khóa toàn quyền của Admin. |
| `no-access/` | Thông báo khi tài khoản chưa được cấp quyền xem phân hệ nào. |
| `shared/ui/page-toolbar.component.ts` | Thanh tiêu đề, mô tả, tìm kiếm và vùng nút thao tác dùng lại ở các màn hình. |
| `shared/styles/foundation.scss` | Token màu, font, nền và quy tắc nền tảng của toàn ứng dụng. |
| `shared/styles/index.scss` | Nạp toàn bộ stylesheet dùng chung từ một đầu mối. |
| `shared/styles/*.scss` | CSS tập trung cho shell, form, danh mục, đơn hàng và phân quyền; không đặt CSS trong feature. |

Mỗi feature chỉ giữ `.ts` xử lý logic và `.html` lắp các shared component cần dùng. Khi đổi giao diện của thành phần dùng chung, sửa tại `shared/ui` và `shared/styles`, không thêm CSS riêng vào từng màn hình.

## 5. Database

| File | Công dụng |
|---|---|
| `QuanLyDonDatHangDB.sql` | Script đầy đủ tạo database, bảng, khóa, index, trigger, function và dữ liệu mẫu. |
| `002_AddProductStock.sql` | Bổ sung `SoLuongTon` cho database đã tồn tại và cập nhật tồn mẫu. |
| `003_EnsureAdminFullPermissions.sql` | Bổ sung mọi quyền còn thiếu cho Admin, chạy lại an toàn. |
| `004_AddAccountPermissions.sql` | Thêm cờ quyền riêng và bảng cấp quyền trực tiếp cho tài khoản. |

Trigger `trg_CapNhatTongTienDonHang` tính lại `TongTienHang`. `ThanhTien` và `TongThanhToan` là computed column nên API không ghi trực tiếp.

## 6. Quy tắc nghiệp vụ

- Người tạo đơn lấy từ `employee_id` trong JWT; frontend không thể gán người khác.
- Ngày đặt từ 10 năm trước đến hiện tại.
- Ngày/giờ giao dự kiến bắt buộc, sau hiện tại và sau ngày đặt.
- Một hàng hóa chỉ xuất hiện một lần trong đơn.
- Số lượng đặt được lớn hơn tồn kho nhưng giao diện cảnh báo.
- Trang Hàng hóa tính `tồn sau đơn = tồn thực tế - số lượng trong các đơn chưa giao/chưa hủy`; kết quả âm và sản phẩm thiếu được tô đỏ.
- Tổng thanh toán âm bị chặn ở Angular và Repository.
- Lọc nâng cao theo người tạo, người giao, khoảng tổng tiền; sắp xếp ngày giao/tổng tiền.
- Bộ lọc và phân trang của danh sách đơn được giữ cố định; chỉ thân bảng cuộn.
- Nhóm Admin luôn nhận toàn bộ quyền hoạt động ở cả dữ liệu cấp quyền và lúc phát JWT.
- Thay đổi quyền áp dụng cho tài khoản ở lần đăng nhập tiếp theo.
- Tài khoản mặc định kế thừa quyền nhóm; khi tùy chỉnh, tập quyền riêng thay thế quyền nhóm.
- Chọn lại “Theo quyền nhóm” sẽ xóa quyền riêng và tiếp tục kế thừa các lần thay đổi sau.
- Menu Đơn đặt hàng, Khách hàng, Hàng hóa và Phân quyền chỉ hiện khi phiên đăng nhập có quyền xem tương ứng.

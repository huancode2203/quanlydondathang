# Quản lý đơn đặt hàng

Ứng dụng Angular và ASP.NET Core Web API quản lý đơn đặt hàng, kết nối trực tiếp database `QuanLyDonDatHangDB` trên SQL Server.

## Chức năng

- Tải danh sách đơn đặt hàng có phân trang.
- Đăng nhập JWT bằng tài khoản trong `tbl_TaiKhoan`.
- Tìm kiếm cơ bản và lọc nâng cao theo người tạo, người giao, khoảng tổng tiền.
- Sắp xếp theo ngày giao hoặc tổng tiền tăng/giảm.
- Thêm, xem/sửa và xóa đơn cùng toàn bộ chi tiết hàng hóa.
- Tạo nhanh khách hàng ngay trong form thêm đơn.
- Quản lý khách hàng, hàng hóa và tồn kho ở các màn hình tối giản.
- Cảnh báo số lượng đặt vượt tồn kho nhưng vẫn cho phép lưu.
- Kiểm tra miền ngày và chặn tổng thanh toán âm.
- Tính thành tiền từng dòng và tổng thanh toán.
- Kiểm tra quyền từ `tbl_TaiKhoan`, `tbl_NhomQuyen`, `tbl_CapQuyen`, `tbl_Quyen`.
- Giao diện responsive bằng Angular.

## Kiến trúc

```text
Angular
  └─ OrderApiService
       ↓ http://localhost:5000
API Gateway (YARP)
       ↓ http://localhost:5001
Services/Order/Order.Api/Controllers/OrdersController
       ↓
Common/sv.Order/Repository/Interface/IOrderRepository
       ↓
Common/sv.Order/Repository/Implement/OrderRepository
       ├─ Dapper: tìm kiếm, chi tiết, danh mục, đăng nhập
       └─ EF Core: thêm, sửa, xóa và transaction
              ↓
SQL Server / QuanLyDonDatHangDB
```

## Cấu hình hiện tại

- SQL Server: `HUANPHAM\MSSQLSERVER01`
- Database: `QuanLyDonDatHangDB`
- API Gateway: `http://localhost:5000`
- Order API: `http://localhost:5001`
- Angular: `http://localhost:4200`
- Tài khoản demo: `admin` / `123456`

Chuỗi kết nối nằm tại `Backend/Services/Order/Order.Api/appsettings.json`.

## Chạy dự án
Mở ba terminal.

Terminal 1 — Order API:

```powershell
cd ...\Backend\Services\Order\Order.Api
dotnet run
```

Terminal 2 — API Gateway:

```powershell
cd ...\Backend\APIGateway
dotnet run
```

Terminal 3 — Angular:

```powershell
cd ...\Frontend\OrderManagement.Web
npm install
npm start
```

Truy cập `http://localhost:4200`.

## API

| Method | Endpoint | Chức năng | Quyền |
|---|---|---|---|
| POST | `/api/auth/login` | Đăng nhập và nhận JWT | Không yêu cầu |
| GET | `/api/orders` | Danh sách và tìm kiếm | `ORDER_VIEW` |
| GET | `/api/orders/{id}` | Chi tiết đơn | `ORDER_VIEW` |
| GET | `/api/orders/lookups` | Khách hàng, hàng hóa, nhân viên | `ORDER_VIEW` |
| POST | `/api/orders` | Thêm đơn | `ORDER_CREATE` |
| PUT | `/api/orders/{id}` | Sửa đơn | `ORDER_UPDATE` |
| DELETE | `/api/orders/{id}` | Xóa đơn | `ORDER_DELETE` |
| GET/POST/PUT/DELETE | `/api/customers` | Quản lý khách hàng | `CUSTOMER_*` |
| GET/POST/PUT/DELETE | `/api/products` | Quản lý hàng hóa và tồn kho | `PRODUCT_*` |

Angular gửi JWT Bearer tự động. Người tạo đơn được lấy từ claim `employee_id`, không lấy từ dữ liệu do frontend tự nhập.

## Database

Script gốc nằm tại `Database/QuanLyDonDatHangDB.sql`. File `Database/002_AddProductStock.sql`

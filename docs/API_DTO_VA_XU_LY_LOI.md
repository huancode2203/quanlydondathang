# Contract API và vai trò DTO

## Phân biệt Entity và DTO

EF Core không tạo sẵn DTO cho HTTP API. Khi scaffold database, EF tạo **Entity** và **DbContext**: Entity ánh xạ bảng, khóa, cột, quan hệ và phục vụ theo dõi thay đổi. DTO là dữ liệu vào/ra của từng nghiệp vụ và cần được thiết kế theo nhu cầu màn hình.

Ví dụ `OrderEntity` có `ManagerEmployeeId`, `StockDeducted`, ngày tạo/cập nhật và navigation `Items`. Khi tạo đơn, client không được tự gán người tạo, cờ đã trừ kho hoặc tổng tiền đã tính. Request DTO chỉ nhận các trường người dùng được phép nhập; người tạo lấy từ JWT và tổng tiền được hệ thống tính. Response chi tiết lại cần tên khách hàng, tên nhân viên, thành tiền từng dòng: dữ liệu tổng hợp không tương ứng một bảng duy nhất.

| Loại | Trách nhiệm | Ví dụ |
|---|---|---|
| Entity | Ánh xạ và lưu dữ liệu bằng EF Core | `OrderEntity`, `AccountEntity` |
| Request DTO | Contract đầu vào, kiểu dữ liệu, validation | `SaveOrderRequest`, `OrderSearchRequest` |
| Response DTO | Chỉ trả trường màn hình cần | `OrderDetailDto`, `LoginResponse` |
| API envelope | Trạng thái xử lý và thông báo thống nhất | `status`, `value`, `message` |

Tách DTO giúp không lộ hash mật khẩu hay navigation nhạy cảm, tránh client gửi thêm trường để sửa cột hệ thống, và tránh đổi schema kéo theo thay đổi giao diện. Có thể dùng EF `Select(...)` để chiếu trực tiếp sang DTO; không cần đọc Entity đầy đủ rồi sao chép tất cả. Với Dapper, kết quả SQL cũng được map trực tiếp sang DTO.

## Kế thừa DTO cho bộ lọc

Các bộ lọc dùng chung từ khóa, phân trang được gom vào lớp cơ sở: `SearchRequest` → `PagedFilterRequest` → `OrderSearchRequest`. Lớp con bổ sung trạng thái, người tạo/người giao, khoảng ngày, khoảng tiền và sắp xếp. Validation chung nằm ở lớp cơ sở. Angular có kiểu bộ lọc tương ứng để cùng một tên trường xuyên suốt.

Kế thừa chỉ dùng khi có quan hệ thực sự: bộ lọc đơn là một bộ lọc có phân trang; request cập nhật là dữ liệu lưu có thêm ID. Không cho DTO kế thừa Entity vì sẽ mang theo các cột nội bộ và làm mất ranh giới giữa contract và lưu trữ.

## Quy ước request/response

Tất cả API nghiệp vụ dùng `POST` và `Content-Type: application/json`. ID nằm trong body. Lọc không nằm trên query string.

```json
{
  "page": 1,
  "pageSize": 20,
  "keyword": "DH001",
  "status": "CHO_XAC_NHAN",
  "minTotal": 0,
  "maxTotal": 10000000
}
```

Body trên gửi đến `/api/orders/search`. Chi tiết và xóa dùng `{ "id": 123 }`. Cập nhật dùng `{ "id": 123, ...các trường được phép lưu }`. Thao tác không có điều kiện như `/api/orders/lookups` dùng `{}`. Không bọc thêm `status/value/message` vào request: đây là contract phản hồi. Trường `status` trong request đơn là trạng thái nghiệp vụ, khác `status` số ở envelope.

```json
{
  "status": 200,
  "value": { "items": [], "total": 0, "page": 1, "pageSize": 20 },
  "message": "Thành công"
}
```

`status` là mã HTTP số, trùng với HTTP status thật. `value` chứa dữ liệu kết quả hoặc `null` khi lỗi. `message` là thông báo cho người sử dụng, viết đúng tên trường thay vì `messeger`. Lỗi validation có thêm `errors` theo tên trường; lỗi có mã tra cứu `traceId` để đối chiếu log máy chủ.

Không trả HTTP 200 khi thất bại. Angular dùng client chung để gửi POST, lấy `value` và đọc thông báo/lỗi từng trường; mỗi màn hình không tự viết lại quy tắc giải mã.

## Những request không hợp lệ

| Trường hợp | HTTP |
|---|---|
| JSON lỗi cú pháp, thiếu body/trường bắt buộc, gửi sai kiểu, trường lạ, ID/tiền/ngày/bộ lọc không hợp lệ | 400 |
| Chưa đăng nhập hoặc token không hợp lệ/hết hạn | 401 |
| Đã đăng nhập nhưng thiếu quyền | 403 |
| Không tìm thấy đường dẫn hoặc bản ghi | 404 |
| Gọi API bằng phương thức khác POST | 405 |
| Trùng mã, trạng thái không cho sửa, xung đột dữ liệu | 409 |
| Body vượt giới hạn ứng dụng | 413 |
| Content-Type không được hỗ trợ | 415 |
| Lỗi bất ngờ trong ứng dụng | 500 |
| Gateway không kết nối được API | 502/503/504 |

Lỗi dự đoán được trả thông báo nghiệp vụ. Lỗi chưa dự đoán được được ghi log có mã tra cứu, trả thông báo an toàn thay vì SQL, stack trace hay chuỗi kết nối. Lớp xử lý lỗi chung bao phủ toàn pipeline; controller tập trung nhận request, kiểm quyền và gọi repository, không chứa SQL.

Quyền trên đơn được kiểm tra lại trong API: `ORDER_APPROVE` cần cho các bước xác nhận/chuẩn bị, `ORDER_DELIVERY` cần cho các bước giao nhận; tài khoản giao hàng không thể dùng body cập nhật để sửa khách hàng, giá hay số lượng. Sửa nội dung đơn cần `ORDER_UPDATE`. Cả hai quyền theo luồng trạng thái đều cần `ORDER_VIEW`.

Giới hạn và validation ở ba lớp bổ sung cho nhau: Angular phản hồi sớm; API là nơi kiểm soát request; ràng buộc SQL bảo vệ dữ liệu cả khi ghi ngoài API. Ràng buộc miền giá trị trong SQL không nên dùng quy tắc “ngày giao luôn lớn hơn hiện tại”, vì một đơn hợp lệ hôm nay vẫn phải đọc/cập nhật được khi đã giao trễ vào ngày mai.

Request tải trang, tài nguyên tĩnh và CORS preflight thuộc giao thức trình duyệt, không phải phương thức của API nghiệp vụ. Không đổi toàn bộ HTTP của website sang POST vì khi đó trình duyệt không thể mở trang và tải tài nguyên theo cách thông thường.

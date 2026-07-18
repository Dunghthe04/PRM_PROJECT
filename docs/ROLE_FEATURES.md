# FSchool — Chức năng theo vai trò & Endpoint

Tài liệu dành cho người vào làm tiếp: mỗi chức năng ghi **mục đích** + **endpoint cụ thể** (đang có trên API).

- Prefix: `/api`
- Auth: JWT (`Authorization: Bearer <token>`), trừ mục đánh dấu *Public*
- App: 1 codebase Flutter (mobile + Admin dùng Web/Desktop)
- Seed demo: `backend/Common/DbSeeder.cs` · MK chung `123456`
- Chi tiết kỹ thuật cũ hơn (có thể lệch UI): [API_ENDPOINTS.md](API_ENDPOINTS.md) · SRS: [SRS.md](SRS.md)

### Tài khoản demo (sau seed)

| SĐT | Role | Ghi chú |
|---|---|---|
| `0900000000` | Admin | Quản trị |
| `0900000002` | Teacher | GV chính (Nguyễn Văn A) |
| `0900000006` | Teacher | GV phụ (Tiếng Anh) |
| `0900000003` | Parent | 2 con → Switch Profile |
| `0900000004` | Student | Nguyễn Văn An (con 1) |
| `0900000005` | Student | Nguyễn Thị Bình (con 2) |
| `0900000007` | Student | Cùng lớp 10A1 |

---

## Công nghệ áp dụng trong dự án

### Tổng quan kiến trúc

| Tầng | Công nghệ | Mục đích |
|---|---|---|
| Mobile / Admin UI | **Flutter** (Dart) | 1 app cho 4 role; Admin chạy `flutter run -d chrome` (layout rộng) |
| Backend API | **.NET 8** Web API (C#) | REST JSON, JWT, Swagger |
| Database | **SQL Server** + **EF Core** | Lưu nghiệp vụ; migration tạo schema |
| Auth | **JWT** + **BCrypt** | Token đăng nhập; hash mật khẩu |
| Push | **Firebase Cloud Messaging** | Thông báo đẩy (FCM) |
| Thanh toán | **VNPay** · **PayOS** | Cổng học phí (không lưu thẻ) |

```
Flutter App (dio + JWT)
        │  HTTP /api/*
        ▼
.NET 8 Web API  →  Controllers → Services → Repositories
        │
        ▼
   SQL Server (EF Core)
```

---

### Mobile (Flutter) — thư mục `mobile-app/`

| Công nghệ / package | Mục đích trong dự án | File / chỗ dùng điển hình |
|---|---|---|
| **Flutter + Dart** | UI đa nền tảng (Android / iOS / Web Admin) | `mobile-app/lib/` |
| **MVC** (`controller` / `model` / `view`) | Tách màn hình · model DTO · gọi API | `lib/vn/edu/fpt/{controller,model,view}` |
| **dio** | HTTP client gọi endpoint backend (GET/POST/…) | `service/api_client.dart` → mọi `*Controller` |
| **Interceptor JWT** | Tự gắn `Authorization: Bearer …` vào mọi request | Trong `ApiClient` (dio interceptor) |
| **flutter_secure_storage** | Lưu JWT an toàn (không để SharedPreferences thô) | `service/token_storage.dart` |
| **go_router** | Điều hướng theo route / role sau login | `main.dart` / router |
| **setState** | State management (không dùng Bloc/Riverpod) | Các `StatefulWidget` trong `view/` |
| **firebase_core** + **firebase_messaging** | Nhận push FCM trên thiết bị | Đăng ký token → `POST /api/devices/register` |
| **image_picker** | Chọn ảnh (avatar, giấy y tế đơn nghỉ) | Upload qua `/api/files/upload` hoặc avatar |
| **url_launcher** | Mở link cổng thanh toán VNPay/PayOS trong browser | Màn học phí |
| **qr_flutter** | Vẽ QR VietQR (PayOS) trên app | Màn thanh toán |
| Theme brand | Cam `#FF6B00` + trắng | `main.dart` |

**Luồng gọi API điển hình (mobile):**

1. User login → `AuthController` dùng **dio** `POST /api/user/login`
2. Lưu token bằng **flutter_secure_storage**
3. Màn sau (vd. TKB) → `TimetableController` → dio `GET /api/timetable/me` (interceptor gắn JWT)
4. Parse JSON → `*Model.fromJson` → `setState` cập nhật UI

---

### Backend (.NET) — thư mục `backend/`

| Công nghệ / package | Mục đích trong dự án | File / chỗ dùng điển hình |
|---|---|---|
| **.NET 8 Web API** | Host REST API | `backend/Program.cs` |
| **Kiến trúc 3 lớp** | Controller → Service → Repository | `Controllers/` · `Services/` · `Repositories/` |
| **Entity Framework Core** | ORM map C# ↔ SQL Server | `Models/AppDbContext.cs` |
| **EF Migrations** | Tạo/cập nhật **schema** (bảng, cột, index) — *không phải seed data* | `backend/Migrations/` |
| **DbSeeder** | Insert/xóa **dữ liệu demo** lúc start Development | `Common/DbSeeder.cs` |
| **SQL Server** | Database chính | Connection string `appsettings*.json` |
| **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`) | Xác thực request; claim `userId` + `role` | `Common/JwtHelper.cs` · `[Authorize]` |
| **BCrypt.Net-Next** | Hash / verify mật khẩu | `UserService` · seed |
| **Swashbuckle (Swagger)** | Thử API trên browser (`/swagger`) | `Program.cs` |
| **FirebaseAdmin** | Gửi push FCM từ server | `PushNotificationService` |
| **VNPay / PayOS SDK-style helpers** | Tạo link thanh toán + verify checksum/webhook | `Services/PaymentService.cs` · `Common/*Helper` |

**Luồng 1 request API điển hình:**

1. Client dio gọi `GET /api/grades/me` + header JWT  
2. **JWT middleware** kiểm tra token → lấy `userId`, `role`  
3. **GradesController** → **GradeService** (nghiệp vụ) → **GradeRepository** (EF query)  
4. Trả JSON DTO → Flutter parse thành `GradeModel`

---

### Tích hợp bên thứ ba

| Dịch vụ | Mục đích | Ghi chú |
|---|---|---|
| **Firebase FCM** | Push thông báo (điểm mới, đơn duyệt, nhắc phí…) | Mobile: Messaging · Server: FirebaseAdmin |
| **VNPay** | Thanh toán học phí | `POST /api/payments/vnpay/create` + return/IPN |
| **PayOS** | Thanh toán học phí (+ QR) | `POST /api/payments/payos/create` + webhook |
| **OTP SMS** | Xác thực đăng ký (SĐT) | Dev: log console `[DEV OTP SMS]` |
| **OTP Email** | Quên mật khẩu (FR1.2) | Gửi Gmail thật qua SMTP (`Smtp` trong appsettings) |

---

### Công cụ dev thường dùng

| Công cụ | Mục đích |
|---|---|
| `dotnet run` / Visual Studio | Chạy API (port vd. `http://127.0.0.1:5177`) |
| Swagger UI | Test endpoint không cần app |
| `flutter run` / `-d chrome` | App mobile hoặc Admin web |
| SSMS / Azure Data Studio | Xem dữ liệu SQL Server |
| `dotnet ef migrations add` / `database update` | Đổi schema DB |

---

## 0. Chung mọi role (sau khi đăng nhập)

### 0.1 Đăng ký / Đăng nhập / Quên mật khẩu

| Mục đích | Method | Endpoint | Role |
|---|---|---|---|
| Đăng nhập SĐT + MK → JWT | POST | `/api/user/login` | *Public* |
| Đăng ký SĐT + MK → gửi OTP | POST | `/api/user/register` | *Public* |
| Xác thực OTP đăng ký | POST | `/api/auth/verify-phone` | *Public* |
| Gửi lại OTP | POST | `/api/auth/resend-otp` | *Public* |
| Quên MK → gửi OTP về Email | POST | `/api/auth/forgot-password` | *Public* |
| Verify OTP quên MK (email) → `resetToken` | POST | `/api/auth/verify-otp` | *Public* |
| Đặt MK mới bằng `resetToken` | POST | `/api/auth/reset-password` | *Public* |

### 0.2 Hồ sơ cá nhân

| Mục đích | Method | Endpoint | Role |
|---|---|---|---|
| Xem hồ sơ đang đăng nhập | GET | `/api/account/me` | Mọi user |
| Sửa họ tên / email (không đổi SĐT) | PUT | `/api/account/me` | Mọi user |
| Cập nhật URL avatar | PUT | `/api/account/avatar` | Mọi user |
| Upload file avatar | POST | `/api/account/avatar/upload` | Mọi user |
| Đổi mật khẩu (cần MK cũ) | PUT | `/api/account/change-password` | Mọi user |

### 0.3 Thông báo in-app + Push device

| Mục đích | Method | Endpoint | Role |
|---|---|---|---|
| Danh sách thông báo (phân trang) | GET | `/api/notifications` | Mọi user |
| Số chưa đọc (badge) | GET | `/api/notifications/unread-count` | Mọi user |
| Đánh dấu 1 tin đã đọc | PUT | `/api/notifications/{id}/read` | Mọi user |
| Đánh dấu tất cả đã đọc | PUT | `/api/notifications/read-all` | Mọi user |
| Đăng ký FCM token | POST | `/api/devices/register` | Mọi user |
| Gỡ FCM token | DELETE | `/api/devices/{token}` | Mọi user |

### 0.4 Bảng tin / Upload file / App

| Mục đích | Method | Endpoint | Role |
|---|---|---|---|
| Xem bảng tin (toàn trường + lớp liên quan) | GET | `/api/announcements` | Mọi user |
| Chi tiết 1 bảng tin | GET | `/api/announcements/{id}` | Mọi user |
| Upload ảnh/file (đơn nghỉ, avatar…) | POST | `/api/files/upload` | Mọi user |
| Force update — kiểm tra version | GET | `/api/app/version` | *Public* |
| Health check | GET | `/api/health` | *Public* |

### 0.5 Danh mục dùng chung (chỉ đọc)

| Mục đích | Method | Endpoint | Role |
|---|---|---|---|
| DS học kỳ (lọc TKB/điểm) | GET | `/api/semesters` | Đã login |
| Chi tiết học kỳ | GET | `/api/semesters/{id}` | Đã login |
| DS môn học | GET | `/api/subjects` | Đã login |
| DS lớp (`?semesterId=`) | GET | `/api/classes` | Đã login |
| Roster HS trong lớp | GET | `/api/classes/{id}/students` | Đã login (GV điểm danh) |

---

## 1. Student (Học sinh)

> Mục tiêu: xem học tập của chính mình, nộp đơn nghỉ, xem học phí (nếu có), nhận thông báo.

### 1.1 Thời khóa biểu

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| Xem TKB tuần của mình | GET | `/api/timetable/me?weekStart=&semesterId=` | Bỏ qua `studentId` |

### 1.2 Bảng điểm

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| Xem điểm đã **Published** | GET | `/api/grades/me?semesterId=` | Chỉ điểm công bố |

### 1.3 Chuyên cần

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| Xem lịch sử điểm danh của mình | GET | `/api/attendance/me?from=&to=` | P / A / L |

### 1.4 Đơn xin nghỉ

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| DS đơn của mình | GET | `/api/leave-requests/me` | |
| Chi tiết đơn | GET | `/api/leave-requests/{id}` | |
| Tạo đơn (có thể kèm ảnh y tế) | POST | `/api/leave-requests` | Upload ảnh trước qua `/api/files/upload` |
| Hủy đơn khi còn Pending | DELETE | `/api/leave-requests/{id}` | |

### 1.5 Học phí / biên lai

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| DS hóa đơn của mình | GET | `/api/fee-invoices/me` | |
| Chi tiết hóa đơn | GET | `/api/fee-invoices/{id}` | |
| Xem biên lai khi đã Paid | GET | `/api/fee-invoices/{id}/receipt` | |
| Poll trạng thái thanh toán | GET | `/api/payments/status/{orderCode}` | Thường PH thanh toán |

### 1.6 UI app — không còn

- **Bài tập**: UI đã bỏ. Entity/DB vẫn có (seed); **không còn controller `/assignments`**.

---

## 2. Parent (Phụ huynh)

> Mục tiêu: quản lý nhiều con (Switch Profile), xem học tập/học phí theo con, nộp đơn hộ, thanh toán.

### 2.1 Switch Profile (chọn con)

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| Lấy danh sách con liên kết | GET | `/api/account/children` | Bắt buộc trước khi gọi API theo con |

**Quy ước khi gọi API học tập:** truyền `studentId` = id con đang chọn (nếu endpoint hỗ trợ).  
Ví dụ TKB: `GET /api/timetable/me?studentId={childId}&semesterId=`

### 2.2 Thời khóa biểu / Điểm / Chuyên cần theo con

| Mục đích | Method | Endpoint |
|---|---|---|
| TKB của con | GET | `/api/timetable/me?studentId=&weekStart=&semesterId=` |
| Điểm đã công bố của con | GET | `/api/grades/me?semesterId=` *(service lấy theo quan hệ PH–HS)* |
| Chuyên cần của con | GET | `/api/attendance/me?from=&to=` |

### 2.3 Đơn xin nghỉ (nộp hộ)

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| DS đơn của các con | GET | `/api/leave-requests/me` | |
| Tạo đơn hộ (body có `studentId`) | POST | `/api/leave-requests` | `SubmittedBy` = PH |
| Hủy đơn Pending | DELETE | `/api/leave-requests/{id}` | |

### 2.4 Học phí & thanh toán

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| Hóa đơn của các con | GET | `/api/fee-invoices/me` | Có `studentName` để phân biệt |
| Biên lai | GET | `/api/fee-invoices/{id}/receipt` | |
| Tạo thanh toán VNPay | POST | `/api/payments/vnpay/create` | Body: `{ feeInvoiceId }` |
| Tạo thanh toán PayOS | POST | `/api/payments/payos/create` | Body: `{ feeInvoiceId }` |
| Lịch sử giao dịch | GET | `/api/payments/history?studentId=` | |
| Poll đã Paid chưa | GET | `/api/payments/status/{orderCode}` | |
| (Dev) giả lập đã thanh toán | POST | `/api/payments/dev/simulate-paid?orderCode=` | Chỉ Development |

Webhook cổng (không gọi từ app):

| Mục đích | Method | Endpoint |
|---|---|---|
| VNPay return browser | GET | `/api/payments/vnpay/return` |
| VNPay IPN | POST | `/api/payments/vnpay/ipn` |
| PayOS webhook | POST | `/api/payments/payos/webhook` |

---

## 3. Teacher (Giáo viên)

> Mục tiêu: lịch dạy, điểm danh lớp, duyệt đơn nghỉ, gửi thông báo lớp.

### 3.1 Lớp được phân công / lịch dạy

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| DS lớp–môn được phân công | GET | `/api/teachers/{id}/classes` | `id` = userId GV |
| TKB / lịch dạy của GV | GET | `/api/timetable/teacher?weekStart=&semesterId=` | App tab “Lịch dạy” |
| Roster HS để điểm danh | GET | `/api/classes/{id}/students` | |

Phân công giảng dạy (thường Admin/Swagger/seed — **không có UI dựng phân công**):

| Mục đích | Method | Endpoint | Role |
|---|---|---|---|
| DS phân công | GET | `/api/teacher-assignments` | Login |
| Tạo / sửa / xóa phân công | POST/PUT/DELETE | `/api/teacher-assignments` … | **Admin** |

### 3.2 Điểm danh (online-only)

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| Xem điểm danh lớp theo ngày | GET | `/api/attendance?classId=&date=` | |
| Thống kê chuyên cần lớp | GET | `/api/attendance/summary?classId=&from=&to=` | |
| Lưu điểm danh hàng loạt P/A/L | POST | `/api/attendance/batch` | |
| Sửa 1 bản ghi | PUT | `/api/attendance/{id}` | |

> Offline sync đã **bỏ** — không còn `POST /api/attendance/sync`.

### 3.3 Duyệt đơn nghỉ

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| DS đơn (lọc lớp / status) | GET | `/api/leave-requests?classId=&status=` | Chỉ lớp được phân công |
| Duyệt đơn | PUT | `/api/leave-requests/{id}/approve` | → notify PH/HS |
| Từ chối đơn | PUT | `/api/leave-requests/{id}/reject` | Body: lý do |

### 3.4 Gửi bảng tin / thông báo lớp

| Mục đích | Method | Endpoint | Ghi chú |
|---|---|---|---|
| Đăng bảng tin lớp (Type=Class) | POST | `/api/announcements` | Có `targetClassId`, optional `subjectId` |
| Sửa / xóa bảng tin mình tạo | PUT/DELETE | `/api/announcements/{id}` | |

### 3.5 Điểm số (API có — UI GV nhập điểm đã bỏ)

Theo phạm vi dự án: **điểm Admin/Swagger/seed**; HS/PH chỉ xem. API vẫn dùng được:

| Mục đích | Method | Endpoint |
|---|---|---|
| Xem bảng điểm lớp | GET | `/api/grades?classId=&subjectId=&semesterId=` |
| Nhập điểm hàng loạt (Nháp) | POST | `/api/grades/batch` |
| Công bố điểm | POST | `/api/grades/publish` |
| Sửa / xóa điểm | PUT/DELETE | `/api/grades/{id}` |

### 3.6 UI app — không còn

- **Bài tập GV**: UI + controller `/assignments` đã bỏ.

---

## 4. Admin (Quản trị — Flutter Web/Desktop)

> Mục tiêu: user, danh mục, học phí, bảng tin toàn trường, báo cáo. Phân công GV / TKB / điểm thường seed hoặc Swagger.

### 4.1 Quản lý người dùng (FR5.1)

| Mục đích | Method | Endpoint |
|---|---|---|
| DS user (phân trang, lọc role/search) | GET | `/api/users?page=&pageSize=&role=&search=&isLocked=` |
| Chi tiết user | GET | `/api/users/{id}` |
| Tạo tài khoản | POST | `/api/users` |
| Sửa họ tên / email / role | PUT | `/api/users/{id}` |
| Xóa user | DELETE | `/api/users/{id}` |
| Khóa tài khoản | PUT | `/api/users/{id}/lock` |
| Mở khóa | PUT | `/api/users/{id}/unlock` |
| Reset mật khẩu | POST | `/api/users/{id}/reset-password` |

> Import Excel đã **bỏ** (không còn `/api/users/import`).

### 4.2 Danh mục: Kỳ · Môn · Lớp (FR5.2)

| Mục đích | Method | Endpoint |
|---|---|---|
| CRUD học kỳ | GET/POST/PUT/DELETE | `/api/semesters`, `/api/semesters/{id}` |
| CRUD môn | GET/POST/PUT/DELETE | `/api/subjects`, `/api/subjects/{id}` |
| CRUD lớp | GET/POST/PUT/DELETE | `/api/classes`, `/api/classes/{id}` |
| Gán HS vào lớp | POST | `/api/classes/{id}/students` |
| Gỡ HS khỏi lớp | DELETE | `/api/classes/{id}/students/{studentId}` |

### 4.3 Phân công GV + TKB (API — UI dựng đã bỏ)

| Mục đích | Method | Endpoint |
|---|---|---|
| CRUD phân công | GET/POST/PUT/DELETE | `/api/teacher-assignments` … |
| Xem TKB theo lớp | GET | `/api/timetable?classId=&weekStart=` |
| Thêm / sửa / xóa tiết TKB | POST/PUT/DELETE | `/api/timetable`, `/api/timetable/{id}` |

### 4.4 Tài chính (FR4.1, FR4.2)

| Mục đích | Method | Endpoint |
|---|---|---|
| CRUD loại khoản thu | GET/POST/PUT/DELETE | `/api/fee-categories` … |
| DS hóa đơn (lọc) | GET | `/api/fee-invoices?studentId=&status=` |
| Tạo hóa đơn 1 HS | POST | `/api/fee-invoices` |
| Tạo hóa đơn theo lớp (batch) | POST | `/api/fee-invoices/batch` |
| Xem cấu hình cổng | GET | `/api/payment-config` |
| Cập nhật cấu hình VNPay/PayOS | PUT | `/api/payment-config` |
| Lịch sử GD | GET | `/api/payments/history` |

### 4.5 Bảng tin toàn trường (FR5.4)

| Mục đích | Method | Endpoint |
|---|---|---|
| Đăng tin Global | POST | `/api/announcements` | Type = `Global` |
| Sửa / xóa | PUT/DELETE | `/api/announcements/{id}` |

### 4.6 Báo cáo (FR5.5) — chỉ JSON trên màn

| Mục đích | Method | Endpoint |
|---|---|---|
| Dashboard tổng quan | GET | `/api/reports/dashboard?classId=` |
| Báo cáo điểm | GET | `/api/reports/grades?classId=&semesterId=` |
| Báo cáo chuyên cần | GET | `/api/reports/attendance?classId=&from=&to=` |
| Báo cáo học phí | GET | `/api/reports/fees?classId=` |

> Xuất Excel/PDF đã **bỏ**.

### 4.7 Duyệt điểm (tuỳ chọn)

| Mục đích | Method | Endpoint |
|---|---|---|
| Admin duyệt điểm đã publish | PUT | `/api/grades/{id}/approve` |

---

## 5. Map nhanh: muốn làm X → gọi đâu?

| Muốn làm | Role | Endpoint chính |
|---|---|---|
| Login lấy token | Tất cả | `POST /api/user/login` |
| PH chọn con | Parent | `GET /api/account/children` |
| Xem TKB | Student/Parent | `GET /api/timetable/me` |
| Xem lịch dạy | Teacher | `GET /api/timetable/teacher` |
| Xem điểm | Student/Parent | `GET /api/grades/me` |
| Điểm danh | Teacher | `POST /api/attendance/batch` |
| Nộp / duyệt đơn nghỉ | PH-HS / GV | `POST /api/leave-requests` · `PUT .../approve\|reject` |
| Thanh toán học phí | Parent | `POST /api/payments/payos/create` (hoặc vnpay) |
| Quản lý user | Admin | `/api/users` |
| Báo cáo trường | Admin | `/api/reports/*` |

---

## 6. Ghi chú cho người maintain

1. **Controller** nằm `backend/Controllers/` — mỗi file ≈ 1 nhóm endpoint.  
2. **Service / Repository / DTO / Model** theo pattern 3 lớp.  
3. **Seed wipe**: `Seed:ForceReset` trong `appsettings.Development.json` — `true` = xóa hết + seed lại khi start API.  
4. **Không còn**: bài tập UI+API controller, import Excel user, sync offline điểm danh, xuất Excel/PDF báo cáo.  
5. Khi thêm chức năng mới: cập nhật file này + SRS nếu đổi phạm vi.

---

*Cập nhật theo codebase sau Ngày 20–21 (cleanup + seed demo THPT).*

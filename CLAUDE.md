# FSchool — Sổ liên lạc điện tử

Nền tảng quản lý & giao tiếp giáo dục **một chiều** (Trường/Giáo viên → Học sinh/Phụ huynh).
Monorepo gồm Mobile App (Flutter) + Backend API (.NET 8). Tài liệu gốc: [docs/SRS.md](docs/SRS.md).

## Tech Stack
- **Mobile**: Flutter — `dio`, `go_router`, state management = `setState`. Cấu trúc `lib/vn/edu/fpt/{controller,model,view}` (MVC).
- **Backend**: .NET 8 Web API, EF Core, SQL Server. Kiến trúc 3 lớp: **Controllers → Services → Repositories**.
- **Auth**: JWT.

## Quy tắc code
1. Bám sát [docs/SRS.md](docs/SRS.md) và cấu trúc thư mục đã thống nhất.
2. Backend tuân thủ SOLID + pattern 3 lớp (mỗi nghiệp vụ: Model → DTO → Repository → Service → Controller).
3. UI Flutter dùng màu thương hiệu: **Cam `#FF6B00`** + Trắng `#FFFFFF`. Bottom Navigation Bar đồng nhất.
4. 4 vai trò RBAC: Admin, Teacher, Parent, Student. (Đã bỏ HeadOfDept theo phản hồi giảng viên — các thao tác nhập liệu/quản trị chỉ cần insert DB.)

---

# 🗺️ TIMELINE THỰC HIỆN (theo ngày — phủ 100% SRS)

> **👉 VỊ TRÍ HIỆN TẠI: Cuối NGÀY 19.** Tiếp theo Ngày 20 — Admin: Tài chính, Bảng tin toàn trường, Báo cáo (xem trên màn).

Ký hiệu: ✅ xong · 🟡 đang làm · ⬜ chưa làm. "Ngày" = 1 buổi làm tập trung, tự map vào lịch thật.
Mỗi ngày kết thúc bằng **chạy thử + commit**.

> 📱 **Quyết định kiến trúc (vì đây là môn App Mobile):** KHÔNG làm Web Portal riêng. **Cả 4 vai trò dùng chung 1 app Flutter.** Admin dùng bản **Flutter Web/Desktop** (`flutter run -d chrome`) với layout rộng — cùng codebase, cùng API. Báo cáo ghi: *"Web Portal hiện thực bằng Flutter Web responsive"*.

> 📱 **Auth:** Đăng ký / đăng nhập bằng **SĐT + mật khẩu**. OTP chỉ gửi về **điện thoại** (xác thực đăng ký + quên MK). Dev: OTP log ra console (`[DEV OTP SMS]`).

> ⚠️ **1 thứ phải bổ sung so với code hiện tại** (chưa có trong model):
> - **Entity Thời khóa biểu** (`TimetableSlot`: lớp, môn, GV, thứ, tiết, phòng) — cho FR2.3 → thêm ở Ngày 4.

---

## 🅐 GIAI ĐOẠN BACKEND (Ngày 1–12) — dựng đủ API cho mọi nghiệp vụ

### ✅ NGÀY 1 — Dựng nền & lên DB
- [x] Monorepo + domain model đầy đủ + 2 migration + JWT/Swagger + luồng User mẫu
- [x] `appsettings.Development.json` + `dotnet ef database update` → DB `FSchoolDb` (18 bảng)

### ✅ NGÀY 2 — Bảo mật Auth (FR1.1 + NFR4.3)
- [x] ⚠️ **Hash mật khẩu BCrypt** — [api/Services/UserService.cs](api/Services/UserService.cs)
- [x] ⚠️ **Nhúng `userId` + `role` vào JWT claims** — [api/Common/JwtHelper.cs](api/Common/JwtHelper.cs) + [api/Controllers/UserController.cs](api/Controllers/UserController.cs)
- [x] Áp `[Authorize(Roles=...)]`; test register/login trên Swagger

### ✅ NGÀY 3 — Hồ sơ & Khôi phục MK (FR1.2, FR1.3) · *mọi role*
- [x] Xem/sửa hồ sơ, đổi mật khẩu, cập nhật avatar (FR1.3) — `/api/account/*`
- [x] Quên mật khẩu qua OTP Email/SĐT (FR1.2) — `/api/auth/forgot|verify|reset`

### ✅ NGÀY 4 — Danh mục + TKB + Phân công (FR5.2, FR5.3, FR2.3) · *Admin / insert DB*
- [x] CRUD Khối, Semester, Subject, Class; gán Student vào Class (FR5.2)
- [x] Phân công giảng dạy TeacherAssignment (FR5.3) — API/DB; **không làm UI app**
- [x] ➕ **Thêm entity `TimetableSlot` + migration** + API xem TKB theo tuần (FR2.3) — **không làm UI dựng TKB**

### ✅ NGÀY 5 — Quản lý người dùng (FR5.1) · *Admin*
- [x] CRUD user, Khóa/Mở tài khoản, Reset mật khẩu (API)
- [x] ~~Import Excel~~ → **BỎ khỏi UI app** (API có sẵn nhưng không dựng màn)

### ✅ NGÀY 6 — Điểm số (FR5.6, FR2.3) · *insert DB / HS / PH xem*
- [x] API nhập điểm hàng loạt theo AssessmentType, cơ chế **Nháp → Publish** (dùng qua Swagger/DB; **không làm UI app**)
- [x] API xem bảng điểm cho HS/PH (FR2.3)

### ✅ NGÀY 7 — Điểm danh (FR3.1) · *GV / HS / PH*
- [x] API điểm danh P/A/L theo lớp + ngày; API tra cứu chuyên cần
- [x] ~~Offline SQLite~~ → **BỎ** (điểm danh online-only — không làm trên điện thoại lúc mất mạng)

### ✅ NGÀY 8 — Bài tập (FR3.5, FR2.4) · *GV / HS*
- [x] CRUD Assignment (deadline, đề bài) + Submission + chấm điểm/feedback
- [x] API trạng thái nộp bài (To-Do / Done / Overdue)

### ✅ NGÀY 9 — Đơn xin nghỉ (FR2.5, FR3.3) · *PH / HS / GV*
- [x] Tạo đơn (đính kèm ảnh y tế) + theo dõi trạng thái
- [x] GV Approve/Reject → tự bắn thông báo kết quả

### ✅ NGÀY 10 — Thông báo & Bảng tin (FR1.4, FR3.4, FR5.4) · *mọi role*
- [x] Notification (in-app) + Announcement (lớp / toàn trường)
- [x] Hạ tầng **Push Notification (FCM)** + auto-notify các sự kiện (điểm mới, đơn duyệt, nhắc học phí…)

### ✅ NGÀY 11 — Tài chính & Cổng thanh toán (FR4.1, FR4.2, FR2.6) · *Admin / PH*
- [x] Quản lý Khoản thu/Hóa đơn (FeeCategory, FeeInvoice) + sửa precision `decimal(18,2)`
- [x] Tích hợp **VNPay** + **PayOS** (tạo giao dịch + verify checksum/signature)
- [x] Webhook đối soát giao dịch + biên lai điện tử (NFR: không lưu thông tin thẻ)

### ✅ NGÀY 12 — Báo cáo & Thống kê (FR5.5) · *Admin*
- [x] API tổng hợp JSON: dashboard, bảng điểm, tỷ lệ chuyên cần, tình trạng học phí
- [x] ~~Xuất Excel/PDF~~ → **BỎ khỏi phạm vi app** (không làm trên điện thoại; báo cáo xem trên màn hình là đủ)

---

## 🅑 GIAI ĐOẠN MOBILE (Ngày 13–18) — Student, Parent, Teacher

### ✅ NGÀY 13 — Nền Mobile (FR1.1, FR1.3, FR4.4)
- [x] Đổi theme cam `#FF6B00` ([mobile/lib/main.dart](mobile/lib/main.dart) đang `Colors.blue`)
- [x] `dio` client + interceptor JWT, model từ DTO, secure storage
- [x] Màn Đăng nhập + Bottom Navigation + điều hướng theo vai trò
- [x] Màn Hồ sơ/đổi MK (FR1.3) + **Force Update** version check (FR4.4)

### ✅ NGÀY 14 — Dashboard, Bảng tin, Thông báo (FR2.2, FR1.4, FR2.1)
- [x] Dashboard theo vai trò + Bảng tin (FR2.2) + Trung tâm Thông báo + badge chưa đọc (FR1.4)
- [x] **Phụ huynh: Switch Profile** quản lý nhiều con (FR2.1) — API `/api/account/children`
- [x] **Push notification FCM thực** (FirebaseAdmin) + `google-services.json` + `DbSeeder` demo data

### ✅ NGÀY 15 — Học sinh/PH: Học tập (FR2.3, FR2.4)
- [x] Thời khóa biểu theo tuần + Bảng điểm chi tiết (FR2.3)
- [x] Bài tập: xem (To-Do/Done/Overdue) + nộp link/file (FR2.4)
- [x] Backend: cho phép PH truyền `studentId` (Switch Profile) ở TKB/Bài tập/Điểm
- [x] Seed dữ liệu học tập (lớp 10A1, môn, TKB, điểm, bài tập) để test giao diện

### ✅ NGÀY 16 — Học sinh/PH: Đơn nghỉ & Học phí (FR2.5, FR2.6)
- [x] Tạo & theo dõi đơn xin nghỉ + đính kèm ảnh y tế (FR2.5)
- [x] Thanh toán học phí VNPay/PayOS + lịch sử + biên lai (FR2.6)
- [x] Seed loại khoản thu + hóa đơn để test; hỗ trợ `dev/simulate-paid`

### ✅ NGÀY 17 — Giáo viên: Điểm danh (FR3.1)
- [x] Điểm danh nhanh P/A/L theo lớp + ngày (FR3.1, **online-only**)
- [x] Tab "Lớp học" cho GV: chọn lớp → Điểm danh
- [x] ~~Nhập điểm của GV~~ → **BỎ** (điểm do Admin insert DB — FR5.6; HS/PH vẫn xem được)
- [x] ~~Offline SQLite~~ → **BỎ** (không làm)

### ✅ NGÀY 18 — Giáo viên: Bài tập, Duyệt đơn, Gửi TB (FR3.5, FR3.3, FR3.4)
- [x] Tạo/sửa/xóa & chấm bài tập (FR3.5)
- [x] Duyệt/từ chối đơn nghỉ (FR3.3)
- [x] Soạn & gửi Push Notification cho lớp (FR3.4)

---

## 🅒 GIAI ĐOẠN ADMIN — màn trong app Flutter (Ngày 19–20)
*(chạy bản Flutter Web/Desktop với layout rộng + sidebar; KHÔNG dựng project web riêng)*

### ✅ NGÀY 19 — Quản lý người dùng & Danh mục (FR5.1, FR5.2)
- [x] Layout admin (sidebar khi rộng, Drawer khi hẹp) — `AdminShell`
- [x] Màn Quản lý người dùng (FR5.1): thêm / sửa / khóa-mở / reset mật khẩu
- [x] ~~Import Excel tài khoản~~ → **BỎ** (không làm trên app)
- [x] Màn danh mục: Kỳ · Môn · Lớp (FR5.2) — không có entity Khối riêng (gắn trong tên lớp)
- [x] ~~Nhập điểm (FR5.6)~~ → **BỎ UI** — insert DB / seeder
- [x] ~~Phân công GV (FR5.3) + dựng TKB~~ → **BỎ UI** — insert DB / seeder

### NGÀY 20 — Tài chính, Bảng tin, Báo cáo (FR4.1, FR4.2, FR5.4, FR5.5)
- [ ] Màn Khoản thu/đợt thu + cấu hình API key VNPay/PayOS + đối soát (FR4.1, FR4.2)
- [ ] Đăng thông báo toàn trường (FR5.4)
- [ ] Dashboard báo cáo **xem trên màn** (JSON): bảng điểm / chuyên cần / học phí (FR5.5)
- [x] ~~Xuất Excel/PDF~~ → **BỎ** (không phù hợp app điện thoại)

---

## 🅓 HOÀN THIỆN (Ngày 21–22) — Phi chức năng (NFR4)
### NGÀY 21 — Hiệu năng & trải nghiệm
- [ ] Phân trang / Lazy loading mọi danh sách dài (NFR4.2)
- [ ] Rà thời gian phản hồi API < 2s (Điểm danh, tra cứu điểm/TKB)
- [x] ~~Offline cache~~ → **BỎ** (không làm)

### NGÀY 22 — Kiểm thử & bảo mật toàn diện
- [ ] Test end-to-end đủ 4 vai trò
- [ ] Soát bảo mật: HTTPS, signature thanh toán, không lưu thẻ, JWT hết hạn (NFR4.3)
- [ ] Sửa lỗi + chốt tài liệu

---

## ✅ BẢNG ĐỐI CHIẾU ĐỘ PHỦ (kiểm tra "đủ tất cả")
| Vai trò | Được phục vụ ở ngày |
|---|---|
| Học sinh | 6,7,8,13–16 |
| Phụ huynh | 9,11,13–16 (+Switch Profile N14) |
| Giáo viên | 7,8,9,17,18 (điểm danh, bài tập, đơn nghỉ, gửi TB) |
| Admin | 4,5,6,10,11,12,19,20 (app Flutter Web + insert DB) |

| Phân hệ FR | Ngày |
|---|---|
| FR1 (Auth/Hồ sơ/Thông báo) | 2,3,10,13,14 |
| FR2 (HS/PH) | 14,15,16 (API: 4,6,8,9,11) |
| FR3 (Giáo viên) | 6,7,8,9,17,18 |
| FR4 (Tài chính) | 11,20 |
| FR5 (Admin) | 4,5,12,19,20 |
| NFR (hiệu năng/bảo mật/force-update) | 2,11,13,21,22 |

---

## 👉 HÔM NAY LÀM GÌ (Ngày 20 — Admin: Tài chính, Bảng tin, Báo cáo)
1. Màn Khoản thu / hóa đơn + cấu hình VNPay/PayOS + đối soát.
2. Đăng thông báo toàn trường.
3. Dashboard báo cáo xem trên màn (điểm / chuyên cần / học phí).

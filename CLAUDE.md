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
4. 5 vai trò RBAC: Admin, HeadOfDept, Teacher, Parent, Student.

---

# 🗺️ TIMELINE THỰC HIỆN (theo ngày — phủ 100% SRS)

> **👉 VỊ TRÍ HIỆN TẠI: Cuối NGÀY 12 (xong).** Tiếp theo Ngày 13 — Nền Mobile.

Ký hiệu: ✅ xong · 🟡 đang làm · ⬜ chưa làm. "Ngày" = 1 buổi làm tập trung, tự map vào lịch thật.
Mỗi ngày kết thúc bằng **chạy thử + commit**.

> 📱 **Quyết định kiến trúc (vì đây là môn App Mobile):** KHÔNG làm Web Portal riêng. **Tất cả 5 vai trò dùng chung 1 app Flutter.** Admin & Trưởng bộ môn dùng bản **Flutter Web/Desktop** (`flutter run -d chrome`) với layout rộng — cùng codebase, cùng API. Báo cáo ghi: *"Web Portal hiện thực bằng Flutter Web responsive"*.

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

### ✅ NGÀY 4 — Danh mục + TKB + Phân công (FR5.2, FR5.3, FR2.3) · *Admin/Trưởng bộ môn*
- [x] CRUD Khối, Semester, Subject, Class; gán Student vào Class (FR5.2)
- [x] Phân công giảng dạy TeacherAssignment (FR5.3)
- [x] ➕ **Thêm entity `TimetableSlot` + migration** + API xem TKB theo tuần (FR2.3)

### ✅ NGÀY 5 — Quản lý người dùng (FR5.1) · *Admin*
- [x] CRUD user, Khóa/Mở tài khoản, Reset mật khẩu
- [x] Import tài khoản hàng loạt từ file Excel

### ✅ NGÀY 6 — Điểm số (FR3.2, FR2.3) · *GV / HS / PH / Trưởng bộ môn*
- [x] Nhập điểm hàng loạt theo AssessmentType, cơ chế **Nháp → Publish**
- [x] (tuỳ chọn) Trưởng bộ môn duyệt điểm; API xem bảng điểm cho HS/PH

### ✅ NGÀY 7 — Điểm danh (FR3.1) · *GV / HS / PH*
- [x] API điểm danh P/A/L theo lớp + ngày; API tra cứu chuyên cần
- [x] (chuẩn bị endpoint đồng bộ batch để mobile dùng offline ở Ngày 17)

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

### ✅ NGÀY 12 — Báo cáo & Thống kê (FR5.5) · *Admin / Trưởng bộ môn*
- [x] API tổng hợp: dashboard, bảng điểm, tỷ lệ chuyên cần, tình trạng học phí
- [x] Xuất **Excel (ClosedXML) / PDF (QuestPDF)** — `?format=json|excel|pdf`

---

## 🅑 GIAI ĐOẠN MOBILE (Ngày 13–18) — Student, Parent, Teacher

### 🟡 NGÀY 13 — Nền Mobile (FR1.1, FR1.3, FR4.4)  ⬅️ LÀM TIẾP
- [ ] Đổi theme cam `#FF6B00` ([mobile/lib/main.dart](mobile/lib/main.dart) đang `Colors.blue`)
- [ ] `dio` client + interceptor JWT, model từ DTO, secure storage
- [ ] Màn Đăng nhập + Bottom Navigation + điều hướng theo vai trò
- [ ] Màn Hồ sơ/đổi MK (FR1.3) + **Force Update** version check (FR4.4)

### NGÀY 14 — Dashboard, Bảng tin, Thông báo (FR2.2, FR1.4, FR2.1)
- [ ] Dashboard + Bảng tin (FR2.2) + Trung tâm Thông báo nhận push (FR1.4)
- [ ] **Phụ huynh: Switch Profile** quản lý nhiều con (FR2.1)

### NGÀY 15 — Học sinh/PH: Học tập (FR2.3, FR2.4)
- [ ] Thời khóa biểu theo tuần + Bảng điểm chi tiết (FR2.3)
- [ ] Bài tập: xem (To-Do/Done/Overdue) + nộp link/file (FR2.4)

### NGÀY 16 — Học sinh/PH: Đơn nghỉ & Học phí (FR2.5, FR2.6)
- [ ] Tạo & theo dõi đơn xin nghỉ (FR2.5)
- [ ] Thanh toán học phí VNPay/PayOS + lịch sử + biên lai (FR2.6)

### NGÀY 17 — Giáo viên: Điểm danh & Điểm số (FR3.1, FR3.2)
- [ ] Điểm danh nhanh P/A/L + **offline cache SQLite, tự đồng bộ khi có mạng** (FR3.1, NFR4.2)
- [ ] Nhập điểm hàng loạt + Publish (FR3.2)

### NGÀY 18 — Giáo viên: Bài tập, Duyệt đơn, Gửi TB (FR3.5, FR3.3, FR3.4)
- [ ] Tạo/sửa/xóa & chấm bài tập (FR3.5)
- [ ] Duyệt/từ chối đơn nghỉ (FR3.3)
- [ ] Soạn & gửi Push Notification cho lớp (FR3.4)

---

## 🅒 GIAI ĐOẠN ADMIN/TRƯỞNG BỘ MÔN — màn trong app Flutter (Ngày 19–20)
*(chạy bản Flutter Web/Desktop với layout rộng + sidebar; KHÔNG dựng project web riêng)*

### NGÀY 19 — Quản lý người dùng & Danh mục (FR5.1, FR5.2, FR5.3)
- [ ] Layout admin (sidebar/rộng), điều hướng riêng cho role Admin/Trưởng bộ môn
- [ ] Màn Quản lý người dùng: CRUD, khóa, reset MK, import Excel (FR5.1)
- [ ] Màn quản lý Khối/Lớp/Môn/Kỳ + Phân công giảng dạy + dựng TKB (FR5.2, FR5.3, FR2.3)

### NGÀY 20 — Tài chính, Bảng tin, Báo cáo (FR4.1, FR4.2, FR5.4, FR5.5)
- [ ] Màn Khoản thu/đợt thu + cấu hình API key VNPay/PayOS + đối soát (FR4.1, FR4.2)
- [ ] Đăng thông báo toàn trường (FR5.4) + (tuỳ chọn) Trưởng bộ môn duyệt điểm
- [ ] Dashboard báo cáo + xuất Excel/PDF (FR5.5)

---

## 🅓 HOÀN THIỆN (Ngày 21–22) — Phi chức năng (NFR4)
### NGÀY 21 — Hiệu năng & trải nghiệm
- [ ] Phân trang / Lazy loading mọi danh sách dài (NFR4.2)
- [ ] Rà thời gian phản hồi API < 2s (Điểm danh, Nhập điểm)
- [ ] Kiểm tra offline cache (TKB + Điểm danh) hoạt động đúng

### NGÀY 22 — Kiểm thử & bảo mật toàn diện
- [ ] Test end-to-end đủ 5 vai trò
- [ ] Soát bảo mật: HTTPS, signature thanh toán, không lưu thẻ, JWT hết hạn (NFR4.3)
- [ ] Sửa lỗi + chốt tài liệu

---

## ✅ BẢNG ĐỐI CHIẾU ĐỘ PHỦ (kiểm tra "đủ tất cả")
| Vai trò | Được phục vụ ở ngày |
|---|---|
| Học sinh | 6,7,8,13–16 |
| Phụ huynh | 9,11,13–16 (+Switch Profile N14) |
| Giáo viên | 6,7,8,9,17,18 |
| Trưởng bộ môn | 4,6,12,19,20 (app Flutter Web) |
| Admin | 5,10,11,12,19,20 (app Flutter Web) |

| Phân hệ FR | Ngày |
|---|---|
| FR1 (Auth/Hồ sơ/Thông báo) | 2,3,10,13,14 |
| FR2 (HS/PH) | 14,15,16 (API: 4,6,8,9,11) |
| FR3 (Giáo viên) | 6,7,8,9,17,18 |
| FR4 (Tài chính) | 11,20 |
| FR5 (Admin/Trưởng bộ môn) | 4,5,12,19,20 |
| NFR (hiệu năng/bảo mật/offline/force-update) | 2,11,13,17,21,22 |

---

## 👉 HÔM NAY LÀM GÌ (Ngày 13 — Nền Mobile)
1. Đổi theme cam `#FF6B00`, dựng `dio` client + interceptor JWT.
2. Màn Đăng nhập + Bottom Navigation điều hướng theo vai trò.
3. Màn Hồ sơ/đổi MK + Force Update version check.

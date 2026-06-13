# TÀI LIỆU ĐẶC TẢ YÊU CẦU PHẦN MỀM (SRS)

**Tên dự án:** Ứng dụng Quản lý & Giao tiếp Giáo dục FSchool
**Nền tảng mục tiêu:** 
- Mobile App (Android / iOS): Dành cho Học sinh, Phụ huynh, Giáo viên.
- Web Portal (PC): Dành cho Quản trị viên, Trưởng bộ môn/Giáo vụ.
**Phiên bản tài liệu:** 1.1

---

## 1. Giới thiệu chung (Introduction)

### 1.1. Mục đích
Dự án FSchool là một nền tảng sổ liên lạc điện tử, nhằm số hóa và tối ưu hóa quy trình quản lý học tập và truyền thông 1 chiều (từ Nhà trường/Giáo viên đến Học sinh/Phụ huynh). Ứng dụng hướng tới việc xây dựng một hệ sinh thái giáo dục minh bạch, giúp cập nhật thông tin tức thời và hỗ trợ thanh toán học phí trực tuyến tiện lợi.

### 1.2. Phạm vi dự án
Hệ thống cung cấp các công cụ cốt lõi để theo dõi điểm số, lịch học, chuyên cần, bảng tin nội bộ và thanh toán học phí. FSchool giúp giáo viên giảm thiểu thủ tục hành chính, đồng thời hỗ trợ phụ huynh/học sinh nắm bắt tình hình học tập, các thông báo và nghĩa vụ tài chính theo thời gian thực (real-time). 

*(Lưu ý: Hệ thống không tích hợp tính năng trò chuyện/nhắn tin nội bộ, mọi luồng thông tin giao tiếp đều thông qua hình thức Thông báo - Notifications).*

---

## 2. Các vai trò trong hệ thống (Actors/Roles)
Hệ thống FSchool phân quyền truy cập dựa trên 5 vai trò (Role-Based Access Control) với mức độ thẩm quyền khác nhau:

1. **Học sinh/Sinh viên (Student - Mobile App):** Đối tượng thụ hưởng giáo dục. Được phép xem dữ liệu cá nhân (lịch học, điểm số, bài tập, học phí) và thực hiện tương tác cơ bản (nộp bài, xem thông báo).
2. **Phụ huynh (Parent - Mobile App):** Người giám hộ. Có quyền hạn tra cứu tương đương Học sinh, **được phép tạo đơn xin nghỉ học** và thực hiện **thanh toán học phí**. Đặc biệt hỗ trợ tính năng **Chuyển đổi hồ sơ (Switch Profile)** để quản lý nhiều con em đang học cùng hệ thống.
3. **Giáo viên (Teacher - Mobile App):** Người vận hành trực tiếp lớp học. Có thẩm quyền điểm danh, nhập điểm số, tạo/quản lý bài tập, duyệt đơn xin nghỉ và phát hành thông báo đẩy (Push notification) đến lớp mình phụ trách.
4. **Trưởng bộ môn / Giáo vụ (Head of Dept - Web Portal):** Người điều hành chuyên môn. Thẩm quyền tạo danh sách lớp học, môn học, phân công giáo viên giảng dạy và phê duyệt điểm số (nếu có).
5. **Quản trị viên (Admin - Web Portal):** Người quản lý toàn bộ hệ thống lõi. Toàn quyền quản lý dữ liệu người dùng (CRUD tài khoản), thiết lập cấu hình hệ thống, tích hợp cổng thanh toán và phát hành thông báo toàn trường.

---

## 3. Đặc tả Yêu cầu Chức năng (Functional Requirements)

Hệ thống được chia thành 6 phân hệ nghiệp vụ chính:

### Phân hệ 1: Xác thực, Cấu hình & Thông báo chung (Common - Mobile)
*   **FR1.1 - Đăng nhập:** Đăng nhập bằng ID (Mã HS/GV) và Mật khẩu do nhà trường cấp phát.
*   **FR1.2 - Phục hồi mật khẩu:** Yêu cầu cấp lại mật khẩu thông qua mã xác thực (OTP) gửi về Email hoặc Số điện thoại.
*   **FR1.3 - Quản lý hồ sơ:** Xem thông tin cá nhân, cập nhật Avatar và thay đổi mật khẩu định kỳ.
*   **FR1.4 - Trung tâm Thông báo (Notification Center):** Nơi lưu trữ tất cả thông báo In-app và Push notification. Các luồng thông báo tự động bao gồm:
    *   Có điểm mới, có bài tập mới/sắp đến hạn nộp.
    *   Đơn xin nghỉ học được duyệt/từ chối.
    *   Thông báo nhắc nợ học phí/xác nhận thanh toán thành công.
    *   Thông báo từ Giáo viên lớp hoặc thông báo toàn trường từ Admin.

### Phân hệ 2: Dành cho Phụ huynh / Học sinh (Mobile App)
*   **FR2.1 - Chuyển đổi hồ sơ (Dành riêng cho Phụ huynh):** Cho phép thao tác chuyển đổi nhanh (Switch profile) để theo dõi thông tin của từng con em nếu có nhiều con học cùng trường.
*   **FR2.2 - Dashboard & Bảng tin:** Hiển thị luồng tin tức, sự kiện của trường/lớp.
*   **FR2.3 - Quản lý Học tập:**
    *   Xem Thời khóa biểu theo tuần (chi tiết môn, phòng, giáo viên).
    *   Tra cứu bảng điểm tổng hợp và điểm chi tiết (các đầu điểm thành phần).
*   **FR2.4 - Quản lý Bài tập (Assignments):** Xem danh sách bài tập (To-Do, Done, Overdue). Nộp bài tập qua link đính kèm hoặc upload file.
*   **FR2.5 - Xin phép nghỉ học (Do Phụ huynh/HS thực hiện):** Điền form đơn xin nghỉ (chọn ngày, lý do, đính kèm ảnh chứng nhận y tế) và theo dõi trạng thái duyệt từ giáo viên.
*   **FR2.6 - Thanh toán Học phí trực tuyến:**
    *   Xem chi tiết các khoản thu (học phí, phụ phí, BHYT...).
    *   Thực hiện thanh toán trực tuyến qua tích hợp cổng **VNPay** và **PayOS**.
    *   Xem lịch sử thanh toán và biên lai điện tử.

### Phân hệ 3: Dành cho Giáo viên (Teacher Dashboard - Mobile App)
*   **FR3.1 - Điểm danh thông minh:** Hiển thị danh sách lớp. Cho phép thao tác điểm danh nhanh: Có mặt (P), Vắng mặt (A), Đi muộn (L). *Hỗ trợ điểm danh Offline và tự động đồng bộ khi có mạng.*
*   **FR3.2 - Quản lý Điểm số:** Nhập điểm số hàng loạt cho học sinh theo loại đầu điểm (Assessment Type). Điểm có thể lưu nháp trước khi bấm "Công bố" (Publish) để gửi thông báo đến HS/PH.
*   **FR3.3 - Quản lý Đơn từ:** Nhận, xem xét đơn xin nghỉ của học sinh và thao tác Duyệt (Approve) / Từ chối (Reject). Hệ thống tự bắn thông báo kết quả cho HS/PH.
*   **FR3.4 - Gửi Thông báo Lớp:** Soạn thảo văn bản, đính kèm tài liệu và gửi Push Notification (1 chiều) đến toàn bộ phụ huynh/học sinh thuộc lớp mình phụ trách.
*   **FR3.5 - Quản lý Bài tập (Assignments):** Tạo mới, chỉnh sửa, xóa bài tập (set deadline, đính kèm đề bài). Theo dõi trạng thái nộp bài, chấm điểm và ghi chú (feedback) trực tiếp vào bài nộp.

### Phân hệ 4: Thanh toán & Tài chính (Backend/Web Admin)
*   **FR4.1 - Quản lý Khoản thu:** Kế toán/Admin tạo các đợt thu học phí, gán mức phí áp dụng cho từng khối/lớp hoặc học sinh cụ thể.
*   **FR4.2 - Tích hợp & Đối soát:** Quản lý cấu hình API Key của **VNPay** và **PayOS**. Theo dõi, đối soát trạng thái giao dịch (Thành công, Thất bại, Chờ thanh toán) thông qua Webhook.

### Phân hệ 5: Dành cho Quản trị viên & Trưởng bộ môn (Web Portal)
*   **FR5.1 - Quản lý Người dùng (Admin):** Thêm, Sửa, Xóa, Khóa tài khoản. Hỗ trợ Reset mật khẩu, Import tài khoản hàng loạt bằng file Excel.
*   **FR5.2 - Quản lý Danh mục (Trưởng bộ môn/Admin):** Quản lý thực thể lõi: Khối, Lớp học, Môn học, Kỳ học.
*   **FR5.3 - Phân công Giảng dạy:** Gán Giáo viên vào Lớp học và Môn học. Quản lý luân chuyển giáo viên.
*   **FR5.4 - Bảng tin Toàn trường (Global Newsfeed):** Đăng tải thông báo quan trọng (Lịch nghỉ lễ, sự kiện) đến toàn bộ thiết bị App.
*   **FR5.5 - Báo cáo & Thống kê:** Xuất file Excel/PDF các báo cáo: Bảng điểm toàn trường/lớp, Tỷ lệ chuyên cần, Tình trạng đóng học phí.

---

## 4. Yêu cầu Phi chức năng (Non-Functional Requirements)

### 4.1. UI/UX (Giao diện người dùng)
*   **Màu sắc thương hiệu:** Tông màu chủ đạo là Cam (#FF6B00) và Trắng (#FFFFFF).
*   **Tính đồng nhất:** App Mobile bắt buộc đồng bộ thanh điều hướng dưới cùng (Bottom Navigation Bar). Web Admin dùng cấu trúc Sidebar Navigation tiêu chuẩn.
*   **Ngôn ngữ:** Tiếng Việt chuẩn mực sư phạm.

### 4.2. Hiệu năng hệ thống (Performance)
*   Thời gian phản hồi API cho các thao tác quan trọng (Điểm danh, Nhập điểm) < 2 giây.
*   **Lazy Loading / Phân trang:** Áp dụng bắt buộc cho các danh sách dài (Bảng tin, Danh sách học sinh, Lịch sử thông báo, Lịch sử giao dịch).
*   **Offline Support:** Chức năng Thời khóa biểu và Điểm danh cho giáo viên cần được cache dữ liệu cục bộ (Local Storage/SQLite) để thao tác khi không có mạng.

### 4.3. Bảo mật & Tính toàn vẹn (Security & Integrity)
*   **Bảo mật luồng thanh toán:** Toàn bộ giao dịch tài chính phải được thực hiện qua giao thức HTTPS, xác thực chữ ký số (Checksum/Signature) từ VNPay/PayOS để chống giả mạo giao dịch. Không lưu trữ thông tin thẻ ngân hàng của người dùng trên hệ thống FSchool.
*   **Xác thực API:** Bảo vệ bằng JSON Web Token (JWT).
*   **Bảo mật dữ liệu:** Mật khẩu người dùng phải được băm (Bcrypt/Argon2) kết hợp với muối (salt).

### 4.4. Cập nhật Ứng dụng (App Versioning)
*   **Force Update:** Ứng dụng Mobile phải có cơ chế kiểm tra phiên bản (Version Check) lúc khởi động. Nếu phát hiện bản cập nhật quan trọng (ví dụ: đổi luồng thanh toán, vá lỗi bảo mật), bắt buộc người dùng chuyển hướng tới App Store/Google Play để cập nhật mới cho phép sử dụng tiếp.

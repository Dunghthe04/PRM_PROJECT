# FSchool — Danh sách API Endpoints

Bản thiết kế API suy ra từ [SRS.md](SRS.md) + domain model hiện có. Cả 5 vai trò dùng chung app Flutter (Admin/Trưởng bộ môn dùng bản Flutter Web) và đều gọi vào các endpoint này.

- **Tổng số:** ~118 endpoint, chia 19 nhóm (controller).
- **Quy ước:** prefix `/api`. Bảo vệ bằng JWT; cột **Role** ghi vai trò được phép (— = mọi user đã đăng nhập, *Public* = không cần token).
- **Trạng thái:** ✅ đã có · ⬜ chưa làm. (Hiện chỉ mới có 3 endpoint User.)

| Nhóm | Số endpoint |
|---|---|
| 1. Auth & Account | 9 |
| 2. Users (Admin) | 10 |
| 3. Parent–Student | 4 |
| 4. Semesters | 5 |
| 5. Subjects | 5 |
| 6. Classes | 8 |
| 7. Teaching Assignment | 5 |
| 8. Timetable (TKB) ➕mới | 6 |
| 9. Grades | 7 |
| 10. Attendance | 6 |
| 11. Assignments | 7 |
| 12. Submissions | 5 |
| 13. Leave Requests | 7 |
| 14. Announcements | 5 |
| 15. Notifications & Devices | 6 |
| 16. Fees | 9 |
| 17. Payments | 8 |
| 18. Reports | 5 |
| 19. App / System | 2 |
| **TỔNG** | **~118** |

---

## 1. Auth & Account — FR1.1, FR1.2, FR1.3 · Ngày 2–3
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 1 | POST | `/api/auth/login` | *Public* | Đăng nhập bằng ID + mật khẩu, trả JWT ✅ |
| 2 | POST | `/api/auth/refresh` | *Public* | Làm mới access token |
| 3 | POST | `/api/auth/forgot-password` | *Public* | Gửi OTP về Email/SĐT |
| 4 | POST | `/api/auth/verify-otp` | *Public* | Xác thực mã OTP |
| 5 | POST | `/api/auth/reset-password` | *Public* | Đặt lại mật khẩu sau OTP |
| 6 | GET | `/api/account/me` | — | Lấy hồ sơ người dùng hiện tại |
| 7 | PUT | `/api/account/me` | — | Cập nhật thông tin cá nhân |
| 8 | PUT | `/api/account/avatar` | — | Đổi ảnh đại diện |
| 9 | PUT | `/api/account/change-password` | — | Đổi mật khẩu |

## 2. Users — FR5.1 · Ngày 5 · Role: **Admin**
| # | Method | Endpoint | Mô tả |
|---|---|---|---|
| 10 | GET | `/api/users` | Danh sách user (phân trang, lọc theo role) |
| 11 | GET | `/api/users/{id}` | Chi tiết user ✅ |
| 12 | POST | `/api/users` | Tạo user ✅ (đang là `/register`) |
| 13 | PUT | `/api/users/{id}` | Sửa user |
| 14 | DELETE | `/api/users/{id}` | Xóa user |
| 15 | PUT | `/api/users/{id}/lock` | Khóa tài khoản |
| 16 | PUT | `/api/users/{id}/unlock` | Mở khóa tài khoản |
| 17 | POST | `/api/users/{id}/reset-password` | Reset mật khẩu |
| 18 | POST | `/api/users/import` | Import tài khoản từ Excel |
| 19 | GET | `/api/users/import/template` | Tải file Excel mẫu |

## 3. Parent–Student (Switch Profile) — FR2.1 · Ngày 4/14
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 20 | GET | `/api/account/children` | Parent | DS con của phụ huynh hiện tại (Switch Profile) |
| 21 | GET | `/api/parents/{id}/children` | Admin | DS con của 1 phụ huynh |
| 22 | POST | `/api/parents/{id}/children/{studentId}` | Admin | Liên kết PH–HS |
| 23 | DELETE | `/api/parents/{id}/children/{studentId}` | Admin | Gỡ liên kết |

## 4. Semesters — FR5.2 · Ngày 4 · Role: Admin/Trưởng bộ môn
| # | Method | Endpoint | Mô tả |
|---|---|---|---|
| 24 | GET | `/api/semesters` | Danh sách kỳ học |
| 25 | GET | `/api/semesters/{id}` | Chi tiết |
| 26 | POST | `/api/semesters` | Tạo |
| 27 | PUT | `/api/semesters/{id}` | Sửa |
| 28 | DELETE | `/api/semesters/{id}` | Xóa |

## 5. Subjects — FR5.2 · Ngày 4 · Role: Admin/Trưởng bộ môn
| # | Method | Endpoint | Mô tả |
|---|---|---|---|
| 29 | GET | `/api/subjects` | Danh sách môn |
| 30 | GET | `/api/subjects/{id}` | Chi tiết |
| 31 | POST | `/api/subjects` | Tạo |
| 32 | PUT | `/api/subjects/{id}` | Sửa |
| 33 | DELETE | `/api/subjects/{id}` | Xóa |

## 6. Classes — FR5.2 · Ngày 4 · Role: Admin/Trưởng bộ môn (GET: mở rộng)
| # | Method | Endpoint | Mô tả |
|---|---|---|---|
| 34 | GET | `/api/classes` | Danh sách lớp (phân trang) |
| 35 | GET | `/api/classes/{id}` | Chi tiết lớp |
| 36 | POST | `/api/classes` | Tạo lớp |
| 37 | PUT | `/api/classes/{id}` | Sửa lớp |
| 38 | DELETE | `/api/classes/{id}` | Xóa lớp |
| 39 | GET | `/api/classes/{id}/students` | DS học sinh trong lớp |
| 40 | POST | `/api/classes/{id}/students` | Thêm HS vào lớp |
| 41 | DELETE | `/api/classes/{id}/students/{studentId}` | Bỏ HS khỏi lớp |

## 7. Teaching Assignment — FR5.3 · Ngày 4 · Role: Admin/Trưởng bộ môn
| # | Method | Endpoint | Mô tả |
|---|---|---|---|
| 42 | GET | `/api/teacher-assignments` | DS phân công (lọc theo lớp/GV/môn) |
| 43 | POST | `/api/teacher-assignments` | Gán GV vào lớp + môn |
| 44 | PUT | `/api/teacher-assignments/{id}` | Sửa phân công (luân chuyển) |
| 45 | DELETE | `/api/teacher-assignments/{id}` | Gỡ phân công |
| 46 | GET | `/api/teachers/{id}/classes` | Các lớp một GV phụ trách |

## 8. Timetable (Thời khóa biểu) ➕ entity mới — FR2.3 · Ngày 4
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 47 | GET | `/api/timetable?classId=&week=` | — | TKB theo tuần của 1 lớp (môn, phòng, GV) |
| 48 | GET | `/api/timetable/me?week=` | Student/Parent | TKB của HS hiện tại |
| 49 | GET | `/api/timetable/teacher?week=` | Teacher | Lịch dạy của GV |
| 50 | POST | `/api/timetable` | Admin/Trưởng BM | Tạo tiết học |
| 51 | PUT | `/api/timetable/{id}` | Admin/Trưởng BM | Sửa tiết học |
| 52 | DELETE | `/api/timetable/{id}` | Admin/Trưởng BM | Xóa tiết học |

## 9. Grades — FR3.2, FR2.3 · Ngày 6
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 53 | GET | `/api/grades?classId=&subjectId=&semesterId=` | Teacher | Bảng điểm lớp |
| 54 | GET | `/api/grades/me?semesterId=` | Student/Parent | Bảng điểm đã công bố |
| 55 | POST | `/api/grades/batch` | Teacher | Nhập điểm hàng loạt (lưu Nháp) |
| 56 | PUT | `/api/grades/{id}` | Teacher | Sửa 1 điểm |
| 57 | POST | `/api/grades/publish` | Teacher | Công bố điểm → bắn thông báo |
| 58 | PUT | `/api/grades/{id}/approve` | Trưởng BM | Duyệt điểm (tuỳ chọn) |
| 59 | DELETE | `/api/grades/{id}` | Teacher | Xóa điểm (khi còn Nháp) |

## 10. Attendance — FR3.1 · Ngày 7
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 60 | GET | `/api/attendance?classId=&date=` | Teacher | Điểm danh của lớp theo ngày |
| 61 | GET | `/api/attendance/me?from=&to=` | Student/Parent | Lịch sử chuyên cần |
| 62 | GET | `/api/attendance/summary?classId=` | Teacher/Trưởng BM | Tỷ lệ chuyên cần |
| 63 | POST | `/api/attendance/batch` | Teacher | Điểm danh hàng loạt P/A/L |
| 64 | POST | `/api/attendance/sync` | Teacher | Đồng bộ dữ liệu điểm danh offline |
| 65 | PUT | `/api/attendance/{id}` | Teacher | Sửa 1 bản ghi |

## 11. Assignments — FR3.5, FR2.4 · Ngày 8
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 66 | GET | `/api/assignments?classId=&subjectId=` | — | DS bài tập của lớp |
| 67 | GET | `/api/assignments/me` | Student | Bài tập của HS (To-Do/Done/Overdue) |
| 68 | GET | `/api/assignments/{id}` | — | Chi tiết bài tập |
| 69 | POST | `/api/assignments` | Teacher | Tạo bài tập (deadline, đề bài) |
| 70 | PUT | `/api/assignments/{id}` | Teacher | Sửa bài tập |
| 71 | DELETE | `/api/assignments/{id}` | Teacher | Xóa bài tập |
| 72 | GET | `/api/assignments/{id}/submissions` | Teacher | DS bài nộp |

## 12. Submissions — FR2.4, FR3.5 · Ngày 8
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 73 | POST | `/api/assignments/{id}/submissions` | Student | Nộp bài (link/file) |
| 74 | GET | `/api/submissions/{id}` | — | Chi tiết bài nộp |
| 75 | PUT | `/api/submissions/{id}` | Student | Nộp lại |
| 76 | PUT | `/api/submissions/{id}/grade` | Teacher | Chấm điểm + feedback |
| 77 | POST | `/api/files/upload` | — | Upload file (bài nộp, ảnh y tế…) |

## 13. Leave Requests — FR2.5, FR3.3 · Ngày 9
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 78 | GET | `/api/leave-requests?classId=&status=` | Teacher | DS đơn nghỉ cần duyệt |
| 79 | GET | `/api/leave-requests/me` | Student/Parent | Đơn của tôi/con tôi |
| 80 | GET | `/api/leave-requests/{id}` | — | Chi tiết đơn |
| 81 | POST | `/api/leave-requests` | Parent/Student | Tạo đơn (đính kèm ảnh y tế) |
| 82 | PUT | `/api/leave-requests/{id}/approve` | Teacher | Duyệt → bắn thông báo |
| 83 | PUT | `/api/leave-requests/{id}/reject` | Teacher | Từ chối → bắn thông báo |
| 84 | DELETE | `/api/leave-requests/{id}` | Parent/Student | Hủy đơn (khi còn Pending) |

## 14. Announcements (Bảng tin) — FR3.4, FR5.4 · Ngày 10
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 85 | GET | `/api/announcements?type=` | — | Bảng tin (toàn trường + lớp của tôi) |
| 86 | GET | `/api/announcements/{id}` | — | Chi tiết |
| 87 | POST | `/api/announcements` | Teacher(lớp)/Admin(toàn trường) | Đăng + gửi push |
| 88 | PUT | `/api/announcements/{id}` | Teacher/Admin | Sửa |
| 89 | DELETE | `/api/announcements/{id}` | Teacher/Admin | Xóa |

## 15. Notifications & Devices — FR1.4 · Ngày 10
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 90 | GET | `/api/notifications` | — | DS thông báo cá nhân (phân trang) |
| 91 | GET | `/api/notifications/unread-count` | — | Số chưa đọc |
| 92 | PUT | `/api/notifications/{id}/read` | — | Đánh dấu đã đọc |
| 93 | PUT | `/api/notifications/read-all` | — | Đọc tất cả |
| 94 | POST | `/api/devices/register` | — | Đăng ký FCM token (nhận push) |
| 95 | DELETE | `/api/devices/{token}` | — | Hủy đăng ký thiết bị |

## 16. Fees — FR4.1, FR2.6 · Ngày 11
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 96 | GET | `/api/fee-categories` | Admin | DS loại phí |
| 97 | POST | `/api/fee-categories` | Admin | Tạo loại phí |
| 98 | PUT | `/api/fee-categories/{id}` | Admin | Sửa |
| 99 | DELETE | `/api/fee-categories/{id}` | Admin | Xóa |
| 100 | GET | `/api/fee-invoices?studentId=&isPaid=` | Admin | DS hóa đơn |
| 101 | GET | `/api/fee-invoices/me` | Student/Parent | Các khoản thu của tôi/con tôi |
| 102 | POST | `/api/fee-invoices` | Admin | Tạo hóa đơn cho 1 HS |
| 103 | POST | `/api/fee-invoices/batch` | Admin | Gán phí cho cả khối/lớp |
| 104 | GET | `/api/fee-invoices/{id}/receipt` | Student/Parent | Biên lai điện tử |

## 17. Payments — FR2.6, FR4.2 · Ngày 11
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 105 | POST | `/api/payments/vnpay/create` | Parent | Tạo URL thanh toán VNPay |
| 106 | GET | `/api/payments/vnpay/return` | *Public* | Return URL sau thanh toán |
| 107 | POST | `/api/payments/vnpay/ipn` | *Public* | Webhook/IPN VNPay (verify signature) |
| 108 | POST | `/api/payments/payos/create` | Parent | Tạo link thanh toán PayOS |
| 109 | POST | `/api/payments/payos/webhook` | *Public* | Webhook PayOS (verify checksum) |
| 110 | GET | `/api/payments/history?studentId=` | Parent/Admin | Lịch sử giao dịch |
| 111 | GET | `/api/payment-config` | Admin | Xem cấu hình cổng thanh toán |
| 112 | PUT | `/api/payment-config` | Admin | Cập nhật API key VNPay/PayOS |

## 18. Reports — FR5.5 · Ngày 12
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 113 | GET | `/api/reports/grades?classId=&format=` | Admin/Trưởng BM | Báo cáo bảng điểm (Excel/PDF) |
| 114 | GET | `/api/reports/attendance?classId=&format=` | Admin/Trưởng BM | Báo cáo chuyên cần |
| 115 | GET | `/api/reports/fees?classId=&format=` | Admin | Báo cáo học phí |
| 116 | GET | `/api/reports/dashboard` | Admin/Trưởng BM | Số liệu tổng quan |

## 19. App / System — FR4.4
| # | Method | Endpoint | Role | Mô tả |
|---|---|---|---|---|
| 117 | GET | `/api/app/version` | *Public* | Kiểm tra phiên bản (Force Update) |
| 118 | GET | `/api/health` | *Public* | Health check |

---

## Ghi chú thiết kế
- **Phân trang:** mọi endpoint `GET` trả danh sách dài đều nhận `?page=&pageSize=` (NFR4.2).
- **Phân quyền:** dùng `[Authorize(Roles = "...")]`; các API "của tôi" (`/me`) tự lọc theo `userId` trong JWT.
- **Upload file** (#77) dùng chung cho bài nộp & ảnh chứng nhận y tế — trả về URL để lưu vào entity tương ứng.
- **Webhook thanh toán** (#107, #109) là *Public* nhưng **bắt buộc verify chữ ký số** từ VNPay/PayOS (NFR4.3).
- Con số ~118 là **ước lượng mục tiêu**; khi code thực tế có thể gộp/tách ±10 endpoint.

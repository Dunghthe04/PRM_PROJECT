# FSchool Web Portal (React)

Cổng quản trị **Admin + Giáo viên** — React (Vite + Ant Design), gọi chung API .NET 8.
Học sinh / phụ huynh dùng **app Flutter** (`mobile-app/`).

## Chạy local

```bash
# Terminal 1 — Backend API (port 5177)
cd backend
dotnet run

# Terminal 2 — Web portal (port 5173)
cd web-portal
npm install
npm run dev
```

Mở http://localhost:5173

Biến môi trường: `web-portal/.env.development` → `VITE_API_BASE_URL=http://localhost:5177/api`

## Tài khoản demo (seed)

| Role | SĐT | MK |
|------|-----|-----|
| Admin | 0900000001 | 123456 |
| GV (CN 10A1) | 0900000002 | 123456 |

## Tính năng chính

**Admin:** người dùng (+ Import Excel), danh mục Kỳ/Môn/Lớp, phân công GV, học phí, thông báo, báo cáo.

**Giáo viên:** lớp phụ trách, điểm danh (CN), duyệt đơn nghỉ (CN), Import Excel điểm, gửi TB lớp.

## Import Excel

| Endpoint | Role |
|----------|------|
| `GET/POST /api/import/users` | Admin |
| `GET/POST /api/import/grades?classId=&subjectId=` | Teacher, Admin |

**Nhập điểm (GV):** chọn Kỳ → Lớp → Môn → tải mẫu (sổ THPT: Miệng 1–3, 15 phút 1–3, 1 tiết 1–2, Giữa kỳ, Cuối kỳ) → điền → upload. Điểm công bố ngay.

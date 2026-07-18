# FSchool Monorepo

Nền tảng sổ liên lạc điện tử: **Mobile (Flutter)** + **Backend (.NET 8)** + **Web Portal (React)** cho Admin/GV.

## Cấu trúc thư mục

| Folder | Vai trò |
|--------|---------|
| `backend/` | API .NET 8 |
| `mobile-app/` | App Flutter (HS / PH / GV) |
| `web-portal/` | Portal React (Admin + Giáo viên) |
| `docs/` | Tài liệu SRS / API / phân quyền |

## Backend (.NET 8)

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

Swagger: http://localhost:5177/swagger

## Web Portal (Admin + Giáo viên)

```bash
cd web-portal
npm install
npm run dev
```

http://localhost:5173 — hướng dẫn: [web-portal/README.md](web-portal/README.md)

## Mobile App (Flutter)

```bash
cd mobile-app
flutter pub get
flutter run
```

## Tài khoản demo (seed)

| Role | SĐT | MK |
|------|-----|-----|
| Admin | 0900000001 | 123456 |
| Giáo viên | 0900000002 | 123456 |

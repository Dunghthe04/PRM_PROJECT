# FSchool Monorepo

## Mobile App (Flutter)
The mobile app is located in the `mobile` folder.

### Setup
1. `cd mobile`
2. `flutter pub get`
3. `flutter run`

## API (.NET 8)
The backend API is located in the `api` folder. It uses a 3-layer architecture.

### Setup
1. `cd api`
2. `dotnet restore`
3. Update `appsettings.Development.json` if needed (currently set to LocalDB).
4. Apply migrations: `dotnet ef migrations add InitialCreate` and `dotnet ef database update`
5. `dotnet run`

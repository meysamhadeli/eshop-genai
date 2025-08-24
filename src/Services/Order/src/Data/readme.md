dotnet ef migrations add initial --context OrderDbContext -o "Data\Migrations"
dotnet ef database update --context OrderDbContext

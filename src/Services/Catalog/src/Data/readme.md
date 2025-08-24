dotnet ef migrations add initial --context CatalogDbContext -o "Data\Migrations"
dotnet ef database update --context CatalogDbContext

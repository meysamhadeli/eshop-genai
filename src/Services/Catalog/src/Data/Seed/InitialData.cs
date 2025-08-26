using System;
using System.Collections.Generic;
using Catalog.Products.Models;

namespace Flight.Data.Seed;

public static class InitialData
{
    public static List<Product> Products { get; }

    static InitialData()
    {
        Products = new List<Product>
        {
            new Product()
            {
                Id = Guid.CreateVersion7(),
                Name = "iPhone 16",
                Description = "Latest iPhone model with advanced camera system",
                Price = 999.99m,
                ImageUrl = "Images/iphone-16.jpg",
                CreatedAt = DateTime.UtcNow,
            },
            new Product()
            {
                Id = Guid.CreateVersion7(),
                Name = "Asus Laptop",
                Description = "High-performance Asus laptop with OLED display",
                Price = 1299.99m,
                ImageUrl = "Images/laptop-asus.jpg",
                CreatedAt = DateTime.UtcNow,
            },
            new Product()
            {
                Id = Guid.CreateVersion7(),
                Name = "HP 14 Laptop",
                Description = "Compact HP 14-inch laptop with long battery life",
                Price = 699.99m,
                ImageUrl = "Images/laptop-hp14.jpg",
                CreatedAt = DateTime.UtcNow,
            },
            new Product()
            {
                Id = Guid.CreateVersion7(),
                Name = "Lenovo Laptop",
                Description = "Business-grade Lenovo laptop with robust security",
                Price = 899.99m,
                ImageUrl = "Images/laptop-lenovo.jpg",
                CreatedAt = DateTime.UtcNow,
            },
            new Product()
            {
                Id = Guid.CreateVersion7(),
                Name = "Microsoft Laptop",
                Description = "Sleek Microsoft Surface laptop with touchscreen",
                Price = 1199.99m,
                ImageUrl = "Images/laptop-microsoft.jpg",
                CreatedAt = DateTime.UtcNow,
            },
            new Product()
            {
                Id = Guid.CreateVersion7(),
                Name = "Redmi Note 14",
                Description = "Affordable Redmi smartphone with great camera",
                Price = 249.99m,
                ImageUrl = "Images/redmi-note14.jpg",
                CreatedAt = DateTime.UtcNow,
            },
            new Product()
            {
                Id = Guid.CreateVersion7(),
                Name = "Samsung S20",
                Description = "Premium Samsung flagship smartphone",
                Price = 799.99m,
                ImageUrl = "Images/samsung-s20.jpg",
                CreatedAt = DateTime.UtcNow,
            },
            new Product()
            {
                Id = Guid.CreateVersion7(),
                Name = "Samsung S20 Plus",
                Description = "Flagship smartphone with Dynamic AMOLED display",
                Price = 900.99m,
                ImageUrl = "Images/samsung-s20-plus.jpg",
                CreatedAt = DateTime.UtcNow
            },
        };
    }
}
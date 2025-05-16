using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence
{
    public class DbInitializer : IDbInitializer
    {
        private readonly VoltyksDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;



        public DbInitializer(VoltyksDbContext context
            , UserManager<AppUser> userManager
            , RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task InitializeAsync()
        {

            try
            {
                // Create For The Data If it's doesn't Exists & Apply any Pending Migrations
                if (_context.Database.GetPendingMigrations().Any())
                {
                    await _context.Database.MigrateAsync();
                }

                //// Data Seeding 
                //// Seeding For ProductBrands Form Json File
                //if (!_context.ProductBrands.Any())
                //{
                //    // 1. Read All Data brands From Json File
                //    var brandsData = await File.ReadAllTextAsync(@"..\Infrastructure\Persistence\Data\Seeding\brands.json");

                //    // 2. Transform The Data To List<ProdcutBrand>
                //    var brands = JsonSerializer.Deserialize<List<ProductBrand>>(brandsData);

                //    // 3. Add List<ProdcutBrand> To Database
                //    if (brands is not null && brands.Any())
                //    {
                //        await _context.ProductBrands.AddRangeAsync(brands);
                //        await _context.SaveChangesAsync();
                //    }
                //}

                //// Seeding For ProductTypes Form Json File
                //if (!_context.ProductTypes.Any())
                //{
                //    // 1. Read All Data types From Json File
                //    var typesData = await File.ReadAllTextAsync(@"..\Infrastructure\Persistence\Data\Seeding\types.json");

                //    // 2. Transform The Data To List<ProductType>
                //    var types = JsonSerializer.Deserialize<List<ProductType>>(typesData);

                //    // 3. Add List<ProductType> To Database
                //    if (types is not null && types.Any())
                //    {
                //        await _context.ProductTypes.AddRangeAsync(types);
                //        await _context.SaveChangesAsync();
                //    }
                //}


                //// Seeding For Products Form Json File
                //if (!_context.Products.Any())
                //{
                //    // 1. Read All Data products From Json File
                //    var productsData = await File.ReadAllTextAsync(@"..\Infrastructure\Persistence\Data\Seeding\products.json");

                //    // 2. Transform The Data To List<Product>
                //    var products = JsonSerializer.Deserialize<List<Product>>(productsData);

                //    // 3. Add List<Product> To Database
                //    if (products is not null && products.Any())
                //    {
                //        await _context.Products.AddRangeAsync(products);
                //        await _context.SaveChangesAsync();
                //    }
                //}
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task InitializeIdentityAsync()
        {

            // Create For The Data If it's doesn't Exists & Apply any Pending Migrations
            if (_context.Database.GetPendingMigrations().Any())
            {
                await _context.Database.MigrateAsync();
            }

            // Data Seeding 
            // Seeding For Roles 
            if (!_roleManager.Roles.Any())
            {
                await _roleManager.CreateAsync(new IdentityRole()
                {
                    Name = "Admin"
                });               
            }



            // Seeding For Users 
            if (!_userManager.Users.Any())
            {

                // التأكد من الأدوار
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));

               

                // Create User            
                var adminUser = new AppUser()
                {
                    FullName = "VoltyksOwner",
                    FirstName = "Voltyks",
                    LastName = "Owner",
                    Email = "Admin@gmail.com",
                    UserName = "Admin",
                    PhoneNumber = "01000000000"
                };

                var result1 = await _userManager.CreateAsync(adminUser, "Voltyks1041998@");
                if (result1.Succeeded)
                    await _userManager.AddToRoleAsync(adminUser, "Admin");

              

                // تحقق من الأخطاء إن وجدت
                if (!result1.Succeeded)
                    Console.WriteLine($"Admin Errors: {string.Join(", ", result1.Errors.Select(e => e.Description))}");

               
            }








        }
    }
}

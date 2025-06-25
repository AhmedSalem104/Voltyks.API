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
using Voltyks.Persistence.Entities.Main;

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
                //// Seeding For Brands Form Json File
                if (!_context.Brands.Any())
                {
                    // 1. Read All Data brands From Json File
                    var brandsData = await File.ReadAllTextAsync(@"..\Voltyks.Persistence\Data\Seeding\brands_seed.json");

                    // 2. Transform The Data To List<Brands>
                    var brands = JsonSerializer.Deserialize<List<Brand>>(brandsData);

                    // 3. Add List<Brands> To Database
                    if (brands is not null && brands.Any())
                    {
                        await _context.Brands.AddRangeAsync(brands);
                        await _context.SaveChangesAsync();
                    }
                }

                //// Seeding For Models Form Json File
                if (!_context.Models.Any())
                {
                // 1. Read All Data types From Json File

                var typesData = await File.ReadAllTextAsync(@"..\Voltyks.Persistence\Data\Seeding\models_seed.json");

                    // 2. Transform The Data To List<Models>
                    var types = JsonSerializer.Deserialize<List<Model>>(typesData);

                    // 3. Add List<Models> To Database
                    if (types is not null && types.Any())
                    {
                        await _context.Models.AddRangeAsync(types);
                        await _context.SaveChangesAsync();
                    }
                }

                if (!_context.PriceOptions.Any())
                {
                    var prices = new List<PriceOption>();
                    for (decimal i = 5; i <= 100; i += 5)
                    {
                        prices.Add(new PriceOption { Value = i });
                    }
                    _context.PriceOptions.AddRange(prices);
                    _context.SaveChanges();
                }
                // Seeding Protocols
                if (!_context.Protocols.Any())
                {
                    var protocols = new List<Protocol>
            {
                new Protocol { Name = "Chinese" },
                new Protocol { Name = "European" }
            };
                    _context.Protocols.AddRange(protocols);
                    await _context.SaveChangesAsync();

                }

                // Seeding Capacities
                if (!_context.Capacities.Any())
                {
                    var capacities = new List<Capacity>
            {
                new Capacity { KW = 7 },
                new Capacity { KW = 15 },
                new Capacity { KW = 22 }
            };
                    _context.Capacities.AddRange(capacities);
                    await _context.SaveChangesAsync();

                }

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

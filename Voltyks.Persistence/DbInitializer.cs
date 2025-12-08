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

                // Base path for seeding files
                var seedingBasePath = Path.Combine(AppContext.BaseDirectory, "Data", "Seeding");

                //// Data Seeding
                //// Seeding For Brands Form Json File
                if (!_context.Brands.Any())
                {
                    try
                    {
                        var seedPath = Path.Combine(seedingBasePath, "brands_seed.json");
                        var brandsData = await File.ReadAllTextAsync(seedPath);
                        var brands = JsonSerializer.Deserialize<List<Brand>>(brandsData);
                        if (brands is not null && brands.Any())
                        {
                            await _context.Brands.AddRangeAsync(brands);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch { /* Skip seeding if file not found - data already exists in production */ }
                }

                //// Seeding For Models Form Json File
                if (!_context.Models.Any())
                {
                    try
                    {
                        var seedPath = Path.Combine(seedingBasePath, "models_seed.json");
                        var typesData = await File.ReadAllTextAsync(seedPath);
                        var types = JsonSerializer.Deserialize<List<Model>>(typesData);
                        if (types is not null && types.Any())
                        {
                            await _context.Models.AddRangeAsync(types);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch { /* Skip seeding if file not found */ }
                }                    

                //// Seeding For Protocols Form Json File
                if (!_context.Protocols.Any())
                {
                    try
                    {
                        var seedPath = Path.Combine(seedingBasePath, "protocal_seed.json");
                        var ProtocolsData = await File.ReadAllTextAsync(seedPath);
                        var Protocols = JsonSerializer.Deserialize<List<Protocol>>(ProtocolsData);
                        if (Protocols is not null && Protocols.Any())
                        {
                            await _context.Protocols.AddRangeAsync(Protocols);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch { /* Skip seeding if file not found */ }
                }

                //// Seeding For priceOption Form Json File
                if (!_context.PriceOptions.Any())
                {
                    try
                    {
                        var seedPath = Path.Combine(seedingBasePath, "priceOption_seed.json");
                        var PriceOptionsData = await File.ReadAllTextAsync(seedPath);
                        var PriceOptions = JsonSerializer.Deserialize<List<PriceOption>>(PriceOptionsData);
                        if (PriceOptions is not null && PriceOptions.Any())
                        {
                            await _context.PriceOptions.AddRangeAsync(PriceOptions);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch { /* Skip seeding if file not found */ }
                }

                //// Seeding For Capacities Form Json File
                if (!_context.Capacities.Any())
                {
                    try
                    {
                        var seedPath = Path.Combine(seedingBasePath, "capacity_seed.json");
                        var CapacitiesData = await File.ReadAllTextAsync(seedPath);
                        var Capacities = JsonSerializer.Deserialize<List<Capacity>>(CapacitiesData);
                        if (Capacities is not null && Capacities.Any())
                        {
                            await _context.Capacities.AddRangeAsync(Capacities);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch { /* Skip seeding if file not found */ }
                }

                //// Seeding For UserTypes Form Json File
                if (!_context.UserTypes.Any())
                {
                    try
                    {
                        var seedPath = Path.Combine(seedingBasePath, "UserTypes_seed.json");
                        var UserTypesData = await File.ReadAllTextAsync(seedPath);
                        var UserTypes = JsonSerializer.Deserialize<List<UserType>>(UserTypesData);
                        if (UserTypes is not null && UserTypes.Any())
                        {
                            await _context.UserTypes.AddRangeAsync(UserTypes);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch { /* Skip seeding if file not found */ }
                }


                //// Seeding For TermsDocuments From Json File
                if (!_context.termsDocuments.Any())
                {
                    try
                    {
                        var seedPath = Path.Combine(seedingBasePath, "TermsDocuments_seed.json");
                        var json = await File.ReadAllTextAsync(seedPath);
                        var docs = JsonSerializer.Deserialize<List<SeedItem>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (docs is not null && docs.Any())
                        {
                            foreach (var d in docs)
                            {
                                var entity = new TermsDocument
                                {
                                    VersionNumber = d.VersionNumber,
                                    Lang = (d.Lang?.ToLowerInvariant()) == "ar" ? "ar" : "en",
                                    IsActive = d.IsActive,
                                    PublishedAt = DateTime.UtcNow,
                                    PayloadJson = d.PayloadJson.GetRawText()
                                };
                                await _context.termsDocuments.AddAsync(entity);
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch { /* Skip seeding if file not found */ }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        private class SeedItem
        {
            public int VersionNumber { get; set; }
            public string? Lang { get; set; }                // "en" | "ar"
            public bool IsActive { get; set; }
            public DateTime? PublishedAt { get; set; }       // اختياري في الملف
            public JsonElement PayloadJson { get; set; }     // نقرأ الـ object كما هو
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



            //// Seeding For Users 
            //if (!_userManager.Users.Any())
            //{

            //    // التأكد من الأدوار
            //    if (!await _roleManager.RoleExistsAsync("Admin"))
            //        await _roleManager.CreateAsync(new IdentityRole("Admin"));

               

            //    // Create User            
            //    var adminUser = new AppUser()
            //    {
            //        FullName = "VoltyksOwner",
            //        FirstName = "Voltyks",
            //        LastName = "Owner",
            //        Email = "Admin@gmail.com",
            //        UserName = "Admin",
            //        PhoneNumber = "01000000000"
            //    };

            //    var result1 = await _userManager.CreateAsync(adminUser, "Voltyks1041998@");
            //    if (result1.Succeeded)
            //        await _userManager.AddToRoleAsync(adminUser, "Admin");

              

            //    // تحقق من الأخطاء إن وجدت
            //    if (!result1.Succeeded)
            //        Console.WriteLine($"Admin Errors: {string.Join(", ", result1.Errors.Select(e => e.Description))}");

               
            //}


            // Seeding For Users & Roles - wrapped in try-catch for Azure production
            try
            {
                // 1) تأكيد الأدوار
                var roles = new[] { "Admin", "Operator", "Viewer" };
                foreach (var role in roles)
                    if (!await _roleManager.RoleExistsAsync(role))
                        await _roleManager.CreateAsync(new IdentityRole(role));

                // 2) إنشاء Admin إذا لم يكن موجوداً
                var existingAdmin = await _userManager.FindByNameAsync("Admin");
                if (existingAdmin == null)
                {
                    var adminUser = new AppUser
                    {
                        FullName = "VoltyksOwner",
                        FirstName = "Voltyks",
                        LastName = "Owner",
                        Email = "Admin@gmail.com",
                        UserName = "Admin",
                        PhoneNumber = "+201000000000",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        Address = new Address
                        {
                            City = "Cairo",
                            Country = "Egypt",
                            Street = "Admin Street"
                        }
                    };

                    var resultAdmin = await _userManager.CreateAsync(adminUser, "Voltyks1041998@");
                    if (resultAdmin.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(adminUser, "Admin");
                        await _userManager.AddClaimsAsync(adminUser, new[]
                        {
                            new System.Security.Claims.Claim("role-level", "admin"),
                            new System.Security.Claims.Claim("can-manage-terms-fees", "true"),
                            new System.Security.Claims.Claim("can-transfer-fees", "true"),
                        });
                    }
                }

                // 3) إنشاء Operator إذا لم يكن موجوداً
                var existingOperator = await _userManager.FindByNameAsync("operator");
                if (existingOperator == null)
                {
                    var operatorUser = new AppUser
                    {
                        FullName = "VoltyksOperator",
                        FirstName = "Voltyks",
                        LastName = "Operator",
                        Email = "operator@voltyks.com",
                        UserName = "operator",
                        PhoneNumber = "+201000000001",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        Address = new Address
                        {
                            City = "Cairo",
                            Country = "Egypt",
                            Street = "Operator Street"
                        }
                    };

                    var resultOp = await _userManager.CreateAsync(operatorUser, "Voltyks@Operator");
                    if (resultOp.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(operatorUser, "Operator");
                        await _userManager.AddClaimsAsync(operatorUser, new[]
                        {
                            new System.Security.Claims.Claim("role-level", "operator"),
                            new System.Security.Claims.Claim("can-manage-terms-fees", "false"),
                            new System.Security.Claims.Claim("can-transfer-fees", "true"),
                        });
                    }
                }
            }
            catch { /* Skip user seeding if fails - users already exist in production */ }
        }

        public async Task ForceSeedAsync()
        {
            var seedingBasePath = Path.Combine(AppContext.BaseDirectory, "Data", "Seeding");

            // Force Seed Brands
            try
            {
                var seedPath = Path.Combine(seedingBasePath, "brands_seed.json");
                var brandsData = await File.ReadAllTextAsync(seedPath);
                var brands = JsonSerializer.Deserialize<List<Brand>>(brandsData);
                if (brands is not null && brands.Any())
                {
                    var existingNames = await _context.Brands.Select(b => b.Name).ToListAsync();
                    var newBrands = brands.Where(b => !existingNames.Contains(b.Name)).ToList();
                    if (newBrands.Any())
                    {
                        await _context.Brands.AddRangeAsync(newBrands);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            // Force Seed Models
            try
            {
                var seedPath = Path.Combine(seedingBasePath, "models_seed.json");
                var data = await File.ReadAllTextAsync(seedPath);
                var items = JsonSerializer.Deserialize<List<Model>>(data);
                if (items is not null && items.Any())
                {
                    var existingNames = await _context.Models.Select(m => m.Name).ToListAsync();
                    var newItems = items.Where(m => !existingNames.Contains(m.Name)).ToList();
                    if (newItems.Any())
                    {
                        await _context.Models.AddRangeAsync(newItems);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            // Force Seed Protocols
            try
            {
                var seedPath = Path.Combine(seedingBasePath, "protocal_seed.json");
                var data = await File.ReadAllTextAsync(seedPath);
                var items = JsonSerializer.Deserialize<List<Protocol>>(data);
                if (items is not null && items.Any())
                {
                    var existingNames = await _context.Protocols.Select(p => p.Name).ToListAsync();
                    var newItems = items.Where(p => !existingNames.Contains(p.Name)).ToList();
                    if (newItems.Any())
                    {
                        await _context.Protocols.AddRangeAsync(newItems);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            // Force Seed PriceOptions
            try
            {
                var seedPath = Path.Combine(seedingBasePath, "priceOption_seed.json");
                var data = await File.ReadAllTextAsync(seedPath);
                var items = JsonSerializer.Deserialize<List<PriceOption>>(data);
                if (items is not null && items.Any() && !await _context.PriceOptions.AnyAsync())
                {
                    await _context.PriceOptions.AddRangeAsync(items);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            // Force Seed Capacities
            try
            {
                var seedPath = Path.Combine(seedingBasePath, "capacity_seed.json");
                var data = await File.ReadAllTextAsync(seedPath);
                var items = JsonSerializer.Deserialize<List<Capacity>>(data);
                if (items is not null && items.Any() && !await _context.Capacities.AnyAsync())
                {
                    await _context.Capacities.AddRangeAsync(items);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            // Force Seed UserTypes
            try
            {
                var seedPath = Path.Combine(seedingBasePath, "UserTypes_seed.json");
                var data = await File.ReadAllTextAsync(seedPath);
                var items = JsonSerializer.Deserialize<List<UserType>>(data);
                if (items is not null && items.Any() && !await _context.UserTypes.AnyAsync())
                {
                    await _context.UserTypes.AddRangeAsync(items);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }






        }
    }
}

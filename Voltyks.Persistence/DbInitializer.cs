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

                //// Seeding For Protocols Form Json File
                if (!_context.Protocols.Any())
                {
                    // 1. Read All Data Protocols From Json File
                    var ProtocolsData = await File.ReadAllTextAsync(@"..\Voltyks.Persistence\Data\Seeding\protocal_seed.json");

                    // 2. Transform The Data To List<Protocols>
                    var Protocols = JsonSerializer.Deserialize<List<Protocol>>(ProtocolsData);

                    // 3. Add List<Protocols> To Database
                    if (Protocols is not null && Protocols.Any())
                    {
                        await _context.Protocols.AddRangeAsync(Protocols);
                        await _context.SaveChangesAsync();
                    }
                }

                //// Seeding For priceOption Form Json File
                if (!_context.PriceOptions.Any())
                {
                    // 1. Read All Data PriceOptions From Json File
                    var PriceOptionsData = await File.ReadAllTextAsync(@"..\Voltyks.Persistence\Data\Seeding\priceOption_seed.json");

                    // 2. Transform The Data To List<PriceOptions>
                    var PriceOptions = JsonSerializer.Deserialize<List<PriceOption>>(PriceOptionsData);

                    // 3. Add List<PriceOptions> To Database
                    if (PriceOptions is not null && PriceOptions.Any())
                    {
                        await _context.PriceOptions.AddRangeAsync(PriceOptions);
                        await _context.SaveChangesAsync();
                    }
                }

                //// Seeding For Capacities Form Json File
                if (!_context.Capacities.Any())
                {
                    // 1. Read All Data Capacities From Json File
                    var CapacitiesData = await File.ReadAllTextAsync(@"..\Voltyks.Persistence\Data\Seeding\capacity_seed.json");

                    // 2. Transform The Data To List<Capacities>
                    var Capacities = JsonSerializer.Deserialize<List<Capacity>>(CapacitiesData);

                    // 3. Add List<PriceOptions> To Database
                    if (Capacities is not null && Capacities.Any())
                    {
                        await _context.Capacities.AddRangeAsync(Capacities);
                        await _context.SaveChangesAsync();
                    }
                }

                //// Seeding For UserTypes Form Json File
                if (!_context.UserTypes.Any())
                {
                    // 1. Read All Data Capacities From Json File
                    var UserTypesData = await File.ReadAllTextAsync(@"..\Voltyks.Persistence\Data\Seeding\UserTypes_seed.json");

                    // 2. Transform The Data To List<UserTypes>
                    var UserTypes = JsonSerializer.Deserialize<List<UserType>>(UserTypesData);

                    // 3. Add List<UserTypes> To Database
                    if (UserTypes is not null && UserTypes.Any())
                    {
                        await _context.UserTypes.AddRangeAsync(UserTypes);
                        await _context.SaveChangesAsync();
                    }
                }


                //// Seeding For TermsDocuments From Json File
                if (!_context.termsDocuments.Any())
                {
                    // 1) Read all data from Json file
                    var json = await File.ReadAllTextAsync(@"..\Voltyks.Persistence\Data\Seeding\TermsDocuments_seed.json");

                    // 2) Transform to List<TermsDocument>
                    var docs = JsonSerializer.Deserialize<List<SeedItem>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // 3) Add to DB
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
                                PayloadJson = d.PayloadJson.GetRawText() // نخزن JSON كما هو string
                            };
                            await _context.termsDocuments.AddAsync(entity);
                        }
                        await _context.SaveChangesAsync();
                    }
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


            // Seeding For Users & Roles
            if (!_userManager.Users.Any())
            {
                // 1) تأكيد الأدوار
                var roles = new[] { "Admin", "Operator", "Viewer" };
                foreach (var role in roles)
                    if (!await _roleManager.RoleExistsAsync(role))
                        await _roleManager.CreateAsync(new IdentityRole(role));

                // 2) إنشاء Admin
                var adminUser = new AppUser
                {
                    FullName = "VoltyksOwner",
                    FirstName = "Voltyks",
                    LastName = "Owner",
                    Email = "Admin@gmail.com",
                    UserName = "Admin",
                    PhoneNumber = "01000000000",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true
                };

                var resultAdmin = await _userManager.CreateAsync(adminUser, "Voltyks1041998@");
                if (resultAdmin.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    // Claims اختيارية لو عايز تميّز الأدمن في الفرونت/اللوغز
                    await _userManager.AddClaimsAsync(adminUser, new[]
                    {
            new System.Security.Claims.Claim("role-level", "admin"),
            new System.Security.Claims.Claim("can-manage-terms-fees", "true"),
            new System.Security.Claims.Claim("can-transfer-fees", "true"),
        });
                }
                else
                {
                    Console.WriteLine($"Admin Errors: {string.Join(", ", resultAdmin.Errors.Select(e => e.Description))}");
                }

                // 3) إنشاء مستخدم بدور آخر (Operator)
                var operatorUser = new AppUser
                {
                    FullName = "VoltyksOperator",
                    FirstName = "Voltyks",
                    LastName = "Operator",
                    Email = "operator@voltyks.com",
                    UserName = "operator",
                    PhoneNumber = "01000000001",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true
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
                else
                {
                    Console.WriteLine($"Operator Errors: {string.Join(", ", resultOp.Errors.Select(e => e.Description))}");
                }

                // لو حابب تضيف Viewer كمان
                // var viewerUser = new AppUser { ... };
                // await _userManager.CreateAsync(viewerUser, "Strong@Pass1");
                // await _userManager.AddToRoleAsync(viewerUser, "Viewer");
            }






        }
    }
}

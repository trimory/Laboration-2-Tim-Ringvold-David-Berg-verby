using Laboration2_MVC.Models;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// Register TransactionDbContext with dependency injection
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//// Add this to Program.cs right before app.Run()
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var dbContext = services.GetRequiredService<TransactionDbContext>();

//    // Delete the database and recreate it
//    dbContext.Database.EnsureDeleted();
//    dbContext.Database.EnsureCreated();

//    // Log basic info
//    Console.WriteLine($"Database path: {dbContext.Database.GetConnectionString()}");
//    Console.WriteLine($"Database provider: {dbContext.Database.ProviderName}");

//    // Create test data
//    if (!dbContext.Categories.Any())
//    {
//        // Add some test categories
//        var testcategories = new[]
//        {
//            new Category { Name = "Mat" },
//            new Category { Name = "Transport" },
//            new Category { Name = "Nöje" }
//        };
//        dbContext.Categories.AddRange(testcategories);
//        dbContext.SaveChanges();
//        Console.WriteLine("Added default categories");
//    }

//    // Check if tables exist
//    var categories = dbContext.Categories.Count();
//    var rules = dbContext.CategoryRules.Count();
//    Console.WriteLine($"Database has {categories} categories and {rules} rules");
//}

app.Run();


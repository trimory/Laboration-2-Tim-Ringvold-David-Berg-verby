using Laboration2MVC.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<DatabaseModel>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


/*using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbModel = services.GetRequiredService<DatabaseModel>();

    if (!System.IO.File.Exists(dbModel.databaseFilePath))
    {
        Console.WriteLine("⚙️ Database file missing. Creating a new one...");
        await dbModel.CreateDatabase(); // ✅ Run in an async scope
        Console.WriteLine("✅ Database created successfully.");
    }
}
*/

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

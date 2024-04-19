using System.Text;
using WebSite;
using WebSite.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<CustService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseWebSockets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{param?}");
app.Run("http://*:80");
using Auth.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Hydra Admin API client
var hydraAdminUrl = (builder.Configuration["Hydra:AdminUrl"] ?? "http://localhost:4445").TrimEnd('/') + "/";
builder.Services.AddHttpClient("hydra", client => client.BaseAddress = new Uri(hydraAdminUrl));
builder.Services.AddScoped<IHydraService, HydraService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

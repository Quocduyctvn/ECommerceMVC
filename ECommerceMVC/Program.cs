using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDistributedMemoryCache();
// SECTION 
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<Hshop2023Context>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("HShop"));
});
//COOKIE 
builder.Services.AddAuthentication()
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {
        options.LoginPath = "/KhachHang/";   // action nào y/c đăng nhập- mà ch đ/n thì đá về trang đăng kí 
        options.AccessDeniedPath = "/AccessDenied";
    });



// dâng ký dịch vụ automapper
// https://docs.automapper.org/en/stable/Dependency-injection.html
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));


//Đăng Ký PaypalClient dạng SingleTon - Chỉ có 1 instance duy nhất  trong toàn bộ ứng dụng 
builder.Services.AddSingleton(x => new PaypalClient
(
	builder.Configuration["PaypalOptions:AppId"],
	builder.Configuration["PaypalOptions:AppSecret"],
	builder.Configuration["PaypalOptions:Mode"]

));

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


app.UseSession();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=HangHoa}/{action=Index}/{id?}");

app.Run();

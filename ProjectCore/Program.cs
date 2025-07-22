using Microsoft.EntityFrameworkCore;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//when we add something to service class we are adding it to dependency injection
//and telling the application to do this configuration whenever it is called for implementation
builder.Services.AddDbContext<Bulky.DataAccess.Data.ApplicationDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddIdentity<IdentityUser,IdentityRole>().AddEntityFrameworkStores<Bulky.DataAccess.Data.ApplicationDbContext>().AddDefaultTokenProviders(); //AddDefaultTokenProviders is needed for email confirmation token, generally default identity has implementation of default token providers
builder.Services.AddRazorPages();//we need to notify when razor pages are present in the project(i.e. Identity pages)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();//basically checking if user name and password is valid
app.UseAuthorization();//access to pages is restricted by roles
app.MapRazorPages();//this will make sure rounting is added to map the razor pages
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Model;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AuthDbContext>(options =>
 options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnectionString")));

builder.Services.AddDataProtection();
builder.Services.AddScoped<IProtectionService, ProtectionService>();

// Recaptcha
builder.Services.Configure<RecaptchaOptions>(builder.Configuration.GetSection("Recaptcha"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRecaptchaService, RecaptchaService>();

// Email
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
 options.Password.RequiredLength =12;
 options.Password.RequireDigit = true;
 options.Password.RequireLowercase = true;
 options.Password.RequireUppercase = true;
 options.Password.RequireNonAlphanumeric = true;

 options.Lockout.MaxFailedAccessAttempts =3;
 options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
 options.Lockout.AllowedForNewUsers = true;

 options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
 options.Cookie.HttpOnly = true;
 // enforce1 minute max lifetime for authentication cookie
 options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
 // do not allow sliding expiration so session cannot be extended
 options.SlidingExpiration = false;
 options.LoginPath = "/Login";
});

// Antiforgery is added by default for Razor Pages, session for tracking
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
 // IdleTimeout changed to1 minute as requested
 options.IdleTimeout = TimeSpan.FromMinutes(1);
 options.Cookie.HttpOnly = true;
 options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
 app.UseExceptionHandler("/Error");
 // The default HSTS value is30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
 app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Redirect root path to home page
app.Use(async (context, next) =>
{
 if (context.Request.Path == "/")
 {
 context.Response.Redirect("/Index");
 return;
 }
 await next();
});

// Provide custom status code pages (404,403, etc.)
app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");

app.UseAuthentication();

// Middleware to enforce password max age after sign-in
app.Use(async (context, next) =>
{
 if (context.User?.Identity?.IsAuthenticated == true)
 {
 var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
 var db = context.RequestServices.GetRequiredService<AuthDbContext>();
 var user = await userManager.GetUserAsync(context.User);
 if (user != null)
 {
 var last = db.PasswordHistories.Where(p => p.UserId == user.Id).OrderByDescending(p => p.ChangedAt).FirstOrDefault();
 var maxDays = builder.Configuration.GetValue<int>("PasswordPolicy:MaxAgeDays");
 if (last != null && (DateTime.UtcNow - last.ChangedAt).TotalDays > maxDays)
 {
 // redirect to change password page
 context.Response.Redirect("/Account/ChangePassword");
 return;
 }
 }
 }
 await next();
});

app.UseAuthorization();

app.UseSession();

app.MapRazorPages();

app.Run();

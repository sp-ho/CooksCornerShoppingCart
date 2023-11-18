using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShoppingCart.Data;
using Microsoft.AspNetCore.Identity;
using ShoppingCart.Models;
namespace ShoppingCart
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<ShoppingCartContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ShoppingCartContext") ?? throw new InvalidOperationException("Connection string 'ShoppingCartContext' not found.")));

            // ORIGINAL CODE of IdentityUser
            /*builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ShoppingCartContext>();*/

            // MODIFIED CODE of IdentityUser
            // define user roles
            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ShoppingCartContext>();


            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // MODIFIED CODE: check seed data codes
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                SeedData.Initialize(services); // initialize the data in SeedData model
            }


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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            // MODIFIED CODE
            // change new roles
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = 
                    scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                
                // create array for the user roles we want in the app
                var roles = new[] { "Admin", "Customer" };

                foreach(var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // seed admin
            using (var scope = app.Services.CreateScope())
            {
                var userManager =
                    scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                string email = "admin@cookscorner.com";
                string password = "Abcd@1234";

                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new IdentityUser();
                    user.UserName = email;
                    user.Email = email;

                    // password
                    await userManager.CreateAsync(user, password);
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            app.Run();
        }
    }
}
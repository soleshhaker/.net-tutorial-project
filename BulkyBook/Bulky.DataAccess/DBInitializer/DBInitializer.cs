using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.DBInitializer
{
    public class DBInitializer : IDBInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDBContext _db;

        public DBInitializer(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDBContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public void Initialize()
        {
            //migrate if not applied
            try
            {
                if(_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex){ }

            //create roles if there are none
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();

                //if no roles create admin as well
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@admin.com",
                    Email = "admin@admin.com",
                    Name = "Admin",
                    PhoneNumber = "1234567890",
                    StreetAddress = "Street test",
                    State = "SomeState",
                    PostalCode = "12-345",
                    City = "SomeCity"
                }, "Asd123!").GetAwaiter().GetResult();

                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(x => x.Email == "admin@admin.com");
                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
            }
            return;
        }
    }
}

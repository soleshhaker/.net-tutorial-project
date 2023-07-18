using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bulky.DataAccess.Data;
using System.Data.Entity;
using Microsoft.AspNetCore.Identity;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDBContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty]
        public UserViewModel UserViewModel { get; set; }
        public UserController(ApplicationDBContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(string id)
        {
            var userFromDb = _db.ApplicationUsers.FirstOrDefault(x => x.Id == id);
            var companyId = userFromDb.CompanyId == null ? 0 : userFromDb.CompanyId;
            var roles = _db.Roles.ToList();
            var roleId = _db.UserRoles.FirstOrDefault(x => x.UserId == userFromDb.Id).RoleId;
            UserViewModel = new()
            {
                RoleList = _db.Roles.Select(x => x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                }),
                CompanyList = _db.Companies.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                ApplicationUser = userFromDb,
                CompanyId = companyId,
            };

            return View(UserViewModel);
        }
        [HttpPost]
        public IActionResult Details()
        {
            var RoleId = _db.UserRoles.FirstOrDefault(x => x.UserId == UserViewModel.ApplicationUser.Id).RoleId;
            var oldRole = _db.Roles.FirstOrDefault(x => x.Id == RoleId).Name;

            var applicationUserFromDb = _db.ApplicationUsers.FirstOrDefault(x => x.Id == UserViewModel.ApplicationUser.Id);

            if (UserViewModel.ApplicationUser.Role != oldRole)
            {
                if(UserViewModel.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUserFromDb.CompanyId = UserViewModel.ApplicationUser.CompanyId;
                }
                if(oldRole == SD.Role_Company)
                {
                    applicationUserFromDb.CompanyId = null;
                }
                _userManager.RemoveFromRoleAsync(applicationUserFromDb, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUserFromDb, UserViewModel.ApplicationUser.Role).GetAwaiter().GetResult();
            }

            applicationUserFromDb.Name = UserViewModel.ApplicationUser.Name;
            _db.SaveChanges();

            return RedirectToAction("Index");
        }
        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            IEnumerable<ApplicationUser> objUserList = _db.ApplicationUsers.Include(x => x.Company).ToList();
            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();
            foreach (var user in objUserList)
            {
                var roleId = userRoles.FirstOrDefault(x => x.UserId == user.Id).RoleId;
                user.Role = _db.Roles.FirstOrDefault(x => x.Id == roleId).Name;

                if (user.Company == null)
                {
                    user.Company = new() { Name = "" };
                }
            }

            return Json(new { data = objUserList });
        }
        [HttpPost]
        public IActionResult LockUnlock([FromBody] string? id)
        {
            var objFromDb = _db.ApplicationUsers.FirstOrDefault(x => x.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });

            }
            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                //is locked, unlock
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _db.SaveChanges();
            return Json(new { success = true, message = "Operation Successful" });
        }
        #endregion
    }
}

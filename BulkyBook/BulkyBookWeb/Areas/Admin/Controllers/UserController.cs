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
    [Route("[area]/[controller]")]
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        [BindProperty]
        public UserViewModel UserViewModel { get; set; }
        public UserController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("Index")]
        public IActionResult Index() 
        {
            return View();
        }

        [HttpGet]
        [Route("Details")]
        public IActionResult Details(string id)
        {
            var userFromDb = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == id, includeProperties:"Company");
            if(userFromDb == null)
            {
                return NotFound();
            }
            var roles = _roleManager.Roles.ToList();
            UserViewModel = new()
            {
                RoleList = roles.Select(x => x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                }),
                CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                ApplicationUser = userFromDb,
            };
            UserViewModel.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == id)).GetAwaiter().GetResult().FirstOrDefault();
            return View(UserViewModel);
        }

        [HttpPost]
        [Route("Details_POST")]
        public IActionResult Details_POST()
        {
            var oldRole = _userManager
                .GetRolesAsync(_unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == UserViewModel.ApplicationUser.Id)).GetAwaiter().GetResult().FirstOrDefault();

            var applicationUserFromDb = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == UserViewModel.ApplicationUser.Id);


            if (UserViewModel.ApplicationUser.Role != oldRole)
            {
                if (UserViewModel.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUserFromDb.CompanyId = UserViewModel.ApplicationUser.CompanyId;
                }
                if (oldRole == SD.Role_Company)
                {
                    applicationUserFromDb.CompanyId = null;
                }
                _unitOfWork.ApplicationUser.UpdateRoles(applicationUserFromDb, UserViewModel.ApplicationUser.Role, oldRole);
                applicationUserFromDb.Role = UserViewModel.ApplicationUser.Role;
            }
            else
            {
                if(oldRole==SD.Role_Company && applicationUserFromDb.CompanyId != UserViewModel.ApplicationUser.CompanyId)
                {
                    applicationUserFromDb.CompanyId = UserViewModel.ApplicationUser.CompanyId;
                }
            }
            applicationUserFromDb.Name = UserViewModel.ApplicationUser.Name;
            _unitOfWork.ApplicationUser.Update(applicationUserFromDb);
            _unitOfWork.Save();

            return RedirectToAction("Index");
        }
        #region API CALLS

        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            IEnumerable<ApplicationUser> objUserList = _unitOfWork.ApplicationUser.GetAll(
            includeProperties: "Company"
            ).ToList();

            foreach (var user in objUserList)
            {
                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

                if (user.Company == null)
                {
                    user.Company = new() { Name = "" };
                }
            }

            return Json(new { data = objUserList });
        }

        [HttpPost("LockUnlock")]
        public IActionResult LockUnlock([FromBody] string? id)
        {
            var objFromDb = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == id);
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
            _unitOfWork.ApplicationUser.Update(objFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Operation Successful" });
        }
        #endregion
    }
}

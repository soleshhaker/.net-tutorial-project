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
using Serilog;
using Newtonsoft.Json;

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
            try
            {
                // Log that the Index method has been accessed
                Log.Information("User {UserName} accessed the UserController Index method at {Timestamp}", User.Identity.Name, DateTime.Now);

                return View();
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while processing the UserController Index method for User {UserName} at {Timestamp}", User.Identity.Name, DateTime.Now);

                TempData["error"] = "An error occurred while processing the UserController Index method. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("Details")]
        public IActionResult Details(string id)
        {
            try
            {
                // Log that the Details method has been accessed
                Log.Information("User {UserName} accessed the UserController Details method at {Timestamp}", User.Identity.Name, DateTime.Now);

                var userFromDb = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == id, includeProperties: "Company");
                if (userFromDb == null)
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
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while processing the UserController Details method for User {UserName} at {Timestamp}", User.Identity.Name, DateTime.Now);

                TempData["error"] = "An error occurred while processing the UserController Details method. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Route("Details_POST")]
        public IActionResult Details_POST()
        {
            ApplicationUser applicationUserFromDb = null;
            try
            {
                var oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == UserViewModel.ApplicationUser.Id)).GetAwaiter().GetResult().FirstOrDefault();

                applicationUserFromDb = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == UserViewModel.ApplicationUser.Id);

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
                    if (oldRole == SD.Role_Company && applicationUserFromDb.CompanyId != UserViewModel.ApplicationUser.CompanyId)
                    {
                        applicationUserFromDb.CompanyId = UserViewModel.ApplicationUser.CompanyId;
                    }
                }
                applicationUserFromDb.Name = UserViewModel.ApplicationUser.Name;
                _unitOfWork.ApplicationUser.Update(applicationUserFromDb);
                _unitOfWork.Save();

                // Log successful update
                Log.Information("User {UserId} updated successfully at {Timestamp}. Details: {UserDetails}",
                    User.Identity.Name, DateTime.Now, GetUserDetails(applicationUserFromDb));

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while updating user {UserId} at {Timestamp}. Details: {UserDetails}",
                    User.Identity.Name, DateTime.Now, GetUserDetails(applicationUserFromDb));

                TempData["error"] = "An error occurred while updating user. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        private string GetUserDetails(ApplicationUser applicationUser)
        {
            var userDetails = new
            {
                Id = applicationUser.Id,
                UserName = applicationUser.UserName,
                Role = applicationUser.Role,
                CompanyId = applicationUser.CompanyId,
                Name = applicationUser.Name
            };

            // Log the user details
            Log.Information("User Details: {@UserDetails}", userDetails);

            return JsonConvert.SerializeObject(userDetails, Formatting.Indented);
        }

        #region API CALLS

        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            try
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

                // Log the number of users fetched
                Log.Information("Fetched {UserCount} users from the database", objUserList.Count());

                return Json(new { data = objUserList });
            }
            catch (Exception ex)
            {
                // Log the exception
                Log.Error(ex, "Error occurred while fetching users from the database");
                return BadRequest();
            }
        }

        [HttpPost("LockUnlock")]
        public IActionResult LockUnlock([FromBody] string? id)
        {
            try
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

                // Log the lock/unlock operation
                string lockUnlockOperation = objFromDb.LockoutEnd > DateTime.Now ? "Unlocked" : "Locked";
                Log.Information("{LockUnlockOperation} user with ID {UserId}", lockUnlockOperation, id);

                return Json(new { success = true, message = "Operation Successful" });
            }
            catch (Exception ex)
            {
                // Log the exception
                Log.Error(ex, "Error occurred while Locking/Unlocking user with ID {UserId}", id);
                return BadRequest();
            }
        }
        #endregion
    }
}

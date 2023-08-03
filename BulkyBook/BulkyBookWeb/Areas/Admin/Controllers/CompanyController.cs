using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    [Route("[area]/[controller]")]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CompanyController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            try
            {
                // Log that the admin accessed the company index page
                Log.Information("Admin accessed the company index page at {Timestamp}", DateTime.Now);

                IEnumerable<Company> objCompanyList = _unitOfWork.Company.GetAll();

                // Log the number of companies returned
                Log.Information("Returned {CompanyCount} company(s) at {Timestamp}", objCompanyList.Count(), DateTime.Now);

                return View(objCompanyList);
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while processing the company index page at {Timestamp}", DateTime.Now);

                TempData["error"] = "An error occurred while processing the company index page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("Upsert/{id?}")]
        public IActionResult Upsert(int? id)
        {
            try
            {
                if (id == null || id == 0)
                {
                    // Log that the admin accessed the company creation page
                    Log.Information("Admin accessed the company creation page at {Timestamp}", DateTime.Now);

                    // create
                    var company = new Company();
                    return View(company);
                }
                else
                {
                    // Log that the admin accessed the company update page
                    Log.Information("Admin accessed the company update page for company ID {CompanyId} at {Timestamp}", id, DateTime.Now);

                    // update
                    var CompanyFromDb = _unitOfWork.Company.GetFirstOrDefault(x => x.Id == id);
                    return View(CompanyFromDb);
                }
            }
            catch (Exception ex)
            {
                // Log the error along with the company ID
                Log.Error(ex, "An error occurred while accessing the company page for company ID {CompanyId} at {Timestamp}", id, DateTime.Now);

                TempData["error"] = "An error occurred while accessing the company page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("Upsert")]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj, IFormFile? file)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (obj.Id == 0)
                    {
                        _unitOfWork.Company.Add(obj);
                    }
                    else
                    {
                        _unitOfWork.Company.Update(obj);
                    }
                    _unitOfWork.Save();

                    // Log the company creation or update
                    Log.Information("Company with ID {CompanyId} was {Action} at {Timestamp}", obj.Id, (obj.Id == 0 ? "created" : "updated"), DateTime.Now);

                    TempData["success"] = "Company created/updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return View(obj);
                }
            }
            catch (Exception ex)
            {
                // Log the error along with the company ID
                Log.Error(ex, "An error occurred while {Action} the company with ID {CompanyId} at {Timestamp}", (obj.Id == 0 ? "creating" : "updating"), obj.Id, DateTime.Now);

                TempData["error"] = "An error occurred while creating/updating the company. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region API CALLS

        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            try
            {
                IEnumerable<Company> objProductList = _unitOfWork.Company.GetAll();

                // Log the number of companies returned in the API call
                Log.Information("Returned {CompanyCount} company(s) in the API call at {Timestamp}", objProductList.Count(), DateTime.Now);

                return Json(new { data = objProductList });
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred in the company API call at {Timestamp}", DateTime.Now);

                return Json(new { error = "An error occurred while fetching companies. Please try again later." });
            }
        }
        [HttpDelete("Delete")]
        public IActionResult Delete(int? id)
        {
            try
            {
                var companyToBeDeleted = _unitOfWork.Company.GetFirstOrDefault(x => x.Id == id);
                if (companyToBeDeleted == null)
                {
                    // Log the error when attempting to delete a company that doesn't exist
                    Log.Error("Attempt to delete a company with ID {CompanyId} failed. Company not found at {Timestamp}", id, DateTime.Now);

                    return Json(new { success = false, message = "Error while deleting. Company not found." });
                }

                _unitOfWork.Company.Remove(companyToBeDeleted);
                _unitOfWork.Save();

                // Log the successful deletion of the company
                Log.Information("Company with ID {CompanyId} was deleted successfully at {Timestamp}", id, DateTime.Now);

                return Json(new { success = true, message = "Delete successful" });
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during deletion
                Log.Error(ex, "An error occurred while deleting the company with ID {CompanyId} at {Timestamp}", id, DateTime.Now);

                return Json(new { success = false, message = "An error occurred while deleting the company. Please try again later." });
            }
        }
        #endregion
    }
}

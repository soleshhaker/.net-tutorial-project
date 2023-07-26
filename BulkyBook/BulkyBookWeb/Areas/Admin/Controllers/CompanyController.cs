using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
            IEnumerable<Company> objCompanyList = _unitOfWork.Company.GetAll();
            return View(objCompanyList);
        }

        [HttpGet("Upsert/{id?}")]
        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                //create
                var company = new Company();
                return View(company);
            }
            else
            {
                //update
                var CompanyFromDb = _unitOfWork.Company.GetFirstOrDefault(x => x.Id == id);
                return View(CompanyFromDb);
            }
        }

        //POST
        [HttpPost("Upsert")]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj, IFormFile? file)
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
                TempData["success"] = "Company created succesfully";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return View(obj);
            }
        }
        #region API CALLS

        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            IEnumerable<Company> objProductList = _unitOfWork.Company.GetAll();
            return Json(new { data = objProductList });
        }
        [HttpDelete("Delete")]
        public IActionResult Delete(int? id)
        {
            var companyToBeDeleted = _unitOfWork.Company.GetFirstOrDefault(x => x.Id == id);
            if (companyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.Company.Remove(companyToBeDeleted);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}

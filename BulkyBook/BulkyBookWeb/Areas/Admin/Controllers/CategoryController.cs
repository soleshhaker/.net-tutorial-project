using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Bulky.Utility;
using Serilog;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    [Route("[area]/[controller]")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("Index")]
        public IActionResult Index()
        {
            try
            {
                // Log that the admin accessed the category index page
                Log.Information("Admin accessed the category index page at {Timestamp}", DateTime.Now);

                IEnumerable<Category> objCategoryList = _unitOfWork.Category.GetAll();

                // Log the number of categories returned
                Log.Information("Returned {CategoryCount} category(s) at {Timestamp}", objCategoryList.Count(), DateTime.Now);

                return View(objCategoryList);
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while processing the category index page at {Timestamp}", DateTime.Now);

                TempData["error"] = "An error occurred while processing the category index page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        //GET
        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            try
            {
                // Log that the admin accessed the category creation page
                Log.Information("Admin accessed the category creation page at {Timestamp}", DateTime.Now);

                return View();
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while processing the category creation page at {Timestamp}", DateTime.Now);

                TempData["error"] = "An error occurred while processing the category creation page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        //POST
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            try
            {
                if (obj.Name == obj.DisplayOrder.ToString())
                {
                    ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name.");
                }
                if (!ModelState.IsValid) return View(obj);

                _unitOfWork.Category.Add(obj);
                _unitOfWork.Save();

                // Log the category creation
                Log.Information("New category created with ID {CategoryId} at {Timestamp}", obj.Id, DateTime.Now);

                TempData["success"] = "Category created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while creating a new category at {Timestamp}", DateTime.Now);

                TempData["error"] = "An error occurred while creating the category. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // EDIT
        [HttpGet]
        [Route("Edit")]
        public IActionResult Edit(int? id)
        {
            try
            {
                if (id == null || id == 0) return NotFound();

                var categoryFromDb = _unitOfWork.Category.GetFirstOrDefault(c => c.Id == id);
                if (categoryFromDb == null) return NotFound();

                // Log that the admin accessed the category edit page
                Log.Information("Admin accessed the category edit page for category ID {CategoryId} at {Timestamp}", id, DateTime.Now);

                return View(categoryFromDb);
            }
            catch (Exception ex)
            {
                // Log the error along with the category ID
                Log.Error(ex, "An error occurred while accessing the category edit page for category ID {CategoryId} at {Timestamp}", id, DateTime.Now);

                TempData["error"] = "An error occurred while accessing the category edit page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }


        // EDIT
        [HttpPost]
        [Route("Edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            try
            {
                if (obj.Name == obj.DisplayOrder.ToString())
                {
                    ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name.");
                }
                if (!ModelState.IsValid) return View(obj);

                _unitOfWork.Category.Update(obj);
                _unitOfWork.Save();

                // Log the category edit
                Log.Information("Category with ID {CategoryId} edited at {Timestamp}", obj.Id, DateTime.Now);

                TempData["success"] = "Category edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while editing the category with ID {CategoryId} at {Timestamp}", obj.Id, DateTime.Now);

                TempData["error"] = "An error occurred while editing the category. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // DELETE
        [HttpGet]
        [Route("Delete")]
        public IActionResult Delete(int? id)
        {
            try
            {
                if (id == null || id == 0) return NotFound();

                var categoryFromDb = _unitOfWork.Category.GetFirstOrDefault(c => c.Id == id);
                if (categoryFromDb == null) return NotFound();

                // Log that the admin accessed the category delete page
                Log.Information("Admin accessed the category delete page for category ID {CategoryId} at {Timestamp}", id, DateTime.Now);

                return View(categoryFromDb);
            }
            catch (Exception ex)
            {
                // Log the error along with the category ID
                Log.Error(ex, "An error occurred while accessing the category delete page for category ID {CategoryId} at {Timestamp}", id, DateTime.Now);

                TempData["error"] = "An error occurred while accessing the category delete page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [Route("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            try
            {
                var categoryFromDb = _unitOfWork.Category.GetFirstOrDefault(c => c.Id == id);
                if (categoryFromDb == null) return NotFound();

                _unitOfWork.Category.Remove(categoryFromDb);
                _unitOfWork.Save();

                // Log the category deletion
                Log.Information("Category with ID {CategoryId} deleted at {Timestamp}", id, DateTime.Now);

                TempData["success"] = "Category deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error along with the category ID
                Log.Error(ex, "An error occurred while deleting the category with ID {CategoryId} at {Timestamp}", id, DateTime.Now);

                TempData["error"] = "An error occurred while deleting the category. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

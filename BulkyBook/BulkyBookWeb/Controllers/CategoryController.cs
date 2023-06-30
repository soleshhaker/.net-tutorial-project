using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;

namespace BulkyBookWeb.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Category> objCategoryList = _unitOfWork.Category.GetAll();
            return View(objCategoryList);
        }
        
        //GET
        public IActionResult Create()
        {
            return View();  
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name.");
            }
            if (!ModelState.IsValid) return View(obj);

            _unitOfWork.Category.Add(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category created succesfully";
            return RedirectToAction(nameof(Index));
        }
        //EDIT
        public IActionResult Edit(int? id)
        {
            if(id==null || id == 0) return NotFound();
            
            var categoryFromDb = _unitOfWork.Category.GetFirstOrDefault(c => c.Id == id);
            if (categoryFromDb == null) return NotFound();

            return View(categoryFromDb);
        }

        //EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name.");
            }
            if (!ModelState.IsValid) return View(obj);

            _unitOfWork.Category.Update(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category edited succesfully";
            return RedirectToAction(nameof(Index));
        }
        //DELETE
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var categoryFromDb = _unitOfWork.Category.GetFirstOrDefault(c => c.Id == id);
            if (categoryFromDb == null) return NotFound();

            return View(categoryFromDb);
        }
        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var categoryFromDb = _unitOfWork.Category.GetFirstOrDefault(c => c.Id == id);
            if (categoryFromDb == null) return NotFound();

            _unitOfWork.Category.Remove(categoryFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Category deleted succesfully";
            return RedirectToAction(nameof(Index));
        }
    }
}

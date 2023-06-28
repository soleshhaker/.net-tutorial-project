using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BulkyBookRazor_Temp.Data;
using BulkyBookRazor_Temp.Models;

namespace BulkyBookRazor_Temp.Pages.Categories
{
    public class EditModel : PageModel
    {
        private readonly BulkyBookRazor_Temp.Data.ApplicationDBContext _context;

        public EditModel(BulkyBookRazor_Temp.Data.ApplicationDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Category Category { get; set; } = default!;

        public void OnGet(int? id)
        {
            if (id != null || id != 0) Category = _context.Categories.First(c => c.Id == id);
        }

        public IActionResult OnPost()
        {
            if (Category.Name == Category.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name.");
            }
            if (!ModelState.IsValid) return Page();

            _context.Categories.Update(Category);
            _context.SaveChanges();
            TempData["success"] = "Category updated succesfully";
            return RedirectToPage("Index");
        }
    }
}

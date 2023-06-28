using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BulkyBookRazor_Temp.Data;
using BulkyBookRazor_Temp.Models;

namespace BulkyBookRazor_Temp.Pages.Categories
{
    public class DeleteModel : PageModel
    {
        private readonly BulkyBookRazor_Temp.Data.ApplicationDBContext _context;

        public DeleteModel(BulkyBookRazor_Temp.Data.ApplicationDBContext context)
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
            Category? obj = _context.Categories.Find(Category.Id);
            if(obj == null)   return NotFound();

            _context.Categories.Remove(obj);
            _context.SaveChanges();
            TempData["success"] = "Category deleted succesfully";
            return RedirectToPage("Index");
        }
    }
}

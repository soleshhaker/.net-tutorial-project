using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using BulkyBookRazor_Temp.Data;
using BulkyBookRazor_Temp.Models;

namespace BulkyBookRazor_Temp.Pages.Categories
{
    public class CreateModel : PageModel
    {
        private readonly BulkyBookRazor_Temp.Data.ApplicationDBContext _context;

        public CreateModel(BulkyBookRazor_Temp.Data.ApplicationDBContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Category Category { get; set; } = default!;
        

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
          if (!ModelState.IsValid || _context.Categories == null || Category == null)
            {
                return Page();
            }

            _context.Categories.Add(Category);
            await _context.SaveChangesAsync();
            TempData["success"] = "Category created succesfully";
            return RedirectToPage("Index");
        }
    }
}

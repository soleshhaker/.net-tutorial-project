﻿using System;
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
    public class DetailsModel : PageModel
    {
        private readonly BulkyBookRazor_Temp.Data.ApplicationDBContext _context;

        public DetailsModel(BulkyBookRazor_Temp.Data.ApplicationDBContext context)
        {
            _context = context;
        }

      public Category Category { get; set; } = default!; 

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            else 
            {
                Category = category;
            }
            return Page();
        }
    }
}

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
    public class IndexModel : PageModel
    {
        private readonly BulkyBookRazor_Temp.Data.ApplicationDBContext _context;

        public IndexModel(BulkyBookRazor_Temp.Data.ApplicationDBContext context)
        {
            _context = context;
        }

        public IList<Category> Category { get;set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.Categories != null)
            {
                Category = await _context.Categories.ToListAsync();
            }
        }
    }
}

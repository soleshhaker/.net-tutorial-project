using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.DTO
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Author { get; set; }
        public double ListPrice { get; set; }
        public double Price100 { get; set; }
        public ProductImage ProductImage { get; set; }
    }
}

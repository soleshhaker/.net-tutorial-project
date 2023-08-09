using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.DTO
{
    public class DetailedProductDto
    {
        public int Id { get; set; }
        public string Author { get; set; }
        public double ListPrice { get; set; }
        public double Price100 { get; set; }
        public List<ProductImage> ProductImages { get; set; }
        public string ISBN { get; set; }
        public double Price { get; set; }
        public double Price50 { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}

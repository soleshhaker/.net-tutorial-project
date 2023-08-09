using Bulky.Models.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.ViewModels
{
    public class OrderViewModel
    {
        public OrderHeaderDTO OrderHeaderDTO { get; set; }
        public IEnumerable<OrderDetail> OrderDetail { get; set; }
    }
}

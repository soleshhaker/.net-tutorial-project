using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private ApplicationDBContext _db;
        public ProductRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Product obj)
        {
            var objFromDB = _db.Products.FirstOrDefault(x => x.Id == obj.Id);
            if (objFromDB != null)
            {
                objFromDB.Title = obj.Title;
                objFromDB.ISBN  = obj.ISBN;
                objFromDB.Price = obj.Price;
                objFromDB.Price50 = obj.Price50;
                objFromDB.ListPrice = obj.ListPrice;
                objFromDB.Price100 = obj.Price100;
                objFromDB.Description = obj.Description;
                objFromDB.CategoryId = obj.CategoryId;
                objFromDB.Author = obj.Author;
                if(obj.ImageURL != null)
                {
                    objFromDB.ImageURL = obj.ImageURL;
                }
            }
        }
    }
}

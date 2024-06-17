using BookCart.DataAccess.Data;
using BookCart.DataAccess.Repository.IRepository;
using BookCart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookCart.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext dbContext;

        public ProductRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            this.dbContext = dbContext;
        }

        public void Update(Product product)
        {
            var objProduct = dbContext.Products.FirstOrDefault(u => u.Id == product.Id);
            if (objProduct != null)
            {
                objProduct.Title = product.Title;
                objProduct.Description = product.Description;
                objProduct.CategoryId = product.CategoryId;
                objProduct.ISBN = product.ISBN;
                objProduct.Price = product.Price;
                objProduct.Author = product.Author;
                objProduct.ListPrice = product.ListPrice;
                objProduct.Price50 = product.Price50;
                objProduct.Price100 = product.Price100;
                if(product.ImageUrl != null)
                {
                    objProduct.ImageUrl = product.ImageUrl;
                }
            }
        }
    }
}

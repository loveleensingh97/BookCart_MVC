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
    public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
    {
        private readonly ApplicationDbContext dbContext;

        public ShoppingCartRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            this.dbContext = dbContext;
        }

        public void Update(ShoppingCart cart)
        {
            dbContext.ShoppingCarts.Update(cart);
        }
    }
}

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
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
    {
        private readonly ApplicationDbContext dbContext;

        public OrderDetailRepository(ApplicationDbContext dbContext): base(dbContext)
        {
            this.dbContext = dbContext;
        }

        public void Update(OrderDetail orderDetail)
        {
            dbContext.OrderDetails.Update(orderDetail);
        }
    }
}

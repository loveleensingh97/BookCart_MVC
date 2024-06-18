using BookCart.DataAccess.Data;
using BookCart.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookCart.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _dbcontext;
        public ICategoryRepository CategoryRepository { get; private set; }
        public IProductRepository ProductRepository { get; private set; }
        public ICompanyRepository CompanyRepository { get; private set; }
        public IShoppingCartRepository ShoppingCartRepository { get; private set; }
        public IApplicationUserRepository ApplicationUserRepository { get; private set; }

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbcontext = dbContext;
            CategoryRepository = new CategoryRepository(_dbcontext);
            ProductRepository = new ProductRepository(_dbcontext);
            CompanyRepository = new CompanyRepository(_dbcontext);
            ShoppingCartRepository = new ShoppingCartRepository(_dbcontext);
            ApplicationUserRepository = new ApplicationUserRepository(_dbcontext);
        }

        public void Save()
        {
            _dbcontext.SaveChanges();
        }
    }
}

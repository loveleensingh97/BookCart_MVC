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
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly ApplicationDbContext dbContext;

        public CompanyRepository(ApplicationDbContext dbContext): base(dbContext)
        {
            this.dbContext = dbContext;
        }

        public void Update(Company company)
        {
            dbContext.Companies.Update(company);
        }
    }
}

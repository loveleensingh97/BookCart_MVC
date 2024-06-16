using Microsoft.EntityFrameworkCore;
using BookCart.Web_Temp.Models;

namespace BookCart.Web_Temp.Data
{
    public class RazorApplicationDbContext : DbContext
    {
        public RazorApplicationDbContext(DbContextOptions<RazorApplicationDbContext> dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Action",
                    DisplayOrder = 1
                },
                new Category
                {
                    Id = 2,
                    Name = "SciFi",
                    DisplayOrder = 2
                },
                new Category
                {
                    Id = 3,
                    Name = "History",
                    DisplayOrder = 3
                });
        }
    }
}

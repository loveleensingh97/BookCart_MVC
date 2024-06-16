using BookCart.Web_Temp.Data;
using BookCart.Web_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookCart.Web_Temp.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly RazorApplicationDbContext dbContext;
        public List<Category> CategoryList { get; set; }

        public IndexModel(RazorApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public void OnGet()
        {
            CategoryList = dbContext.Categories.ToList();
        }
    }
}

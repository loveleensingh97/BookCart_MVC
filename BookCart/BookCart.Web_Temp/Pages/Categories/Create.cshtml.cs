using BookCart.Web_Temp.Data;
using BookCart.Web_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookCart.Web_Temp.Pages.Categories
{
    public class CreateModel : PageModel
    {
        private readonly RazorApplicationDbContext dbContext;
        [BindProperty]
        public Category Category { get; set; }

        public CreateModel(RazorApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            dbContext.Categories.Add(Category);
            dbContext.SaveChanges();
            TempData["Success"] = "Category Created Successfully";
            return RedirectToPage("Index");
        }
    }
}

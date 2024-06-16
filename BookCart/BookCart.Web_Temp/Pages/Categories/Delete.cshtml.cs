using BookCart.Web_Temp.Data;
using BookCart.Web_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookCart.Web_Temp.Pages.Categories
{
    [BindProperties]
    public class DeleteModel : PageModel
    {
        private readonly RazorApplicationDbContext dbContext;
        public Category Category { get; set; }

        public DeleteModel(RazorApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public void OnGet(int? id)
        {
            if (id != null && id != 0)
            {
                Category = dbContext.Categories.Find(id);
            }
        }

        public IActionResult OnPost()
        {
            Category? obj = dbContext.Categories.Find(Category.Id);
            if (obj == null)
            {
                return NotFound();
            }
            dbContext.Categories.Remove(obj);
            dbContext.SaveChanges();
            TempData["Success"] = "Category Created Successfully";
            return RedirectToAction("Index");
        }
    }
}

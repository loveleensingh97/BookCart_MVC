using BookCart.DataAccess.Repository.IRepository;
using BookCart.Models;
using BookCart.Models.ViewModels;
using BookCart.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookCart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticDetails.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product> productList = _unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList();
            return View(productList);
        }

        public IActionResult Upsert(int? id)
        {
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.CategoryRepository
                .GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });

            //ViewBag.CategoryList = CategoryList;
            ViewData["CategoryList"] = CategoryList;
            ProductViewModel productViewModel = new()
            {
                CategoryList = CategoryList,
                Product = new Product()
            };
            if (id == null || id == 0)
            {
                //create
                return View(productViewModel);
            }
            else
            {
                //update
                productViewModel.Product = _unitOfWork.ProductRepository.Get(u => u.Id == id);
                return View(productViewModel);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductViewModel productViewModel, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    if (!string.IsNullOrEmpty(productViewModel.Product.ImageUrl))
                    {
                        //delete the old image
                        var oldImagePath = Path.Combine(wwwRootPath, productViewModel.Product.ImageUrl.TrimStart('/'));

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    productViewModel.Product.ImageUrl = @"\images\product\" + fileName;
                }

                if (productViewModel.Product.Id == 0)
                {
                    _unitOfWork.ProductRepository.Add(productViewModel.Product);
                }
                else
                {
                    _unitOfWork.ProductRepository.Update(productViewModel.Product);
                }

                _unitOfWork.Save();
                TempData["Success"] = "Product Created Successfully";
                return RedirectToAction("Index", "Product");
            }
            else
            {
                productViewModel.CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productViewModel);
            }
        }

        //public IActionResult Edit(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    //Product product = dbContext.Companies.Find(id);
        //    //Product? product = dbContext.Companies.FirstOrDefault(c => c.Id == id);
        //    //Product? product = dbContext.Companies.Where(c => c.Id == id).FirstOrDefault();
        //    Product? product = _unitOfWork.ProductRepository.Get(c => c.Id == id);

        //    if (product == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(product);
        //}

        //[HttpPost]
        //public IActionResult Edit(Product product)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.ProductRepository.Update(product);
        //        _unitOfWork.Save();
        //        TempData["Success"] = "Product Updated Successfully";
        //        return RedirectToAction("Index", "Product");
        //    }
        //    return View();
        //}

        //public IActionResult Delete(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Product? product = _unitOfWork.ProductRepository.Get(c => c.Id == id);

        //    if (product == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(product);
        //}

        //[HttpPost, ActionName("Delete")]
        //public IActionResult DeletePOST(int? id)
        //{
        //    Product? obj = _unitOfWork.ProductRepository.Get(c => c.Id == id);
        //    if (obj == null)
        //    {
        //        return NotFound();
        //    }
        //    _unitOfWork.ProductRepository.Remove(obj);
        //    _unitOfWork.Save();
        //    TempData["Success"] = "Product Deleted Successfully";
        //    return RedirectToAction("Index", "Product");
        //}


        #region API_Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> productList = _unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = productList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.ProductRepository.Get(u => u.Id == id);
            if(productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            //delete the old image
            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('/'));

            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _unitOfWork.ProductRepository.Remove(productToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}

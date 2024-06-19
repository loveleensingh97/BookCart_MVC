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
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Company> CompanyList = _unitOfWork.CompanyRepository.GetAll().ToList();
            return View(CompanyList);
        }

        public IActionResult Upsert(int? id)
        {           
            if (id == null || id == 0)
            {
                //create
                return View(new Company());
            }
            else
            {
                //update
                Company companyObj = _unitOfWork.CompanyRepository.Get(u => u.Id == id);
                return View(companyObj);
            }
        }

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id == 0)
                {
                    _unitOfWork.CompanyRepository.Add(company);
                }
                else
                {
                    _unitOfWork.CompanyRepository.Update(company);
                }

                _unitOfWork.Save();
                TempData["Success"] = "Company Created Successfully";
                return RedirectToAction("Index", "Company");
            }
            else
            {
                return View(company);
            }
        }

        //public IActionResult Edit(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    //Company Company = dbContext.Companys.Find(id);
        //    //Company? Company = dbContext.Companys.FirstOrDefault(c => c.Id == id);
        //    //Company? Company = dbContext.Companys.Where(c => c.Id == id).FirstOrDefault();
        //    Company? Company = _unitOfWork.CompanyRepository.Get(c => c.Id == id);

        //    if (Company == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(Company);
        //}

        //[HttpPost]
        //public IActionResult Edit(Company Company)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.CompanyRepository.Update(Company);
        //        _unitOfWork.Save();
        //        TempData["Success"] = "Company Updated Successfully";
        //        return RedirectToAction("Index", "Company");
        //    }
        //    return View();
        //}

        //public IActionResult Delete(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Company? Company = _unitOfWork.CompanyRepository.Get(c => c.Id == id);

        //    if (Company == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(Company);
        //}

        //[HttpPost, ActionName("Delete")]
        //public IActionResult DeletePOST(int? id)
        //{
        //    Company? obj = _unitOfWork.CompanyRepository.Get(c => c.Id == id);
        //    if (obj == null)
        //    {
        //        return NotFound();
        //    }
        //    _unitOfWork.CompanyRepository.Remove(obj);
        //    _unitOfWork.Save();
        //    TempData["Success"] = "Company Deleted Successfully";
        //    return RedirectToAction("Index", "Company");
        //}


        #region API_Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> CompanyList = _unitOfWork.CompanyRepository.GetAll().ToList();
            return Json(new { data = CompanyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var CompanyToBeDeleted = _unitOfWork.CompanyRepository.Get(u => u.Id == id);
            if(CompanyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.CompanyRepository.Remove(CompanyToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}

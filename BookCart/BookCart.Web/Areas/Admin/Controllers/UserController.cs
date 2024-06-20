using BookCart.DataAccess.Data;
using BookCart.DataAccess.Repository;
using BookCart.DataAccess.Repository.IRepository;
using BookCart.Models;
using BookCart.Models.ViewModels;
using BookCart.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookCart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticDetails.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string userId)
        {
            string roleId = _dbContext.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;

            RoleManagementViewModel RoleVM = new RoleManagementViewModel()
            {
                ApplicationUser = _dbContext.ApplicationUsers.Include(u  => u.Company).FirstOrDefault(u => u.Id == userId),
                RoleList = _dbContext.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                CompanyList = _dbContext.Companies.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
            };

            RoleVM.ApplicationUser.Role = _dbContext.Roles.FirstOrDefault(u => u.Id == roleId).Name;
            return View(RoleVM);
        }

        [HttpPost]
        public IActionResult RoleManagement(RoleManagementViewModel roleManagmentViewModel)
        {

            string roleId = _dbContext.UserRoles.FirstOrDefault(u => u.UserId == roleManagmentViewModel.ApplicationUser.Id).RoleId;
            string oldRole = _dbContext.Roles.FirstOrDefault(u => u.Id == roleId).Name;

            if (!(roleManagmentViewModel.ApplicationUser.Role == oldRole))
            {
                ApplicationUser applicationUser = _dbContext.ApplicationUsers.FirstOrDefault(u => u.Id == roleManagmentViewModel.ApplicationUser.Id);
                //a role was updated
                if (roleManagmentViewModel.ApplicationUser.Role == StaticDetails.Role_Company)
                {
                    applicationUser.CompanyId = roleManagmentViewModel.ApplicationUser.CompanyId;
                }
                if (oldRole == StaticDetails.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                _dbContext.SaveChanges();

                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagmentViewModel.ApplicationUser.Role).GetAwaiter().GetResult();

            }

            return RedirectToAction("Index");
        }


        #region API_Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> userList = _dbContext.ApplicationUsers.Include(u => u.Company).ToList();

            var userRoles = _dbContext.UserRoles.ToList();
            var roles = _dbContext.Roles.ToList();
            
            foreach (var user in userList)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(u => u.Id == roleId).Name;
                //user.Role = userMan.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

                if (user.Company == null)
                {
                    user.Company = new Company()
                    {
                        Name = ""
                    };
                }
            }

            return Json(new { data = userList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {

            var objFromDb = _dbContext.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                //user is currently locked and we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            //_dbContext.ApplicationUsers.Update(objFromDb);
            _dbContext.SaveChanges();
            return Json(new { success = true, message = "Operation Successful" });
        }


        #endregion
    }
}

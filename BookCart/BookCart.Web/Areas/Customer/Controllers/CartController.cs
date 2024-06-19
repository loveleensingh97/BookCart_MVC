using BookCart.DataAccess.Repository.IRepository;
using BookCart.Models;
using BookCart.Models.ViewModels;
using BookCart.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BookCart.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartViewModel ShoppingCartViewModel { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;    
        }

        public IActionResult Index()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;

            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim.Value;

            ShoppingCartViewModel = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach(var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartViewModel);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;

            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim.Value;

            ShoppingCartViewModel = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartViewModel.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId);

            ShoppingCartViewModel.OrderHeader.Name = ShoppingCartViewModel.OrderHeader.ApplicationUser.Name;
            ShoppingCartViewModel.OrderHeader.PhoneNumber = ShoppingCartViewModel.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartViewModel.OrderHeader.StreetAddress = ShoppingCartViewModel.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartViewModel.OrderHeader.City = ShoppingCartViewModel.OrderHeader.ApplicationUser.City;
            ShoppingCartViewModel.OrderHeader.State = ShoppingCartViewModel.OrderHeader.ApplicationUser.State;
            ShoppingCartViewModel.OrderHeader.PostalCode = ShoppingCartViewModel.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartViewModel);
        }

        [HttpPost]
        [ActionName("Summary")]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;

			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			var userId = claim.Value;

            ShoppingCartViewModel.ShoppingCartList = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product");

            ShoppingCartViewModel.OrderHeader.OrderDate = System.DateTime.Now;
            ShoppingCartViewModel.OrderHeader.ApplicationUserId = userId;

			ApplicationUser applicationUser = _unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId);

			foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //it is a regular customer account
                ShoppingCartViewModel.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusPending;
                ShoppingCartViewModel.OrderHeader.OrderStatus = StaticDetails.StatusPending;
            }
            else
            {
                //it is a company user
                ShoppingCartViewModel.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusDelayedPayment;
                ShoppingCartViewModel.OrderHeader.OrderStatus = StaticDetails.StatusApproved;
            }

   //         OrderHeader orderHeader = new()
   //         {
   //             ApplicationUserId = ShoppingCartViewModel.OrderHeader.ApplicationUserId,
   //             OrderDate = ShoppingCartViewModel.OrderHeader.OrderDate,
			//	ShippingDate = ShoppingCartViewModel.OrderHeader.ShippingDate,
			//	OrderTotal = ShoppingCartViewModel.OrderHeader.OrderTotal,
			//	OrderStatus = ShoppingCartViewModel.OrderHeader.OrderStatus,
			//	PaymentStatus = ShoppingCartViewModel.OrderHeader.PaymentStatus,
			//	TrackingNumber = ShoppingCartViewModel.OrderHeader.TrackingNumber,
   //             Carrier = ShoppingCartViewModel.OrderHeader.Carrier,
			//	PaymentDate = ShoppingCartViewModel.OrderHeader.PaymentDate,
			//	PaymentDueDate = ShoppingCartViewModel.OrderHeader.PaymentDueDate,
			//	PaymentIntentId = ShoppingCartViewModel.OrderHeader.PaymentIntentId,
			//	PhoneNumber = ShoppingCartViewModel.OrderHeader.PhoneNumber,
			//	StreetAddress = ShoppingCartViewModel.OrderHeader.StreetAddress,
			//	City = ShoppingCartViewModel.OrderHeader.City,
			//	State = ShoppingCartViewModel.OrderHeader.State,
			//	PostalCode = ShoppingCartViewModel.OrderHeader.PostalCode,
			//	Name = ShoppingCartViewModel.OrderHeader.Name
			//};

            _unitOfWork.OrderHeaderRepository.Add(ShoppingCartViewModel.OrderHeader);
            _unitOfWork.Save();
            foreach(var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartViewModel.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetailRepository.Add(orderDetail);
                _unitOfWork.Save();
            }

            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
				//it is a regular customer account and we need to capture payment
				//stripe logic

				var domain = "https://localhost:7013/";
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartViewModel.OrderHeader.Id}",
					CancelUrl = domain + "customer/cart/index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};

                foreach(var item in ShoppingCartViewModel.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

				var service = new SessionService();
				Session session = service.Create(options);
                _unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(ShoppingCartViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
			}

			return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartViewModel.OrderHeader.Id});
		}

        public IActionResult OrderConfirmation(int id)
        {
			OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != StaticDetails.PaymentStatusDelayedPayment)
            {
                //this is an order by customer

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeaderRepository.UpdateStatus(id, StaticDetails.StatusApproved, StaticDetails.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

			List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCartRepository
				.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

			_unitOfWork.ShoppingCartRepository.RemoveRange(shoppingCarts);
			_unitOfWork.Save();

			return View(id);
        }

		public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCartRepository.Get(u => u.Id == cartId);
            cartFromDb.Count += 1;
            _unitOfWork.ShoppingCartRepository.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCartRepository.Get(u => u.Id == cartId);
            if(cartFromDb.Count <= 0)
            {
                //remove that from cart
                _unitOfWork.ShoppingCartRepository.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCartRepository.Update(cartFromDb);
            }
            
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCartRepository.Get(u => u.Id == cartId);

            //remove that from cart
            _unitOfWork.ShoppingCartRepository.Remove(cartFromDb);

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if(shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if(shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else
                {
                    return shoppingCart.Product.Price100;
                }
            }
        }
    }
}

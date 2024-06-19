using BookCart.DataAccess.Repository.IRepository;
using BookCart.Models;
using BookCart.Models.ViewModels;
using BookCart.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Security.Claims;

namespace BookCart.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderViewModel OrderViewModel { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
		{
			return View();
		}

        public IActionResult Details(int orderId)
        {
            OrderViewModel = new()
            {
                OrderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetailRepository.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };

            return View(OrderViewModel);
        }

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == OrderViewModel.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderViewModel.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderViewModel.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderViewModel.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderViewModel.OrderHeader.City;
            orderHeaderFromDb.State = OrderViewModel.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderViewModel.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderViewModel.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.Carrier = OrderViewModel.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";


            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeaderRepository.UpdateStatus(OrderViewModel.OrderHeader.Id, StaticDetails.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult ShipOrder()
        {

            var orderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == OrderViewModel.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderViewModel.OrderHeader.Carrier;
            orderHeader.OrderStatus = StaticDetails.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if (orderHeader.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOfWork.OrderHeaderRepository.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
        public IActionResult CancelOrder()
        {

            var orderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == OrderViewModel.OrderHeader.Id);

            if (orderHeader.PaymentStatus == StaticDetails.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeader.Id, StaticDetails.StatusCancelled, StaticDetails.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeader.Id, StaticDetails.StatusCancelled, StaticDetails.StatusCancelled);
            }
            _unitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });

        }

        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            OrderViewModel.OrderHeader = _unitOfWork.OrderHeaderRepository
                .Get(u => u.Id == OrderViewModel.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderViewModel.OrderDetails = _unitOfWork.OrderDetailRepository
                .GetAll(u => u.OrderHeaderId == OrderViewModel.OrderHeader.Id, includeProperties: "Product");

            //stripe logic
            var domain = "https://localhost:7013/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderViewModel.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderViewModel.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderViewModel.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
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
            _unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(OrderViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {

            OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment)
            {
                //this is an order by company

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeaderRepository.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, StaticDetails.PaymentStatusApproved);
                    _unitOfWork.Save();
                }


            }


            return View(orderHeaderId);
        }

        #region API_Calls
        [HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orderHeaderList;

            if (User.IsInRole(StaticDetails.Role_Admin) || User.IsInRole(StaticDetails.Role_Employee))
            {
                orderHeaderList = _unitOfWork.OrderHeaderRepository.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                orderHeaderList = _unitOfWork.OrderHeaderRepository
                    .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    orderHeaderList = orderHeaderList.Where(u => u.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaderList = orderHeaderList.Where(u => u.OrderStatus == StaticDetails.StatusInProcess);
                    break;
                case "completed":
                    orderHeaderList = orderHeaderList.Where(u => u.OrderStatus == StaticDetails.StatusShipped);
                    break;
                case "approved":
                    orderHeaderList = orderHeaderList.Where(u => u.OrderStatus == StaticDetails.StatusApproved);
                    break;
                default:
                    break;

            }

            return Json(new { data = orderHeaderList });
		}

		#endregion
	}
}

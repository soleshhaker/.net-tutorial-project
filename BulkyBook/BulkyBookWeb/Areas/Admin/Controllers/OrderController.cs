using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("admin")]
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
                OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(x => x.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderViewModel);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderViewModel.OrderHeader.Id);
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
                orderHeaderFromDb.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderViewModel.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderViewModel.OrderHeader.Id);
            orderHeaderFromDb.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
            orderHeaderFromDb.Carrier = OrderViewModel.OrderHeader.Carrier;
            orderHeaderFromDb.OrderStatus = SD.StatusShipped;
            orderHeaderFromDb.ShippingDate = DateTime.Now;
            if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeaderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Shipped Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderViewModel.OrderHeader.Id);
            if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved)
            {
                //refund
                var options = new RefundCreateOptions()
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDb.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _unitOfWork.Save();
            TempData["Success"] = "Order Canceled Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [ActionName("Details")]
        public IActionResult Details_PAY_NOW()
        {
            OrderViewModel.OrderHeader = _unitOfWork.OrderHeader
                .GetFirstOrDefault(x => x.Id == OrderViewModel.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderViewModel.OrderDetail = _unitOfWork.OrderDetail
                .GetAll(x => x.OrderHeaderId == OrderViewModel.OrderHeader.Id, includeProperties: "Product");

            //stripe
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {

                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderViewModel.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderViewModel.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderViewModel.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions()
                    {
                        UnitAmount = (long)(item.Price * 100), // 20.50 = 2050
                        Currency = "pln",
                        ProductData = new SessionLineItemPriceDataProductDataOptions()
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
            _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
        public IActionResult PaymentConfirmation(int orderHeaderid)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderHeaderid);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //order by company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderid, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderid, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            return View(orderHeaderid);
        }
        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(x => x.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(x => x.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(x => x.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(x => x.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(x => x.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }

            return Json(new { data = objOrderHeaders });
        }
        #endregion
    }
}

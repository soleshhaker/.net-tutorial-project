﻿using AutoMapper;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.DTO;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize]
    [Route("[area]/[controller]")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        [BindProperty]
        public OrderViewModel OrderViewModel { get; set; }
        public OrderController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            try
            {
                // Log the user accessing the order index page
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                Log.Information("User {UserId} accessed the order index page at {Timestamp}", userId, DateTime.Now);

                return View();
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during the index page access
                Log.Error(ex, "An error occurred while accessing the order index page at {Timestamp}. Error message: {ErrorMessage}", DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while accessing the order index page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("Details")]
        public IActionResult Details(int orderId)
        {
            try
            {
                OrderViewModel = new OrderViewModel
                {
                    OrderHeaderDTO = _mapper.Map<OrderHeaderDTO>(_unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderId, includeProperties: "ApplicationUser")),
                    OrderDetail = _unitOfWork.OrderDetail.GetAll(x => x.OrderHeaderId == orderId, includeProperties: "Product")
                };

                // Log the user accessing the order details page with the specific order ID
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                Log.Information("User {UserId} accessed the order details page for Order ID {OrderId} at {Timestamp}", userId, orderId, DateTime.Now);

                return View(OrderViewModel);
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during the order details page access
                Log.Error(ex, "An error occurred while accessing the order details page for Order ID {OrderId} at {Timestamp}. Error message: {ErrorMessage}", orderId, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while accessing the order details page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [Route("UpdateOrderDetail")]
        public IActionResult UpdateOrderDetail()
        {
            try
            {
                var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderViewModel.OrderHeaderDTO.Id);
                orderHeaderFromDb = _mapper.Map(OrderViewModel.OrderHeaderDTO, orderHeaderFromDb);
                _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
                _unitOfWork.Save();

                // Log the successful update of order details
                Log.Information("Order details updated successfully for Order ID {OrderId} at {Timestamp}", OrderViewModel.OrderHeaderDTO.Id, DateTime.Now);

                TempData["Success"] = "Order Details Updated Successfully.";
                return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during order details update
                Log.Error(ex, "An error occurred while updating order details for Order ID {OrderId} at {Timestamp}. Error message: {ErrorMessage}", OrderViewModel.OrderHeaderDTO.Id, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while updating order details. Please try again later.";
                return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeaderDTO.Id });
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [Route("StartProcessing")]
        public IActionResult StartProcessing()
        {
            try
            {
                _unitOfWork.OrderHeader.UpdateStatus(OrderViewModel.OrderHeaderDTO.Id, SD.StatusInProcess);
                _unitOfWork.Save();

                // Log the successful start of order processing
                Log.Information("Order processing started successfully for Order ID {OrderId} at {Timestamp}", OrderViewModel.OrderHeaderDTO.Id, DateTime.Now);

                TempData["Success"] = "Order Details Updated Successfully.";
                return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeaderDTO.Id });
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during order processing start
                Log.Error(ex, "An error occurred while starting order processing for Order ID {OrderId} at {Timestamp}. Error message: {ErrorMessage}", OrderViewModel.OrderHeaderDTO.Id, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while starting order processing. Please try again later.";
                return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeaderDTO.Id });
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [Route("ShipOrder")]
        public IActionResult ShipOrder()
        {
            try
            {
                var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderViewModel.OrderHeaderDTO.Id);
                orderHeaderFromDb = _mapper.Map(OrderViewModel.OrderHeaderDTO, orderHeaderFromDb);

                orderHeaderFromDb.OrderStatus = SD.StatusShipped;
                orderHeaderFromDb.ShippingDate = DateTime.Now;
                if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
                {
                    orderHeaderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
                }
                _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
                _unitOfWork.Save();

                // Log the successful shipment of the order
                Log.Information("Order {OrderId} shipped successfully at {Timestamp}", OrderViewModel.OrderHeaderDTO.Id, DateTime.Now);

                TempData["Success"] = "Order Shipped Successfully.";

                return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeaderDTO.Id });
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during order shipment
                Log.Error(ex, "An error occurred while shipping order {OrderId} at {Timestamp}. Error message: {ErrorMessage}", OrderViewModel.OrderHeaderDTO.Id, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while shipping the order. Please try again later.";
                return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeaderDTO.Id });
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [Route("CancelOrder")]
        public IActionResult CancelOrder()
        {
            try
            {
                var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderViewModel.OrderHeaderDTO.Id);
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

                // Log the successful cancellation of the order
                Log.Information("Order {OrderId} canceled successfully at {Timestamp}", OrderViewModel.OrderHeaderDTO.Id, DateTime.Now);

                TempData["Success"] = "Order Canceled Successfully.";

                return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeaderDTO.Id });
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during order cancellation
                Log.Error(ex, "An error occurred while canceling order {OrderId} at {Timestamp}. Error message: {ErrorMessage}", OrderViewModel.OrderHeaderDTO.Id, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while canceling the order. Please try again later.";
                return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeaderDTO.Id });
            }
        }

        [HttpPost]
        [ActionName("Details_PAY_NOW")]
        [Route("Details_PAY_NOW")]
        public IActionResult Details_PAY_NOW()
        {
            try
            {
                OrderViewModel.OrderHeaderDTO = _mapper.Map<OrderHeaderDTO>(
                    _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderViewModel.OrderHeaderDTO.Id, includeProperties: "ApplicationUser"));

                OrderViewModel.OrderDetail = _unitOfWork.OrderDetail.GetAll(
                    x => x.OrderHeaderId == OrderViewModel.OrderHeaderDTO.Id, includeProperties: "Product");

                //stripe
                var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                var options = new SessionCreateOptions
                {

                    SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderViewModel.OrderHeaderDTO.Id}",
                    CancelUrl = domain + $"admin/order/details?orderId={OrderViewModel.OrderHeaderDTO.Id}",
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
                _unitOfWork.OrderHeader.UpdateStripePaymentId(
                    OrderViewModel.OrderHeaderDTO.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during payment process
                Log.Error(ex, "An error occurred while processing the payment for order {OrderId} at {Timestamp}. Error message: {ErrorMessage}", OrderViewModel.OrderHeaderDTO.Id, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while processing the payment. Please try again later.";
                return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeaderDTO.Id });
            }
        }


        [HttpGet]
        [Route("PaymentConfirmation")]
        public IActionResult PaymentConfirmation(int orderHeaderid)
        {
            try
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

                // Log the successful payment confirmation
                Log.Information("Payment for order {OrderId} confirmed successfully at {Timestamp}", orderHeaderid, DateTime.Now);

                return View(orderHeaderid);
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during payment confirmation
                Log.Error(ex, "An error occurred while confirming payment for order {OrderId} at {Timestamp}. Error message: {ErrorMessage}", orderHeaderid, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while confirming the payment. Please try again later.";
                return RedirectToAction(nameof(Details), new { orderId = orderHeaderid });
            }
        }
        #region API CALLS

        [HttpGet]
        [Route("GetAll")]
        public IActionResult GetAll(string status)
        {
            try
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

                // Log the successful retrieval of orders
                Log.Information("Successfully retrieved {OrderCount} orders with status '{OrderStatus}' at {Timestamp}", objOrderHeaders.Count(), status, DateTime.Now);

                return Json(new { data = objOrderHeaders });
            }
            catch (Exception ex)
            {
                // Log the error when an exception occurs during order retrieval
                Log.Error(ex, "An error occurred while retrieving orders with status '{OrderStatus}' at {Timestamp}. Error message: {ErrorMessage}", status, DateTime.Now, ex.Message);

                return StatusCode(500, "An error occurred while retrieving orders.");
            }
        }

        #endregion
    }
}

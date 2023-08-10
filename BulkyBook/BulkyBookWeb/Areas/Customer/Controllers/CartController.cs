using AutoMapper;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("customer")]
    [Authorize]
    [Route("[area]/[controller]")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        [BindProperty]
        public ShoppingCartViewModel ShoppingCartViewModel { get; set; }

        public CartController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            string userId = null;

            try
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Log user identification
                Log.Information("User {UserId} accessed the cart index page at {Timestamp}", userId, DateTime.Now);

                ShoppingCartViewModel = new()
                {
                    ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product"),
                    OrderHeader = new()
                };

                IEnumerable<ProductImage> productImages = _unitOfWork.ProductImage.GetAll();

                foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
                {
                    cart.Product.ProductImages = productImages.Where(x => x.ProductId == cart.Product.Id).ToList();
                    cart.Price = GetPriceBasedOnQuantity(cart);
                    ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                }

                // Log the total number of items in the cart
                Log.Information("User {UserId} has {CartItemCount} item(s) in the cart at {Timestamp}", userId, ShoppingCartViewModel.ShoppingCartList.Count(), DateTime.Now);

                return View(ShoppingCartViewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while processing the cart index page for User {UserId} at {Timestamp}. Error message: {ErrorMessage}", userId, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while processing the cart index page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("Summary")]
        public IActionResult Summary()
        {
            string userId = null;

            try
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Log user identification
                Log.Information("User {UserId} accessed the cart summary page at {Timestamp}", userId, DateTime.Now);

                ShoppingCartViewModel = new()
                {
                    ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product"),
                    OrderHeader = new()
                };
                ShoppingCartViewModel.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == userId);
                ShoppingCartViewModel.OrderHeader = _mapper.Map(ShoppingCartViewModel.OrderHeader.ApplicationUser, ShoppingCartViewModel.OrderHeader);

                foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
                {
                    cart.Price = GetPriceBasedOnQuantity(cart);
                    ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                }

                // Log the total number of items in the cart
                Log.Information("User {UserId} has {CartItemCount} item(s) in the cart at {Timestamp}", userId, ShoppingCartViewModel.ShoppingCartList.Count(), DateTime.Now);

                return View(ShoppingCartViewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while processing the cart summary page for User {UserId} at {Timestamp}. Error message: {ErrorMessage}", userId, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while processing the cart summary page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost("SummaryPOST")]
        [ActionName("SummaryPOST")]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartViewModel.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product");

            ShoppingCartViewModel.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartViewModel.OrderHeader.ApplicationUserId = userId;

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == userId);

            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //regular customer account
                ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                //company user
                ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            // Log order details before adding to the database
            Log.Information("Order details before saving: {@OrderHeader}", ShoppingCartViewModel.OrderHeader);
            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                Log.Information("Order detail for ProductId {ProductId}: Price: {Price}, Quantity: {Quantity}", cart.ProductId, cart.Price, cart.Count);
            }

            _unitOfWork.OrderHeader.Add(ShoppingCartViewModel.OrderHeader);
            _unitOfWork.Save();
            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartViewModel.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //regular customer account
                //stripe logic
                var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                var options = new SessionCreateOptions
                {

                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartViewModel.OrderHeader.Id}",
                    CancelUrl = domain + "customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in ShoppingCartViewModel.ShoppingCartList)
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
                _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }

            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartViewModel.OrderHeader.Id });
        }

        [HttpGet("OrderConfirmation")]
        public IActionResult OrderConfirmation(int id)
        {
            try
            {
                // Log the order confirmation request with the order ID
                Log.Information("Order confirmation request received for Order ID {OrderId} at {Timestamp}", id, DateTime.Now);

                OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == id, includeProperties: "ApplicationUser");
                if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
                {
                    //order by customer
                    var service = new SessionService();
                    Session session = service.Get(orderHeader.SessionId);

                    if (session.PaymentStatus.ToLower() == "paid")
                    {
                        _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                        _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                        _unitOfWork.Save();
                    }
                    HttpContext.Session.Clear();
                }

                List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                    .GetAll(x => x.ApplicationUserId == orderHeader.ApplicationUserId)
                    .ToList();

                _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
                _unitOfWork.Save();

                // Log the successful completion of the order confirmation
                Log.Information("Order confirmation completed for Order ID {OrderId} at {Timestamp}", id, DateTime.Now);

                return View(id);
            }
            catch (Exception ex)
            {
                // Log the error along with the order ID
                Log.Error(ex, "An error occurred while processing the order confirmation for Order ID {OrderId} at {Timestamp}. Error message: {ErrorMessage}", id, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while processing the order confirmation. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost("Plus")]
        public IActionResult Plus(int cartId)
        {
            try
            {
                var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);
                cartFromDb.Count += 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();

                // Log the cart item count update
                Log.Information("Cart item with ID {CartId} increased by 1 at {Timestamp}", cartId, DateTime.Now);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error along with the cart ID
                Log.Error(ex, "An error occurred while updating the cart item with ID {CartId} by 1 at {Timestamp}. Error message: {ErrorMessage}", cartId, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while updating the cart item. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("Minus")]
        public IActionResult Minus(int cartId)
        {
            try
            {
                var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);
                if (cartFromDb.Count <= 1)
                {
                    //remove
                    _unitOfWork.ShoppingCart.Remove(cartFromDb);
                    HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
                }
                else
                {
                    cartFromDb.Count -= 1;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }
                _unitOfWork.Save();

                // Log the cart item count update
                Log.Information("Cart item with ID {CartId} decreased by 1 at {Timestamp}", cartId, DateTime.Now);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error along with the cart ID
                Log.Error(ex, "An error occurred while updating the cart item with ID {CartId} by -1 at {Timestamp}. Error message: {ErrorMessage}", cartId, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while updating the cart item. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost("Remove")]
        public IActionResult Remove(int cartId)
        {
            try
            {
                var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
                _unitOfWork.Save();

                // Log the removal of a cart item
                Log.Information("Cart item with ID {CartId} removed from the cart at {Timestamp}", cartId, DateTime.Now);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error along with the cart ID
                Log.Error(ex, "An error occurred while removing the cart item with ID {CartId} at {Timestamp}. Error message: {ErrorMessage}", cartId, DateTime.Now, ex.Message);

                TempData["error"] = "An error occurred while removing the cart item. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            try
            {
                if (shoppingCart.Count <= 50)
                {
                    return shoppingCart.Product.Price;
                }
                else
                {
                    if (shoppingCart.Count <= 1000)
                    {
                        return shoppingCart.Product.Price50;
                    }
                    else
                    {
                        return shoppingCart.Product.Price100;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error along with the cart item ID and count
                Log.Error(ex, "An error occurred while calculating the price for cart item with ID {CartItemId} and count {CartItemCount} at {Timestamp}. Error message: {ErrorMessage}", shoppingCart.Id, shoppingCart.Count, DateTime.Now, ex.Message);

                return shoppingCart.Product.Price;
            }
        }

    }
}

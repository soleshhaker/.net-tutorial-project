using AutoMapper;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.DTO;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            IEnumerable<Product> products = _unitOfWork.Product.GetAll(includeProperties: "ProductImages");

            IEnumerable<ProductDto> productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);

            int productCount = productDtos.Count(); // Get the number of products returned
            Log.Information("Returned {ProductCount} product(s) at {Timestamp}", productCount, DateTime.Now);
            return View(productDtos);
        }

        [HttpGet("Details")]
        public IActionResult Details(int ProductId)
        {
            var product = _unitOfWork.Product.GetFirstOrDefault(
                x => x.Id == ProductId,
                includeProperties: "Category,ProductImages");

            if (product == null)
            {
                Log.Information("Product of ID {ProductId} not found at {Timestamp}", ProductId, DateTime.Now);
                return NotFound();
            }

            var shoppingCart = new ShoppingCart
            {
                Product = product,
                Count = 1,
                ProductId = ProductId
            };

            return View(shoppingCart);
        }
        [HttpPost("Details")]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            string userId = null;
            try
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                shoppingCart.ApplicationUserId = userId;

                // Log user identification
                Log.Information("User {UserId} is performing a cart operation at {Timestamp}", userId, DateTime.Now);

                ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.ApplicationUserId == userId && x.ProductId == shoppingCart.ProductId);
                if (cartFromDb != null)
                {
                    //cart exists
                    cartFromDb.Count += shoppingCart.Count;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                    _unitOfWork.Save();

                    // Log cart update information
                    Log.Information("Cart updated for User {UserId}. ProductId: {ProductId}, Quantity Added: {QuantityAdded} at {Timestamp}",
                                    userId, shoppingCart.ProductId, shoppingCart.Count, DateTime.Now);

                    HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId).Count());
                }
                else
                {
                    //add cart
                    _unitOfWork.ShoppingCart.Add(shoppingCart);
                    _unitOfWork.Save();

                    // Log cart creation information
                    Log.Information("Cart created for User {UserId}. ProductId: {ProductId}, Quantity: {Quantity} at {Timestamp}",
                                    userId, shoppingCart.ProductId, shoppingCart.Count, DateTime.Now);

                    HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId).Count());
                }

                TempData["success"] = "Cart updated successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "An error occurred while processing the cart operation for User {UserId} at {Timestamp}. Error message: {ErrorMessage}", userId, DateTime.Now, ex.Message);

                // Optionally, you can handle the error in a meaningful way, like showing a user-friendly error message or redirecting to an error page.
                TempData["error"] = "An error occurred while processing your cart. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet("Error")]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Mango.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;
        private readonly ICouponService _couponService;
        public CartController(IProductService productService,ICartService cartService, ICouponService couponService)
        {
            _productService = productService;
            _couponService = couponService;
            _cartService= cartService;
        }
        public async Task<IActionResult> CartIndex()
        {
            return View(await LoadCartDtoBasdOnLoggedInUser());
        }

        [HttpPost]
        [ActionName("ApplyCoupon")]
        public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
        {
            //cartDto.CartDetails = new () { };
            var userId = User.Claims.Where(x => x.Type == "sub")?.FirstOrDefault().Value;
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.ApplyCoupon<ResponseDto>(cartDto, accessToken);

            if (response != null && response.isSuccess)
            {
                return RedirectToAction(nameof(CartIndex));
            }

            return View();
        }

        [HttpPost]
        [ActionName("RemoveCoupon")]
        public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
        {
            var userId = User.Claims.Where(x => x.Type == "sub")?.FirstOrDefault().Value;
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.RemoveCoupon<ResponseDto>(cartDto.CartHeader.UserId, accessToken);

            if (response != null && response.isSuccess)
            {
                return RedirectToAction(nameof(CartIndex));
            }

            return View();
        }

        public async Task<IActionResult> Remove(int cartDetailsId)
        {
            var userId = User.Claims.Where(x => x.Type == "sub")?.FirstOrDefault().Value;
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.RemoveFromCartAsync<ResponseDto>(cartDetailsId, accessToken);
            
            if (response != null && response.isSuccess)
            {
                return RedirectToAction(nameof(CartIndex));
            }
            return View();
        }

        public async Task<IActionResult> Checkout()
        {
            return View(await LoadCartDtoBasdOnLoggedInUser());
        }
        [HttpPost]
        public async Task<IActionResult> Checkout(CartDto cartDto)
        {
            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                cartDto.CartHeader.PickupDateTime = DateTime.UtcNow;
                cartDto.CartHeader.CartTotalItems = 0;
                
                var response = await _cartService.Checkout<ResponseDto>(cartDto.CartHeader, accessToken);
                return RedirectToAction(nameof(Confirmation));
            }
            catch (Exception e)
            {

                return View(cartDto);
            }
        }

        public async Task<IActionResult> Confirmation()
        {
            return View();
        }

        private async Task<CartDto> LoadCartDtoBasdOnLoggedInUser() {
            var userId = User.Claims.Where(x => x.Type == "sub")?.FirstOrDefault().Value;
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.GetCartByUserIdAsync<ResponseDto>(userId,accessToken);
            CartDto cartDto = new CartDto();
            if (response != null && response.isSuccess) {
                cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));
            }

            if (cartDto.CartHeader !=null)
            {
                if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
                {
                    var couponResp = await _couponService.GetCoupon<ResponseDto>(cartDto.CartHeader.CouponCode, accessToken);
                    if (couponResp != null && couponResp.isSuccess)
                    {
                        var couponObj = JsonConvert.DeserializeObject<CouponDto>(Convert.ToString(couponResp.Result));
                        cartDto.CartHeader.DiscountTotal = couponObj.DiscountAmount;
                    }
                }
                foreach (var detail in cartDto.CartDetails)
                {
                    cartDto.CartHeader.OrderTotal += (detail.Product.Price * detail.Count);
                }

                cartDto.CartHeader.OrderTotal  -= cartDto.CartHeader.DiscountTotal;

            }

            return cartDto;

        }

    }
}

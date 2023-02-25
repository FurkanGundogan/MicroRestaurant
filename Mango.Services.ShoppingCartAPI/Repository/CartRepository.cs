using AutoMapper;
using Mango.Services.ShoppingCartAPI.DbContexts;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Repository
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private IMapper _mapper;
        public CartRepository(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<bool> ClearCart(string userId)
        {
            var cartHeaderFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == userId);
            if (cartHeaderFromDb != null)
            {
                _db.CartDetails.RemoveRange(_db.CartDetails.Where(x => x.CartHeaderId == cartHeaderFromDb.CartHeaderId));
                _db.CartHeaders.Remove(cartHeaderFromDb);
                await _db.SaveChangesAsync();
                return true;
            }
            else
            {
                return false;
            }

        }

        public async Task<CartDto> CreateUpdateCart(CartDto cartDto)
        {
            Cart cart = _mapper.Map<Cart>(cartDto);
            // check if product exist in db, if not create it
            var productInDb = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == cartDto.CartDetails.FirstOrDefault().ProductId);
            if (productInDb == null)
            {
                _db.Products.Add(cart.CartDetails.FirstOrDefault().Product);
                await _db.SaveChangesAsync();
            }
            // check if header is null
            var cartHeaderFromDb = await _db.CartHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == cart.CartHeader.UserId);
            if (cartHeaderFromDb == null)
            {
                // create header and details
                _db.CartHeaders.Add(cart.CartHeader);
                await _db.SaveChangesAsync();
                cart.CartDetails.FirstOrDefault().CartHeaderId = cart.CartHeader.CartHeaderId;
                cart.CartDetails.FirstOrDefault().Product = null;
                _db.CartDetails.Add(cart.CartDetails.FirstOrDefault());
                await _db.SaveChangesAsync();

            }
            else
            {
                // if header is not null
                // check if details has same prodcut
                var cartDetailsFromDb = await _db.CartDetails.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == cart.CartDetails.FirstOrDefault().ProductId &&
                x.CartHeaderId == cartHeaderFromDb.CartHeaderId);

                if (cartDetailsFromDb == null)
                {
                    // create details
                    var element = cart.CartDetails.FirstOrDefault();
                    var cd = new CartDetails();
                    cd.CartHeaderId = cartHeaderFromDb.CartHeaderId;
                    cd.ProductId = element.ProductId;
                    cd.CartDetailsId = element.CartDetailsId;
                    cd.Count = element.Count;
                    _db.CartDetails.Add(cd);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // update count / cart details
                    cart.CartDetails.FirstOrDefault().Count += cartDetailsFromDb.Count;
                    _db.CartDetails.Update(cart.CartDetails.FirstOrDefault());
                    await _db.SaveChangesAsync();
                }

            }
            return _mapper.Map<CartDto>(cart);




            
        }

        public async Task<CartDto> GetCartByUserId(string userId)
        {
            var cartHeader = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == userId);
            var cartDetails = _db.CartDetails.Where(x => x.CartHeaderId == cartHeader.CartHeaderId).Include(x=>x.Product);
            Cart cart = new Cart()
            {
                CartHeader = cartHeader,
                CartDetails = cartDetails
            };

            return _mapper.Map<CartDto>(cart);
        }

        public async Task<bool> RemoveFromCart(int cartDetailsId)
        {
            try
            {
                CartDetails cartDetails = await _db.CartDetails.FirstOrDefaultAsync(x => x.CartDetailsId == cartDetailsId);
                int totalCountOfCartItem = _db.CartDetails.Where(x => x.CartHeaderId == cartDetails.CartHeaderId).Count();
                _db.CartDetails.Remove(cartDetails);
                if (totalCountOfCartItem == 1)
                {
                    var cartHeaderToRemove = await _db.CartHeaders.FirstOrDefaultAsync(x => x.CartHeaderId == cartDetails.CartHeaderId);
                    _db.CartHeaders.Remove(cartHeaderToRemove);

                }
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            
        }
    }
}

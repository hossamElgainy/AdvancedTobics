using AdvancedTobics.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdvancedTobics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly RedisCacheService _cacheService;

        public ProductsController(
            AppDbContext context,
            RedisCacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            string cacheKey = $"product:{id}";

            // 1- Try get from Redis
            var cachedProduct =
                await _cacheService.GetDataAsync<Product>(cacheKey);

            if (cachedProduct is not null)
            {
                return Ok(new
                {
                    Source = "Redis Cache",
                    Data = cachedProduct
                });
            }

            // 2- Get from Database
            var product = await _context.Products.FindAsync(id);

            if (product is null)
                return NotFound();

            // 3- Save to Redis
            await _cacheService.SetDataAsync(
                cacheKey,
                product,
                TimeSpan.FromMinutes(5));

            return Ok(new
            {
                Source = "SQL Database",
                Data = product
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            Product updatedProduct)
        {
            var product =
                await _context.Products.FindAsync(id);

            if (product is null)
                return NotFound();

            product.Name = updatedProduct.Name;
            product.Price = updatedProduct.Price;
            product.Stock = updatedProduct.Stock;

            await _context.SaveChangesAsync();

            // Remove old cache
            await _cacheService.RemoveDataAsync($"product:{id}");

            return Ok(product);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product =
                await _context.Products.FindAsync(id);

            if (product is null)
                return NotFound();

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            // Remove cache
            await _cacheService.RemoveDataAsync($"product:{id}");

            return Ok();
        }
    }
}

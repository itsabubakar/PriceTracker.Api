using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace PriceTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PriceTrackerController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IBackgroundJobClient _jobs;

        public PriceTrackerController(AppDbContext db, IBackgroundJobClient jobs)
        {
            _db = db;
            _jobs = jobs;
        }

        [HttpPost]
        public async Task<IActionResult> AddUrl([FromBody] AddUrlRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url)) return BadRequest("Url can't be empty");
            var record = new Product
            {
                Url = request.Url,
                Store = GenerateStore(request.Url)
            };
            await _db.Products.AddAsync(record);
            await _db.SaveChangesAsync();

            _jobs.Enqueue<PriceUpdatedService>(x => x.UpdateSingleProduct(record.Id));
            return Ok(new { message = "Url added", record });
        }
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _db.Products.OrderByDescending(x => x.CreatedAt).ToListAsync();
            return Ok(products);
        }
        [HttpGet("product/{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }
        [HttpPatch("product/{id:int}")]
        public async Task<IActionResult> EditProduct(int id, [FromBody] UpdateProductNameDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingProduct = await _db.Products.FindAsync(id);
            if (existingProduct == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Name cannot be empty" });

            existingProduct.Name = dto.Name;
            await _db.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete("product/{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            _db.Remove(product);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        public string GenerateStore(string url)
        {
            url.Trim();
            if (url.Contains("jumia", StringComparison.OrdinalIgnoreCase))
            {
                return "jumia";
            }
            else if (url.Contains("konga", StringComparison.OrdinalIgnoreCase))
            {
                return "konga";
            }
            else
            {
                throw new Exception("Url must be from jumia or konga");
            }

        }

    }
}
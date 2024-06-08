using Commerce.Data;
using Commerce.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;

namespace Commerce.Controllers
{
    public class ProductsController : Controller
    {
        private readonly CommerceDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(CommerceDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            var products = _context.Products.OrderByDescending(p => p.Id).ToList();
            return View(products);
        }

        [HttpGet]
        public IActionResult Search(string searchName, string searchCategory)
        {
            var products = _context.Products.OrderByDescending(p => p.Id).ToList();

            // Filter products based on searchName and searchCategory
            if (!string.IsNullOrEmpty(searchName))
            {
                products = products.Where(p => p.Name.Contains(searchName)).ToList();
            }
            if (!string.IsNullOrEmpty(searchCategory))
            {
                products = products.Where(p => p.Category == searchCategory).ToList();
            }

            return View("Index", products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(ProductDto productDto)
        {
            if (productDto.ImageFile == null || productDto.ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "The image file is required.");
                return View(productDto);
            }

            if (!ModelState.IsValid)
            {
                return View(productDto);
            }

            string uploadsDir = Path.Combine(_environment.WebRootPath, "products");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            string newFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(productDto.ImageFile.FileName);
            string filePath = Path.Combine(uploadsDir, newFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                productDto.ImageFile.CopyTo(stream);
            }

            Product product = new Product()
            {
                Name = productDto.Name,
                Brand = productDto.Brand,
                Category = productDto.Category,
                Price = productDto.Price,
                Description = productDto.Description,
                ImageFileName = newFileName,
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            var productDto = new ProductDto()
            {
                Name = product.Name,
                Brand = product.Brand,
                Category = product.Category,
                Price = product.Price,
                Description = product.Description,
            };

            ViewData["ProductId"] = product.Id;
            ViewData["ImageFileName"] = product.ImageFileName;
            ViewData["CreatedAt"] = product.CreatedAt.ToString("MM/dd/yyyy");
            return View(productDto);
        }

        [HttpPost]
        public IActionResult Edit(int id, ProductDto productDto)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            if (!ModelState.IsValid)
            {
                ViewData["ProductId"] = product.Id;
                ViewData["ImageFileName"] = product.ImageFileName;
                ViewData["CreatedAt"] = product.CreatedAt.ToString("MM/dd/yyyy");

                return View(productDto);
            }

            string newFileName = product.ImageFileName;
            if (productDto.ImageFile != null)
            {
                newFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(productDto.ImageFile.FileName);
                string imageFullPath = Path.Combine(_environment.WebRootPath, "products", newFileName);

                using (var stream = System.IO.File.Create(imageFullPath))
                {
                    productDto.ImageFile.CopyTo(stream);
                }

                string oldImageFullPath = Path.Combine(_environment.WebRootPath, "products", product.ImageFileName);
                System.IO.File.Delete(oldImageFullPath);
            }

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Brand = productDto.Brand;
            product.Price = productDto.Price;
            product.ImageFileName = newFileName;

            _context.SaveChanges();
            return RedirectToAction("Index", "Products");
        }

        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            string imageFullPath = Path.Combine(_environment.WebRootPath, "products", product.ImageFileName);
            System.IO.File.Delete(imageFullPath);
            _context.Products.Remove(product);
            _context.SaveChanges(true);

            return RedirectToAction("Index", "Products");
        }
    }
}

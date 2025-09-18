using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Aplicacion_pedidos.Data;
using Aplicacion_pedidos.Models;
using Microsoft.AspNetCore.Authorization;
using Aplicacion_pedidos.Filters;

namespace Aplicacion_pedidos.Controllers
{
    [Authorize]  // Requiere que el usuario esté autenticado para todas las acciones
    public class ProductsController : Controller
    {
        private readonly PedidosDBContext _context;

        public ProductsController(PedidosDBContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(string searchString, string categoria, decimal? precioMin, decimal? precioMax)
        {
            // Inicializar la consulta base
            var productos = from p in _context.Products
                            select p;

            // Aplicar filtros si se proporcionaron
            if (!string.IsNullOrEmpty(searchString))
            {
                productos = productos.Where(p => p.Nombre.Contains(searchString) || p.Descripcion.Contains(searchString));
                ViewData["CurrentFilter"] = searchString;
            }

            if (!string.IsNullOrEmpty(categoria) && categoria != "Todas")
            {
                productos = productos.Where(p => p.Categoria == categoria);
                ViewData["CurrentCategoria"] = categoria;
            }

            if (precioMin.HasValue)
            {
                productos = productos.Where(p => p.Precio >= precioMin.Value);
                ViewData["CurrentPrecioMin"] = precioMin.Value;
            }

            if (precioMax.HasValue)
            {
                productos = productos.Where(p => p.Precio <= precioMax.Value);
                ViewData["CurrentPrecioMax"] = precioMax.Value;
            }

            // Obtener categorías únicas para el filtro de categorías
            var categorias = await _context.Products
                                    .Select(p => p.Categoria)
                                    .Distinct()
                                    .Where(c => c != null)
                                    .ToListAsync();
            ViewData["Categorias"] = new SelectList(new List<string> { "Todas" }.Concat(categorias));
            
            // Obtener precios mínimos y máximos para referencia
            ViewData["PrecioMinimoDisponible"] = await productos.MinAsync(p => (decimal?)p.Precio) ?? 0;
            ViewData["PrecioMaximoDisponible"] = await productos.MaxAsync(p => (decimal?)p.Precio) ?? 1000;

            return View(await productos.ToListAsync());
        }

        // GET: Products/GetProductInfo
        [HttpGet]
        public async Task<IActionResult> GetProductInfo(int id)
        {
            var producto = await _context.Products.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            // Devolver solo la información necesaria para evitar exponer datos sensibles
            return Json(new { 
                id = producto.Id,
                nombre = producto.Nombre, 
                precio = producto.Precio,
                stock = producto.Stock,
                disponible = producto.Disponible
            });
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productModel = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productModel == null)
            {
                return NotFound();
            }

            return View(productModel);
        }

        // GET: Products/Create
        [AuthorizeRoles(UserModel.ROLE_ADMIN, UserModel.ROLE_EMPLEADO)]  // Solo admins y empleados pueden crear
        public IActionResult Create()
        {
            var producto = new ProductModel
            {
                Disponible = true,
                Stock = 0
            };
            return View(producto);
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN, UserModel.ROLE_EMPLEADO)]  // Solo admins y empleados pueden crear
        public async Task<IActionResult> Create([Bind("Id,Nombre,Descripcion,Precio,Stock,Disponible,Categoria")] ProductModel productModel)
        {
            // Validaciones adicionales específicas
            if (productModel.Precio <= 0)
            {
                ModelState.AddModelError("Precio", "El precio debe ser mayor que cero");
            }
            
            if (productModel.Stock < 0)
            {
                ModelState.AddModelError("Stock", "El stock no puede ser negativo");
            }
            
            if (ModelState.IsValid)
            {
                _context.Add(productModel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Producto creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(productModel);
        }

        // GET: Products/Edit/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN, UserModel.ROLE_EMPLEADO)]  // Solo admins y empleados pueden editar
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productModel = await _context.Products.FindAsync(id);
            if (productModel == null)
            {
                return NotFound();
            }
            return View(productModel);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN, UserModel.ROLE_EMPLEADO)]  // Solo admins y empleados pueden editar
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Precio,Stock,Disponible,Categoria")] ProductModel productModel)
        {
            if (id != productModel.Id)
            {
                return NotFound();
            }

            // Validaciones adicionales específicas
            if (productModel.Precio <= 0)
            {
                ModelState.AddModelError("Precio", "El precio debe ser mayor que cero");
            }
            
            if (productModel.Stock < 0)
            {
                ModelState.AddModelError("Stock", "El stock no puede ser negativo");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productModel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductModelExists(productModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(productModel);
        }

        // GET: Products/Delete/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]  // Solo admins pueden eliminar
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productModel = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productModel == null)
            {
                return NotFound();
            }

            return View(productModel);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]  // Solo admins pueden eliminar
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productModel = await _context.Products.FindAsync(id);
            if (productModel != null)
            {
                _context.Products.Remove(productModel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Producto eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Products/GetAvailableProductsJson
        [HttpGet]
        public async Task<IActionResult> GetAvailableProductsJson()
        {
            var productos = await _context.Products
                .Where(p => p.Disponible && p.Stock > 0)
                .Select(p => new {
                    id = p.Id,
                    nombre = p.Nombre,
                    precio = p.Precio,
                    stock = p.Stock,
                    categoria = p.Categoria
                })
                .ToListAsync();

            return Json(productos);
        }

        private bool ProductModelExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}

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
using Microsoft.Extensions.Logging;

namespace Aplicacion_pedidos.Controllers
{
    [Authorize]  
    public class ProductsController : Controller
    {
        private readonly PedidosDBContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(PedidosDBContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Products
        public async Task<IActionResult> Index(string searchString, string categoria, decimal? precioMin, decimal? precioMax)
        {
            try
            {
                _logger.LogInformation("Obteniendo lista de productos con filtros: searchString={SearchString}, categoria={Categoria}, precioMin={PrecioMin}, precioMax={PrecioMax}", 
                    searchString, categoria, precioMin, precioMax);
                
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de productos con filtros");
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar la lista de productos. Por favor, inténtelo de nuevo.";
                return View(new List<ProductModel>());
            }
        }

        // GET: Products/GetProductInfo
        [HttpGet]
        public async Task<IActionResult> GetProductInfo(int id)
        {
            try
            {
                _logger.LogInformation("Obteniendo información del producto con ID: {ProductId}", id);
                var producto = await _context.Products.FindAsync(id);
                
                if (producto == null)
                {
                    _logger.LogWarning("Producto con ID {ProductId} no encontrado para GetProductInfo", id);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información del producto con ID {ProductId}", id);
                return StatusCode(500, new { error = "Error al obtener información del producto" });
            }
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de acceder a detalles de producto sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de producto válido.";
                    return RedirectToAction(nameof(Index));
                }

                var productModel = await _context.Products
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (productModel == null)
                {
                    _logger.LogWarning("Producto con ID {ProductId} no encontrado", id);
                    TempData["ErrorMessage"] = $"No se encontró el producto con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }

                return View(productModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del producto con ID {ProductId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar los detalles del producto. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Products/Create
        [AuthorizeRoles(UserModel.ROLE_ADMIN, UserModel.ROLE_EMPLEADO)]  // Solo admins y empleados pueden crear
        public IActionResult Create()
        {
            try
            {
                _logger.LogInformation("Cargando formulario de creación de producto");
                var producto = new ProductModel
                {
                    Disponible = true,
                    Stock = 0
                };
                return View(producto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la vista de creación de producto");
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar el formulario de creación de producto. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN, UserModel.ROLE_EMPLEADO)]  // Solo admins y empleados pueden crear
        public async Task<IActionResult> Create([Bind("Id,Nombre,Descripcion,Precio,Stock,Disponible,Categoria")] ProductModel productModel)
        {
            try
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
                    _logger.LogInformation("Producto creado correctamente: ID {ProductId}, {ProductName}", productModel.Id, productModel.Nombre);
                    TempData["SuccessMessage"] = "Producto creado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                return View(productModel);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al crear producto: {ProductName}", productModel.Nombre);
                ModelState.AddModelError("", "No se pudo guardar los cambios. Puede que haya un problema con la conexión a la base de datos.");
                return View(productModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear producto: {ProductName}", productModel.Nombre);
                ModelState.AddModelError("", "Ha ocurrido un error inesperado al crear el producto. Por favor, inténtelo de nuevo.");
                return View(productModel);
            }
        }

        // GET: Products/Edit/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN, UserModel.ROLE_EMPLEADO)]  // Solo admins y empleados pueden editar
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de editar producto sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de producto válido.";
                    return RedirectToAction(nameof(Index));
                }

                var productModel = await _context.Products.FindAsync(id);
                if (productModel == null)
                {
                    _logger.LogWarning("Producto con ID {ProductId} no encontrado para edición", id);
                    TempData["ErrorMessage"] = $"No se encontró el producto con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }
                return View(productModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto con ID {ProductId} para edición", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar los datos del producto para editar. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN, UserModel.ROLE_EMPLEADO)]  // Solo admins y empleados pueden editar
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Precio,Stock,Disponible,Categoria")] ProductModel productModel)
        {
            try
            {
                if (id != productModel.Id)
                {
                    _logger.LogWarning("ID de producto no coincide en edición: recibido {ReceivedId}, esperado {ExpectedId}", 
                        productModel.Id, id);
                    TempData["ErrorMessage"] = "ID de producto no válido.";
                    return RedirectToAction(nameof(Index));
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
                        _logger.LogInformation("Producto actualizado correctamente: ID {ProductId}, {ProductName}", productModel.Id, productModel.Nombre);
                        TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!ProductModelExists(productModel.Id))
                        {
                            _logger.LogWarning("Intento de actualizar producto no existente: ID {ProductId}", productModel.Id);
                            TempData["ErrorMessage"] = $"El producto con ID {productModel.Id} ya no existe.";
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            _logger.LogError(ex, "Error de concurrencia al actualizar producto: ID {ProductId}", productModel.Id);
                            ModelState.AddModelError("", "El registro fue modificado por otro usuario. Por favor, actualice la página e intente de nuevo.");
                            return View(productModel);
                        }
                    }
                    catch (DbUpdateException ex)
                    {
                        _logger.LogError(ex, "Error de base de datos al actualizar producto: ID {ProductId}", productModel.Id);
                        ModelState.AddModelError("", "No se pudo guardar los cambios. Puede que haya un problema con la conexión a la base de datos.");
                        return View(productModel);
                    }
                }
                return View(productModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar producto: ID {ProductId}", productModel.Id);
                ModelState.AddModelError("", "Ha ocurrido un error inesperado al actualizar el producto. Por favor, inténtelo de nuevo.");
                return View(productModel);
            }
        }

        // GET: Products/Delete/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]  // Solo admins pueden eliminar
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de eliminar producto sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de producto válido.";
                    return RedirectToAction(nameof(Index));
                }

                var productModel = await _context.Products
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (productModel == null)
                {
                    _logger.LogWarning("Producto con ID {ProductId} no encontrado para eliminación", id);
                    TempData["ErrorMessage"] = $"No se encontró el producto con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si el producto está asociado a algún pedido
                var hasOrderItems = await _context.OrderItems.AnyAsync(o => o.ProductId == id);
                if (hasOrderItems)
                {
                    _logger.LogWarning("Intento de eliminar producto con pedidos asociados: ID {ProductId}", id);
                    TempData["ErrorMessage"] = "No se puede eliminar este producto porque está asociado a uno o más pedidos.";
                    return RedirectToAction(nameof(Index));
                }

                return View(productModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vista de eliminación para producto con ID {ProductId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar la vista de eliminación. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]  // Solo admins pueden eliminar
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var productModel = await _context.Products.FindAsync(id);
                if (productModel == null)
                {
                    _logger.LogWarning("Producto con ID {ProductId} no encontrado para eliminación confirmada", id);
                    TempData["ErrorMessage"] = $"No se encontró el producto con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si el producto está asociado a algún pedido
                var hasOrderItems = await _context.OrderItems.AnyAsync(o => o.ProductId == id);
                if (hasOrderItems)
                {
                    _logger.LogWarning("Intento de eliminar producto con pedidos asociados: ID {ProductId}", id);
                    TempData["ErrorMessage"] = "No se puede eliminar este producto porque está asociado a uno o más pedidos.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Products.Remove(productModel);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Producto eliminado correctamente: ID {ProductId}, {ProductName}", id, productModel.Nombre);
                TempData["SuccessMessage"] = "Producto eliminado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al eliminar producto: ID {ProductId}", id);
                TempData["ErrorMessage"] = "No se pudo eliminar el producto debido a referencias en la base de datos. Es posible que esté asociado a pedidos.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar producto: ID {ProductId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error inesperado al eliminar el producto. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Products/GetAvailableProductsJson
        [HttpGet]
        public async Task<IActionResult> GetAvailableProductsJson()
        {
            try
            {
                _logger.LogInformation("Obteniendo lista de productos disponibles en formato JSON");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de productos disponibles en formato JSON");
                return StatusCode(500, new { error = "Error al obtener la lista de productos disponibles" });
            }
        }

        private bool ProductModelExists(int id)
        {
            try
            {
                return _context.Products.Any(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de producto: ID {ProductId}", id);
                // Devolvemos false para evitar excepciones en los métodos que llaman a esta función
                return false;
            }
        }
    }
}

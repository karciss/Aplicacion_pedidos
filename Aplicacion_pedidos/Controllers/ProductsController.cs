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
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
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
        public async Task<IActionResult> Create([Bind("Id,Nombre,Descripcion,Precio,Stock,Disponible")] ProductModel productModel)
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Precio,Stock,Disponible")] ProductModel productModel)
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

        private bool ProductModelExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}

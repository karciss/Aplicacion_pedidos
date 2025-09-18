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

namespace Aplicacion_pedidos.Controllers
{
    [Authorize]
    public class OrderItemController : Controller
    {
        private readonly PedidosDBContext _context;

        public OrderItemController(PedidosDBContext context)
        {
            _context = context;
        }

        // GET: OrderItem
        public async Task<IActionResult> Index()
        {
            var pedidosDBContext = _context.OrderItems.Include(o => o.Order).Include(o => o.Producto);
            return View(await pedidosDBContext.ToListAsync());
        }

        // GET: OrderItem/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderItemModel = await _context.OrderItems
                .Include(o => o.Order)
                .Include(o => o.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (orderItemModel == null)
            {
                return NotFound();
            }

            return View(orderItemModel);
        }

        // GET: OrderItem/Create
        public IActionResult Create(int? orderId = null)
        {
            // Si se proporciona un orderId, preseleccionar ese pedido
            if (orderId.HasValue)
            {
                ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderId.Value);
            }
            else
            {
                ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id");
            }
            
            // Solo mostrar productos disponibles y con stock
            ViewData["ProductId"] = new SelectList(_context.Products.Where(p => p.Disponible && p.Stock > 0), "Id", "Nombre");
            
            var orderItem = new OrderItemModel();
            if (orderId.HasValue)
            {
                orderItem.OrderId = orderId.Value;
            }
            
            return View(orderItem);
        }

        // POST: OrderItem/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,OrderId,ProductId,Cantidad,Subtotal")] OrderItemModel orderItemModel)
        {
            // Validar el stock antes de continuar
            var producto = await _context.Products.FindAsync(orderItemModel.ProductId);
            
            if (producto == null)
            {
                ModelState.AddModelError("ProductId", "El producto seleccionado no existe.");
                ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
                ViewData["ProductId"] = new SelectList(_context.Products.Where(p => p.Disponible && p.Stock > 0), "Id", "Nombre", orderItemModel.ProductId);
                return View(orderItemModel);
            }
            
            // Verificar si el producto está disponible
            if (!producto.Disponible)
            {
                ModelState.AddModelError("ProductId", "El producto seleccionado no está disponible para la venta.");
                ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
                ViewData["ProductId"] = new SelectList(_context.Products.Where(p => p.Disponible && p.Stock > 0), "Id", "Nombre", orderItemModel.ProductId);
                return View(orderItemModel);
            }
            
            // Verificar el stock disponible
            if (orderItemModel.Cantidad > producto.Stock)
            {
                ModelState.AddModelError("Cantidad", $"Stock insuficiente. Solo hay {producto.Stock} unidades disponibles.");
                ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
                ViewData["ProductId"] = new SelectList(_context.Products.Where(p => p.Disponible && p.Stock > 0), "Id", "Nombre", orderItemModel.ProductId);
                return View(orderItemModel);
            }
            
            // Calcular automáticamente el subtotal basado en el precio del producto
            orderItemModel.Subtotal = producto.Precio * orderItemModel.Cantidad;

            if (ModelState.IsValid)
            {
                // Todo está validado, se procede a guardar el elemento
                _context.Add(orderItemModel);
                
                // Actualizar el stock del producto
                producto.Stock -= orderItemModel.Cantidad;
                _context.Update(producto);
                
                // Actualizar el total del pedido
                var orden = await _context.Orders.FindAsync(orderItemModel.OrderId);
                if (orden != null)
                {
                    orden.Total += orderItemModel.Subtotal;
                    _context.Update(orden);
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Elemento de pedido agregado correctamente.";
                
                // Redirigir a la página de detalles del pedido
                return RedirectToAction("Details", "Orders", new { id = orderItemModel.OrderId });
            }
            
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
            ViewData["ProductId"] = new SelectList(_context.Products.Where(p => p.Disponible && p.Stock > 0), "Id", "Nombre", orderItemModel.ProductId);
            return View(orderItemModel);
        }

        // GET: OrderItem/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderItemModel = await _context.OrderItems.FindAsync(id);
            if (orderItemModel == null)
            {
                return NotFound();
            }
            
            // Guardar la cantidad original para calcular la diferencia después
            TempData["CantidadOriginal"] = orderItemModel.Cantidad;
            TempData["SubtotalOriginal"] = orderItemModel.Subtotal;
            
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
            return View(orderItemModel);
        }

        // POST: OrderItem/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderId,ProductId,Cantidad,Subtotal")] OrderItemModel orderItemModel, int cantidadOriginal)
        {
            if (id != orderItemModel.Id)
            {
                return NotFound();
            }

            // Si no se proporciona cantidadOriginal, intentar recuperarla del TempData
            if (cantidadOriginal == 0 && TempData["CantidadOriginal"] != null)
            {
                cantidadOriginal = Convert.ToInt32(TempData["CantidadOriginal"]);
            }
            
            decimal subtotalOriginal = TempData["SubtotalOriginal"] != null ? 
                Convert.ToDecimal(TempData["SubtotalOriginal"]) : orderItemModel.Subtotal;

            // Validar el stock antes de continuar
            var producto = await _context.Products.FindAsync(orderItemModel.ProductId);
            
            if (producto == null)
            {
                ModelState.AddModelError("ProductId", "El producto seleccionado no existe.");
                ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
                ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
                return View(orderItemModel);
            }
            
            // Verificar si el producto está disponible
            if (!producto.Disponible)
            {
                ModelState.AddModelError("ProductId", "El producto seleccionado no está disponible para la venta.");
                ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
                ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
                return View(orderItemModel);
            }
            
            // Calcular cuántas unidades adicionales se están solicitando
            int diferenciaCantidad = orderItemModel.Cantidad - cantidadOriginal;
            
            // Si se están solicitando más unidades, verificar el stock
            if (diferenciaCantidad > 0 && diferenciaCantidad > producto.Stock)
            {
                ModelState.AddModelError("Cantidad", $"Stock insuficiente. Solo hay {producto.Stock} unidades adicionales disponibles.");
                ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
                ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
                return View(orderItemModel);
            }
            
            // Calcular automáticamente el subtotal basado en el precio del producto
            orderItemModel.Subtotal = producto.Precio * orderItemModel.Cantidad;

            if (ModelState.IsValid)
            {
                try
                {
                    // Actualizar el elemento del pedido
                    _context.Update(orderItemModel);
                    
                    // Actualizar el stock del producto
                    if (diferenciaCantidad != 0)
                    {
                        producto.Stock -= diferenciaCantidad;
                        _context.Update(producto);
                    }
                    
                    // Actualizar el total del pedido
                    var orden = await _context.Orders.FindAsync(orderItemModel.OrderId);
                    if (orden != null)
                    {
                        orden.Total = orden.Total - subtotalOriginal + orderItemModel.Subtotal;
                        _context.Update(orden);
                    }
                    
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Elemento de pedido actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderItemModelExists(orderItemModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Orders", new { id = orderItemModel.OrderId });
            }
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
            return View(orderItemModel);
        }

        // GET: OrderItem/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderItemModel = await _context.OrderItems
                .Include(o => o.Order)
                .Include(o => o.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (orderItemModel == null)
            {
                return NotFound();
            }

            return View(orderItemModel);
        }

        // POST: OrderItem/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderItemModel = await _context.OrderItems
                .Include(o => o.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (orderItemModel != null)
            {
                // Guardar el ID del pedido para redirigir después
                int orderId = orderItemModel.OrderId;
                
                // Restaurar el stock del producto
                var producto = orderItemModel.Producto;
                if (producto != null)
                {
                    producto.Stock += orderItemModel.Cantidad;
                    _context.Update(producto);
                }
                
                // Actualizar el total del pedido
                var orden = await _context.Orders.FindAsync(orderItemModel.OrderId);
                if (orden != null)
                {
                    orden.Total -= orderItemModel.Subtotal;
                    _context.Update(orden);
                }
                
                // Eliminar el elemento del pedido
                _context.OrderItems.Remove(orderItemModel);
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Elemento de pedido eliminado correctamente.";
                
                // Redirigir a la página de detalles del pedido
                return RedirectToAction("Details", "Orders", new { id = orderId });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrderItemModelExists(int id)
        {
            return _context.OrderItems.Any(e => e.Id == id);
        }
    }
}

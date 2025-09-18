using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Aplicacion_pedidos.Data;
using Aplicacion_pedidos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Aplicacion_pedidos.Controllers
{
    [Authorize]
    public class OrderItemController : Controller
    {
        private readonly PedidosDBContext _context;
        private readonly ILogger<OrderItemController> _logger;

        public OrderItemController(PedidosDBContext context, ILogger<OrderItemController> logger)
        {
            _context = context;
            _logger = logger;
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
            _logger.LogInformation("Iniciando Create GET con orderId: {OrderId}", orderId);

            try
            {
                var orderItem = new OrderItemModel { Cantidad = 1 };
                
                if (orderId.HasValue)
                {
                    _logger.LogInformation("OrderId proporcionado: {OrderId}", orderId.Value);
                    orderItem.OrderId = orderId.Value;
                    
                    // Cargar solo el pedido específico
                    var pedido = _context.Orders.Find(orderId.Value);
                    if (pedido != null)
                    {
                        ViewData["OrderId"] = new SelectList(new List<OrderModel> { pedido }, "Id", "Id", orderId.Value);
                    }
                    else
                    {
                        ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id");
                    }
                }
                else
                {
                    ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id");
                }

                // Solo mostrar productos disponibles y con stock
                var productos = _context.Products.Where(p => p.Disponible && p.Stock > 0).ToList();
                _logger.LogInformation("Encontrados {ProductCount} productos disponibles con stock", productos.Count);
                ViewData["ProductId"] = new SelectList(productos, "Id", "Nombre");

                return View(orderItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar la vista Create");
                TempData["ErrorMessage"] = "Error al cargar la página: " + ex.Message;
                return RedirectToAction("Index", "Orders");
            }
        }

        // POST: OrderItem/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int OrderId, int ProductId, int Cantidad)
        {
            _logger.LogInformation("Iniciando Create POST con OrderId: {OrderId}, ProductId: {ProductId}, Cantidad: {Cantidad}", 
                OrderId, ProductId, Cantidad);

            // Validación básica
            if (OrderId <= 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un pedido válido";
                return RedirectToAction(nameof(Create), new { orderId = OrderId });
            }

            if (ProductId <= 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un producto válido";
                return RedirectToAction(nameof(Create), new { orderId = OrderId });
            }

            if (Cantidad <= 0)
            {
                TempData["ErrorMessage"] = "La cantidad debe ser mayor que cero";
                return RedirectToAction(nameof(Create), new { orderId = OrderId });
            }

            try
            {
                // Validar el stock antes de continuar
                var producto = await _context.Products.FindAsync(ProductId);
                if (producto == null)
                {
                    TempData["ErrorMessage"] = "El producto seleccionado no existe.";
                    return RedirectToAction(nameof(Create), new { orderId = OrderId });
                }

                // Verificar si el producto está disponible
                if (!producto.Disponible)
                {
                    TempData["ErrorMessage"] = "El producto seleccionado no está disponible para la venta.";
                    return RedirectToAction(nameof(Create), new { orderId = OrderId });
                }

                // Verificar el stock disponible
                if (Cantidad > producto.Stock)
                {
                    TempData["ErrorMessage"] = $"Stock insuficiente. Solo hay {producto.Stock} unidades disponibles.";
                    return RedirectToAction(nameof(Create), new { orderId = OrderId });
                }

                // Verificar que el pedido existe
                var orden = await _context.Orders.FindAsync(OrderId);
                if (orden == null)
                {
                    TempData["ErrorMessage"] = "El pedido seleccionado no existe.";
                    return RedirectToAction("Index", "Orders");
                }

                // Crear el elemento de pedido
                var orderItem = new OrderItemModel
                {
                    OrderId = OrderId,
                    ProductId = ProductId,
                    Cantidad = Cantidad,
                    Subtotal = producto.Precio * Cantidad
                };

                // Guardar el elemento de pedido
                _context.Add(orderItem);
                
                // Actualizar el stock del producto
                producto.Stock -= Cantidad;
                _context.Update(producto);

                // Actualizar el total del pedido
                orden.Total += orderItem.Subtotal;
                _context.Update(orden);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Elemento de pedido creado con éxito: OrderId={OrderId}, ProductId={ProductId}, Cantidad={Cantidad}, Subtotal={Subtotal}", 
                    orderItem.OrderId, orderItem.ProductId, orderItem.Cantidad, orderItem.Subtotal);
                
                TempData["SuccessMessage"] = "Elemento de pedido agregado correctamente.";
                
                // Redirigir a la página de detalles del pedido
                return RedirectToAction("Details", "Orders", new { id = OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear elemento de pedido");
                TempData["ErrorMessage"] = "Error al crear el elemento de pedido: " + ex.Message;
                return RedirectToAction(nameof(Create), new { orderId = OrderId });
            }
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderId,ProductId,Cantidad")] OrderItemModel orderItemModel, int cantidadOriginal)
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
                Convert.ToDecimal(TempData["SubtotalOriginal"]) : 0;

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

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
            try
            {
                _logger.LogInformation("Obteniendo lista de elementos de pedido");
                var pedidosDBContext = _context.OrderItems
                    .Include(o => o.Order)
                    .Include(o => o.Producto);
                return View(await pedidosDBContext.ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de elementos de pedido");
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar la lista de elementos de pedido. Por favor, inténtelo de nuevo.";
                return View(new List<OrderItemModel>());
            }
        }

        // GET: OrderItem/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de acceder a detalles de elemento de pedido sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de elemento de pedido válido.";
                    return RedirectToAction("Index", "Orders");
                }

                var orderItemModel = await _context.OrderItems
                    .Include(o => o.Order)
                    .Include(o => o.Producto)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (orderItemModel == null)
                {
                    _logger.LogWarning("Elemento de pedido con ID {OrderItemId} no encontrado", id);
                    TempData["ErrorMessage"] = $"No se encontró el elemento de pedido con ID: {id}.";
                    return RedirectToAction("Index", "Orders");
                }

                return View(orderItemModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del elemento de pedido con ID {OrderItemId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar los detalles del elemento de pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction("Index", "Orders");
            }
        }

        // GET: OrderItem/Create
        public IActionResult Create(int? orderId = null)
        {
            try
            {
                _logger.LogInformation("Iniciando Create GET con orderId: {OrderId}", orderId);

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
                        _logger.LogWarning("Pedido con ID {OrderId} no encontrado para añadir elementos", orderId.Value);
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
                
                if (productos.Count == 0)
                {
                    TempData["WarningMessage"] = "No hay productos disponibles con stock. Agregue productos antes de continuar.";
                }
                
                ViewData["ProductId"] = new SelectList(productos, "Id", "Nombre");

                return View(orderItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar la vista Create para elemento de pedido");
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar el formulario de creación de elemento. Por favor, inténtelo de nuevo.";
                return RedirectToAction("Index", "Orders");
            }
        }

        // POST: OrderItem/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int OrderId, int ProductId, int Cantidad)
        {
            try
            {
                _logger.LogInformation("Iniciando Create POST con OrderId: {OrderId}, ProductId: {ProductId}, Cantidad: {Cantidad}", 
                    OrderId, ProductId, Cantidad);

                if (OrderId <= 0)
                {
                    _logger.LogWarning("Intento de crear elemento de pedido sin ID de pedido válido: {OrderId}", OrderId);
                    TempData["ErrorMessage"] = "Debe seleccionar un pedido válido";
                    return RedirectToAction(nameof(Create), new { orderId = OrderId });
                }

                if (ProductId <= 0)
                {
                    _logger.LogWarning("Intento de crear elemento de pedido sin ID de producto válido: {ProductId}", ProductId);
                    TempData["ErrorMessage"] = "Debe seleccionar un producto válido";
                    return RedirectToAction(nameof(Create), new { orderId = OrderId });
                }

                if (Cantidad <= 0)
                {
                    _logger.LogWarning("Intento de crear elemento de pedido con cantidad inválida: {Cantidad}", Cantidad);
                    TempData["ErrorMessage"] = "La cantidad debe ser mayor que cero";
                    return RedirectToAction(nameof(Create), new { orderId = OrderId });
                }

                // Validar el stock antes de continuar
                var producto = await _context.Products.FindAsync(ProductId);
                if (producto == null)
                {
                    _logger.LogWarning("Intento de crear elemento de pedido con producto inexistente: {ProductId}", ProductId);
                    TempData["ErrorMessage"] = "El producto seleccionado no existe.";
                    return RedirectToAction(nameof(Create), new { orderId = OrderId });
                }

                if (!producto.Disponible)
                {
                    _logger.LogWarning("Intento de crear elemento con producto no disponible: ProductId={ProductId}", ProductId);
                    TempData["ErrorMessage"] = "El producto seleccionado no está disponible para la venta.";
                    return RedirectToAction(nameof(Create), new { orderId = OrderId });
                }

                if (Cantidad > producto.Stock)
                {
                    _logger.LogWarning("Intento de crear elemento con cantidad que excede stock: Solicitado={Cantidad}, Disponible={Stock}", 
                        Cantidad, producto.Stock);
                    TempData["ErrorMessage"] = $"Stock insuficiente. Solo hay {producto.Stock} unidades disponibles.";
                    return RedirectToAction(nameof(Create), new { orderId = OrderId });
                }

                var orden = await _context.Orders.FindAsync(OrderId);
                if (orden == null)
                {
                    _logger.LogWarning("Intento de crear elemento para pedido inexistente: {OrderId}", OrderId);
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
                
                return RedirectToAction("Details", "Orders", new { id = OrderId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al crear elemento de pedido: OrderId={OrderId}, ProductId={ProductId}", OrderId, ProductId);
                TempData["ErrorMessage"] = "No se pudo guardar el elemento de pedido. Puede que haya un problema con la conexión a la base de datos.";
                return RedirectToAction(nameof(Create), new { orderId = OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear elemento de pedido: OrderId={OrderId}, ProductId={ProductId}", OrderId, ProductId);
                TempData["ErrorMessage"] = "Ha ocurrido un error inesperado al crear el elemento de pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Create), new { orderId = OrderId });
            }
        }

        // GET: OrderItem/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de editar elemento de pedido sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de elemento de pedido válido.";
                    return RedirectToAction("Index", "Orders");
                }

                var orderItemModel = await _context.OrderItems
                    .Include(o => o.Producto)
                    .Include(o => o.Order)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (orderItemModel == null)
                {
                    _logger.LogWarning("Elemento de pedido con ID {OrderItemId} no encontrado para edición", id);
                    TempData["ErrorMessage"] = $"No se encontró el elemento de pedido con ID: {id}.";
                    return RedirectToAction("Index", "Orders");
                }
                
                // Guardar la cantidad original para calcular la diferencia después
                TempData["CantidadOriginal"] = orderItemModel.Cantidad;
                TempData["SubtotalOriginal"] = orderItemModel.Subtotal;
                
                // Crear el SelectList para el ID del pedido
                var pedido = await _context.Orders.FindAsync(orderItemModel.OrderId);
                if (pedido != null)
                {
                    ViewData["OrderId"] = new SelectList(new List<OrderModel> { pedido }, "Id", "Id", orderItemModel.OrderId);
                }
                else
                {
                    ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
                }
                
                // Crear el SelectList para el ID del producto
                var producto = await _context.Products.FindAsync(orderItemModel.ProductId);
                if (producto != null)
                {
                    ViewData["ProductId"] = new SelectList(new List<ProductModel> { producto }, "Id", "Nombre", orderItemModel.ProductId);
                    
                    int stockDisponibleTotal = producto.Stock + orderItemModel.Cantidad;
                    if (stockDisponibleTotal <= 5)
                    {
                        ViewData["StockWarning"] = $"Atención: Stock disponible limitado ({stockDisponibleTotal} unidades)";
                    }
                }
                else
                {
                    ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
                }
                
                return View(orderItemModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener elemento de pedido con ID {OrderItemId} para edición", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar los datos del elemento para editar. Por favor, inténtelo de nuevo.";
                return RedirectToAction("Index", "Orders");
            }
        }

        // POST: OrderItem/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderId,ProductId,Cantidad")] OrderItemModel orderItemModel, int cantidadOriginal)
        {
            try
            {
                if (id != orderItemModel.Id)
                {
                    _logger.LogWarning("ID de elemento de pedido no coincide en edición: recibido {ReceivedId}, esperado {ExpectedId}", 
                        orderItemModel.Id, id);
                    TempData["ErrorMessage"] = "ID de elemento de pedido no válido.";
                    return RedirectToAction("Index", "Orders");
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
                    _logger.LogWarning("Intento de editar elemento con producto inexistente: {ProductId}", orderItemModel.ProductId);
                    ModelState.AddModelError("ProductId", "El producto seleccionado no existe.");
                    ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
                    ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
                    return View(orderItemModel);
                }
                
                // Verificar si el producto está disponible
                if (!producto.Disponible)
                {
                    _logger.LogWarning("Intento de editar elemento con producto no disponible: ProductId={ProductId}", orderItemModel.ProductId);
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
                    _logger.LogWarning("Intento de editar elemento con cantidad que excede stock: Solicitado adicional={Diferencia}, Disponible={Stock}", 
                        diferenciaCantidad, producto.Stock);
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
                        _logger.LogInformation("Elemento de pedido actualizado exitosamente: ID={OrderItemId}, Nueva cantidad={Cantidad}", 
                            orderItemModel.Id, orderItemModel.Cantidad);
                        TempData["SuccessMessage"] = "Elemento de pedido actualizado correctamente.";
                        return RedirectToAction("Details", "Orders", new { id = orderItemModel.OrderId });
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!OrderItemExists(orderItemModel.Id))
                        {
                            _logger.LogWarning("Intento de actualizar elemento de pedido no existente: ID {OrderItemId}", orderItemModel.Id);
                            TempData["ErrorMessage"] = $"El elemento de pedido con ID {orderItemModel.Id} ya no existe.";
                            return RedirectToAction("Index", "Orders");
                        }
                        else
                        {
                            _logger.LogError(ex, "Error de concurrencia al actualizar elemento de pedido: ID {OrderItemId}", orderItemModel.Id);
                            ModelState.AddModelError("", "El registro fue modificado por otro usuario. Por favor, actualice la página e intente de nuevo.");
                        }
                    }
                    catch (DbUpdateException ex)
                    {
                        _logger.LogError(ex, "Error de base de datos al actualizar elemento de pedido: ID {OrderItemId}", orderItemModel.Id);
                        ModelState.AddModelError("", "No se pudo guardar los cambios. Puede que haya un problema con la conexión a la base de datos.");
                    }
                }
                
                ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
                ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
                return View(orderItemModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar elemento de pedido: ID {OrderItemId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error inesperado al actualizar el elemento de pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction("Index", "Orders");
            }
        }

        // GET: OrderItem/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de eliminar elemento de pedido sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de elemento de pedido válido.";
                    return RedirectToAction("Index", "Orders");
                }

                var orderItemModel = await _context.OrderItems
                    .Include(o => o.Order)
                    .Include(o => o.Producto)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (orderItemModel == null)
                {
                    _logger.LogWarning("Elemento de pedido con ID {OrderItemId} no encontrado para eliminación", id);
                    TempData["ErrorMessage"] = $"No se encontró el elemento de pedido con ID: {id}.";
                    return RedirectToAction("Index", "Orders");
                }

                return View(orderItemModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vista de eliminación para elemento de pedido con ID {OrderItemId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar la vista de eliminación. Por favor, inténtelo de nuevo.";
                return RedirectToAction("Index", "Orders");
            }
        }

        // POST: OrderItem/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var orderItemModel = await _context.OrderItems
                    .Include(o => o.Producto)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (orderItemModel == null)
                {
                    _logger.LogWarning("Elemento de pedido con ID {OrderItemId} no encontrado para eliminación confirmada", id);
                    TempData["ErrorMessage"] = $"No se encontró el elemento de pedido con ID: {id}.";
                    return RedirectToAction("Index", "Orders");
                }
                
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
                _logger.LogInformation("Elemento de pedido eliminado correctamente: ID={OrderItemId}, OrderId={OrderId}", id, orderId);
                TempData["SuccessMessage"] = "Elemento de pedido eliminado correctamente.";
                
                // Redirigir a la página de detalles del pedido
                return RedirectToAction("Details", "Orders", new { id = orderId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al eliminar elemento de pedido: ID {OrderItemId}", id);
                TempData["ErrorMessage"] = "No se pudo eliminar el elemento de pedido debido a un problema con la base de datos.";
                return RedirectToAction("Index", "Orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar elemento de pedido: ID {OrderItemId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error inesperado al eliminar el elemento de pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction("Index", "Orders");
            }
        }

        private bool OrderItemExists(int id)
        {
            try
            {
                return _context.OrderItems.Any(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de elemento de pedido: ID {OrderItemId}", id);
                // Devolvemos false para evitar excepciones en los métodos que llaman a esta función
                return false;
            }
        }
    }
}

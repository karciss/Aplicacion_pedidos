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
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Aplicacion_pedidos.Controllers
{
    [Authorize] //Acceso authorizado
    public class OrdersController : Controller
    {
        private readonly PedidosDBContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(PedidosDBContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Obteniendo lista de pedidos");
                var orders = await _context.Orders
                    .Include(o => o.Cliente)
                    .OrderByDescending(o => o.FechaPedido)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de pedidos");
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar la lista de pedidos. Por favor, inténtelo de nuevo.";
                return View(new List<OrderModel>());
            }
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de acceder a detalles de pedido sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de pedido válido.";
                    return RedirectToAction(nameof(Index));
                }

                var orderModel = await _context.Orders
                    .Include(o => o.Cliente)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Producto)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (orderModel == null)
                {
                    _logger.LogWarning("Pedido con ID {OrderId} no encontrado", id);
                    TempData["ErrorMessage"] = $"No se encontró el pedido con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }

                return View(orderModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del pedido con ID {OrderId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar los detalles del pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            try
            {
                _logger.LogInformation("Cargando formulario de creación de pedido");
                
                var clientes = _context.Users.ToList();
                if (!clientes.Any())
                {
                    _logger.LogWarning("Intento de crear pedido sin clientes registrados");
                    TempData["ErrorMessage"] = "No hay clientes registrados. Registre al menos un cliente antes de crear un pedido.";
                    return RedirectToAction("Index", "Users");
                }
                
                // Crear el SelectList con los valores correctos (Id, Nombre) para los usuarios
                ViewData["UserId"] = new SelectList(clientes, "Id", "Nombre");
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la vista de creación de pedido");
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar el formulario de creación de pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Orders/CreateSimple 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSimple(int userId, DateTime fechaPedido, OrderStatus estado)
        {
            try
            {
                _logger.LogInformation("Creando pedido con: UserId={UserId}, FechaPedido={FechaPedido}, Estado={Estado}", 
                    userId, fechaPedido, estado);
                    
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    _logger.LogWarning("Intento de crear pedido con usuario inexistente: UserId={UserId}", userId);
                    TempData["ErrorMessage"] = "El cliente seleccionado no existe.";
                    return RedirectToAction(nameof(Create));
                }
                
                var orderModel = new OrderModel
                {
                    UserId = userId,
                    FechaPedido = fechaPedido,
                    Estado = estado,
                    Total = 0,
                    Items = new List<OrderItemModel>()
                };
                
                _context.Orders.Add(orderModel);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Pedido creado exitosamente con ID: {OrderId}", orderModel.Id);
                TempData["SuccessMessage"] = "Pedido creado correctamente.";
                
                return RedirectToAction(nameof(Details), new { id = orderModel.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al crear pedido: UserId={UserId}", userId);
                TempData["ErrorMessage"] = "No se pudo guardar el pedido. Puede que haya un problema con la conexión a la base de datos.";
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear pedido: {ErrorMessage}", ex.Message);
                TempData["ErrorMessage"] = "Ha ocurrido un error inesperado al crear el pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Create));
            }
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de editar pedido sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de pedido válido.";
                    return RedirectToAction(nameof(Index));
                }

                var orderModel = await _context.Orders
                    .Include(o => o.Cliente)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Producto)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (orderModel == null)
                {
                    _logger.LogWarning("Pedido con ID {OrderId} no encontrado para edición", id);
                    TempData["ErrorMessage"] = $"No se encontró el pedido con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }
                
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "Nombre", orderModel.UserId);
                return View(orderModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pedido con ID {OrderId} para edición", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar los datos del pedido para editar. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderModel orderModel)
        {
            try
            {
                if (id != orderModel.Id)
                {
                    _logger.LogWarning("ID de pedido no coincide en edición: recibido {ReceivedId}, esperado {ExpectedId}", 
                        orderModel.Id, id);
                    TempData["ErrorMessage"] = "ID de pedido no válido.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Iniciando edición de pedido ID={OrderId}, Total recibido={Total}", id, orderModel.Total);

                var originalOrder = await _context.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Producto)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (originalOrder != null)
                {
                    orderModel.Total = originalOrder.Total;
                    orderModel.Items = originalOrder.Items;
                    _logger.LogInformation("Usando total original: {Total}", orderModel.Total);
                }

                try
                {
                    
                    if (ModelState.IsValid)
                    {
                        _logger.LogInformation("Modelo válido, actualizando pedido");
                        _context.Update(orderModel);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Pedido actualizado correctamente.";
                        return RedirectToAction(nameof(Details), new { id = orderModel.Id });
                    }
                    else
                    {
                        
                        foreach (var error in ModelState)
                        {
                            if (error.Value.Errors.Any())
                            {
                                _logger.LogWarning("Error de validación: {Field} - {ErrorMessages}", 
                                    error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                            }
                        }
                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!OrderModelExists(orderModel.Id))
                    {
                        _logger.LogWarning("Intento de actualizar pedido no existente: ID {OrderId}", orderModel.Id);
                        TempData["ErrorMessage"] = $"El pedido con ID {orderModel.Id} ya no existe.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogError(ex, "Error de concurrencia al actualizar pedido: ID {OrderId}", orderModel.Id);
                        ModelState.AddModelError("", "El registro fue modificado por otro usuario. Por favor, actualice la página e intente de nuevo.");
                    }
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error de base de datos al actualizar pedido: ID {OrderId}", orderModel.Id);
                    ModelState.AddModelError("", "No se pudo guardar los cambios. Puede que haya un problema con la conexión a la base de datos.");
                }
                
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "Nombre", orderModel.UserId);
                
                orderModel = await _context.Orders
                    .Include(o => o.Cliente)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Producto)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                return View(orderModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar pedido: ID {OrderId}", orderModel.Id);
                TempData["ErrorMessage"] = "Ha ocurrido un error inesperado al actualizar el pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Edit), new { id = orderModel.Id });
            }
        }

        // POST: Orders/EditBasic/5 - Método simplificado para editar pedidos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBasic(int id, int userId, DateTime fechaPedido, OrderStatus estado)
        {
            try
            {
                _logger.LogInformation("Editando pedido con método simplificado: ID={OrderId}, UserId={UserId}, FechaPedido={FechaPedido}, Estado={Estado}",
                    id, userId, fechaPedido, estado);
                    
                // Verificar que el usuario existe
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    _logger.LogWarning("Intento de actualizar pedido con usuario inexistente: UserId={UserId}", userId);
                    TempData["ErrorMessage"] = "El cliente seleccionado no existe.";
                    return RedirectToAction(nameof(Edit), new { id });
                }
                
                // Obtener el pedido original con todos sus elementos
                var orderModel = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);
                    
                if (orderModel == null)
                {
                    _logger.LogWarning("Pedido con ID {OrderId} no encontrado para actualización simplificada", id);
                    TempData["ErrorMessage"] = $"No se encontró el pedido con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Actualizar solo los campos necesarios
                orderModel.UserId = userId;
                orderModel.FechaPedido = fechaPedido;
                orderModel.Estado = estado;
                // No tocar el Total ni los Items
                
                _context.Update(orderModel);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Pedido actualizado exitosamente con método simplificado: ID={OrderId}", id);
                TempData["SuccessMessage"] = "Pedido actualizado correctamente.";
                
                return RedirectToAction(nameof(Details), new { id = orderModel.Id });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!OrderModelExists(id))
                {
                    _logger.LogWarning("Intento de actualizar pedido no existente: ID {OrderId}", id);
                    TempData["ErrorMessage"] = $"El pedido con ID {id} ya no existe.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogError(ex, "Error de concurrencia al actualizar pedido con método simplificado: ID {OrderId}", id);
                    TempData["ErrorMessage"] = "El registro fue modificado por otro usuario. Por favor, actualice la página e intente de nuevo.";
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al actualizar pedido con método simplificado: ID {OrderId}", id);
                TempData["ErrorMessage"] = "No se pudo guardar los cambios. Puede que haya un problema con la conexión a la base de datos.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al actualizar pedido con método simplificado: ID {OrderId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error inesperado al actualizar el pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de eliminar pedido sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de pedido válido.";
                    return RedirectToAction(nameof(Index));
                }

                var orderModel = await _context.Orders
                    .Include(o => o.Cliente)
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(m => m.Id == id);
                
                if (orderModel == null)
                {
                    _logger.LogWarning("Pedido con ID {OrderId} no encontrado para eliminación", id);
                    TempData["ErrorMessage"] = $"No se encontró el pedido con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si el pedido tiene elementos antes de permitir eliminarlo
                if (orderModel.Items != null && orderModel.Items.Any())
                {
                    _logger.LogWarning("Intento de eliminar pedido con elementos asociados: ID {OrderId}", id);
                    TempData["ErrorMessage"] = "No se puede eliminar el pedido porque contiene elementos. Elimine primero los elementos del pedido.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                return View(orderModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vista de eliminación para pedido con ID {OrderId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar la vista de eliminación. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var orderModel = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (orderModel == null)
                {
                    _logger.LogWarning("Pedido con ID {OrderId} no encontrado para eliminación confirmada", id);
                    TempData["ErrorMessage"] = $"No se encontró el pedido con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si el pedido tiene elementos antes de permitir eliminarlo
                if (orderModel.Items != null && orderModel.Items.Any())
                {
                    _logger.LogWarning("Intento de eliminar pedido con elementos asociados: ID {OrderId}", id);
                    TempData["ErrorMessage"] = "No se puede eliminar el pedido porque contiene elementos. Elimine primero los elementos del pedido.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                
                _context.Orders.Remove(orderModel);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Pedido eliminado correctamente: ID {OrderId}", id);
                TempData["SuccessMessage"] = "Pedido eliminado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al eliminar pedido: ID {OrderId}", id);
                TempData["ErrorMessage"] = "No se pudo eliminar el pedido debido a referencias en la base de datos.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar pedido: ID {OrderId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error inesperado al eliminar el pedido. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool OrderModelExists(int id)
        {
            try
            {
                return _context.Orders.Any(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de pedido: ID {OrderId}", id);
                return false;
            }
        }
    }
}

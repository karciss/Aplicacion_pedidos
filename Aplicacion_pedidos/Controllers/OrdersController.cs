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
    [Authorize]
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
            // Simplificado - sin filtros
            var orders = await _context.Orders
                .Include(o => o.Cliente)
                .OrderByDescending(o => o.FechaPedido)
                .ToListAsync();

            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderModel = await _context.Orders
                .Include(o => o.Cliente)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (orderModel == null)
            {
                return NotFound();
            }

            return View(orderModel);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            // Asegurarse de que hay clientes disponibles
            var clientes = _context.Users.ToList();
            if (!clientes.Any())
            {
                TempData["ErrorMessage"] = "No hay clientes registrados. Registre al menos un cliente antes de crear un pedido.";
                return RedirectToAction("Index", "Users");
            }
            
            // Crear el SelectList con los valores correctos (Id, Nombre) para los usuarios
            ViewData["UserId"] = new SelectList(clientes, "Id", "Nombre");
            
            return View();
        }

        // POST: Orders/CreateSimple - Método simplificado para crear pedidos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSimple(int userId, DateTime fechaPedido, OrderStatus estado)
        {
            _logger.LogInformation("Creando pedido con: UserId={UserId}, FechaPedido={FechaPedido}, Estado={Estado}", 
                userId, fechaPedido, estado);
                
            try
            {
                // Verificar que el usuario existe
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear pedido: {ErrorMessage}", ex.Message);
                TempData["ErrorMessage"] = $"Error al crear el pedido: {ex.Message}";
                return RedirectToAction(nameof(Create));
            }
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Cargar el pedido con sus elementos y productos relacionados
            var orderModel = await _context.Orders
                .Include(o => o.Cliente)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (orderModel == null)
            {
                return NotFound();
            }
            
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Nombre", orderModel.UserId);
            return View(orderModel);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderModel orderModel)
        {
            if (id != orderModel.Id)
            {
                return NotFound();
            }

            _logger.LogInformation("Iniciando edición de pedido ID={OrderId}, Total recibido={Total}", id, orderModel.Total);

            // Recuperar el pedido original para mantener el total y los elementos
            var originalOrder = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (originalOrder != null)
            {
                // Usar el total del pedido original
                orderModel.Total = originalOrder.Total;
                orderModel.Items = originalOrder.Items;
                _logger.LogInformation("Usando total original: {Total}", orderModel.Total);
            }

            try
            {
                // Comprobar si el modelo es válido
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
                    // Registrar errores de validación
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar pedido: {ErrorMessage}", ex.Message);
                TempData["ErrorMessage"] = $"Error al actualizar el pedido: {ex.Message}";
            }
            
            // Si llegamos aquí, algo falló. Recargamos las relaciones y volvemos a la vista
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Nombre", orderModel.UserId);
            
            // Asegurarse de cargar los elementos del pedido para mostrarlos en la vista
            orderModel = await _context.Orders
                .Include(o => o.Cliente)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            return View(orderModel);
        }

        // POST: Orders/EditBasic/5 - Método simplificado para editar pedidos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBasic(int id, int userId, DateTime fechaPedido, OrderStatus estado)
        {
            _logger.LogInformation("Editando pedido con método simplificado: ID={OrderId}, UserId={UserId}, FechaPedido={FechaPedido}, Estado={Estado}",
                id, userId, fechaPedido, estado);
                
            try
            {
                // Verificar que el usuario existe
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    TempData["ErrorMessage"] = "El cliente seleccionado no existe.";
                    return RedirectToAction(nameof(Edit), new { id });
                }
                
                // Obtener el pedido original con todos sus elementos
                var orderModel = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);
                    
                if (orderModel == null)
                {
                    return NotFound();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar pedido con método simplificado: {ErrorMessage}", ex.Message);
                TempData["ErrorMessage"] = $"Error al actualizar el pedido: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderModel = await _context.Orders
                .Include(o => o.Cliente)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (orderModel == null)
            {
                return NotFound();
            }

            // Verificar si el pedido tiene elementos antes de permitir eliminarlo
            if (orderModel.Items != null && orderModel.Items.Any())
            {
                TempData["ErrorMessage"] = "No se puede eliminar el pedido porque contiene elementos. Elimine primero los elementos del pedido.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            return View(orderModel);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderModel = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (orderModel != null)
            {
                // Verificar si el pedido tiene elementos antes de permitir eliminarlo
                if (orderModel.Items != null && orderModel.Items.Any())
                {
                    TempData["ErrorMessage"] = "No se puede eliminar el pedido porque contiene elementos. Elimine primero los elementos del pedido.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                
                _context.Orders.Remove(orderModel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Pedido eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrderModelExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}

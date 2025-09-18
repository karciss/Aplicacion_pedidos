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
        public async Task<IActionResult> Index(int? clienteId, string estado, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            // Iniciar la consulta base
            var query = _context.Orders
                .Include(o => o.Cliente)
                .AsQueryable();

            // Aplicar filtros si se proporcionan
            if (clienteId.HasValue && clienteId.Value > 0)
            {
                query = query.Where(o => o.UserId == clienteId.Value);
                ViewBag.ClienteSeleccionado = clienteId.Value;
            }

            if (!string.IsNullOrEmpty(estado))
            {
                if (Enum.TryParse<OrderStatus>(estado, out var statusEnum))
                {
                    query = query.Where(o => o.Estado == statusEnum);
                    ViewBag.EstadoSeleccionado = estado;
                }
            }

            if (fechaDesde.HasValue)
            {
                query = query.Where(o => o.FechaPedido >= fechaDesde.Value);
                ViewBag.FechaDesde = fechaDesde.Value.ToString("yyyy-MM-dd");
            }

            if (fechaHasta.HasValue)
            {
                // Ajustar al final del día para incluir todas las órdenes del día seleccionado
                var fechaHastaFinal = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.FechaPedido <= fechaHastaFinal);
                ViewBag.FechaHasta = fechaHasta.Value.ToString("yyyy-MM-dd");
            }

            // Ordenar por fecha más reciente primero
            query = query.OrderByDescending(o => o.FechaPedido);

            // Preparar datos para los filtros
            ViewBag.Clientes = new SelectList(await _context.Users
                .OrderBy(u => u.Nombre)
                .Select(u => new { u.Id, NombreCompleto = $"{u.Nombre} ({u.Email})" })
                .ToListAsync(), "Id", "NombreCompleto", clienteId);

            return View(await query.ToListAsync());
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
            
            ViewData["UserId"] = new SelectList(clientes, "Id", "Nombre");
            
            // Inicializar con valores predeterminados
            var orderModel = new OrderModel
            {
                FechaPedido = DateTime.Now,
                Estado = OrderStatus.Pendiente,
                Total = 0
            };
            
            return View(orderModel);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderModel orderModel)
        {
            _logger.LogInformation("Intentando crear un nuevo pedido: {OrderDetails}", 
                $"UserId: {orderModel.UserId}, FechaPedido: {orderModel.FechaPedido}, Estado: {orderModel.Estado}");
            
            // Verificar el UserId
            if (orderModel.UserId <= 0)
            {
                ModelState.AddModelError("UserId", "Debe seleccionar un cliente válido");
                _logger.LogWarning("UserId inválido en la creación de pedido: {UserId}", orderModel.UserId);
            }

            // Asegurar que se establece la fecha si no viene en la petición
            if (orderModel.FechaPedido == default)
            {
                orderModel.FechaPedido = DateTime.Now;
            }
            
            // Asegurar que el total comienza en 0
            orderModel.Total = 0;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(orderModel);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Pedido creado exitosamente con ID: {OrderId}", orderModel.Id);
                    TempData["SuccessMessage"] = "Pedido creado correctamente.";
                    
                    return RedirectToAction(nameof(Details), new { id = orderModel.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear pedido: {ErrorMessage}", ex.Message);
                    ModelState.AddModelError(string.Empty, $"Error al crear el pedido: {ex.Message}");
                }
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
            
            // Si llegamos aquí, algo falló, volver al formulario
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Nombre", orderModel.UserId);
            return View(orderModel);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderModel = await _context.Orders.FindAsync(id);
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

            // Recuperar el pedido original para mantener el total
            var originalOrder = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            if (originalOrder != null)
            {
                orderModel.Total = originalOrder.Total;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderModel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Pedido actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderModelExists(orderModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = orderModel.Id });
            }
            
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Nombre", orderModel.UserId);
            return View(orderModel);
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

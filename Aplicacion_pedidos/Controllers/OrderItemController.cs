using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Aplicacion_pedidos.Data;
using Aplicacion_pedidos.Models;

namespace Aplicacion_pedidos.Controllers
{
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
        public IActionResult Create()
        {
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id");
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre");
            return View();
        }

        // POST: OrderItem/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,OrderId,ProductId,Cantidad,Subtotal")] OrderItemModel orderItemModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(orderItemModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
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
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderItemModel.OrderId);
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Nombre", orderItemModel.ProductId);
            return View(orderItemModel);
        }

        // POST: OrderItem/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderId,ProductId,Cantidad,Subtotal")] OrderItemModel orderItemModel)
        {
            if (id != orderItemModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderItemModel);
                    await _context.SaveChangesAsync();
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
                return RedirectToAction(nameof(Index));
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
            var orderItemModel = await _context.OrderItems.FindAsync(id);
            if (orderItemModel != null)
            {
                _context.OrderItems.Remove(orderItemModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderItemModelExists(int id)
        {
            return _context.OrderItems.Any(e => e.Id == id);
        }
    }
}

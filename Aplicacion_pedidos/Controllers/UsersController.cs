using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Aplicacion_pedidos.Data;
using Aplicacion_pedidos.Models;
using Aplicacion_pedidos.Filters;
using Microsoft.AspNetCore.Authorization;

namespace Aplicacion_pedidos.Controllers
{
    [Authorize]  
    public class UsersController : Controller
    {
        private readonly PedidosDBContext _context;

        public UsersController(PedidosDBContext context)
        {
            _context = context;
        }

        // GET: Users
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]  
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userModel = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userModel == null)
            {
                return NotFound();
            }

            return View(userModel);
        }

        // GET: Users/Create
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Email,Password,Rol")] UserModel userModel)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == userModel.Email))
                {
                    ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
                    return View(userModel);
                }

                // Validate role is one of the allowed roles
                if (userModel.Rol != UserModel.ROLE_ADMIN && 
                    userModel.Rol != UserModel.ROLE_CLIENTE && 
                    userModel.Rol != UserModel.ROLE_EMPLEADO)
                {
                    ModelState.AddModelError("Rol", "El rol seleccionado no es válido.");
                    return View(userModel);
                }

                _context.Add(userModel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Usuario creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(userModel);
        }

        // GET: Users/Edit/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userModel = await _context.Users.FindAsync(id);
            if (userModel == null)
            {
                return NotFound();
            }
            return View(userModel);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Email,Password,Rol")] UserModel userModel)
        {
            if (id != userModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists (excluding current user)
                    if (await _context.Users.AnyAsync(u => u.Email == userModel.Email && u.Id != userModel.Id))
                    {
                        ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
                        return View(userModel);
                    }

                    // Validate role is one of the allowed roles
                    if (userModel.Rol != UserModel.ROLE_ADMIN && 
                        userModel.Rol != UserModel.ROLE_CLIENTE && 
                        userModel.Rol != UserModel.ROLE_EMPLEADO)
                    {
                        ModelState.AddModelError("Rol", "El rol seleccionado no es válido.");
                        return View(userModel);
                    }

                    // Check if this is the last admin and trying to change role
                    var originalUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                    if (originalUser.Rol == UserModel.ROLE_ADMIN && 
                        userModel.Rol != UserModel.ROLE_ADMIN && 
                        await _context.Users.CountAsync(u => u.Rol == UserModel.ROLE_ADMIN) <= 1)
                    {
                        ModelState.AddModelError("Rol", "No se puede cambiar el rol del último administrador.");
                        return View(userModel);
                    }

                    _context.Update(userModel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserModelExists(userModel.Id))
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
            return View(userModel);
        }

        // GET: Users/Delete/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userModel = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userModel == null)
            {
                return NotFound();
            }

            return View(userModel);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userModel = await _context.Users.FindAsync(id);
            if (userModel != null)
            {
                // Prevent deleting the last admin
                if (userModel.Rol == UserModel.ROLE_ADMIN && 
                    await _context.Users.CountAsync(u => u.Rol == UserModel.ROLE_ADMIN) <= 1)
                {
                    TempData["ErrorMessage"] = "No se puede eliminar el último administrador del sistema.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(userModel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Usuario eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserModelExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}

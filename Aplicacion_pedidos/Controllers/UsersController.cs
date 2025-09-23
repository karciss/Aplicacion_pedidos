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
using Microsoft.Extensions.Logging;

namespace Aplicacion_pedidos.Controllers
{
    [Authorize]  
    public class UsersController : Controller
    {
        private readonly PedidosDBContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(PedidosDBContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Users
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]  
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Obteniendo lista de usuarios");
                var users = await _context.Users.ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de usuarios");
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar la lista de usuarios. Por favor, inténtelo de nuevo.";
                return View(new List<UserModel>());
            }
        }

        // GET: Users/Details/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de acceder a detalles de usuario sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de usuario válido.";
                    return RedirectToAction(nameof(Index));
                }

                var userModel = await _context.Users
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (userModel == null)
                {
                    _logger.LogWarning("Usuario con ID {UserId} no encontrado", id);
                    TempData["ErrorMessage"] = $"No se encontró el usuario con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }

                return View(userModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del usuario con ID {UserId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar los detalles del usuario. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Users/Create
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public IActionResult Create()
        {
            try
            {
                _logger.LogInformation("Cargando formulario de creación de usuario");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la vista de creación de usuario");
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar el formulario de creación de usuario. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Email,Password,Rol")] UserModel userModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (await _context.Users.AnyAsync(u => u.Email == userModel.Email))
                    {
                        ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
                        return View(userModel);
                    }

                    if (userModel.Rol != UserModel.ROLE_ADMIN && 
                        userModel.Rol != UserModel.ROLE_CLIENTE && 
                        userModel.Rol != UserModel.ROLE_EMPLEADO)
                    {
                        ModelState.AddModelError("Rol", "El rol seleccionado no es válido.");
                        return View(userModel);
                    }

                    _context.Add(userModel);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Usuario creado correctamente: {UserName}, {UserEmail}", userModel.Nombre, userModel.Email);
                    TempData["SuccessMessage"] = "Usuario creado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                return View(userModel);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al crear usuario: {UserName}, {UserEmail}", userModel.Nombre, userModel.Email);
                ModelState.AddModelError("", "No se pudo guardar los cambios. Puede que haya un problema con el correo electrónico o con la conexión a la base de datos.");
                return View(userModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear usuario: {UserName}, {UserEmail}", userModel.Nombre, userModel.Email);
                ModelState.AddModelError("", "Ha ocurrido un error inesperado al crear el usuario. Por favor, inténtelo de nuevo.");
                return View(userModel);
            }
        }

        // GET: Users/Edit/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de editar usuario sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de usuario válido.";
                    return RedirectToAction(nameof(Index));
                }

                var userModel = await _context.Users.FindAsync(id);
                if (userModel == null)
                {
                    _logger.LogWarning("Usuario con ID {UserId} no encontrado para edición", id);
                    TempData["ErrorMessage"] = $"No se encontró el usuario con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }
                return View(userModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario con ID {UserId} para edición", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar los datos del usuario para editar. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Email,Password,Rol")] UserModel userModel)
        {
            try
            {
                if (id != userModel.Id)
                {
                    _logger.LogWarning("ID de usuario no coincide en edición: recibido {ReceivedId}, esperado {ExpectedId}", 
                        userModel.Id, id);
                    TempData["ErrorMessage"] = "ID de usuario no válido.";
                    return RedirectToAction(nameof(Index));
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        if (await _context.Users.AnyAsync(u => u.Email == userModel.Email && u.Id != userModel.Id))
                        {
                            ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
                            return View(userModel);
                        }

                        if (userModel.Rol != UserModel.ROLE_ADMIN && 
                            userModel.Rol != UserModel.ROLE_CLIENTE && 
                            userModel.Rol != UserModel.ROLE_EMPLEADO)
                        {
                            ModelState.AddModelError("Rol", "El rol seleccionado no es válido.");
                            return View(userModel);
                        }

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
                        _logger.LogInformation("Usuario actualizado correctamente: ID {UserId}, {UserName}", userModel.Id, userModel.Nombre);
                        TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!UserModelExists(userModel.Id))
                        {
                            _logger.LogWarning("Intento de actualizar usuario no existente: ID {UserId}", userModel.Id);
                            TempData["ErrorMessage"] = $"El usuario con ID {userModel.Id} ya no existe.";
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            _logger.LogError(ex, "Error de concurrencia al actualizar usuario: ID {UserId}", userModel.Id);
                            ModelState.AddModelError("", "El registro fue modificado por otro usuario. Por favor, actualice la página e intente de nuevo.");
                            return View(userModel);
                        }
                    }
                    catch (DbUpdateException ex)
                    {
                        _logger.LogError(ex, "Error de base de datos al actualizar usuario: ID {UserId}", userModel.Id);
                        ModelState.AddModelError("", "No se pudo guardar los cambios. Puede que haya un problema con el correo electrónico o con la conexión a la base de datos.");
                        return View(userModel);
                    }
                }
                return View(userModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar usuario: ID {UserId}", userModel.Id);
                ModelState.AddModelError("", "Ha ocurrido un error inesperado al actualizar el usuario. Por favor, inténtelo de nuevo.");
                return View(userModel);
            }
        }

        // GET: Users/Delete/5
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Intento de eliminar usuario sin proporcionar ID");
                    TempData["ErrorMessage"] = "Se requiere un ID de usuario válido.";
                    return RedirectToAction(nameof(Index));
                }

                var userModel = await _context.Users
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (userModel == null)
                {
                    _logger.LogWarning("Usuario con ID {UserId} no encontrado para eliminación", id);
                    TempData["ErrorMessage"] = $"No se encontró el usuario con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }

                if (userModel.Rol == UserModel.ROLE_ADMIN && 
                    await _context.Users.CountAsync(u => u.Rol == UserModel.ROLE_ADMIN) <= 1)
                {
                    _logger.LogWarning("Intento de eliminar el último administrador: ID {UserId}", id);
                    TempData["ErrorMessage"] = "No se puede eliminar el último administrador del sistema.";
                    return RedirectToAction(nameof(Index));
                }

                var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
                if (hasOrders)
                {
                    _logger.LogWarning("Intento de eliminar usuario con pedidos asociados: ID {UserId}", id);
                    TempData["ErrorMessage"] = "No se puede eliminar este usuario porque tiene pedidos asociados.";
                    return RedirectToAction(nameof(Index));
                }

                return View(userModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vista de eliminación para usuario con ID {UserId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error al cargar la vista de eliminación. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(UserModel.ROLE_ADMIN)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var userModel = await _context.Users.FindAsync(id);
                if (userModel == null)
                {
                    _logger.LogWarning("Usuario con ID {UserId} no encontrado para eliminación confirmada", id);
                    TempData["ErrorMessage"] = $"No se encontró el usuario con ID: {id}.";
                    return RedirectToAction(nameof(Index));
                }

                if (userModel.Rol == UserModel.ROLE_ADMIN && 
                    await _context.Users.CountAsync(u => u.Rol == UserModel.ROLE_ADMIN) <= 1)
                {
                    _logger.LogWarning("Intento de eliminar el último administrador: ID {UserId}", id);
                    TempData["ErrorMessage"] = "No se puede eliminar el último administrador del sistema.";
                    return RedirectToAction(nameof(Index));
                }

                var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
                if (hasOrders)
                {
                    _logger.LogWarning("Intento de eliminar usuario con pedidos asociados: ID {UserId}", id);
                    TempData["ErrorMessage"] = "No se puede eliminar este usuario porque tiene pedidos asociados.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(userModel);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Usuario eliminado correctamente: ID {UserId}, {UserName}", id, userModel.Nombre);
                TempData["SuccessMessage"] = "Usuario eliminado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al eliminar usuario: ID {UserId}", id);
                TempData["ErrorMessage"] = "No se pudo eliminar el usuario debido a referencias en la base de datos. Es posible que tenga registros asociados.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar usuario: ID {UserId}", id);
                TempData["ErrorMessage"] = "Ha ocurrido un error inesperado al eliminar el usuario. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool UserModelExists(int id)
        {
            try
            {
                return _context.Users.Any(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de usuario: ID {UserId}", id);
                return false;
            }
        }
    }
}

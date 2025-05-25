using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using dosyayonetim.Models;
using dosyayonetim.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using dosyayonetim.Repositories;
using dosyayonetim.DTOs;

namespace dosyayonetim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserStorageRepository _userStorageRepository;
        private readonly IMapper _mapper;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            UserStorageRepository userStorageRepository,
            IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userStorageRepository = userStorageRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets all users in the system
        /// </summary>
        /// <returns>List of all users with their details</returns>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                userDtos.Add(userDto);
            }

            return Ok(userDtos);
        }

        /// <summary>
        /// Updates a user's storage limit
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="updateDto">New storage limit in bytes</param>
        /// <returns>Updated user storage details</returns>
        [HttpPut("users/{userId}/storage")]
        public async Task<IActionResult> UpdateUserStorage(string userId, [FromBody] UserStorageUpdateDTO updateDto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                await _userStorageRepository.UpdateStorageLimitAsync(userId, updateDto.TotalStorage);
                var updatedStorage = await _userStorageRepository.GetByUserIdAsync(userId);
                return Ok(_mapper.Map<UserStorageDTO>(updatedStorage));
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User storage not found");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Assigns admin role to a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Result of the operation</returns>
        [HttpPost("users/{userId}/assign-admin")]
        public async Task<IActionResult> AssignAdminRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (!await _roleManager.RoleExistsAsync(Roles.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            }

            var result = await _userManager.AddToRoleAsync(user, Roles.Admin);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Admin role assigned successfully" });
        }

        /// <summary>
        /// Removes admin role from a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Result of the operation</returns>
        [HttpPost("users/{userId}/remove-admin")]
        public async Task<IActionResult> RemoveAdminRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, Roles.Admin);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Admin role removed successfully" });
        }

        /// <summary>
        /// Gets storage details for all users
        /// </summary>
        /// <returns>List of all users' storage details</returns>
        [HttpGet("storage")]
        public async Task<IActionResult> GetAllUserStorage()
        {
            var userStorages = await _userStorageRepository.GetAllAsync();
            var storageDtos = _mapper.Map<List<UserStorageDTO>>(userStorages);
            return Ok(storageDtos);
        }

        /// <summary>
        /// Gets active user storage details
        /// </summary>
        /// <returns>List of active users' storage details</returns>
        [HttpGet("active-storage")]
        public async Task<IActionResult> GetActiveUserStorage()
        {
            var userStorages = await _userStorageRepository.GetActiveUserStoragesAsync();
            var storageDtos = _mapper.Map<List<UserStorageDTO>>(userStorages);
            return Ok(storageDtos);
        }
    }
} 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using dosyayonetim.Extensions;
using AutoMapper;
using dosyayonetim.Models;
using dosyayonetim.Repositories;
using dosyayonetim.DTOs;

namespace dosyayonetim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _authService = authService;
            _userManager = userManager;
            _mapper = mapper;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="model">User registration details</param>
        /// <returns>Registration result with user details</returns>
        /// <response code="200">Returns the newly created user</response>
        /// <response code="400">If the registration fails</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var result = await _authService.Register(model);
            
            return result.IsSuccess ? 
                Ok(result) : 
                BadRequest(result);
        }

        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="model">Login credentials</param>
        /// <returns>Authentication result with JWT token</returns>
        /// <response code="200">Returns the JWT token</response>
        /// <response code="400">If the login fails</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var result = await _authService.Login(model);
            
            return result.IsSuccess ? 
                Ok(result) : 
                BadRequest(result);
        }

        [HttpPost("register-admin")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterDto model)
        {
            var result = await _authService.Register(model, Roles.Admin);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.FindByNameAsync(User.GetUsername());
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mapper.Map<UserDto>(user);

            return Ok(new
            {
                User = userDto,
                Roles = roles
            });
        }
    }
} 
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using dosyayonetim.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using AutoMapper;
using dosyayonetim.DTOs;

namespace dosyayonetim.Repositories
{
    public interface IAuthService
    {
        Task<AuthResponseDto> Register(RegisterDto model, string role = "User");
        Task<AuthResponseDto> Login(LoginDto model);
        Task<AuthResponseDto> AssignRole(AssignRoleDto model);
    }
    public class AuthRepository : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JWT _jwt;
        private readonly IMapper _mapper;
        private readonly UserStorageRepository _userStorageRepository;

        public AuthRepository(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IMapper mapper,
            UserStorageRepository userStorageRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwt = configuration.GetSection("JWT").Get<JWT>();
            _mapper = mapper;
            _userStorageRepository = userStorageRepository;
        }

        public async Task<AuthResponseDto> Register(RegisterDto model, string role = "User")
        {
            var user = _mapper.Map<ApplicationUser>(model);
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure the role exists
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }

                // Assign role to user
                await _userManager.AddToRoleAsync(user, role);

                // Create user storage with default limit (10GB)
                await _userStorageRepository.CreateUserStorageAsync(user.Id, 10L * 1024 * 1024 * 1024);

                var userDto = _mapper.Map<UserDto>(user);
                return new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = "User registered successfully",
                    User = userDto
                };
            }

            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Registration failed: " + string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        public async Task<AuthResponseDto> AssignRole(AssignRoleDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User not found"
                };
            }

            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Role does not exist"
                };
            }

            var result = await _userManager.AddToRoleAsync(user, model.Role);
            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Failed to assign role: " + string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            var userDto = _mapper.Map<UserDto>(user);
            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = $"Role {model.Role} assigned successfully",
                User = userDto
            };
        }

        public async Task<AuthResponseDto> Login(LoginDto model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid username"
                };
            }

            var result = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!result)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid password"
                };
            }

            var token = await CreateToken(user);
            var userDto = await GetUserDtoAsync(user);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Token = token,
                User = userDto
            };
        }

        private async Task<string> CreateToken(ApplicationUser user)
        {
            var signingCredentials = GetSigningCredentials();
            var claims = await GetClaims(user);
            var tokenOptions = GenerateTokenOptions(signingCredentials, claims);

            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }

        private SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(_jwt.Key);
            var secret = new SymmetricSecurityKey(key);
            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private async Task<List<Claim>> GetClaims(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }

        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var tokenOptions = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwt.DurationInMinutes),
                signingCredentials: signingCredentials);

            return tokenOptions;
        }

        private async Task<UserDto> GetUserDtoAsync(ApplicationUser user)
        {
            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            return userDto;
        }
    }
}
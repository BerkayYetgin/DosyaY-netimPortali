using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dosyayonetim.Models;
using AutoMapper;
using System.Security.Claims;
using dosyayonetim.Repositories;
using dosyayonetim.DTOs;

namespace dosyayonetim.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FolderController : ControllerBase
    {
        private readonly FolderRepository _folderRepository;
        private readonly IMapper _mapper;

        public FolderController(AppDbContext context, IMapper mapper)
        {
            _folderRepository = new FolderRepository(context);
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetFolders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var folders = await _folderRepository.GetRootFoldersAsync(userId);
            var result = _mapper.Map<IEnumerable<FolderDTO>>(folders).ToList();
            var totalSize = await _folderRepository.GetUserTotalFileSizeAsync(userId);
            if (result.Count > 0)
                result[0].UserTotalSize = totalSize;
            else {
                result.Add(new FolderDTO { UserTotalSize = totalSize });
            }
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFolder(int id)
        {
            var folder = await _folderRepository.GetFolderWithContentAsync(id);
            if (folder == null)
                return NotFound();

            if (folder.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            var result = _mapper.Map<FolderWithContentDTO>(folder);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromBody] FolderCreateDTO folderDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var folder = await _folderRepository.CreateFolderAsync(folderDto, userId);
                var result = _mapper.Map<FolderDTO>(folder);
                return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFolder(int id, [FromBody] FolderUpdateDTO folderDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _folderRepository.UpdateFolderAsync(id, folderDto, userId);
                return Ok("Güncellendi");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _folderRepository.DeleteFolderAsync(id, userId);
                return Ok("Silindi");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchFolders([FromQuery] string searchTerm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var folders = await _folderRepository.SearchFoldersAsync(searchTerm, userId);
            var result = _mapper.Map<IEnumerable<FolderDTO>>(folders);
            return Ok(result);
        }

        [HttpGet("{id}/size")]
        public async Task<IActionResult> GetFolderSize(int id)
        {
            try
            {
                var folder = await _folderRepository.GetByIdAsync(id);
                if (folder == null)
                    return NotFound();

                if (folder.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                    return Forbid();

                var size = await _folderRepository.GetFolderSizeAsync(id);
                return Ok(new { Size = size });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
} 
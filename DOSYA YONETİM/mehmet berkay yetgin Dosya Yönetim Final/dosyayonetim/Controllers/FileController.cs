using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dosyayonetim.Models;
using AutoMapper;
using System.Security.Claims;
using System.IO;
using dosyayonetim.Repositories;
using dosyayonetim.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace dosyayonetim.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly FileRepository _fileRepository;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadsPath;

        public FileController(AppDbContext context, IMapper mapper, IWebHostEnvironment environment)
        {
            _fileRepository = new FileRepository(context);
            _mapper = mapper;
            _environment = environment;
            _uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
            
            // Ensure uploads directory exists
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFile(int id)
        {
            var file = await _fileRepository.GetFileWithDetailsAsync(id);
            if (file == null)
                return NotFound();

            if (file.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            var result = _mapper.Map<FileDTO>(file);
            return Ok(result);
        }

        [HttpGet("folder/{folderId}")]
        public async Task<IActionResult> GetFilesByFolder(int folderId)
        {
            var files = await _fileRepository.GetFilesByFolderAsync(folderId);
            var result = _mapper.Map<IEnumerable<FileDTO>>(files);
            return Ok(result);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] int folderId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Create file entity with original name
                var fileEntity = new dosyayonetim.Models.File
                {
                    Name = file.FileName,
                    FileType = file.ContentType,
                    Size = file.Length,
                    FolderId = folderId
                };

                // Upload file to database to get unique name
                var uploadedFile = await _fileRepository.UploadFileAsync(fileEntity, userId);
                
                // Update FilePath with the unique name
                uploadedFile.FilePath = Path.Combine(userId, folderId.ToString(), uploadedFile.Name);
                
                // Save physical file with unique name
                var fullPath = Path.Combine(_uploadsPath, uploadedFile.FilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update the file in database with correct FilePath
                await _fileRepository.UpdateAsync(uploadedFile);

                var result = _mapper.Map<FileDTO>(uploadedFile);
                return CreatedAtAction(nameof(GetFile), new { id = uploadedFile.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                var file = await _fileRepository.GetFileWithDetailsAsync(id);
                if (file == null || file.IsDeleted)
                    return NotFound();

                if (file.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                    return Forbid();

                var fullPath = Path.Combine(_uploadsPath, file.FilePath);
                if (!System.IO.File.Exists(fullPath))
                    return NotFound();

                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                
                // Content-Disposition header'ını düzelt
                var cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = Uri.EscapeDataString(file.Name), // Türkçe karakterler için
                    Inline = false
                };
                Response.Headers.Add("Content-Disposition", cd.ToString());
                Response.Headers.Add("Content-Length", file.Size.ToString());
                Response.Headers.Add("X-Content-Type-Options", "nosniff");

                // MIME type'ı doğru şekilde belirle
                string mimeType = GetMimeType(Path.GetExtension(file.Name));
                if (string.IsNullOrEmpty(mimeType))
                {
                    mimeType = file.FileType;
                }

                return File(fileStream, mimeType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string GetMimeType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";

            extension = extension.ToLower().TrimStart('.');
            
            var mimeTypes = new Dictionary<string, string>
            {
                { "pdf", "application/pdf" },
                { "doc", "application/msword" },
                { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { "xls", "application/vnd.ms-excel" },
                { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { "ppt", "application/vnd.ms-powerpoint" },
                { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
                { "zip", "application/zip" },
                { "rar", "application/x-rar-compressed" },
                { "7z", "application/x-7z-compressed" },
                { "tgz", "application/gzip" },
                { "jpg", "image/jpeg" },
                { "jpeg", "image/jpeg" },
                { "png", "image/png" },
                { "gif", "image/gif" },
                { "txt", "text/plain" }
            };

            return mimeTypes.ContainsKey(extension) ? mimeTypes[extension] : "application/octet-stream";
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileRepository.GetFileWithDetailsAsync(id);
                
                if (file != null)
                {
                    // Delete physical file
                    var fullPath = Path.Combine(_uploadsPath, file.FilePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                await _fileRepository.DeleteFileAsync(id, userId);
                return Ok("Dosya Silindi");
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
        public async Task<IActionResult> SearchFiles([FromQuery] string searchTerm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var files = await _fileRepository.SearchFilesAsync(searchTerm, userId);
            var result = _mapper.Map<IEnumerable<FileDTO>>(files);
            return Ok(result);
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentFiles([FromQuery] int count = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var files = await _fileRepository.GetRecentFilesAsync(userId, count);
            var result = _mapper.Map<IEnumerable<FileDTO>>(files);
            return Ok(result);
        }

        [HttpGet("type/{fileType}")]
        public async Task<IActionResult> GetFilesByType(string fileType)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var files = await _fileRepository.GetFilesByTypeAsync(fileType, userId);
            var result = _mapper.Map<IEnumerable<FileDTO>>(files);
            return Ok(result);
        }

        // Basit dosya indirme endpoint'i
        [AllowAnonymous]
        [HttpGet("simple-download/{id}")]
        public async Task<IActionResult> SimpleDownload(int id)
        {
            var (filePath, fileName, fileType) = await _fileRepository.GetFilePathAndNameByIdAsync(id);
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(fileName))
                return NotFound();

            var fullPath = Path.Combine(_uploadsPath, filePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

            // ASCII olmayan karakterleri filename için temizle
            string asciiFileName = new string(fileName.Select(c => c <= 127 ? c : '_').ToArray());
            string encodedFileName = Uri.EscapeDataString(fileName);

            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{asciiFileName}\"; filename*=UTF-8''{encodedFileName}");
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(fileStream, fileType ?? "application/octet-stream");
        }
    }
} 
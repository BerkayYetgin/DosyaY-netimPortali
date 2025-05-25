using AutoMapper;
using dosyayonetim.DTOs;
using dosyayonetim.Models;

namespace dosyayonetim.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // UserStorage mappings
            CreateMap<UserStorage, UserStorageDTO>();
            CreateMap<UserStorageUpdateDTO, UserStorage>();

            // Folder mappings
            CreateMap<Folder, FolderDTO>();
            CreateMap<FolderCreateDTO, Folder>();
            CreateMap<FolderUpdateDTO, Folder>();
            CreateMap<Folder, FolderWithContentDTO>();

            // File mappings
            CreateMap<dosyayonetim.Models.File, FileDTO>();
            CreateMap<dosyayonetim.Models.File, FileDownloadDTO>();

            CreateMap<ApplicationUser, UserDto>();
            CreateMap<RegisterDto, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username));
        }
    }
} 
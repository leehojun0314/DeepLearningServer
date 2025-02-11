using DeepLearningServer.Models;

namespace DeepLearningServer.Classes;

using AutoMapper;

public class TrainingMappingProfile : Profile
{
    public TrainingMappingProfile()
    {
        // 최상위: CreateAndRunModel → TrainingRecord
        CreateMap<CreateAndRunModel, TrainingRecord>()
            .ForMember(dest => dest.RecipeId, opt => opt.MapFrom(src => src.RecipeId))
            .ForMember(dest => dest.ProcessId, opt => opt.MapFrom(src => src.ProcessId))
            .ForMember(dest => dest.ImageSize, opt => opt.MapFrom(src => src.ImageSize))
            // StartTime, EndTime은 컨트롤러나 서비스에서 설정하셔도 되고,
            // 필요하면 .ForMember(...) 로 지정 가능
            ;

        // Geometry
        CreateMap<GeometryParamsDto, GeometryParams>();

        // Color
        CreateMap<ColorParamsDto, ColorParams>();

        // Noise
        CreateMap<NoiseParamsDto, NoiseParams>();

        // Classifier
        CreateMap<ClassifierParamsDto, ClassifierParams>();
    }
}

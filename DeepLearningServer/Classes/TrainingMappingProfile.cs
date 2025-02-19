using AutoMapper;
using DeepLearningServer.Dtos;
using DeepLearningServer.Models;

namespace DeepLearningServer.Classes
{
    public class TrainingMappingProfile : Profile
    {
        public TrainingMappingProfile()
        {
            // 최상위: CreateAndRunModel → TrainingRecord
            CreateMap<CreateAndRunModel, TrainingRecord>()
                .ForMember(dest => dest.AdmsProcessId, opt => opt.MapFrom(src => src.AdmsProcessId))
                .ForMember(dest => dest.ImageSize, opt => opt.MapFrom(src => src.ImageSize))
                .ForMember(dest => dest.Labels, opt => opt.MapFrom(src => src.Categories
                .Select(static name => new Label { Name = name }).ToList()))

                // 🔹 GeometryDto -> TrainingRecord 속성 매핑
                .ForMember(dest => dest.MaxRotation, opt => opt.MapFrom(src => src.Geometry.MaxRotation))
                .ForMember(dest => dest.MaxVerticalShift, opt => opt.MapFrom(src => src.Geometry.MaxVerticalShift))
                .ForMember(dest => dest.MaxHorizontalShift, opt => opt.MapFrom(src => src.Geometry.MaxHorizontalShift))
                .ForMember(dest => dest.MinScale, opt => opt.MapFrom(src => src.Geometry.MinScale))
                .ForMember(dest => dest.MaxScale, opt => opt.MapFrom(src => src.Geometry.MaxScale))
                .ForMember(dest => dest.MaxVerticalShear, opt => opt.MapFrom(src => src.Geometry.MaxVerticalShear))
                .ForMember(dest => dest.MaxHorizontalShear, opt => opt.MapFrom(src => src.Geometry.MaxHorizontalShear))
                .ForMember(dest => dest.VerticalFlip, opt => opt.MapFrom(src => src.Geometry.VerticalFlip))
                .ForMember(dest => dest.HorizontalFlip, opt => opt.MapFrom(src => src.Geometry.HorizontalFlip))

                // 🔹 ColorDto -> TrainingRecord 속성 매핑
                .ForMember(dest => dest.MaxBrightnessOffset, opt => opt.MapFrom(src => src.Color.MaxBrightnessOffset))
                .ForMember(dest => dest.MaxContrastGain, opt => opt.MapFrom(src => src.Color.MaxContrastGain))
                .ForMember(dest => dest.MinContrastGain, opt => opt.MapFrom(src => src.Color.MinContrastGain))
                .ForMember(dest => dest.MaxGamma, opt => opt.MapFrom(src => src.Color.MaxGamma))
                .ForMember(dest => dest.MinGamma, opt => opt.MapFrom(src => src.Color.MinGamma))
                .ForMember(dest => dest.HueOffset, opt => opt.MapFrom(src => src.Color.HueOffset))
                .ForMember(dest => dest.MaxSaturationGain, opt => opt.MapFrom(src => src.Color.MaxSaturationGain))
                .ForMember(dest => dest.MinSaturationGain, opt => opt.MapFrom(src => src.Color.MinSaturationGain))

                // 🔹 NoiseDto -> TrainingRecord 속성 매핑
                .ForMember(dest => dest.MaxGaussianDeviation, opt => opt.MapFrom(src => src.Noise.MaxGaussianDeviation))
                .ForMember(dest => dest.MinGaussianDeviation, opt => opt.MapFrom(src => src.Noise.MinGaussianDeviation))
                .ForMember(dest => dest.MaxSpeckleDeviation, opt => opt.MapFrom(src => src.Noise.MaxSpeckleDeviation))
                .ForMember(dest => dest.MinSpeckleDeviation, opt => opt.MapFrom(src => src.Noise.MinSpeckleDeviation))
                .ForMember(dest => dest.MaxSaltPepperNoise, opt => opt.MapFrom(src => src.Noise.MaxSaltPepperNoise))
                .ForMember(dest => dest.MinSaltPepperNoise, opt => opt.MapFrom(src => src.Noise.MinSaltPepperNoise))

                // 🔹 ClassifierDto -> TrainingRecord 속성 매핑
                .ForMember(dest => dest.ClassifierCapacity, opt => opt.MapFrom(src => src.Classifier.ClassifierCapacity))
                .ForMember(dest => dest.ImageCacheSize, opt => opt.MapFrom(src => src.Classifier.ImageCacheSize))
                .ForMember(dest => dest.ImageWidth, opt => opt.MapFrom(src => src.Classifier.ImageWidth))
                .ForMember(dest => dest.ImageHeight, opt => opt.MapFrom(src => src.Classifier.ImageHeight))
                .ForMember(dest => dest.ImageChannels, opt => opt.MapFrom(src => src.Classifier.ImageChannels))
                .ForMember(dest => dest.UsePretrainedModel, opt => opt.MapFrom(src => src.Classifier.UsePretrainedModel))
                .ForMember(dest => dest.ComputeHeatMap, opt => opt.MapFrom(src => src.Classifier.ComputeHeatMap))
                .ForMember(dest => dest.EnableHistogramEqualization, opt => opt.MapFrom(src => src.Classifier.EnableHistogramEqualization))
                .ForMember(dest => dest.BatchSize, opt => opt.MapFrom(src => src.Classifier.BatchSize));
        }
    }
}

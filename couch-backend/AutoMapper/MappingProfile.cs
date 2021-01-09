using AutoMapper;
using couch_backend.ModelDTOs.Requests;
using couch_backend.ModelDTOs.Responses;
using couch_backend.Models;
using System;

namespace couch_backend.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Account Controller
            CreateMap<SocialLoginDTO, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(
                    src => src.Email))
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(
                    src => true))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(
                    src => src.Email))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                    srcMember != null));

            CreateMap<string, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(
                    src => src))
                .ForMember(dest => dest.NormalizedUserName, opt => opt.MapFrom(
                    src => src.ToUpper()));

            CreateMap<User, LoginResponseDTO>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(
                    src => src.UserName))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(
                    src => src.Id));

            CreateMap<User, RefreshToken>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(
                    src => src.Id))
                .ForMember(dest => dest.GeneratedTime, opt => opt.MapFrom(
                    src => DateTime.UtcNow))
                .ForMember(dest => dest.ExpiryTime, opt => opt.MapFrom(
                    src => DateTime.UtcNow.AddDays(30)));

            CreateMap<UserSignUpDTO, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(
                    src => src.Email))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(
                    src => src.Email));
        }
    }
}

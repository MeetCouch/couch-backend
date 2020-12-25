﻿using AutoMapper;
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
            CreateMap<User, LoginResponseDTO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(
                    src => src.Name));

            CreateMap<User, RefreshToken>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(
                    src => src.Id))
                .ForMember(dest => dest.GeneratedTime, opt => opt.MapFrom(
                    src => DateTime.UtcNow))
                .ForMember(dest => dest.ExpiryTime, opt => opt.MapFrom(
                    src => DateTime.UtcNow.AddDays(30)));

            CreateMap<UserRegisterDTO, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(
                    src => src.Email))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(
                    src => src.Email));
        }
    }
}

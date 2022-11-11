﻿using AutoMapper;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

namespace WinTenDev.Zizi.Models.AutoMapper;

public class StepHistoryProfile : Profile
{
    public StepHistoryProfile()
    {
        CreateMap<StepHistoryEntity, StepHistoryDto>().ReverseMap();
    }
}
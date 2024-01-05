/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelMappings
 * @created     : Thursday Jan 04, 2024 21:47:18 CST
 */

namespace GatoGPT.WebAPI.MappingProfiles;

using GatoGPT.WebAPI.Entities;
using GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using AutoMapper;
// using GatoGPT.WebAPI.Dtos;
// using GatoGPT.WebAPI.Entities;

public class ModelMappings : Profile
{
    public ModelMappings()
    {
        CreateMap<ModelEntity, ModelDto>().ReverseMap();
        CreateMap<ModelEntity, ModelFullDto>().ReverseMap();
    }
}

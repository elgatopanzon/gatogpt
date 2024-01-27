/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionMappings
 * @created     : Friday Jan 26, 2024 22:22:29 CST
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
using GodotEGP.AI.OpenAI;

using AutoMapper;
// using GatoGPT.WebAPI.Dtos;
// using GatoGPT.WebAPI.Entities;

public class ChatCompletionMappings : Profile
{
    public ChatCompletionMappings()
    {
        CreateMap<ChatCompletionCreateOpenAIDto, ChatCompletionRequest>().ReverseMap();
    }
}

/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionsController
 * @created     : Friday Jan 05, 2024 13:00:42 CST
 */

namespace GatoGPT.WebAPI.v1.Controllers;

using GatoGPT.Service;
using GatoGPT.Config;
using GatoGPT.Event;
using GatoGPT.AI.TextGeneration;
using GatoGPT.WebAPI.Dtos;
using GatoGPT.WebAPI.Entities;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using AutoMapper;
// using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
// using GatoGPT.WebAPI.Dtos;
// using GatoGPT.WebAPI.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

using GodotEGP.AI.OpenAI;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public partial class ChatController : ControllerBase
{
	private readonly IMapper _mapper;
	private readonly TextGenerationModelManager _modelManager;
	private readonly TextGenerationService _inferenceService;

	public ChatController(IMapper mapper)
	{
		_mapper = mapper;
		 _modelManager = ServiceRegistry.Get<TextGenerationModelManager>();
		 _inferenceService = ServiceRegistry.Get<TextGenerationService>();
	}

    [HttpPost("completions", Name = nameof(CreateChatCompletion))]
    public async Task<ActionResult<ChatCompletionDto>> CreateChatCompletion(ApiVersion version, [FromBody] ChatCompletionCreateDto chatCompletionCreateDto)
    {
    	LoggerManager.LogDebug("Recieved chatCompletionCreateDto", "", "create", chatCompletionCreateDto);

    	// validate required params
		if (chatCompletionCreateDto.Model.Length == 0)
		{
			return BadRequest(new InvalidRequestErrorDto(
						message: "You must provide a model parameter",
						code: null,
						param:null
						));
		}

		// check model is valid
		if (!_modelManager.ModelDefinitions.ContainsKey(chatCompletionCreateDto.Model))
		{
    		return NotFound(new InvalidRequestErrorDto(message:$"The model '{chatCompletionCreateDto.Model}' does not exist", code:"model_not_found", param:"model"));
		}

		var modelDefinition = _modelManager.GetModelDefinition(chatCompletionCreateDto.Model);

		if (chatCompletionCreateDto.Messages.Count == 0)
		{
			return BadRequest(new InvalidRequestErrorDto(
						message: "You must provide a messages parameter",
						code: null,
						param:null
						));
		}

		// openai backend passthrough
		if (modelDefinition.Backend == "openai")
		{
			var openAi = new OpenAI(ServiceRegistry.Get<ConfigManager>().Get<GlobalConfig>().OpenAIConfig);

    		var openaiSse = new ServerSentEventManager(HttpContext);

    		openaiSse.Start();

			// stream responses when there's no tool calls
    			openAi.SubscribeOwner<OpenAIServerSentEvent>(async (e) => {
					await openaiSse.SendEvent(e.Event);
    			}, isHighPriority: true);

			var openaiResult = await openAi
					.ChatCompletions(_mapper.Map<ChatCompletionRequest>(new ChatCompletionCreateOpenAIDto(chatCompletionCreateDto)));

			if (chatCompletionCreateDto.Stream)
			{
				return new EmptyResult();
			}

			if (openaiResult == null)
			{
				return BadRequest(openAi.Error);
			}

			return Ok(openaiResult);
		}

		// extract stops
		List<string> stops = new();
		if (chatCompletionCreateDto.Stop is System.String stopString)
		{
			stops.Add(stopString);
		}
		else if (chatCompletionCreateDto.Stop is Newtonsoft.Json.Linq.JArray stopsList)
		{
			stops = stopsList.ToArray().Select(x => x.ToString()).ToList();
		}

		LoggerManager.LogDebug("Completion dto extracted stops", "", "stops", stops);

		// create LoadParams and InferenceParams objects from dto
		AI.TextGeneration.LoadParams loadParams = modelDefinition.ModelProfile.LoadParams.DeepCopy();
		AI.TextGeneration.InferenceParams inferenceParams = modelDefinition.ModelProfile.InferenceParams.DeepCopy();

		var inferenceParamsDefault = new InferenceParams();

		if (chatCompletionCreateDto.Temperature != inferenceParamsDefault.Temp)
			inferenceParams.Temp = chatCompletionCreateDto.Temperature;
		if (chatCompletionCreateDto.FrequencyPenalty != inferenceParamsDefault.FrequencyPenalty)
			inferenceParams.FrequencyPenalty = chatCompletionCreateDto.FrequencyPenalty;
		if (chatCompletionCreateDto.PresencePenalty != inferenceParamsDefault.PresencePenalty)
			inferenceParams.PresencePenalty = chatCompletionCreateDto.PresencePenalty;
		if (chatCompletionCreateDto.RepeatPenalty != inferenceParamsDefault.RepeatPenalty)
			inferenceParams.RepeatPenalty = chatCompletionCreateDto.RepeatPenalty;
		if (chatCompletionCreateDto.MaxTokens != inferenceParamsDefault.NPredict)
			inferenceParams.NPredict = chatCompletionCreateDto.MaxTokens;

		loadParams.Seed = chatCompletionCreateDto.Seed;

		inferenceParams.Antiprompts = inferenceParams.Antiprompts.Concat(stops).ToList();

		if (chatCompletionCreateDto.MinP != inferenceParamsDefault.MinP)
			inferenceParams.MinP = chatCompletionCreateDto.MinP;
		if (chatCompletionCreateDto.TopP != inferenceParamsDefault.TopP)
			inferenceParams.TopP = chatCompletionCreateDto.TopP;
		if (chatCompletionCreateDto.TopK != inferenceParamsDefault.TopK)
			inferenceParams.TopK = chatCompletionCreateDto.TopK;

		// apply extended parameters if they are set
		if (chatCompletionCreateDto.Extended != null)
		{
			if (chatCompletionCreateDto.Extended.Model != null)
			{
				if (chatCompletionCreateDto.Extended.Model.NCtx != null)
					loadParams.NCtx = (int) chatCompletionCreateDto.Extended.Model.NCtx;
				if (chatCompletionCreateDto.Extended.Model.NBatch != null)
					loadParams.NBatch = (int) chatCompletionCreateDto.Extended.Model.NBatch;
				if (chatCompletionCreateDto.Extended.Model.NGpuLayers != null)
					loadParams.NGpuLayers = (int) chatCompletionCreateDto.Extended.Model.NGpuLayers;
				if (chatCompletionCreateDto.Extended.Model.Backend != null)
					modelDefinition.Backend = (string) chatCompletionCreateDto.Extended.Model.Backend;
				if (chatCompletionCreateDto.Extended.Model.PromptCache != null)
					modelDefinition.PromptCache = (bool) chatCompletionCreateDto.Extended.Model.PromptCache;
				if (chatCompletionCreateDto.Extended.Model.RopeFreqBase != null)
					loadParams.RopeFreqBase = (double) chatCompletionCreateDto.Extended.Model.RopeFreqBase;
				if (chatCompletionCreateDto.Extended.Model.RopeFreqScale != null)
					loadParams.RopeFreqScale = (double) chatCompletionCreateDto.Extended.Model.RopeFreqScale;
			}
			if (chatCompletionCreateDto.Extended.Inference != null)
			{
				if (chatCompletionCreateDto.Extended.Inference.NThreads != null)
					inferenceParams.NThreads = (int) chatCompletionCreateDto.Extended.Inference.NThreads;
				if (chatCompletionCreateDto.Extended.Inference.NKeep != null)
					inferenceParams.KeepTokens = (int) chatCompletionCreateDto.Extended.Inference.NKeep;
				if (chatCompletionCreateDto.Extended.Inference.TopK != null)
					inferenceParams.TopK = (int) chatCompletionCreateDto.Extended.Inference.TopK;
				if (chatCompletionCreateDto.Extended.Inference.Tfs != null)
					inferenceParams.Tfs = (double) chatCompletionCreateDto.Extended.Inference.Tfs;
				if (chatCompletionCreateDto.Extended.Inference.Typical != null)
					inferenceParams.Typical = (double) chatCompletionCreateDto.Extended.Inference.Typical;
				if (chatCompletionCreateDto.Extended.Inference.Mirostat != null)
					inferenceParams.Mirostat = (double) chatCompletionCreateDto.Extended.Inference.Mirostat;
				if (chatCompletionCreateDto.Extended.Inference.MirostatLearningRate != null)
					inferenceParams.MirostatLearningRate = (double) chatCompletionCreateDto.Extended.Inference.MirostatLearningRate;
				if (chatCompletionCreateDto.Extended.Inference.MirostatEntropy != null)
					inferenceParams.MirostatEntropy = (double) chatCompletionCreateDto.Extended.Inference.MirostatEntropy;
				if (chatCompletionCreateDto.Extended.Inference.RepeatPenalty != null)
					inferenceParams.RepeatPenalty = (double) chatCompletionCreateDto.Extended.Inference.RepeatPenalty;
				if (chatCompletionCreateDto.Extended.Inference.RepeatLastN != null)
					inferenceParams.RepeatLastN = (int) chatCompletionCreateDto.Extended.Inference.RepeatLastN;
				if (chatCompletionCreateDto.Extended.Inference.Vision != null)
					modelDefinition.Vision = (bool) chatCompletionCreateDto.Extended.Inference.Vision;
				if (chatCompletionCreateDto.Extended.Inference.GrammarResourceId != null)
					inferenceParams.GrammarResourceId = (string) chatCompletionCreateDto.Extended.Inference.GrammarResourceId;
				if (chatCompletionCreateDto.Extended.Inference.ChatMessageTemplate != null)
					inferenceParams.ChatMessageTemplate = (string) chatCompletionCreateDto.Extended.Inference.ChatMessageTemplate;
				if (chatCompletionCreateDto.Extended.Inference.ChatMessageGenerationTemplate != null)
					inferenceParams.ChatMessageGenerationTemplate = (string) chatCompletionCreateDto.Extended.Inference.ChatMessageGenerationTemplate;
				if (chatCompletionCreateDto.Extended.Inference.PrePrompt != null)
					inferenceParams.PrePrompt = (string) chatCompletionCreateDto.Extended.Inference.PrePrompt;
				if (chatCompletionCreateDto.Extended.Inference.CfgNegativePrompt != null)
					inferenceParams.NegativeCfgPrompt = (string) chatCompletionCreateDto.Extended.Inference.CfgNegativePrompt;
				if (chatCompletionCreateDto.Extended.Inference.CfgScale != null)
					inferenceParams.CfgScale = (double) chatCompletionCreateDto.Extended.Inference.CfgScale;
				if (chatCompletionCreateDto.Extended.Inference.PromptCacheId != null)
					inferenceParams.PromptCacheId = (string) chatCompletionCreateDto.Extended.Inference.PromptCacheId;
				if (chatCompletionCreateDto.Extended.Inference.Samplers != null && chatCompletionCreateDto.Extended.Inference.Samplers.Count > 0)
					inferenceParams.Samplers = (List<string>) chatCompletionCreateDto.Extended.Inference.Samplers;
			}
		}

		// setup tools
		string toolChoice = chatCompletionCreateDto.GetToolChoice();
		ChatCompletionToolChoiceDto toolChoiceDto = null;

		// if tool choice is empty, then it contains a function to override
		if (toolChoice == "")
		{
			toolChoiceDto = chatCompletionCreateDto.GetToolChoiceObject();

			LoggerManager.LogDebug("Overriding tool choice", "", "toolChoice", toolChoiceDto);

			// TODO: figured out something nicer?
			toolChoice = toolChoiceDto.Function.Name;
		}
		// automatically choose between listed tools
		else if (toolChoice == "auto")
		{
			LoggerManager.LogDebug("Tool choice set to auto");
		}
		else if (toolChoice == "none")
		{
			LoggerManager.LogDebug("Tool choice set to none");
		}

		// init new chat instance
    	StatefulChat chatInstance = new(modelDefinition.PromptCache, loadParams, inferenceParams);
    	List<StatefulChatMessage> messageEntities = new();

		List<string> imageUrls = new();

		if (modelDefinition.Vision)
		{
    		foreach (var messageCreateDto in chatCompletionCreateDto.Messages)
    		{
    			messageEntities.Add(new StatefulChatMessage() {
					Content = messageCreateDto.GetContent(),
					Role = messageCreateDto.Role,
					Name = messageCreateDto.Name,
					ToolCalls = messageCreateDto.ToolCalls,
    				});

				var dtoContents = messageCreateDto.GetContents();
    			if (dtoContents.Count > 0)
    			{
    				foreach (var content in dtoContents)
    				{
    					if (content.Type == "image_url" && content.ImageUrl.Length > 0)
    					{
    						imageUrls.Add(content.ImageUrl);
    					}
    				}
    			}
    		}

    		LoggerManager.LogDebug("Request parsed image urls", "", "imageUrls", imageUrls);
			
		}

		// take the first image only when we have a mmproj path set
		if (imageUrls.Count == 1 && modelDefinition.ModelProfile.LoadParams.MMProjPath.Length > 0)
		{
			try
			{
        		using (var httpClient = new HttpClient())
        		{
            		// Issue the GET request to a URL and read the response into a 
            		// stream that can be used to load the image
            		var imageContent = await httpClient.GetByteArrayAsync(imageUrls[0]);

            		string filename = Path.GetFileName(imageUrls[0]);
            		string filepath = Path.Combine(OS.GetUserDataDir(), "Downloads", filename);

            		Directory.CreateDirectory(filepath.Replace(filepath.GetFile(), ""));

            		using (var fileStream = new FileStream(filepath, FileMode.Create))
            		{
                		await fileStream.WriteAsync(imageContent, 0, imageContent.Length);
            		}

					inferenceParams.ImagePath = filepath;
        		}
			}
			catch (System.Exception e)
			{
				// throw;
				LoggerManager.LogDebug("Error downloading image content", "", "exception", e);

				return Ok();
			}
		}
		else if (imageUrls.Count >= 1)
		{
			// execute internal api requests to combine the result of the images
			// descriptions using defined Vision model
			List<string> imageInferenceResults = new();

			foreach (string imageUrl in imageUrls)
			{
				ChatCompletionCreateDto imageDto = new();
				imageDto.Model = modelDefinition.ModelProfile.InferenceParams.ImageModelId;
				imageDto.MaxTokens = chatCompletionCreateDto.MaxTokens;
				imageDto.Messages = new();
				imageDto.Messages.Add(new() {
					Role = "user",
					Content = new List<ChatCompletionMessageContentDto>() {
							new() {
								Type = "text",
								Text = messageEntities.Last().Content+". "+"Provide a description and content of the image."
							},
							new() {
								Type = "image_url",
								ImageUrl = imageUrl,
							},
						},
					});

				var res = await CreateChatCompletion(HttpContext.GetRequestedApiVersion(), imageDto);

				LoggerManager.LogDebug("Internal completion request", "", "res", res);

				if (res.Result is OkObjectResult okRes)
				{
					string imageInferenceRes = (string) ((ChatCompletionDto) okRes.Value).Choices[0].Message.Content;
					imageInferenceResults.Add(imageInferenceRes);
				}
			}

			LoggerManager.LogDebug("Image inference results", "", "imageRes", imageInferenceResults);
			
			List<StatefulChatMessage> imageMessages = new();

			// hack together some injected system prompts to give information
			// about the inference results (RAG I guess?)
    		imageMessages.Add(new StatefulChatMessage() {
				Role = "user",
				Name = (string) chatCompletionCreateDto.Messages.Last().Name,
				Content = (string) $"I've browsed the web to look at {imageUrls.Count} images provided",
    			});

			int counter = 0;
			foreach (var imageResult in imageInferenceResults)
			{
    			imageMessages.Add(new StatefulChatMessage() {
					Role = "assistant",
					Content = (string) $"Description of image ({imageUrls[counter]}): {imageResult}",
    				});

				counter++;
			}

    		imageMessages.Add(new StatefulChatMessage() {
				Role = "user",
				Name = (string) chatCompletionCreateDto.Messages.Last().Name,
				Content = (string) messageEntities.Last().Content,
    			});

    		// get the injected images tokenized count and remove it from
    		// npredict to now over-budget context
			ChatCompletionRequest imageChatRequest = new() {
				Messages = new(),
				Model = chatCompletionCreateDto.Model,
			};

			foreach (var msg in imageMessages)
			{
				imageChatRequest.Messages.Add(new() {
					Role = msg.Role,
					Name = msg.Name,
					Content = msg.Content,
				});
			}

			var tokens = _inferenceService.TokenizeString(imageChatRequest.Model, String.Join("", imageChatRequest.Messages.Select(x => x.Content+x.Name)));

			LoggerManager.LogDebug("Image RAG prompt token count", "", "tokenCount", tokens.Count);

			inferenceParams.NPredict -= tokens.Count;

			messageEntities.AddRange(imageMessages);
		}

		// inject a list of tools into the system prompt
		List<string> validFunctionNames = new();
		if (toolChoice != "none" && chatCompletionCreateDto.Tools.Count > 0)
		{
			// create respond function
			var respondFunction = new ChatCompletionCreateToolDto() {
				Type = "function",
				Function = new ChatCompletionCreateFunctionDto() {
					Name = "respond",
					Description = "Write a regular response to the user (used when no function call is required)",
					Parameters = new ChatCompletionCreateFunctionParametersDto() {
						Type = "function",
						Properties = new Dictionary<string, ChatCompletionCreateFunctionPropertyDto>() {
							{ "response", new() {
								Description = "Your response to the user's query"
							} }
						},
						Required = new() {
							"response"
						}
					},
				},
			};
			// chatCompletionCreateDto.Tools.Add(respondFunction);
			LoggerManager.LogDebug("Respond function", "", "respondFunction", respondFunction);

			var lastMessage = chatCompletionCreateDto.Messages.Last();
			var toolsSystemMessage = new StatefulChatMessage() {
				Role = "system",
			};

			string messageOriginalContent = lastMessage.GetContent();
			toolsSystemMessage.Content += $"As an AI assistant you have access to a range of tools. Please select the most suitable function and parameters from the list of available functions below. Provide your response in JSON format when calling a function, otherwise respond as a human would.";
			toolsSystemMessage.Content += $"\nAvailable functions:";

			validFunctionNames.Add("respond");
			toolsSystemMessage.Content += $"\n  respond (Respond normally to the users request without calling a tool):\n    Parameters:\n      response:\n        required: True";
			toolsSystemMessage.Content += "\n      { \"function\": \"respond\", \"arguments\": {\"response\": \"Your response to the user in plain-text.\"}}";

			foreach (var tool in chatCompletionCreateDto.Tools)
			{
				validFunctionNames.Add(tool.Function.Name);

				toolsSystemMessage.Content += $"\n  {tool.Function.Name}";

				if (tool.Function.Description.Length > 0)
				{
					toolsSystemMessage.Content += $" ({tool.Function.Description}):";
				}

				LoggerManager.LogDebug("Params parsed", "", "params", tool.Function.GetParametersDto());

				var functionParams = tool.Function.GetParametersDto();

				toolsSystemMessage.Content += $"\n    Parameters:";

				foreach (var param in functionParams.Properties)
				{
					toolsSystemMessage.Content += $"\n      {param.Key}:";

					toolsSystemMessage.Content += $"\n        data_type: {param.Value.Type}";

					if (param.Value.Description.Length > 0)
					{
						toolsSystemMessage.Content += $"\n        description: {param.Value.Description}";
					}

					if (param.Value.Enum.Count > 0)
					{
						toolsSystemMessage.Content += $"\n        allowed_values: {String.Join(", ", param.Value.Enum)}";
					}

					toolsSystemMessage.Content += $"\n        required: {(functionParams.Required.Contains(param.Key)).ToString()}";
					
				}

				toolsSystemMessage.Content += "\n    Example responses:";
				toolsSystemMessage.Content += "\n      { \"function\": \""+tool.Function.Name+"\", \"arguments\": {\"argument_name\": \"argument_value\"}}";


				foreach (var includeAllParams in new List<bool>() { true, false })
				{
					toolsSystemMessage.Content += "\n      { \"function\": \""+tool.Function.Name+"\"";

					if (functionParams.Properties.Count > 0)
					{
						toolsSystemMessage.Content += "{ \"arguments\": ";

						foreach (var param in functionParams.Properties)
						{
							if (!includeAllParams && !functionParams.Required.Contains(param.Key))
							{
								continue;
							}
							string functionExampleValue = "ARGUMENT_VALUE";

							if (param.Value.Enum.Count > 0)
							{
								functionExampleValue = $"e.g. {String.Join(", ", param.Value.Enum)}";
							}

							toolsSystemMessage.Content += "{\""+param.Key+"\": \""+functionExampleValue+"\"},";
						}

						// toolsSystemMessage.Content = lastMessage.Content.TrimEnd(',');

						toolsSystemMessage.Content += "}";
					}

					toolsSystemMessage.Content += "}";
				}
				toolsSystemMessage.Content += $"\n";
			}

			// if (toolChoice == "auto")
			// {
			// 	toolsSystemMessage.Content += $"\nYou can choose to ignore the above functions and respond normally to the user's request if the request has nothing related to your available functions.";
			// }

			messageEntities.Add(toolsSystemMessage);

			// use json grammar to constrict tool output to json only
			modelDefinition.ModelProfileOverride.InferenceParams.GrammarResourceId = "json";
		}

		LoggerManager.LogDebug("Available tools", "", "tools", chatCompletionCreateDto.Tools);

    	chatInstance.SetChatMessages(messageEntities);

    	// LoggerManager.LogDebug("Prompt to send", "", "prompt", chatInstance.GetPrompt());

		ChatCompletionDto chatCompletionDto = new();

		chatCompletionDto.Id = $"cmpl-{GetHashCode()}-{chatCompletionDto.GetHashCode()}-{chatCompletionCreateDto.GetHashCode()}";
		chatCompletionDto.Created = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds();
		chatCompletionDto.Model = chatCompletionCreateDto.Model;
		chatCompletionDto.SystemFingerprint = GetHashCode().ToString();

		// create SSE manager instance for handling sending of server side
		// events
    	var sse = new ServerSentEventManager(HttpContext);

		// queue and generate responses until N is reached
		int currentIndex = 0;
		while (chatCompletionDto.Choices.Count < chatCompletionCreateDto.N)
		{
			var modelInstance = _inferenceService.GetPersistentInstance(chatCompletionCreateDto.Model, stateful:modelDefinition.PromptCache);

			// create new instance if there's no persistent instances
			if (modelInstance == null)
			{
    			modelInstance = _inferenceService.CreateModelInstance(chatCompletionCreateDto.Model, stateful:modelDefinition.PromptCache);
			}

    		// initiate SSE if stream = true
    		if (chatCompletionCreateDto.Stream)
    		{
    			LoggerManager.LogDebug("Running in stream mode");

    			sse.Start();

				// stream responses when there's no tool calls
				if (chatCompletionCreateDto.Stream && chatCompletionCreateDto.Tools.Count == 0)
				{
    				modelInstance.SubscribeOwner<TextGenerationInferenceToken>(async (e) => {
						LoggerManager.LogDebug("Stream mode dispatching token SSE", "", "token", e.Token);

						await sse.SendEvent(new ChatCompletionChunkDto() {
							Id = chatCompletionDto.Id,
							Model = chatCompletionCreateDto.Model,
							Choices = new() {new() {
								Index = currentIndex,
								Delta = new() {
									Role = "assistant",
									Content = e.Token,
								}}
							},
							InferenceResult = modelInstance.InferenceResult,
						});

    				}, isHighPriority: true);
				}

    			modelInstance.SubscribeOwner<TextGenerationInferenceFinished>(async (e) => {
    				if (chatCompletionDto.Choices.Count >= (chatCompletionCreateDto.N - 1) && modelInstance.InferenceResult.Error == null)
    				{
						LoggerManager.LogDebug("Stream mode finished");
						await sse.Done();
    				}
    			}, isHighPriority:true);
    		}

			StatefulChatMessage message = await chatInstance.ChatAsync(modelInstance);

			chatCompletionDto.Usage.PromptTokens += modelInstance.InferenceResult.PromptTokenCount;
			chatCompletionDto.Usage.CompletionTokens += modelInstance.InferenceResult.GenerationTokenCount;

			ChatCompletionMessageDto messageDto = new() {
				Content = message.Content,
				Role = message.Role,
			};

			string finishReason = ((modelInstance.InferenceResult.Tokens.Count >= chatCompletionCreateDto.MaxTokens && chatCompletionCreateDto.MaxTokens > -1) ? "length" : "stop");

			// parse tool calls
			string toolParseResult = "";
			if (toolChoice != "none" && chatCompletionCreateDto.Tools.Count > 0)
			{
				// strip useless text
				// string toolCallContent = ;
				var match = Regex.Match(message.Content, @"(?<json>{(?:[^{}]|(?<Nested>{)|(?<-Nested>}))*(?(Nested)(?!))})");

				while (match.Success)
				{
					if (match.Groups.Count > 0 && match.Groups[0].Value.Length > 0)
					{
						LoggerManager.LogDebug("JSON tool parse", "", "toolParse", match.Groups[0].Value);
						messageDto.Content = match.Groups[0].Value.Trim();

						finishReason = "tool_call";
					}

					bool parseResult = false;
					try
					{
						var toolCallRawDto = JsonConvert.DeserializeObject<ChatCompletionToolCallRawDto>(messageDto.GetContent());;

						var toolCallDto = new ChatCompletionToolCallDto();
						toolCallDto.Type = "function";
						toolCallDto.Id = $"toolcall-{GetHashCode()}-{toolCallDto.GetHashCode()}";

						LoggerManager.LogDebug("Parsed toolCallRawDto", "", "toolCallRawDto", toolCallRawDto);

						toolCallDto.Function = new ChatCompletionToolFunctionDto() {
							Name = toolCallRawDto.Function,
							Arguments = JsonConvert.SerializeObject(toolCallRawDto.Arguments),
						};

						LoggerManager.LogDebug("Final toolCallDto", "", "toolCallDto", toolCallDto);

						if (toolCallDto.Function.Name == null || !validFunctionNames.Contains(toolCallDto.Function.Name))
						{
							finishReason = "";

							toolParseResult = "Not a valid function name";
						}
						else
						{
							parseResult = true;
							if (toolCallDto.Function.Name == "respond" && toolCallRawDto.Arguments.TryGetValue("response", out object r))
							{
								messageDto.Content = r.ToString();
								finishReason = ((modelInstance.InferenceResult.Tokens.Count >= chatCompletionCreateDto.MaxTokens && chatCompletionCreateDto.MaxTokens > -1) ? "length" : "stop");
							}
							else
								messageDto.Content = null;
								messageDto.ToolCalls.Add(toolCallDto);
						}

					}
					catch (System.Exception)
					{
						LoggerManager.LogDebug("Parsing toolCallDto failed", "", "messageContent", messageDto.Content);
						finishReason = "";

						toolParseResult = "Not a valid JSON response";
					}

					match = match.NextMatch();
				}
			}

			if (chatCompletionCreateDto.Tools.Count > 0 && chatCompletionCreateDto.Stream)
			{
				
				await sse.SendEvent(new ChatCompletionChunkDto() {
					Id = chatCompletionDto.Id,
					Model = chatCompletionCreateDto.Model,
					Choices = new() {new() {
						Index = currentIndex,
						Delta = new() {
							Role = "assistant",
							Content = messageDto.Content,
							ToolCalls = messageDto.ToolCalls,
						}}
					},
					InferenceResult = modelInstance.InferenceResult,
				});
			}


			chatCompletionDto.Choices.Add(new ChatCompletionChoiceDto() {
				FinishReason = finishReason,
				Index = currentIndex,
				InferenceResult = modelInstance.InferenceResult,
				Message = messageDto,
				});

			currentIndex++;

			_inferenceService.DestroyExistingInstances();

			// check for inference error
			if (!modelInstance.InferenceResult.Success)
			{
				LoggerManager.LogError("Inference error", "", "inferenceError", modelInstance.InferenceResult.Error);

				ErrorResultExtended err = new() {
					Error = new() {
						Code = modelInstance.InferenceResult.Error.Code,
						Type = modelInstance.InferenceResult.Error.Type,
						Message = modelInstance.InferenceResult.Error.Message,
						Param = "prompt",
						Exception = modelInstance.InferenceResult.Error.Exception,
					}

				};

				if (chatCompletionCreateDto.Stream)
				{
					HttpContext.Response.Headers.Remove("Content-Type");
				}
				return BadRequest(err);
			}
		}

		LoggerManager.LogDebug("Returning chatCompletionDto", "", "chatCompletionDto", chatCompletionDto);

		if (!chatCompletionCreateDto.Stream)
		{
    		return Ok(chatCompletionDto);
		}
		else {
			LoggerManager.LogDebug("Waiting for SSE to finish");

			await sse.WaitDone();

			LoggerManager.LogDebug("SSE finished!");

			return new EmptyResult();
		}
    }
}


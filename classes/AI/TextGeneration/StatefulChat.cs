/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : StatefulChat
 * @created     : Friday Jan 05, 2024 14:27:34 CST
 */

namespace GatoGPT.AI.TextGeneration;

using GatoGPT.WebAPI.Dtos; // TODO: make this into an entity to be more clean

using GatoGPT.Service;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using Newtonsoft.Json;

using System.Security.Cryptography;

public partial class StatefulChat
{
	public bool _stateful { get; set; }
	public AI.TextGeneration.Backends.ITextGenerationBackend _modelInstance  { get; set; }

	public AI.TextGeneration.LoadParams _loadParams { get; set; }
	public AI.TextGeneration.InferenceParams _inferenceParams { get; set; }

	public List<StatefulChatMessage> _chatHistory { get; set; }
	public List<StatefulChatMessage> _chatHistoryNew { get; set; }

	public TextGenerationService _inferenceService { get; set; }
	public string _instanceStateId { get; set; }

	public List<string> _userNames { get; set; } = new();
	public string _assistantName { get; set; } = "Assistant";

	public StatefulChat(bool stateful,  AI.TextGeneration.LoadParams loadParams, AI.TextGeneration.InferenceParams inferenceParams)
	{
		_stateful = stateful;
		_loadParams = loadParams;
		_inferenceParams = inferenceParams;

		_inferenceService = ServiceRegistry.Get<TextGenerationService>();
		_instanceStateId = "";
	}

	public void SetChatMessages(List<StatefulChatMessage> messages)
	{
		_chatHistory = messages;
	}

	public void UpdateStatefulInstanceId()
	{
		// find existing state instances by looping over the provided messages
		// while adding 1 extra message each time
		// if a state exists for any of the chat, then use that state ID and
		// split the history, marking the future messages as the new ones to
		// send for inference
		int lastNIndex = _chatHistory.Count;
		string foundStateHash = "";

		if (_stateful)
		{
			for (int i = _chatHistory.Count - 1; i > -1; i--)
			{
				// pass the ignore N value
				var messagesHash = GetStateInstanceId(i);

				LoggerManager.LogDebug($"Messages hash for {i}", "", "hash", messagesHash);

				// check if state exists
				if (ChatStateExistsForHash(messagesHash))
				{
					lastNIndex = i;
					foundStateHash = messagesHash;
					break;
				}
			}

			if (foundStateHash.Length > 0)
			{
				LoggerManager.LogDebug("State found!", "", "hash", foundStateHash);
				// lastNIndex++;
				LoggerManager.LogDebug("Messages to skip", "", "skipCount", _chatHistory.Count - lastNIndex);
				LoggerManager.LogDebug("Number of new messages", "", "processCount", lastNIndex);

				_instanceStateId = GetInstanceId(foundStateHash);
			}
			else
			{
				_instanceStateId = GetInstanceId(GetStateInstanceId(0));
			}
		}

		// get reversed chat history and take N off the end for generation
		List<StatefulChatMessage> newMessages = _chatHistory.Skip(_chatHistory.Count - lastNIndex).ToList();

		LoggerManager.LogDebug("Messages to process", "", "newMessages", newMessages);

		_chatHistoryNew = newMessages;
	}

	public bool ChatStateExistsForHash(string hash)
	{
		return Directory.Exists(Path.Combine(OS.GetUserDataDir(), "State", GetInstanceId(hash)));
	}

	public string GetInstanceId(string hash)
	{
		return $"chat-{_modelInstance.ModelDefinition.Id}-{hash}";
	}

	public string GetStateInstanceId(int ignoreLastN = 0)
	{
		// produce a hash of the past messages
		string messagesHash = "";

		// remove the last N when ignore last N is set
		var history = _chatHistory.DeepCopy();
		history.Reverse();
		history = history.Skip(ignoreLastN).ToList();

		foreach (var message in history)
		{
			byte[] bytes;
			using (HashAlgorithm algorithm = SHA256.Create())
        		 bytes = algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{messagesHash}{message.Role}{message.Content}{message.Name}{_loadParams.Seed}"));

			messagesHash = "";
        	foreach (byte b in bytes)
        	{
        		messagesHash += b.ToString("x2");
        	}
		}

		return messagesHash;
	}

	public string GetPrompt(bool newMessagesOnly = true)
	{
		_inferenceParams.TemplateType = "chat-instruct";

		string formattedPrompt = "";

		List<StatefulChatMessage> history;

		if (newMessagesOnly)
		{
			history = _chatHistoryNew;
		}
		else
		{
			history = _chatHistory;
		}

		List<string> formattedMessages = new();
		List<string> systemPrompts = new();


		// prepare messages before formatting them
		foreach (var message in history)
		{
			// skip formatting the message if it's a system message
			if (message.Role == "system")
			{
				systemPrompts.Add(message.Content);
				continue;
			}

			// set the assistant name
			if (message.Role == "assistant")
			{
				_assistantName = message.GetUserName();
			}
			// add user name to known users
			if (message.Role == "user")
			{
				_userNames.Add(message.GetUserName());

				// add the stop to the message
				if (!_inferenceParams.Antiprompts.Contains(message.GetUserName()))
				{
					_inferenceParams.Antiprompts.Add($"{message.GetUserName()}: ");
				}
			}

			// format the message using the ChatMessageTemplate
			string formattedMessage = _inferenceParams.ChatMessageTemplate;

			Dictionary<string, object> templateVars = new();
			templateVars.Add("Role", message.Role);
			templateVars.Add("Name", message.GetUserName());
			templateVars.Add("Message", message.Content);

			foreach (var var in templateVars)
			{
				formattedMessage = formattedMessage.Replace("{{ "+var.Key+" }}", (string) var.Value);
			}

			formattedMessages.Add(formattedMessage);

		}

		systemPrompts.Add($"Contiue the chat and provide a single answer for {_assistantName}.");

		// append a chat message generation string if the template is set
		if (_inferenceParams.ChatMessageGenerationTemplate.Length > 0)
		{
			string formattedMessage = _inferenceParams.ChatMessageGenerationTemplate;

			Dictionary<string, object> templateVars = new();
			templateVars.Add("AssistantName", _assistantName);

			foreach (var var in templateVars)
			{
				formattedMessage = formattedMessage.Replace("{{ "+var.Key+" }}", (string) var.Value);
			}

			formattedMessages.Add(formattedMessage);
		}


		if (systemPrompts.Count > 0)
		{
			_inferenceParams.PrePrompt = String.Join(". ", systemPrompts);
		}

		// join messages to form prompt when message count > 0
		if (formattedMessages.Count > 0)
		{

			formattedPrompt = String.Join("\n", formattedMessages);
		}

		return formattedPrompt;
	}

	public async Task<StatefulChatMessage> ChatAsync(AI.TextGeneration.Backends.ITextGenerationBackend modelInstance)
	{
		_modelInstance = modelInstance;

		// calculate the instance ID hash from messages
		UpdateStatefulInstanceId();

		// set the current instance ID based on the hash (could also be found
		// state)
		_inferenceService.SetModelInstanceId(_modelInstance.InstanceId, _instanceStateId);

		InferenceResult inferenceResult = await _inferenceService.InferAsync(modelInstance.ModelDefinition.Id, GetPrompt(), stateful:_stateful, (_instanceStateId.Length > 0 ? _instanceStateId : _modelInstance.InstanceId), _loadParams, _inferenceParams);

		string content = inferenceResult.Output;

		// append message to history and save context
		_chatHistory.Add(new StatefulChatMessage() {
			Content = inferenceResult.Output,
			Role = "assistant",
		});
		_inferenceService.SetModelInstanceId(_modelInstance.InstanceId, GetInstanceId(GetStateInstanceId(0)));

		// strip out chat names from the response
		foreach (string name in _userNames.Concat(new List<string>() { _assistantName }))
		{
			content = content.Replace($"{name}: ", "");
			content = content.Replace($"{name}:", "");
			content = content.Trim();
		}

		LoggerManager.LogDebug("Instance state id", "", "instanceStateId", _instanceStateId);

		return new StatefulChatMessage() {
			Role = "assistant",
			Content = content,
		};
	}
}

public partial class StatefulChatMessage
{
	public string Content { get; set; }
	public string Role { get; set; }
	internal string Name { get; set; }
	public List<ChatCompletionToolCallDto> ToolCalls { get; set; }

	public string Format()
	{
		return $"{GetUserName()}: {Content}";
	}

	public string GetUserName()
	{
		string userName = Role;
		if (Name != null && Name != "")
		{
			userName = Name;
		}

		return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(userName);
	}
}

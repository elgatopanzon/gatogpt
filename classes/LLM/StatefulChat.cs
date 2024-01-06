/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : StatefulChat
 * @created     : Friday Jan 05, 2024 14:27:34 CST
 */

namespace GatoGPT.LLM;

using GatoGPT.Service;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using System.Security.Cryptography;

public partial class StatefulChat
{
	public bool _stateful { get; set; }
	public LlamaModelInstance _modelInstance  { get; set; }

	public LLM.LoadParams _loadParams { get; set; }
	public LLM.InferenceParams _inferenceParams { get; set; }

	public List<StatefulChatMessage> _chatHistory { get; set; }
	public List<StatefulChatMessage> _chatHistoryNew { get; set; }

	public LlamaInferenceService _inferenceService { get; set; }
	public string _instanceStateId { get; set; }

	public List<string> _knownUserNames { get; set; }

	public StatefulChat(bool stateful, LLM.LoadParams loadParams, LLM.InferenceParams inferenceParams)
	{
		_stateful = stateful;
		_loadParams = loadParams;
		_inferenceParams = inferenceParams;

		_inferenceService = ServiceRegistry.Get<LlamaInferenceService>();
		_instanceStateId = "";
	}

	public void SetChatMessages(List<StatefulChatMessage> messages)
	{
		_chatHistory = messages;

		// find existing state instances by looping over the provided messages
		// while adding 1 extra message each time
		// if a state exists for any of the chat, then use that state ID and
		// split the history, marking the future messages as the new ones to
		// send for inference
		int lastNIndex = _chatHistory.Count;
		string foundStateHash = "";

		if (_stateful)
		{
			for (int i = messages.Count - 1; i > -1; i--)
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
				LoggerManager.LogDebug("Messages to skip", "", "skipCount", _chatHistory.Count - lastNIndex);
				LoggerManager.LogDebug("Number of new messages", "", "processCount", lastNIndex);

				_instanceStateId = GetInstanceId(foundStateHash);
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
		return $"chat-{_modelInstance.InstanceId}-{hash}";
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
        		 bytes = algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{messagesHash}{message.Role}{message.Content}{message.Name}"));

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

		formattedMessages.Add("Below is a conversation:");

		string formattedPrompt = "";

		StatefulChatMessage lastMessage = null;

		string userName = "User";
		string assistantName = "Assistant";

		_knownUserNames = new();

		foreach (var message in history)
		{
			if (message.Role == "system")
			{
				systemPrompts.Add(message.Content);
				continue;
			}

			if (message.Role == "assistant")
			{
				assistantName = message.GetUserName();
				systemPrompts.Add($"Your name is {assistantName}.");
			}
			if (message.Role == "user")
			{
				userName = message.GetUserName();
			}

			formattedMessages.Add(message.Format());

			// add stops to message
			if (!_inferenceParams.Antiprompts.Contains(message.GetUserName()))
			{
			}

			lastMessage = message;

			if (!_knownUserNames.Contains(message.GetUserName()))
			{
				_knownUserNames.Add(message.GetUserName());
			}
		}
		
		// set system prompt from array of system prompts overriding the default
		if (systemPrompts.Count > 0)
		{
			_inferenceParams.PrePrompt = String.Join(". ", systemPrompts);
		}

		// add the next expected reply to the end of the conversation based on
		// the previous role
		if (lastMessage != null)
		{
			if (lastMessage.Role == "user")
			{
				// formattedMessages.Add($"Write your response as if you were {assistantName}, without \"{assistantName}: \"");
				_inferenceParams.Antiprompts.Add(lastMessage.GetUserName()+":");
			}
			else
			{
				// formattedMessages.Add($"Write your response as if you were {assistantName}, without \"{assistantName}: \"");
				_inferenceParams.Antiprompts.Add(lastMessage.GetUserName()+":");
			}
		}

		// format the conversation when there's messages
		if (formattedMessages.Count > 0)
		{

			formattedPrompt = String.Join("\n", formattedMessages);
		}

		return formattedPrompt;
	}

	public async Task<StatefulChatMessage> ChatAsync(LlamaModelInstance modelInstance)
	{
		_modelInstance = modelInstance;

		InferenceResult inferenceResult = await _inferenceService.InferAsync(modelInstance.ModelDefinition.Id, GetPrompt(), stateful:_stateful, (_instanceStateId.Length > 0 ? _instanceStateId : _modelInstance.InstanceId), _loadParams, _inferenceParams);

		string content = inferenceResult.OutputStripped;

		// strip out chat names from the response
		foreach (string name in _knownUserNames)
		{
			content = content.Replace($"{name}: ", "");
			content = content.Replace($"{name}:", "");
			content = content.Trim();
		}

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

	public string Format()
	{
		return $"{GetUserName()}: {Content}";
	}

	public string GetUserName()
	{
		string userName = Role;
		if (Name != "" && Name != null)
		{
			userName = Name;
		}

		return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(userName);
	}
}

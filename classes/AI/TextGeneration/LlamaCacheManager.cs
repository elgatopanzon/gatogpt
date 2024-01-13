/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaCacheManager
 * @created     : Tuesday Jan 09, 2024 22:55:11 CST
 */

namespace GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using LLama;
using LLama.Common;

using System.Security.Cryptography;
using System.Text.RegularExpressions;

public partial class LlamaCacheManager
{
	private string _stateId { get; set; }

	private string _cacheDataDir { get; set; } = Path.Combine(OS.GetUserDataDir(), "Cache");

	public LlamaCacheManager(uint? modelContextSize, float? modelRopeBase, float? modelRopeScale, string modelId, string modelHash)
	{
		_stateId = $"{modelId}-{modelHash}-{modelContextSize}-{modelRopeBase}-{modelRopeScale}";

		LoggerManager.LogDebug("Creating cache manager", "", "stateId", _stateId);

		CreateCacheDir();
	}

	public void CreateCacheDir()
	{
		Directory.CreateDirectory(_cacheDataDir);

		Directory.CreateDirectory(GetCacheBaseDir());
	}

	public string GetCacheBaseDir()
	{
		return Path.Combine(_cacheDataDir, _stateId);
	}

	public string GetCacheSaveDir(string cacheId, string subName = "")
	{
		return Path.Combine(GetCacheBaseDir(), cacheId, subName);
	}

	public async Task<bool> SavePromptCache(string prompt, LLamaContext context, InstructExecutor executor)
	{
		string cacheId = GetPromptContentHash(prompt);
		string promptSavePath = GetCacheSaveDir(cacheId, "prompt");

		LoggerManager.LogDebug("Saving prompt cache", "", "stateId", _stateId);
		LoggerManager.LogDebug("", "", "prompt", prompt);
		LoggerManager.LogDebug("", "", "cacheId", cacheId);
		LoggerManager.LogDebug("", "", "saveDir", promptSavePath);

		// create directory to hold the state files
		Directory.CreateDirectory(GetCacheSaveDir(cacheId));

		// save the full prompt content
		using (StreamWriter writer = new StreamWriter(promptSavePath))
		{
			writer.Write(prompt);
		}

		// save the context and executor states
		context.SaveState(GetCacheSaveDir(cacheId, "context"));
		await executor.SaveState(GetCacheSaveDir(cacheId, "executor"));

		return true;
	}

	public string GetPromptContentHash(string content)
	{
		string hash = "";

		byte[] bytes;

		using (HashAlgorithm algorithm = SHA256.Create())
        	bytes = algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));

		hash = "";
        foreach (byte b in bytes)
        {
        	hash += b.ToString("x2");
        }

        return hash;
	}

	public async Task<string> GetCachedPrompt(string prompt, AI.TextGeneration.InferenceParams inferenceParams, LLamaContext context, InstructExecutor executor)
	{
		// search the cache directory for existing states, then search for
		// prompt files matching the given prompt
		DirectoryInfo dir = new DirectoryInfo (GetCacheBaseDir());

		DirectoryInfo[] dirs = dir.GetDirectories().OrderByDescending(p => p.CreationTime).ToArray();

		foreach (DirectoryInfo cacheDir in dirs)
		{
    		// LoggerManager.LogDebug("Found cache dir", "", "cacheDir", cacheDir.ToString());

			// strip the input prefix and suffix when comparing the prompts
    		string promptCache = File.ReadAllText(Path.Combine(cacheDir.ToString(), "prompt")).Replace(inferenceParams.InputPrefix, "").Replace(inferenceParams.InputSuffix, "");
    		string currentPrompt = prompt.Replace(inferenceParams.InputPrefix, "").Replace(inferenceParams.InputSuffix, "");

    		// LoggerManager.LogDebug("", "", "cachePrompt", promptCache);
    		// LoggerManager.LogDebug("", "", "currentPrompt", currentPrompt);

    		if (promptCache.StartsWith(currentPrompt))
    		{
    			LoggerManager.LogDebug("Prompt cache hit!");

    			string strippedPrompt = ExtractPromptAdditionalText(currentPrompt, promptCache);

    			LoggerManager.LogDebug("Stripped prompt after cache", "", "strippedPrompt", strippedPrompt);

    			// load the state
    			string cacheId = cacheDir.ToString().GetFile();
    			context.LoadState(GetCacheSaveDir(cacheId, "context"));
    			await executor.LoadState(GetCacheSaveDir(cacheId, "executor"));

				// return stripped prompt including the suffix we removed
				// earlier
    			return strippedPrompt+inferenceParams.InputSuffix;
    		}

		}

		LoggerManager.LogDebug("Cache miss!");

		return prompt;
	}

	public string ExtractPromptAdditionalText(string currentPrompt, string promptCache)
	{
		var regex = new Regex(Regex.Escape(promptCache));
		var newText = regex.Replace(currentPrompt, "", 1);

		return newText;
	}

	public void DeleteCache()
	{
		LoggerManager.LogDebug("Deleting all cache", "", "stateId", _stateId);

		Directory.Delete(GetCacheBaseDir(), true);
	}
}


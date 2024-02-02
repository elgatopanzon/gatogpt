/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : StripAntiprompt
 * @created     : Tuesday Jan 30, 2024 21:48:52 CST
 */

namespace GatoGPT.AI.TextGeneration.TokenFilter;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using System.Text.RegularExpressions;

public partial class StripAntiprompt : ITokenFilter
{
	public List<string> Antiprompts { get; set; }

	public StripAntiprompt(List<string> antiprompts)
	{
		Antiprompts = antiprompts;
	}

	public bool Match(string[] tokens, string[] allTokens)
	{
		LoggerManager.LogDebug("Matching with tokens", "", "tokens", tokens);

		bool match = false;
		string tokensString = String.Join("", tokens);

		foreach (string antiprompt in Antiprompts)
		{
			if (tokensString.Trim().Length > 0 && antiprompt.StartsWith(tokensString))
			{
				LoggerManager.LogDebug("Token partial match anti-prompt", "", tokensString, antiprompt);

				match = true;
				break;
			}
		}

		return match;
	}

	public string[] Filter(string[] tokens, string[] allTokens)
	{
		int tokensCount = tokens.Count();
		string tokensString = String.Join("", tokens);

		foreach (var antiprompt in Antiprompts)
		{
			tokensString = tokensString.Replace(antiprompt, String.Empty);
		}

		// make fake array of tokens simply because after stripping some away
		// it's not possible to return them to token form
		string[] fakeArray = new string[] { tokensString };

		return fakeArray;
	}
}


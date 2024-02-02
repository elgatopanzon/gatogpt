/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : StripLeadingSpace
 * @created     : Tuesday Jan 30, 2024 20:19:18 CST
 */

namespace GatoGPT.AI.TextGeneration.TokenFilter;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using System.Text.RegularExpressions;

public partial class StripLeadingSpace : ITokenFilter
{
	public bool Match(string[] tokens, string[] allTokens)
	{
		bool match = false;
		string tokensString = String.Join("", tokens);

		// check if we're dealing with a token which starts with a space and all
		// current tokens once stripped don't equal a length
		match = Regex.IsMatch(tokensString, @"^[ ]+[\w]+$") && String.Join("", allTokens).Trim().Length == 0;

		return match;
	}

	public string[] Filter(string[] tokens, string[] allTokens)
	{
		string str = String.Join("", tokens);
		str = str.Trim();

		LoggerManager.LogDebug("After filter", "", "after", str);

		return new string[] { str };
	}
}


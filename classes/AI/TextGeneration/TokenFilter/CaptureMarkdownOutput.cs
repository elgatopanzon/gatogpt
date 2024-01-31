/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CaptureMarkdownOutput
 * @created     : Tuesday Jan 30, 2024 23:16:15 CST
 */

namespace GatoGPT.AI.TextGeneration.TokenFilter;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using System.Text.RegularExpressions;

public partial class CaptureMarkdownOutput : ITokenFilter
{
	string _codeBlockStartPattern = @"[`]+[\w]*(\n*.)*\n*[`]*";
	string _codeBlockFullPattern = @"```([a-z]*)\n([\s\S]*?)\n```";

	public bool Match(string[] tokens, string[] allTokens)
	{
		LoggerManager.LogDebug("Matching with tokens", "", "tokens", tokens);

		bool match = false;
		string tokensString = String.Join("", tokens);


		Match blockStart = Regex.Match(tokensString, _codeBlockStartPattern, RegexOptions.Multiline);
		Match blockFull = Regex.Match(tokensString, _codeBlockFullPattern, RegexOptions.Multiline);

		if (blockStart.Success)
		{
			match = true;
		}
		if (blockFull.Success)
		{
			LoggerManager.LogDebug("Matched full code block", "", "tokens", tokensString);

			match = false;
		}

		return match;
	}

	public string[] Filter(string[] tokens, string[] allTokens)
	{
		string tokensString = String.Join("", tokens);
		Match blockFull = Regex.Match(tokensString, _codeBlockFullPattern, RegexOptions.Multiline);

		string code = blockFull.Groups[2].Value;
		string codeType = blockFull.Groups[1].Value;

		LoggerManager.LogDebug("Parsed code block from tokens", "", codeType, code);

		return new string[] { code };
	}
}


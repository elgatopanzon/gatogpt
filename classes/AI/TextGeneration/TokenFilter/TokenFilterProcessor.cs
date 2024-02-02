/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : TokenFilterProcessor
 * @created     : Tuesday Jan 30, 2024 19:54:16 CST
 */

namespace GatoGPT.AI.TextGeneration.TokenFilter;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class StreamingTokenFilter
{
	public List<string> FilteredTokens = new();
	public List<string> ReleasedTokens = new();
	public List<ITokenFilter> Filters { get; set; } = new();

	public void AddFilter(ITokenFilter filter)
	{
		Filters.Add(filter);
	}

	public bool FilterToken(string token, string[] allTokens)
	{
		bool filtered = false;

		foreach (ITokenFilter filter in Filters)
		{
			LoggerManager.LogDebug("Running filter", "", filter.GetType().Name, String.Join("", FilteredTokens)+token);

			filtered = filter.Match(FilteredTokens.Concat(new string[] { token }).ToArray(), allTokens);

			LoggerManager.LogDebug("Filter result", "", filter.GetType().Name, filtered);

			if (filtered)
			{
				FilteredTokens.Add(token);

				LoggerManager.LogDebug("Filtering matched token", "", "token", token);
				LoggerManager.LogDebug("Current filtered tokens", "", "filteredTokens", FilteredTokens);

				return filtered;
			}
		}

		if (!filtered)
		{
			if (FilteredTokens.Count > 0 && ReleasedTokens.Count == 0)
			{
				LoggerManager.LogDebug("Release filtered tokens", "", "filteredTokens", FilteredTokens);

				var t = FilteredTokens.Concat(new string[] { token }).ToArray();

				foreach (ITokenFilter filter in Filters)
				{
					if (filter.Match(t, allTokens))
					{
						continue;
					}
					t = filter.Filter(t, allTokens);
				}

				ReleasedTokens = t.ToList();

				FilteredTokens = new();
			}
		}

		return filtered;
	}
}


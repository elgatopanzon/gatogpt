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
			LoggerManager.LogDebug("Running filter", "", "filter", filter.GetType().Name);

			filtered = filter.Match(FilteredTokens.Concat(new string[] { token }).ToArray(), allTokens);

			if (filtered)
			{
				FilteredTokens.Add(token);

				LoggerManager.LogDebug("Filtering matched token", "", "token", token);
				LoggerManager.LogDebug("Current filtered tokens", "", "filteredTokens", FilteredTokens);

				break;
			}
			else
			{
				if (FilteredTokens.Count > 0 && ReleasedTokens.Count == 0)
				{
					LoggerManager.LogDebug("Release filtered tokens", "", "filteredTokens", FilteredTokens);

					var t = FilteredTokens.Concat(new string[] { token }).ToArray();

					foreach (ITokenFilter releaseFilter in Filters)
					{
						ReleasedTokens = releaseFilter.Filter(t, allTokens).ToList();
					}

					FilteredTokens = new();
				}
			}
		}

		return filtered;
	}
}


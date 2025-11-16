using System;
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StudentsMcpServer.Models;

namespace ServerMCP.Models;

[McpServerToolType]
public static class TicketmasterTools
{
    private static readonly TicketmasterService _service = new TicketmasterService();

    [McpServerTool, Description("Get events as JSON array (uses default Ticketmaster request url)")]
    public static string GetEventsJson()
    {
        var task = _service.GetEventsJson();
        return task.GetAwaiter().GetResult();
    }

    [McpServerTool, Description("Get events as JSON array using a custom Ticketmaster request URL")]
    public static string GetEventsJsonWithUrl([Description("Full Ticketmaster request URL, including apikey and query params")] string requestUrl)
    {
        var task = _service.GetEventsJson(requestUrl);
        return task.GetAwaiter().GetResult();
    }

    [McpServerTool, Description("Get all events and return as JSON using simple DTOs")]
    public static string GetEventsAsDtoJson()
    {
        var task = _service.GetEvents();
        var events = task.GetAwaiter().GetResult();
        return JsonSerializer.Serialize(events);
    }

    [McpServerTool, Description("Get an event by id and return as JSON")]
    public static string GetEventByIdJson([Description("The ticketmaster event id to lookup")] string id)
    {
        var task = _service.GetEventById(id);
        var ev = task.GetAwaiter().GetResult();
        if (ev == null) return "Event not found";
        return JsonSerializer.Serialize(ev);
    }

    [McpServerTool, Description("Get events by venue name and return as JSON")]
    public static string GetEventsByVenueJson([Description("Venue name to filter events by")] string venue)
    {
        var task = _service.GetEventsByVenue(venue);
        var list = task.GetAwaiter().GetResult();
        return JsonSerializer.Serialize(list);
    }

    [McpServerTool, Description("Get events by artist name and return as JSON")]
    public static string GetEventsByArtistJson([Description("Artist name to filter events by")] string artist)
    {
        var task = _service.GetEventsByArtist(artist);
        var list = task.GetAwaiter().GetResult();
        return JsonSerializer.Serialize(list);
    }

    [McpServerTool, Description("Show events for an artist from a natural language query, e.g. 'show events for artist taylor swift'")]
    public static string ShowEventsForArtistJson([Description("Natural language query or artist name")] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return "No artist provided";

        // Improved parsing: extract optional location (in/at/near) and strip leading phrases
        var trimmed = query.Trim();
        var lower = trimmed.ToLowerInvariant();
        int? dmaId = null;

        // Try to extract location using " in <location>", " at <location>", or " near <location>" at the end
        var locMatch = System.Text.RegularExpressions.Regex.Match(lower, "\\b(in|at|near)\\s+(.+)$");
        string artistPart = trimmed;
        if (locMatch.Success)
        {
            var locPhrase = locMatch.Groups[2].Value.Trim().Trim(',', '.');
            artistPart = trimmed.Substring(0, locMatch.Index).Trim();

            // mapping of common location names -> dmaId
            var dmaMap = new System.Collections.Generic.Dictionary<string,int>(StringComparer.OrdinalIgnoreCase)
            {
                ["burnaby"] = 504, ["new westminster"] = 504, ["surrey"] = 504,
                ["toronto"] = 527,
                ["vancouver"] = 528
            };

            var loc = locPhrase.ToLowerInvariant();
            foreach (var kv in dmaMap)
            {
                if (loc.Contains(kv.Key.ToLowerInvariant()))
                {
                    dmaId = kv.Value;
                    break;
                }
            }
        }

        // Strip common leading phrases like "show me events for", "show events for artist", etc.
        var artist = artistPart;
        var leadingPatterns = new[] {
            "show me events for artist ", "show me events for ", "show events for artist ", "show events for ",
            "events for artist ", "events for ", "find events for artist ", "find events for ", "show artist "
        };
        var lowerArtist = artist.ToLowerInvariant();
        foreach (var p in leadingPatterns)
        {
            if (lowerArtist.StartsWith(p))
            {
                artist = artist.Substring(p.Length).Trim();
                lowerArtist = artist.ToLowerInvariant();
                break;
            }
        }

        // Also drop a leading 'artist ' token if present
        if (lowerArtist.StartsWith("artist "))
        {
            artist = artist.Substring("artist ".Length).Trim();
            lowerArtist = artist.ToLowerInvariant();
        }

        // Remove surrounding quotes
        artist = artist.Trim('"', '\'');

        if (string.IsNullOrWhiteSpace(artist)) return "No artist extracted from query";

        var task = _service.GetEventsByArtist(artist, dmaId);
        var list = task.GetAwaiter().GetResult();
        return JsonSerializer.Serialize(list);
    }

    [McpServerTool, Description("Search events by name fragment and return JSON")]
    public static string SearchEventsByNameJson([Description("Fragment of the event name to search")] string nameFragment)
    {
        var task = _service.SearchEventsByName(nameFragment);
        var list = task.GetAwaiter().GetResult();
        return JsonSerializer.Serialize(list);
    }

    [McpServerTool, Description("Answer when the next event(s) for an artist are. Accepts natural language queries like 'when is the event for Shawn Desman?'")]
    public static string WhenIsEventForArtist([Description("Artist name or natural language query")] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return "No artist provided";

        // Basic parsing: strip leading question phrases and optional location (in/at/near)
        var trimmed = query.Trim();
        var lower = trimmed.ToLowerInvariant();
        int? dmaId = null;

        // extract location at end if present
        var locMatch = System.Text.RegularExpressions.Regex.Match(lower, "\\b(in|at|near)\\s+(.+)$");
        var artistPart = trimmed;
        if (locMatch.Success)
        {
            var locPhrase = locMatch.Groups[2].Value.Trim().Trim(',', '.');
            artistPart = trimmed.Substring(0, locMatch.Index).Trim();

            var dmaMap = new System.Collections.Generic.Dictionary<string,int>(StringComparer.OrdinalIgnoreCase)
            {
                ["burnaby"] = 504, ["new westminster"] = 504, ["surrey"] = 504,
                ["toronto"] = 527,
                ["vancouver"] = 528
            };

            var loc = locPhrase.ToLowerInvariant();
            foreach (var kv in dmaMap)
            {
                if (loc.Contains(kv.Key.ToLowerInvariant()))
                {
                    dmaId = kv.Value;
                    break;
                }
            }
        }

        // remove common leading question phrasing
        var artist = artistPart;
        var lead = new[] { "when is the event for ", "when is the event for artist ", "when is the event ", "when is the show for ", "when is " };
        var lowerArtist = artist.ToLowerInvariant();
        foreach (var p in lead)
        {
            if (lowerArtist.StartsWith(p))
            {
                artist = artist.Substring(p.Length).Trim();
                lowerArtist = artist.ToLowerInvariant();
                break;
            }
        }

        artist = artist.Trim('"', '\'');
        if (string.IsNullOrWhiteSpace(artist)) return "No artist extracted from query";

        var task = _service.GetEventsByArtist(artist, dmaId);
        var list = task.GetAwaiter().GetResult();
        if (list == null || list.Count == 0)
        {
            return $"It seems that there are currently no upcoming events for {artist} in the selected regions.";
        }

        // Try to parse and sort by StartDate when available
        var parsed = list.Select(ev =>
        {
            DateTime? dt = null;
            if (!string.IsNullOrWhiteSpace(ev.StartDate))
            {
                if (DateTime.TryParse(ev.StartDate, out var d)) dt = d;
            }
            return (Ev: ev, Date: dt);
        })
        .OrderBy(t => t.Date ?? DateTime.MaxValue)
        .ThenBy(t => t.Ev.Name)
        .ToList();

        // Build response: list up to 3 upcoming events
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Upcoming events for {artist}:");
        int max = Math.Min(3, parsed.Count);
        for (int i = 0; i < max; i++)
        {
            var ev = parsed[i].Ev;
            var date = parsed[i].Date;
            var dateStr = date.HasValue ? date.Value.ToString("dddd, MMM d yyyy h:mm tt") : (ev.StartDate ?? "(date unknown)");
            sb.AppendLine($"- {ev.Name ?? artist} at {ev.VenueName ?? "(venue unknown)"} on {dateStr} {(!string.IsNullOrWhiteSpace(ev.Url) ? $"(Info: {ev.Url})" : "")}".Trim());
        }

        if (parsed.Count > max)
        {
            sb.AppendLine($"And {parsed.Count - max} more event(s) available.");
        }

        return sb.ToString().Trim();
    }

    [McpServerTool, Description("Get total cached event count")]
    public static int GetEventCount()
    {
        var task = _service.GetEvents();
        var list = task.GetAwaiter().GetResult();
        return list?.Count ?? 0;
    }

    [McpServerTool, Description("Check whether an event appears to be family friendly (heuristic)")]
    public static string IsEventFamilyFriendly([Description("Event name or query, e.g. 'Top Gun: Maverick in Concert'")] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return "No event name provided";

        // Step 1: search events by name fragment
        var searchTask = _service.SearchEventsByName(query);
        var list = searchTask.GetAwaiter().GetResult();
        if (list == null || list.Count == 0) return $"No events found matching '{query}'";

        // Pick best match: prefer exact/contains match on name
        var qLower = query.ToLowerInvariant();
        var best = list.OrderByDescending(e =>
        {
            var score = 0;
            if (!string.IsNullOrWhiteSpace(e.Name) && e.Name!.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 10;
            if (!string.IsNullOrWhiteSpace(e.Name) && e.Name!.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) score += 5;
            if (!string.IsNullOrWhiteSpace(e.ArtistName) && e.ArtistName!.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) score += 3;
            return score;
        }).First();

        var name = best.Name ?? "(unknown)";
        var url = best.Url ?? string.Empty;

        // If we have an id, fetch the full EventItem to inspect structured fields
        if (!string.IsNullOrWhiteSpace(best.Id))
        {
            var itemTask = _service.GetEventItemById(best.Id!);
            var item = itemTask.GetAwaiter().GetResult();
            if (item != null)
            {
                // Check age restrictions first
                if (item.AgeRestrictions?.LegalAgeEnforced == true)
                {
                    return $"No — '{name}' has legal age enforcement according to the event details. Check: {url}";
                }

                // Check classifications for family flag
                if (item.Classifications != null && item.Classifications.Any(c => c.Family == true))
                {
                    return $"Yes — '{name}' is marked as family friendly in its classification. {(!string.IsNullOrWhiteSpace(url) ? $"More info: {url}" : string.Empty)}".Trim();
                }

                // Check pleaseNote / info for explicit age wording
                var combined = ((item.PleaseNote ?? string.Empty) + " " + (item.Info ?? string.Empty)).ToLowerInvariant();
                var negativeIndicators = new[] { "18+", "21+", "age restriction", "age limit", "adults only", "mature" };
                foreach (var n in negativeIndicators)
                {
                    if (combined.Contains(n))
                    {
                        return $"No — '{name}' appears to have an age restriction ({n}). Check: {url}";
                    }
                }

                var positiveIndicators = new[] { "all ages", "family", "suitable for all", "kids", "children" };
                foreach (var p in positiveIndicators)
                {
                    if (combined.Contains(p))
                    {
                        return $"Yes — '{name}' appears to be family friendly ({p}). {(!string.IsNullOrWhiteSpace(url) ? $"More info: {url}" : string.Empty)}".Trim();
                    }
                }

                // If venue embedded has generalInfo child rule suggesting family friendliness
                if (item.Embedded?.Venues != null && item.Embedded.Venues.Count > 0)
                {
                    var v = item.Embedded.Venues[0];
                    if (v.GeneralInfo != null && !string.IsNullOrWhiteSpace(v.GeneralInfo.ChildRule))
                    {
                        return $"Possibly family friendly — venue child policy: {v.GeneralInfo.ChildRule}. Check: {url}";
                    }
                }
            }
        }

        // Fallback: simple heuristics based on name/venue/artist
        var fallbackCombined = ((best.Name ?? string.Empty) + " " + (best.VenueName ?? string.Empty) + " " + (best.ArtistName ?? string.Empty)).ToLowerInvariant();
        var pos = new[] { "all ages", "family", "kids", "children" };
        foreach (var p in pos)
        {
            if (fallbackCombined.Contains(p))
                return $"Yes — '{name}' appears family friendly ({p}). {(!string.IsNullOrWhiteSpace(url) ? $"More info: {url}" : string.Empty)}".Trim();
        }

        // If still inconclusive, return the ticket URL and suggest checking explicit fields
        return $"I couldn't determine definitively whether '{name}' is family friendly. Check the ticket page for age/ratings: {url}";
    }

    [McpServerTool, Description("Check whether an event has age restrictions (inspects Ticketmaster event details)")]
    public static string IsEventAgeRestricted([Description("Event name or id or natural query, e.g. 'Andy Kim Christmas'")] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return "No event name provided";

        // First try to find events by name fragment
        var searchTask = _service.SearchEventsByName(query);
        var list = searchTask.GetAwaiter().GetResult();
        if (list == null || list.Count == 0)
        {
            return $"No events found matching '{query}'";
        }

        // Choose best candidate (exact/contains scoring)
        var best = list.OrderByDescending(e =>
        {
            var score = 0;
            if (!string.IsNullOrWhiteSpace(e.Name) && e.Name!.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 10;
            if (!string.IsNullOrWhiteSpace(e.Name) && e.Name!.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) score += 5;
            if (!string.IsNullOrWhiteSpace(e.ArtistName) && e.ArtistName!.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) score += 3;
            return score;
        }).First();

        var name = best.Name ?? "(unknown)";
        var url = best.Url ?? string.Empty;

        // If we have an id, fetch the EventItem and inspect age restrictions
        if (!string.IsNullOrWhiteSpace(best.Id))
        {
            var item = _service.GetEventItemById(best.Id!).GetAwaiter().GetResult();
            if (item != null)
            {
                var ar = item.AgeRestrictions;
                if (ar != null)
                {
                    if (ar.LegalAgeEnforced == true)
                    {
                        var desc = string.IsNullOrWhiteSpace(ar.AgeRuleDescription) ? "legal age enforced" : ar.AgeRuleDescription;
                        return $"Yes — '{name}' has age restrictions: {desc}. See {url}";
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(ar.AgeRuleDescription))
                        {
                            return $"Note — '{name}' provides age info: {ar.AgeRuleDescription}. Please check {url} for details.";
                        }
                        return $"No — '{name}' does not appear to have legal age enforcement according to the event details. See {url}";
                    }
                }

                // Fallback: inspect pleaseNote/info for an age rule description
                var combined = ((item.PleaseNote ?? string.Empty) + " " + (item.Info ?? string.Empty)).ToLowerInvariant();
                if (combined.Contains("16 & over") || combined.Contains("16+") || combined.Contains("18+") || combined.Contains("21+"))
                {
                    return $"Yes — '{name}' appears to have an age rule mentioned: {combined}. See {url}";
                }
            }
        }

        // Finally, fallback to heuristic on the cached small DTO
        var fallbackCombined = ((best.Name ?? string.Empty) + " " + (best.VenueName ?? string.Empty) + " " + (best.ArtistName ?? string.Empty)).ToLowerInvariant();
        if (fallbackCombined.Contains("16") || fallbackCombined.Contains("18+") || fallbackCombined.Contains("21+"))
        {
            return $"Likely yes — '{name}' mentions age-sensitive text. Check: {url}";
        }

        return $"I couldn't find explicit age restriction information for '{name}'. Check the ticket page: {url}";
    }
}

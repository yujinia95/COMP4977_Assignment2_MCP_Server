using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ServerMCP.Models;

namespace StudentsMcpServer.Models
{
    // Simple representation of a Ticketmaster event for this service.
    public class TicketmasterEvent
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? VenueName { get; set; }
        public string? ArtistName { get; set; }
        public string? Url { get; set; }
        public string? StartDate { get; set; } // ISO date string if available
        // Additional fields to avoid refetching full EventItem
        public string? PleaseNote { get; set; }
        public bool? LegalAgeEnforced { get; set; }
        public string? AgeRuleDescription { get; set; }
        public override string ToString() => $"{Name} ({Id}) @ {VenueName} on {StartDate}";
    }

    public class TicketmasterService
    {
        private readonly HttpClient _httpClient;
        // Cache keyed by dmaId
        private readonly Dictionary<int, (List<TicketmasterEvent> Events, DateTime CacheTime)> _cacheByDma = new();
        // Cache duration: 15 minutes as requested
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);

        // Known DMA ids to preload (Toronto, Vancouver, and Surrey/Burnaby)
        private static readonly int[] KnownDmaIds = new[] { 504, 527, 528 };

        // Keep API key/config here (ideally load from config/env in production)
        private readonly string? _apiKey;
        private readonly string _baseUrl;
        private const int DefaultDmaId = 500;

        // Constructor for DI (HttpClient injected)
        public TicketmasterService(HttpClient httpClient, Microsoft.Extensions.Options.IOptions<TicketmasterOptions>? options = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            if (options?.Value != null)
            {
                _apiKey = options.Value.ApiKey;
                _baseUrl = options.Value.BaseUrl?.TrimEnd('/') ?? "https://app.ticketmaster.com/discovery/v2";
            }
            else
            {
                _apiKey = null;
                _baseUrl = "https://app.ticketmaster.com/discovery/v2";
            }
        }

        // Parameterless constructor for callers that use 'new TicketmasterService()' (reads appsettings)
        public TicketmasterService()
        {
            _httpClient = new HttpClient();
            try
            {
                var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                _apiKey = config["TicketmasterApi:ApiKey"];
                var baseUrlCfg = config["TicketmasterApi:BaseUrl"];
                _baseUrl = string.IsNullOrWhiteSpace(baseUrlCfg) ? "https://app.ticketmaster.com/discovery/v2" : baseUrlCfg.TrimEnd('/');
            }
            catch
            {
                _apiKey = null;
                _baseUrl = "https://app.ticketmaster.com/discovery/v2";
            }
        }

        private async Task<List<TicketmasterEvent>> FetchEventsFromApi(int dmaId)
        {
            // Build request URL using configured base and api key
            var requestUrl = $"{_baseUrl}/events.json?classificationName=music&dmaId={dmaId}";
            if (!string.IsNullOrWhiteSpace(_apiKey)) requestUrl += $"&apikey={_apiKey}";
            try
            {
                using var resp = await _httpClient.GetAsync(requestUrl);
                if (!resp.IsSuccessStatusCode)
                {
                    await Console.Error.WriteLineAsync($"Ticketmaster API returned {(int)resp.StatusCode} {resp.ReasonPhrase}");
                    return new List<TicketmasterEvent>();
                }

                await using var stream = await resp.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                var root = doc.RootElement;

                var events = new List<TicketmasterEvent>();

                // Ticketmaster wraps events under "_embedded" -> "events"
                if (root.TryGetProperty("_embedded", out var embedded) &&
                    embedded.ValueKind == JsonValueKind.Object &&
                    embedded.TryGetProperty("events", out var eventsElement) &&
                    eventsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in eventsElement.EnumerateArray())
                    {
                        var ev = new TicketmasterEvent();

                        if (el.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
                            ev.Id = idEl.GetString();

                        if (el.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                            ev.Name = nameEl.GetString();

                        if (el.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == JsonValueKind.String)
                            ev.Url = urlEl.GetString();

                        // dates.start.localDate (and possibly localTime)
                        if (el.TryGetProperty("dates", out var datesEl) &&
                            datesEl.ValueKind == JsonValueKind.Object &&
                            datesEl.TryGetProperty("start", out var startEl) &&
                            startEl.ValueKind == JsonValueKind.Object)
                        {
                            if (startEl.TryGetProperty("localDate", out var localDate) && localDate.ValueKind == JsonValueKind.String)
                            {
                                ev.StartDate = localDate.GetString();
                                if (startEl.TryGetProperty("localTime", out var localTime) && localTime.ValueKind == JsonValueKind.String)
                                    ev.StartDate += " " + localTime.GetString();
                            }
                        }

                        // venue: _embedded -> venues[0].name
                        if (el.TryGetProperty("_embedded", out var evEmbedded) &&
                            evEmbedded.ValueKind == JsonValueKind.Object &&
                            evEmbedded.TryGetProperty("venues", out var venuesEl) &&
                            venuesEl.ValueKind == JsonValueKind.Array &&
                            venuesEl.GetArrayLength() > 0)
                        {
                            var firstVenue = venuesEl[0];
                            if (firstVenue.ValueKind == JsonValueKind.Object &&
                                firstVenue.TryGetProperty("name", out var venueNameEl) &&
                                venueNameEl.ValueKind == JsonValueKind.String)
                            {
                                ev.VenueName = venueNameEl.GetString();
                            }
                        }

                        // attractions (artists) under _embedded -> attractions[0].name
                        if (el.TryGetProperty("_embedded", out var evEmb2) &&
                            evEmb2.ValueKind == JsonValueKind.Object &&
                            evEmb2.TryGetProperty("attractions", out var attractionsEl) &&
                            attractionsEl.ValueKind == JsonValueKind.Array &&
                            attractionsEl.GetArrayLength() > 0)
                        {
                            var firstAttraction = attractionsEl[0];
                            if (firstAttraction.ValueKind == JsonValueKind.Object &&
                                firstAttraction.TryGetProperty("name", out var artistNameEl) &&
                                artistNameEl.ValueKind == JsonValueKind.String)
                            {
                                ev.ArtistName = artistNameEl.GetString();
                            }
                        }

                        // pleaseNote and ageRestrictions (if present) — include in cached DTO to avoid extra fetch
                        if (el.TryGetProperty("pleaseNote", out var pleaseNoteEl) && pleaseNoteEl.ValueKind == JsonValueKind.String)
                        {
                            ev.PleaseNote = pleaseNoteEl.GetString();
                        }

                        if (el.TryGetProperty("ageRestrictions", out var arEl) && arEl.ValueKind == JsonValueKind.Object)
                        {
                            if (arEl.TryGetProperty("legalAgeEnforced", out var lae) && (lae.ValueKind == JsonValueKind.True || lae.ValueKind == JsonValueKind.False))
                            {
                                try { ev.LegalAgeEnforced = lae.GetBoolean(); } catch { ev.LegalAgeEnforced = null; }
                            }
                            if (arEl.TryGetProperty("ageRuleDescription", out var ard) && ard.ValueKind == JsonValueKind.String)
                            {
                                ev.AgeRuleDescription = ard.GetString();
                            }
                        }

                        events.Add(ev);
                    }
                }
                else
                {
                    await Console.Error.WriteLineAsync("Ticketmaster response did not contain embedded events.");
                }

                return events;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error fetching events from Ticketmaster: {ex.Message}");
                return new List<TicketmasterEvent>();
            }
        }

        // Preload or refresh caches for all known DMA ids. Skips entries still fresh.
        public async Task PreloadAllDmaCachesAsync()
        {
            var tasks = new List<Task>();
            foreach (var id in KnownDmaIds)
            {
                // if entry exists and still fresh, skip
                if (_cacheByDma.TryGetValue(id, out var existing) && DateTime.UtcNow - existing.CacheTime <= _cacheDuration)
                {
                    continue;
                }

                // capture id for closure
                var dma = id;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var fetched = await FetchEventsFromApi(dma);
                        _cacheByDma[dma] = (fetched, DateTime.UtcNow);
                    }
                    catch (Exception ex)
                    {
                        await Console.Error.WriteLineAsync($"Error preloading DMA {dma}: {ex.Message}");
                    }
                }));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
                // individual tasks already log errors; swallow aggregate exceptions
            }
        }

        public async Task<List<TicketmasterEvent>> GetEvents(int? dmaId = null)
        {
            // If no specific DMA is requested, preload all known DMAs and return aggregated events
            if (dmaId == null)
            {
                await PreloadAllDmaCachesAsync();
                // aggregate cached events across all keys
                var all = _cacheByDma.Values.SelectMany(v => v.Events).ToList();
                return all;
            }

            // Honor incoming dmaId if provided
            int id = dmaId ?? DefaultDmaId;

            if (!_cacheByDma.TryGetValue(id, out var cached) || DateTime.UtcNow - cached.CacheTime > _cacheDuration)
            {
                var fetched = await FetchEventsFromApi(id);
                _cacheByDma[id] = (fetched, DateTime.UtcNow);
            }

            // Fire-and-forget refresh for other DMAs to warm cache in background
            _ = Task.Run(async () =>
            {
                try { await PreloadAllDmaCachesAsync(); } catch { }
            });

            return _cacheByDma[id].Events;
        }

        public async Task<TicketmasterEvent?> GetEventById(string id, int? dmaId = null)
        {
            var events = await GetEvents(dmaId);
            var ev = events.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine(ev == null ? $"No event found with ID {id}" : $"Found event: {ev}");
            return ev;
        }

        public async Task<List<TicketmasterEvent>> GetEventsByVenue(string venue, int? dmaId = null)
        {
            var events = await GetEvents(dmaId);
            var filtered = events
                .Where(e => !string.IsNullOrWhiteSpace(e.VenueName) &&
                            e.VenueName!.Equals(venue, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine(filtered.Count == 0
                ? $"No events found for venue: {venue}"
                : $"Found {filtered.Count} events for venue: {venue}");

            return filtered;
        }

        public async Task<List<TicketmasterEvent>> GetEventsByArtist(string artist, int? dmaId = null)
        {
            var events = await GetEvents(dmaId);
            // Relax matching: match if artist string appears in the attraction name or event name (case-insensitive)
            var filtered = events
                .Where(e => (!string.IsNullOrWhiteSpace(e.ArtistName) &&
                             e.ArtistName!.IndexOf(artist, StringComparison.OrdinalIgnoreCase) >= 0)
                            || (!string.IsNullOrWhiteSpace(e.Name) &&
                                e.Name!.IndexOf(artist, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            Console.WriteLine(filtered.Count == 0
                ? $"No events found for artist: {artist} (tried ArtistName and Event Name)"
                : $"Found {filtered.Count} events for artist: {artist}");

            return filtered;
        }

        public async Task<List<TicketmasterEvent>> SearchEventsByName(string nameFragment, int? dmaId = null)
        {
            var events = await GetEvents(dmaId);
            var filtered = events
                .Where(e => !string.IsNullOrWhiteSpace(e.Name) &&
                            e.Name!.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            Console.WriteLine(filtered.Count == 0
                ? $"No events found matching: {nameFragment}"
                : $"Found {filtered.Count} events matching: {nameFragment}");

            return filtered;
        }

        public async Task<string> GetEventsJson(int? dmaId = null)
        {
            var events = await GetEvents(dmaId);
            return JsonSerializer.Serialize(events);
        }

        public async Task<string> GetEventsJson(string requestUrl)
        {
            if (string.IsNullOrWhiteSpace(requestUrl))
            {
                return await GetEventsJson(); // fallback to default behavior
            }

            try
            {
                using var resp = await _httpClient.GetAsync(requestUrl);
                if (!resp.IsSuccessStatusCode)
                {
                    await Console.Error.WriteLineAsync($"Ticketmaster API returned {(int)resp.StatusCode} {resp.ReasonPhrase} for custom URL: {requestUrl}");
                    return "[]";
                }

                var json = await resp.Content.ReadAsStringAsync();
                return json;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error fetching events from custom Ticketmaster URL: {ex.Message}");
                return "[]";
            }
        }

        // Fetch a full Ticketmaster EventItem by id using the discovery v2 API
        public async Task<EventItem?> GetEventItemById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var requestUrl = $"{_baseUrl}/events/{id}.json";
            if (!string.IsNullOrWhiteSpace(_apiKey)) requestUrl += (requestUrl.Contains("?") ? "&" : "?") + $"apikey={_apiKey}";

            try
            {
                var json = await GetEventsJson(requestUrl);
                if (string.IsNullOrWhiteSpace(json) || json == "[]") return null;
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var item = JsonSerializer.Deserialize<EventItem>(json, opts);
                return item;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error fetching event detail for {id}: {ex.Message}");
                return null;
            }
        }
    }
}
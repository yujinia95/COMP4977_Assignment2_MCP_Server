using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ServerMCP.Models;

// Source generation context for System.Text.Json using the concrete model types
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TicketmasterResponse))]
[JsonSerializable(typeof(List<EventItem>))]
[JsonSerializable(typeof(EventItem))]
[JsonSerializable(typeof(Venue))]
[JsonSerializable(typeof(List<Venue>))]
internal sealed partial class TicketmasterContext : JsonSerializerContext { }
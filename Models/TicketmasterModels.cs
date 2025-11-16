using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ServerMCP.Models
{
    public class TicketmasterResponse
    {
        [JsonPropertyName("_embedded")]
        public EmbeddedRoot? Embedded { get; set; }
    }

    public class EmbeddedRoot
    {
        [JsonPropertyName("events")]
        public List<EventItem>? Events { get; set; }
    }

    public class EventItem
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Id { get; set; }
        public bool? Test { get; set; }
        public string? Url { get; set; }
        public string? Locale { get; set; }
        public List<ImageItem>? Images { get; set; }
        public Sales? Sales { get; set; }
        public Dates? Dates { get; set; }
        public List<Classification>? Classifications { get; set; }
        public string? Info { get; set; }
        public string? PleaseNote { get; set; }
        public Seatmap? Seatmap { get; set; }
        public Accessibility? Accessibility { get; set; }
        public AgeRestrictions? AgeRestrictions { get; set; }
        public DoorsTimes? DoorsTimes { get; set; }
        public Ticketing? Ticketing { get; set; }

        [JsonPropertyName("_links")]
        public Links? Links { get; set; }

        [JsonPropertyName("_embedded")]
        public EventEmbedded? Embedded { get; set; }
    }

    public class ImageItem
    {
        public string? Ratio { get; set; }
        public string? Url { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool? Fallback { get; set; }
    }

    public class Sales
    {
        public PublicSale? Public { get; set; }
    }

    public class PublicSale
    {
        public DateTimeOffset? StartDateTime { get; set; }
        public bool? StartTbd { get; set; }
        public bool? StartTba { get; set; }
        public DateTimeOffset? EndDateTime { get; set; }
    }

    public class Dates
    {
        public Start? Start { get; set; }
        public string? Timezone { get; set; }
        public Status? Status { get; set; }
        public bool? SpanMultipleDays { get; set; }
    }

    public class Start
    {
        public string? LocalDate { get; set; }
        public string? LocalTime { get; set; }
        public DateTimeOffset? DateTime { get; set; }
        public bool? DateTbd { get; set; }
        public bool? DateTba { get; set; }
        public bool? TimeTba { get; set; }
        public bool? NoSpecificTime { get; set; }
    }

    public class Status
    {
        public string? Code { get; set; }
    }

    public class Classification
    {
        public bool? Primary { get; set; }
        public Segment? Segment { get; set; }
        public Genre? Genre { get; set; }
        public Genre? SubGenre { get; set; }
        public Genre? Type { get; set; }
        public Genre? SubType { get; set; }
        public bool? Family { get; set; }
    }

    public class Segment { public string? Id { get; set; } public string? Name { get; set; } }
    public class Genre { public string? Id { get; set; } public string? Name { get; set; } }

    public class Seatmap { public string? StaticUrl { get; set; } }

    public class Accessibility { public string? Info { get; set; } public int? TicketLimit { get; set; } }

    public class AgeRestrictions { public bool? LegalAgeEnforced { get; set; } public string? AgeRuleDescription { get; set; } }

    public class DoorsTimes
    {
        public string? LocalDate { get; set; }
        public string? LocalTime { get; set; }
        public DateTimeOffset? DateTime { get; set; }
    }

    public class Ticketing
    {
        public SafeTix? SafeTix { get; set; }
        public AllInclusivePricing? AllInclusivePricing { get; set; }
    }

    public class SafeTix { public bool? Enabled { get; set; } }
    public class AllInclusivePricing { public bool? Enabled { get; set; } }

    public class Links
    {
        public LinkItem? Self { get; set; }
        public List<LinkItem>? Attractions { get; set; }
        public List<LinkItem>? Venues { get; set; }
    }

    public class LinkItem { public string? Href { get; set; } }

    public class EventEmbedded
    {
        public List<Venue>? Venues { get; set; }
        public List<Attraction>? Attractions { get; set; }
    }

    public class Venue
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Id { get; set; }
        public bool? Test { get; set; }
        public string? Url { get; set; }
        public string? Locale { get; set; }
        public List<ImageItem>? Images { get; set; }
        public string? PostalCode { get; set; }
        public string? Timezone { get; set; }
        public City? City { get; set; }
        public State? State { get; set; }
        public Country? Country { get; set; }
        public Address? Address { get; set; }
        public Location? Location { get; set; }
        public List<Market>? Markets { get; set; }
        public List<Dma>? Dmas { get; set; }
        public BoxOfficeInfo? BoxOfficeInfo { get; set; }
        public string? ParkingDetail { get; set; }
        public string? AccessibleSeatingDetail { get; set; }
        public GeneralInfo? GeneralInfo { get; set; }
        public UpcomingEvents? UpcomingEvents { get; set; }
        [JsonPropertyName("_links")]
        public Links? Links { get; set; }
    }

    public class Attraction { public string? Name { get; set; } public string? Type { get; set; } public string? Id { get; set; } public bool? Test { get; set; } public string? Url { get; set; } public string? Locale { get; set; } }

    public class City { public string? Name { get; set; } }
    public class State { public string? Name { get; set; } public string? StateCode { get; set; } }
    public class Country { public string? Name { get; set; } public string? CountryCode { get; set; } }
    public class Address { public string? Line1 { get; set; } }
    public class Location { public string? Longitude { get; set; } public string? Latitude { get; set; } }
    public class Market { public string? Name { get; set; } public string? Id { get; set; } }
    public class Dma { public int? Id { get; set; } }
    public class BoxOfficeInfo { public string? PhoneNumberDetail { get; set; } public string? OpenHoursDetail { get; set; } public string? AcceptedPaymentDetail { get; set; } public string? WillCallDetail { get; set; } }
    public class GeneralInfo { public string? GeneralRule { get; set; } public string? ChildRule { get; set; } }
    public class UpcomingEvents { public int? Archtics { get; set; } public int? Ticketmaster { get; set; } public int? Total { get; set; } public int? Filtered { get; set; } }
}

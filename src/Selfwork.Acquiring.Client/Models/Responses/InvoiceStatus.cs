using System.Text.Json.Serialization;

namespace Selfwork.Acquiring.Client.Models.Responses;

/// <summary>Possible lifecycle states of a payment invoice.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceStatus
{
    /// <summary>Invoice created, awaiting payment.</summary>
    Pending,

    /// <summary>Payment received and confirmed.</summary>
    Paid,

    /// <summary>Invoice expired without payment.</summary>
    Expired,

    /// <summary>Invoice cancelled by request.</summary>
    Cancelled,

    /// <summary>Payment failed.</summary>
    Failed
}

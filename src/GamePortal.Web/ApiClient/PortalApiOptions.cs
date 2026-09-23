using System.ComponentModel.DataAnnotations;

namespace GamePortal.Web.ApiClient;

public sealed class PortalApiOptions
{
    public const string SectionName = "PortalApi";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}

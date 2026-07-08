// File    : AttachmentModels.cs
// Module  : ICare247.UI.DynamicForms
// Purpose : DTO đính kèm dùng chung giữa renderer (RCL) và impl AttachmentApiService (host).
//           Tách khỏi ICare247_UI.Services để interface IAttachmentApiService khép kín trong RCL.

namespace ICare247.UI.DynamicForms.Models;

/// <summary>Metadata 1 đính kèm (khớp AttachmentInfo backend, camelCase).</summary>
public sealed class AttachmentInfoDto
{
    public long Id { get; set; }
    public string TenFile { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long KichThuoc { get; set; }
    public bool HasThumbnail { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Option truyền sang JS uploader (XHR + nén ảnh client + progress).</summary>
public sealed class AttachmentUploadOptions
{
    public string Url { get; set; } = "";
    public string Token { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string? Loai { get; set; }
    public string? OwnerTable { get; set; }
    public long? OwnerId { get; set; }
    public string? FieldMa { get; set; }
    public bool CompressImages { get; set; } = true;
    public int MaxDimension { get; set; } = 2000;
    public double Quality { get; set; } = 0.85;
}

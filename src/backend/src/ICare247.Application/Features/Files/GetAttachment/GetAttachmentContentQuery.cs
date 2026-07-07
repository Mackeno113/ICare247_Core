// File    : GetAttachmentContentQuery.cs
// Module  : Files
// Layer   : Application
// Purpose : Lấy mô tả nội dung 1 đính kèm (ảnh chính hoặc thumbnail) để controller stream về client.
//           Trả TenFile + TepBlobContent (Storage_Kind + bytes/key) — controller mở stream qua IFileStore.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Files.GetAttachment;

/// <param name="AttachmentId">Id bản ghi TT_TepDinhKem.</param>
/// <param name="Thumbnail">true = lấy thumbnail; false = nội dung chính.</param>
public sealed record GetAttachmentContentQuery(long AttachmentId, bool Thumbnail)
    : IRequest<AttachmentContentDto?>;

/// <summary>Mô tả nội dung để stream: tên hiển thị + blob (nơi lưu + bytes/key).</summary>
/// <param name="TenFile">Tên gốc (dùng cho Content-Disposition).</param>
/// <param name="Blob">Nội dung + mô tả nơi lưu.</param>
public sealed record AttachmentContentDto(string TenFile, TepBlobContent Blob);

// File    : PickerDtos.cs
// Module  : Pickers
// Layer   : Application
// Purpose : DTO chung cho Picker API (spec 31 §3) — response gọn {id, ma, ten, parentId}.

namespace ICare247.Application.Features.Pickers;

/// <summary>1 dòng dữ liệu picker (tỉnh/xã/…): ParentId = node cha (null = gốc).</summary>
public sealed record PickerItemDto(long Id, string? Ma, string Ten, long? ParentId);

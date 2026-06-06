// File    : MasterDataResults.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Các DTO kết quả cho Save/Delete master data (validation + soft-check FK).

using ICare247.Application.Interfaces;

namespace ICare247.Application.Features.MasterData.Models;

/// <summary>Kết quả Insert/Update — thành công kèm Id mới, hoặc fail kèm lỗi validation.</summary>
/// <param name="Success">true = lưu thành công.</param>
/// <param name="Id">Giá trị PK (Insert trả Id mới; Update trả lại Id cũ).</param>
/// <param name="Errors">Danh sách lỗi validation theo field (rỗng khi Success).</param>
public sealed record MasterDataSaveResult(
    bool Success,
    object? Id,
    IReadOnlyList<MasterDataFieldError> Errors);

/// <summary>Một lỗi validation gắn với field.</summary>
public sealed record MasterDataFieldError(string FieldCode, string Message);

/// <summary>Kết quả Delete — thành công, hoặc bị chặn kèm danh sách nơi đang tham chiếu.</summary>
/// <param name="Success">true = đã xóa.</param>
/// <param name="BlockedBy">Nơi đang tham chiếu (rỗng khi Success). Non-rỗng = bị chặn.</param>
public sealed record MasterDataDeleteResult(
    bool Success,
    IReadOnlyList<ReferenceUsage> BlockedBy);

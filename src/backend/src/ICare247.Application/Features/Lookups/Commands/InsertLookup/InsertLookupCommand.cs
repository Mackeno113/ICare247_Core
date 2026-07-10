// File    : InsertLookupCommand.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Command thêm mới 1 bản ghi vào bảng nguồn của LookupBox (tính năng "thêm mới" trên control).

using MediatR;

namespace ICare247.Application.Features.Lookups.Commands.InsertLookup;

/// <summary>
/// Insert một entity mới vào bảng nguồn của field lookup rồi trả về khóa + display.
/// Handler đọc cấu hình từ <c>Ui_Field_Lookup</c> theo <c>FieldId</c> để biết bảng đích.
/// </summary>
/// <param name="FieldId">Field_Id của LookupBox — xác định bảng nguồn.</param>
/// <param name="TenantId">Tenant hiện tại — dựng cache key (ADR-035).</param>
/// <param name="Values">Cặp Cột↔Giá trị từ dialog thêm mới (key = tên cột DB).</param>
/// <param name="UserId">Người thao tác (claim sub) — engine bơm vào <c>CreatedBy</c>. Bảng có
/// khối cột audit mà thiếu UserId → repository ném lỗi rõ ràng thay vì để SQL báo NULL.</param>
public sealed record InsertLookupCommand(
    int                         FieldId,
    int                         TenantId,
    Dictionary<string, object?> Values,
    long?                       UserId = null
) : IRequest<IDictionary<string, object?>?>;

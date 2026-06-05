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
/// <param name="TenantId">Tenant hiện tại — verify ownership.</param>
/// <param name="Values">Cặp Cột↔Giá trị từ dialog thêm mới (key = tên cột DB).</param>
public sealed record InsertLookupCommand(
    int                         FieldId,
    int                         TenantId,
    Dictionary<string, object?> Values
) : IRequest<IDictionary<string, object?>?>;

// File    : QueryDynamicLookupQuery.cs
// Module  : Lookup
// Layer   : Application
// Purpose : Query lấy dynamic lookup rows cho một field cụ thể.
//           Backend tự đọc config từ Ui_Field_Lookup — frontend chỉ gửi FieldId + context.

using MediatR;

namespace ICare247.Application.Features.Lookups.Queries.QueryDynamic;

/// <summary>
/// Truy vấn rows dữ liệu cho field dynamic lookup (ComboBox / LookupBox).
/// Handler đọc cấu hình từ <c>Ui_Field_Lookup</c> theo <c>FieldId</c>, rồi thực thi query.
/// </summary>
/// <param name="FieldId">Field_Id trong Ui_Field — xác định cấu hình lookup cần chạy.</param>
/// <param name="TenantId">Tenant hiện tại — verify tenant ownership + truyền vào FilterSql.</param>
/// <param name="ContextValues">
///   Snapshot giá trị các field trong form — dùng cho cascading lookup (FilterSql @FieldCode).
/// </param>
public sealed record QueryDynamicLookupQuery(
    int                         FieldId,
    int                         TenantId,
    Dictionary<string, object?> ContextValues
) : IRequest<IReadOnlyList<IDictionary<string, object>>>;

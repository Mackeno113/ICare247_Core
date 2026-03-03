// File    : FormEvent.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Input event gửi vào EventEngine để xử lý và tạo UI delta.

using ICare247.Domain.ValueObjects;

namespace ICare247.Domain.Engine.Models;

/// <summary>
/// Event phát sinh khi người dùng tương tác với form.
/// EventEngine nhận event này → evaluate actions → trả về <see cref="UiDeltaResponse"/>.
/// </summary>
/// <param name="EventType">
/// Loại event: 'FIELD_CHANGED' | 'FIELD_BLUR' | 'FORM_LOAD' | 'FORM_SUBMIT' | 'SECTION_TOGGLE'.
/// </param>
/// <param name="SourceField">Field phát sinh event (Field_Code). Null với FORM_LOAD.</param>
/// <param name="FormId">Form đang xử lý.</param>
/// <param name="TenantId">Tenant — bắt buộc.</param>
/// <param name="Context">Snapshot toàn bộ giá trị field tại thời điểm event.</param>
public sealed record FormEvent(
    string EventType,
    string? SourceField,
    int FormId,
    int TenantId,
    EvaluationContext Context);

// File    : EventActionContext.cs
// Module  : Engines/EventActions
// Layer   : Application
// Purpose : Gói dữ liệu 1 action truyền cho handler (AUDIT-5): action config + snapshot giá trị field +
//           thông tin form event. Handler không mutate — EventEngine tự cập nhật context giữa các action.

using ICare247.Domain.Engine.Models;
using ICare247.Domain.Entities.Event;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Engines.EventActions;

/// <summary>
/// Ngữ cảnh thực thi 1 action.
/// </summary>
/// <param name="Action">Cấu hình action (Action_Code + Action_Param_Json).</param>
/// <param name="State">Snapshot giá trị field hiện tại (đã gồm delta SET_VALUE của action trước).</param>
/// <param name="FormEvent">Thông tin event gốc (Form_Id, Tenant_Id, Form_Code, Lang_Code…).</param>
public sealed record EventActionContext(
    EventAction Action,
    EvaluationContext State,
    FormEvent FormEvent);

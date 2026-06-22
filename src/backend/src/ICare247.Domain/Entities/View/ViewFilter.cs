// File    : ViewFilter.cs
// Module  : View
// Layer   : Domain
// Purpose : Một control lọc trên panel trái của View nâng cao — bảng Ui_View_Filter.
//           MỖI THAM SỐ = 1 DÒNG (DateRange tách thành 2 filter: từ/đến).

namespace ICare247.Domain.Entities.View;

/// <summary>
/// Một control lọc của panel trái (<c>Ui_View_Filter</c>) — ánh xạ tới đúng 1 tham số SP/SQL.
/// Text i18n (<see cref="Label"/>/<see cref="Placeholder"/>/<see cref="Tooltip"/>) đã resolve theo langCode.
/// Whitelist tham số: engine chỉ bind <see cref="ParamName"/> khai báo ở đây (chống SQL injection).
/// </summary>
public sealed class ViewFilter
{
    public int FilterId { get; init; }

    /// <summary>Mã kỹ thuật control (unique trong View) — client gửi giá trị theo code này.</summary>
    public string FilterCode { get; init; } = string.Empty;

    /// <summary>Text | Number | Date | Combo | MultiSelect | Checkbox | Radio.</summary>
    public string ControlType { get; init; } = "Text";

    public string? LabelKey { get; init; }

    /// <summary>Nhãn control đã resolve theo langCode.</summary>
    public string? Label { get; init; }

    public string? PlaceholderKey { get; init; }

    /// <summary>Placeholder đã resolve theo langCode.</summary>
    public string? Placeholder { get; init; }

    public string? TooltipKey { get; init; }

    /// <summary>Tooltip đã resolve theo langCode.</summary>
    public string? Tooltip { get; init; }

    // ── Ánh xạ tham số (whitelist) ────────────────────────────
    /// <summary>Tên tham số SP/SQL — VD <c>@MaBN</c>, <c>@TuNgay</c> (literal, không i18n).</summary>
    public string ParamName { get; init; } = string.Empty;

    /// <summary>string | int | decimal | date | bool — quyết định cách ép kiểu giá trị.</summary>
    public string ParamType { get; init; } = "string";

    /// <summary>= | LIKE | &gt;= | &lt;= | IN — LIKE thì engine tự bọc %...%.</summary>
    public string Operator { get; init; } = "=";

    // ── Hành vi ───────────────────────────────────────────────
    /// <summary>Giá trị/Item_Code mặc định (literal — KHÔNG i18n).</summary>
    public string? DefaultValue { get; init; }

    /// <summary>Bắt buộc nhập mới cho Tìm; rỗng → chặn + thông báo + focus.</summary>
    public bool IsRequired { get; init; }
    public bool IsVisible { get; init; } = true;
    public int OrderNo { get; init; }

    /// <summary>Độ rộng trên panel (grid 4-col, giống Ui_Field).</summary>
    public byte ColSpan { get; init; } = 1;

    // ── Nguồn Combo/MultiSelect/Radio ─────────────────────────
    /// <summary>NULL | static (Sys_Lookup) | dynamic (Lookup_Sql).</summary>
    public string? LookupSource { get; init; }

    /// <summary>Sys_Lookup.Lookup_Code khi <see cref="LookupSource"/> = static.</summary>
    public string? LookupCode { get; init; }

    /// <summary>SELECT value,display khi <see cref="LookupSource"/> = dynamic.</summary>
    public string? LookupSql { get; init; }

    public string? PropsJson { get; init; }

    // ── Cascade + prefill (ADR-030) ───────────────────────────
    /// <summary>CSV <c>Filter_Code</c> cha (cascade) — cha đổi → nạp lại options control này. NULL = độc lập.</summary>
    public string? DependsOn { get; init; }

    /// <summary>Field_Code trên form (Edit_Form_Id) nhận giá trị filter khi Thêm mới (prefill). NULL = không.</summary>
    public string? DefaultToField { get; init; }

    /// <summary>Prefill: true = khóa (read-only) · false = đổ sẵn cho sửa lại.</summary>
    public bool DefaultLock { get; init; }

    /// <summary>Các <c>Filter_Code</c> cha tách từ <see cref="DependsOn"/> (CSV) — rỗng nếu độc lập.</summary>
    public IReadOnlyList<string> ParentFilterCodes =>
        string.IsNullOrWhiteSpace(DependsOn)
            ? []
            : DependsOn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

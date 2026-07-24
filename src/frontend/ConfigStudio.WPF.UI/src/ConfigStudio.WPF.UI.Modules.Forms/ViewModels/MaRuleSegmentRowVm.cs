// File    : MaRuleSegmentRowVm.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : 1 dòng đoạn ghép mã trên màn Quy tắc sinh mã (MA-6). Editable; tự tính "Mẫu" hiển thị.

using System;
using System.Runtime.CompilerServices;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>
/// Đoạn của mã (LITERAL/DATE/FIELD/LOOKUP/SEQ). Thay đổi bất kỳ ô nào bắn <see cref="Changed"/>
/// để VM cha tính lại preview + cột "Mẫu". Sample chỉ để XEM: FIELD/LOOKUP dùng placeholder vì
/// ConfigStudio nối Config DB, không đọc giá trị thật ở Data DB.
/// </summary>
public sealed class MaRuleSegmentRowVm : BindableBase
{
    /// <summary>Cha đăng ký để biết đoạn đổi (tính lại preview + reindex số thứ tự cột #).</summary>
    public event Action? Changed;

    public MaRuleSegmentRowVm() { }

    public MaRuleSegmentRowVm(MaRuleSegmentRecord r)
    {
        _segmentType = Norm(r.SegmentType, "LITERAL");
        _textValue = r.TextValue ?? "";
        _fieldCode = r.FieldCode ?? "";
        _lookupTable = r.LookupTable ?? "";
        _lookupKeyCol = r.LookupKeyCol ?? "";
        _lookupValCol = r.LookupValCol ?? "";
        _substringStart = r.SubstringStart;
        _length = r.Length;
        _padChar = r.PadChar ?? "0";
        _padSide = Norm(r.PadSide, "L");
        _textTransform = Norm(r.TextTransform, "NONE");
    }

    private int _orderNo;
    /// <summary>Số thứ tự hiển thị ở cột # (VM cha gán lại khi thêm/xóa/di chuyển).</summary>
    public int OrderNo { get => _orderNo; set => SetProperty(ref _orderNo, value); }

    private string _segmentType = "LITERAL";
    public string SegmentType
    {
        get => _segmentType;
        set
        {
            if (!SetProperty(ref _segmentType, value)) return;
            RaisePropertyChanged(nameof(IsLiteral));
            RaisePropertyChanged(nameof(IsDate));
            RaisePropertyChanged(nameof(IsField));
            RaisePropertyChanged(nameof(IsLookup));
            RaisePropertyChanged(nameof(IsSeq));
            RaisePropertyChanged(nameof(ContentSummary));
            RaisePropertyChanged(nameof(Sample));
            Changed?.Invoke();
        }
    }

    private string _textValue = "";
    public string TextValue { get => _textValue; set => SetAndNotify(ref _textValue, value); }

    private string _fieldCode = "";
    public string FieldCode { get => _fieldCode; set => SetAndNotify(ref _fieldCode, value); }

    private string _lookupTable = "";
    public string LookupTable { get => _lookupTable; set => SetAndNotify(ref _lookupTable, value); }

    private string _lookupKeyCol = "";
    public string LookupKeyCol { get => _lookupKeyCol; set => SetAndNotify(ref _lookupKeyCol, value); }

    private string _lookupValCol = "";
    public string LookupValCol { get => _lookupValCol; set => SetAndNotify(ref _lookupValCol, value); }

    private int? _substringStart;
    public int? SubstringStart { get => _substringStart; set => SetAndNotify(ref _substringStart, value); }

    private int? _length;
    public int? Length { get => _length; set => SetAndNotify(ref _length, value); }

    private string _padChar = "0";
    public string PadChar { get => _padChar; set => SetAndNotify(ref _padChar, value); }

    private string _padSide = "L";
    public string PadSide { get => _padSide; set => SetAndNotify(ref _padSide, value); }

    private string _textTransform = "NONE";
    public string TextTransform { get => _textTransform; set => SetAndNotify(ref _textTransform, value); }

    public bool IsLiteral => SegmentType == "LITERAL";
    public bool IsDate => SegmentType == "DATE";
    public bool IsField => SegmentType == "FIELD";
    public bool IsLookup => SegmentType == "LOOKUP";
    public bool IsSeq => SegmentType == "SEQ";

    /// <summary>Tóm tắt nội dung đoạn hiển thị ở cột "Nội dung" của lưới.</summary>
    public string ContentSummary => SegmentType switch
    {
        "LITERAL" => $"\"{TextValue}\"",
        "DATE" => TextValue,
        "FIELD" => FieldCode,
        "LOOKUP" => $"{FieldCode} → {LookupTable}.{LookupValCol}",
        "SEQ" => "số thứ tự",
        _ => "",
    };

    /// <summary>Chuỗi mẫu của riêng đoạn này — client-side, chỉ để xem trước.</summary>
    public string Sample => BuildSample();

    /// <summary>Ghép mẫu: dùng giá trị thật cho LITERAL/DATE/SEQ; placeholder cho FIELD/LOOKUP.</summary>
    private string BuildSample()
    {
        var raw = SegmentType switch
        {
            "LITERAL" => TextValue,
            "DATE" => SampleDate(TextValue),
            "SEQ" => "1",
            "FIELD" => SampleFixed(FieldCode),
            "LOOKUP" => SampleFixed(LookupValCol),
            _ => "",
        };
        return ApplyFormat(raw);
    }

    /// <summary>DATE mẫu theo hôm nay (đúng ý các token yyyy/yy/MM/dd và ghép).</summary>
    private static string SampleDate(string? fmt)
    {
        if (string.IsNullOrWhiteSpace(fmt)) return "";
        var now = DateTime.Now;
        return fmt
            .Replace("yyyy", now.ToString("yyyy"))
            .Replace("yy", now.ToString("yy"))
            .Replace("MM", now.ToString("MM"))
            .Replace("dd", now.ToString("dd"));
    }

    /// <summary>
    /// FIELD/LOOKUP: không có giá trị thật (Config DB) → nếu đặt Độ rộng thì hiện chuỗi 'XXXX' đúng
    /// độ rộng (để thấy hình dạng/tiền tố cố định); ngược lại hiện ‹tên› gợi ý.
    /// </summary>
    private string SampleFixed(string? name)
    {
        if (Length is > 0)
            return new string(MaRuleUiText.SamplePlaceholderChar[0], Length.Value);
        return string.IsNullOrWhiteSpace(name) ? "‹?›" : $"‹{name}›";
    }

    /// <summary>Áp cắt / độ rộng / đệm / biến đổi chữ cho phần thô (SEQ chỉ dùng độ rộng + đệm).</summary>
    private string ApplyFormat(string value)
    {
        var v = value ?? "";

        if (SubstringStart is > 1 && v.Length >= SubstringStart.Value)
            v = v[(SubstringStart.Value - 1)..];

        if (TextTransform == "UPPER") v = v.ToUpperInvariant();
        else if (TextTransform == "LOWER") v = v.ToLowerInvariant();

        if (Length is > 0)
        {
            var pad = string.IsNullOrEmpty(PadChar) ? ' ' : PadChar[0];
            if (v.Length < Length.Value)
                v = PadSide == "R" ? v.PadRight(Length.Value, pad) : v.PadLeft(Length.Value, pad);
            else if (SegmentType is "FIELD" or "LOOKUP" or "LITERAL" or "DATE" && v.Length > Length.Value)
                v = v[..Length.Value];   // đoạn phi-SEQ có độ rộng cố định thì cắt; SEQ KHÔNG cắt (tràn)
        }
        return v;
    }

    public MaRuleSegmentRecord ToRecord() => new()
    {
        OrderNo = OrderNo,
        SegmentType = SegmentType,
        TextValue = TextValue,
        FieldCode = FieldCode,
        LookupTable = LookupTable,
        LookupKeyCol = LookupKeyCol,
        LookupValCol = LookupValCol,
        SubstringStart = SubstringStart,
        Length = Length,
        PadChar = PadChar,
        PadSide = PadSide,
        TextTransform = TextTransform,
    };

    private void SetAndNotify<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!SetProperty(ref field, value, name)) return;
        RaisePropertyChanged(nameof(ContentSummary));
        RaisePropertyChanged(nameof(Sample));
        Changed?.Invoke();
    }

    private static string Norm(string? v, string fallback)
        => string.IsNullOrWhiteSpace(v) ? fallback : v.Trim().ToUpperInvariant();
}

// File    : MaRuleSegmentRowVm.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : 1 dòng đoạn ghép mã trên màn Quy tắc sinh mã (MA-6). Editable; tự tính "Mẫu" hiển thị.

using System;
using System.Runtime.CompilerServices;
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

    /// <summary>Chuỗi mẫu của riêng đoạn này — client-side, chỉ để xem trước (thuật toán dùng chung ở <see cref="MaRulePreviewCalculator"/>).</summary>
    public string Sample => MaRulePreviewCalculator.BuildSegmentSample(ToRecord());

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

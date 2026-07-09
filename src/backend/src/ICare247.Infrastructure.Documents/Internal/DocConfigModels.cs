// File    : DocConfigModels.cs
// Module  : DocTemplate
// Layer   : Infrastructure (Documents)
// Purpose : Record nội bộ ánh xạ các bảng cấu hình Doc_Template* (Config DB) — không lộ ra ngoài project.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §4.

namespace ICare247.Infrastructure.Documents.Internal;

/// <summary>1 dòng <c>Doc_Template</c> (master).</summary>
internal sealed record DocTemplateRow(long Id, string Ma, string Ten, string MasterProc, byte[]? MasterDocx);

/// <summary>1 dòng <c>Doc_Template_Detail</c> (mảnh chi tiết).</summary>
internal sealed record DocDetailRow(long Id, string Ma, string Ten, string DetailProc, byte[]? DetailDocx, int ThuTu);

/// <summary>1 dòng <c>Doc_Template_Param</c> (ánh xạ tham số proc).</summary>
internal sealed record DocParamRow(long? DetailId, string ParamName, string Nguon, string? NguonKey, string Kieu);

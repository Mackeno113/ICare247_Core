// File    : GetMaDuKienQuery.cs
// Module  : MasterData
// Layer   : Application
// Purpose : Query mã DỰ KIẾN cho form Thêm mới (spec 32 §2/§7, MA-4).
//           Values = giá trị hiện tại trên form — cần cho đoạn FIELD/LOOKUP của quy tắc.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.MasterData.Queries.GetMaDuKien;

/// <summary>Xin mã dự kiến (chỉ đọc, không giữ chỗ số).</summary>
public sealed record GetMaDuKienQuery(
    string FormCode,
    int TenantId,
    Dictionary<string, object?> Values) : IRequest<MaPreviewResult?>;

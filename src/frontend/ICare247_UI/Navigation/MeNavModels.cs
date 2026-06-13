// File    : MeNavModels.cs
// Module  : ICare247_UI (host)
// Layer   : Frontend (UI)
// Purpose : Model nhận menu động từ API /api/v1/me/navigation (node phẳng + cờ quyền).
//           Tách khỏi AppNav (tĩnh) — AppNav nay chỉ còn là fallback khi API rỗng/lỗi.

namespace ICare247_UI.Navigation;

/// <summary>1 node menu trả từ server (đã lọc theo quyền). Khớp MeNavNodeDto backend.</summary>
public sealed record MeNavNode(
    string Ma,
    string Ten,
    string? ChaMa,
    string Loai,
    string? Module,
    string? DuongDan,
    string? Icon,
    string ViTriHienThi,
    int ThuTu,
    bool Xem,
    bool Them,
    bool Sua,
    bool Xoa,
    bool InAn);

/// <summary>Payload /me/navigation.</summary>
public sealed record MeNavigationResult(IReadOnlyList<MeNavNode> Nodes);

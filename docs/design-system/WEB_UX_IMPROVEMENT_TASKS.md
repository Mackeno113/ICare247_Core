# Kế hoạch cải thiện UX Web — 7 task

> Ngày lập: 2026-07-18  
> Trạng thái chung: **Backlog — chưa triển khai**  
> Phạm vi: `src/frontend/ICare247_UI/`, `src/frontend/ICare247.UI.Shared/` và API/backend liên quan trực tiếp đến luồng Web.  
> Ngoài phạm vi: `src/frontend/ConfigStudio.WPF.UI/` và toàn bộ phần WPF đang phát triển.

## Mục đích tài liệu

Tài liệu này là bản trích xuất đầy đủ các phân tích và đề xuất nâng cấp UX Web đã thống nhất. Mỗi đề xuất được mô tả theo cùng một cấu trúc:

1. vấn đề hiện tại;
2. tác động tới người dùng và hệ thống;
3. danh sách công việc cụ thể;
4. khu vực source dự kiến liên quan;
5. tiêu chí nghiệm thu;
6. rủi ro và phụ thuộc.

Đây là **backlog phân tích**, không phải xác nhận rằng code đã được triển khai. Mọi checkbox chỉ được đánh dấu hoàn thành sau khi có code, build/test và kiểm tra trực quan trên màn hình thật.

## Kết luận tổng quan từ quá trình review

| Nhóm vấn đề | Kết luận chính | Đề xuất xử lý |
|---|---|---|
| Hiệu năng dữ liệu | Grid đang có nguy cơ tải tập dữ liệu quá lớn về trình duyệt | WEB-UX-01 |
| Khai thác không gian | Workspace danh sách chưa tận dụng tốt màn hình desktop và filter làm hẹp grid | WEB-UX-02 |
| An toàn thao tác | Form thiếu cơ chế dirty-state thống nhất và phản hồi lưu chưa đủ rõ | WEB-UX-03 |
| Tính nhất quán thị giác | Có nhiều lớp CSS/component khác nhau, nhưng không được chuẩn hóa bằng thay đổi toàn cục thiếu visual regression | WEB-UX-04 |
| Lookup nghiệp vụ | Lookup đa cột chưa biểu diễn đúng cấu trúc dữ liệu và chưa phân biệt rõ các trạng thái tải | WEB-UX-05 |
| Hành động trên grid | Action, icon, selection và cardinality chưa có một model thống nhất | WEB-UX-06 |
| Accessibility | Keyboard, focus lifecycle và ARIA của picker/lookup chưa hoàn chỉnh | WEB-UX-07 |

## Baseline trực quan bắt buộc bảo toàn

Màn form hiện có trước khi nâng cấp là mốc so sánh, không phải phần phải “làm lại” từ đầu. Những đặc điểm tốt phải được giữ:

- modal rộng vừa đủ cho hai cột, không kéo giãn theo toàn màn hình;
- nhóm nghiệp vụ có đường viền nhẹ, radius và padding rõ ràng;
- tiêu đề nhóm nhỏ, viết hoa, tương phản vừa phải;
- label đặt trên input, hai cột thẳng hàng và có nhịp dọc ổn định;
- control DevExpress và lookup tùy biến phải có cùng cảm giác về chiều cao, viền, radius và trạng thái focus;
- nền, border và placeholder dùng sắc trung tính nhẹ; CTA xanh chỉ xuất hiện ở hành động chính;
- footer hành động luôn nhìn thấy nhưng không che nội dung cuối form.

Khung nhóm trong form này là **panel nghiệp vụ có chủ đích**, không được xóa máy móc chỉ vì quy tắc “không card hóa mọi thứ”. Quy tắc chống card hóa dùng để ngăn việc bọc mọi toolbar, bảng và vùng nội dung trong nhiều card/shadow không cần thiết; không phủ nhận một group container đang giúp form dễ quét.

## Cổng kiểm soát trước khi sửa giao diện

- [ ] Chụp baseline tại tối thiểu các viewport 1440×900, 1280×720 và 768×1024.
- [ ] Lập ma trận control thật: `DxTextBox`, `DxDateEdit`, `DxComboBox`, LookupBox, TreeLookupBox, textarea, readonly, disabled, focus và validation error.
- [ ] Kiểm tra DOM/computed style của DevExpress phiên bản đang chạy trước khi viết selector.
- [ ] Không đổi token toàn cục và migrate nhiều màn trong cùng một bước.
- [ ] Thử trên một màn canary; người phụ trách sản phẩm duyệt ảnh trước–sau rồi mới mở rộng.
- [ ] Mỗi thay đổi phải có ảnh so sánh, kiểm tra scroll/footer và test keyboard.
- [ ] Nếu bản mới làm giảm hierarchy, độ thẳng hàng hoặc tính nhất quán của control thì rollback ngay, không tiếp tục “vá” bằng selector toàn cục.

## Nguyên tắc thực hiện

- Không chỉnh sửa WPF. Chỉ đọc cấu hình WPF khi thật sự cần đối chiếu ý nghĩa metadata dùng cho Web.
- Giữ theme **Fluent Light** và token đã khóa; không ghi đè biến nội bộ `--dxbl-*` của DevExpress.
- Giao diện là công cụ quản trị mật độ cao: ưu tiên bảng, form, toolbar rõ ràng; không card hóa mọi vùng.
- Mỗi màn chỉ có một CTA chính. Hành động nguy hiểm phải có xác nhận và không được nổi bật hơn hành động chính.
- Chuỗi hiển thị phải đi qua hệ thống i18n hiện tại (`Loc.L`, `Sys_Resource` hoặc cơ chế tương đương của dự án).
- Mọi thay đổi backend/API trong các task dưới đây cần được bàn giao đúng ownership trước khi triển khai.

## Thứ tự khuyến nghị

| Task | Mức ưu tiên | Phụ thuộc chính |
|---|---:|---|
| WEB-UX-01 — Server-side data loading | P0 | Không |
| WEB-UX-02 — Workspace full-width và responsive | P1 | Có thể làm song song Task 1 |
| WEB-UX-03 — Chống mất dữ liệu form | P0 | Không |
| WEB-UX-04 — Chuẩn hóa design system | P1 | Nền tảng cho Task 5, 6, 7 |
| WEB-UX-05 — LookupBox đa cột thực sự | P1 | Task 1 và Task 4 |
| WEB-UX-06 — Chuẩn hóa action và selection | P1 | Task 4 |
| WEB-UX-07 — Keyboard và ARIA | P1 | Task 4; phối hợp Task 5, 6 |

---

## WEB-UX-01 — Chuyển lưới dữ liệu sang tải trang phía server

**Trạng thái:** ⛔ **KHÔNG chuyển sang phân trang server — quyết định giữ load-all lên client (2026-07-19).**

Đã thử code phân trang server cho nguồn Table/View rồi **rollback theo quyết định user**. Lý do (user
chốt): end user chỉ quan tâm "sao tôi lọc/sort mà không thấy thứ tôi cần" — ưu tiên **sort cột, Filter
Row, ô tìm kiếm chạy đúng trên TOÀN BỘ tập dữ liệu** quan trọng hơn việc giảm tải lúc mở màn. Phân trang
server (dở dang) làm sort/filter chỉ đúng trong 1 trang đang tải → trải nghiệm tệ hơn cái "nặng nhưng đủ".

**Cách làm cuối (giữ nguyên hành vi cũ, có chủ đích):**
- `ViewPage.ReloadDataAsync` tải TOÀN BỘ bản ghi 1 lần (`pageSize: int.MaxValue`) cho nguồn Table/View →
  DxGrid tự phân trang/cuộn/sort/lọc/tìm phía client trên đủ dữ liệu ⇒ tất cả đúng toàn tập.
- Bảng cực lớn: admin bật **`Virtual_Scroll` trong WPF** (`ViewManagerView.xaml`) — DxGrid vẫn giữ đủ dữ
  liệu (sort/lọc đúng) nhưng chỉ render phần đang nhìn thấy → cuộn/sort tập lớn vẫn mượt. Đây là cờ cấu
  hình đã có sẵn, đúng nguyên tắc "hành vi theo cấu hình WPF".
- Nguồn Sp/Sql: không đổi (vốn đã tải hết + client phân trang).
- Đánh đổi đã chấp nhận: màn có tập dữ liệu rất lớn (vd lọc ra 100k dòng) sẽ nặng lúc tải/dựng ở client —
  chấp nhận vì "hầu hết trường hợp" không tới mức đó, và khi tới thì "nặng nhưng thấy đủ" vẫn tốt hơn.

**Hai thứ ĐỘC LẬP đã giữ lại từ lần thử (không liên quan phân trang, đều có ích):**
1. `Show_Search_Box` (cờ metadata trước đây mồ côi, chưa render gì) → nay nối vào ô tìm kiếm gốc của
   DxGrid (`ShowSearchBox="View.ShowSearchBox"`) — DxGrid tự lọc client trên toàn bộ dữ liệu đã tải.
2. **Endpoint export server-side** `GET /api/v1/views/{code}/export?search=&format=xlsx|csv` +
   `IViewRepository.GetAllDataAsync` (TOP 200.000, cùng FROM/JOIN/WHERE với `GetDataAsync` qua helper
   `BuildQueryContextAsync`) + `IViewExportBuilder`/`ViewExportBuilder` (DevExpress.Spreadsheet) +
   `ViewApiService.ExportDataAsync`. **Giữ làm hạ tầng dự phòng** — HIỆN CHƯA nối vào nút nào: vì load-all
   nên nút Export dùng `DxGrid.ExportToXlsxAsync/ExportToCsvAsync` (đã có đủ dữ liệu + tôn trọng đúng
   sort/lọc/tìm hiện tại của lưới). Endpoint để sẵn nếu sau này cần xuất tập rất lớn không qua bộ nhớ client.

**Kết luận:** Task WEB-UX-01 (chuyển server paging) **đóng lại — không triển khai**. Phần "Việc cần thực
hiện" bên dưới giữ để tham khảo lịch sử phân tích, KHÔNG phải backlog còn mở.

**Mục tiêu (ban đầu — không còn theo đuổi):** Giảm thời gian chờ ban đầu, dung lượng truyền và bộ nhớ trình duyệt; bảo đảm màn danh sách vẫn ổn định khi dữ liệu tăng lớn.

### Hiện trạng cần xử lý

- `ViewPage.razor` đang yêu cầu dữ liệu với `pageSize: int.MaxValue`.
- Paging hoặc virtualization ở `DxGrid` hiện chỉ xử lý tập dữ liệu đã tải về client; không giải quyết chi phí API, DB và bộ nhớ khi mở màn.
- Việc sort/filter trên client có thể tạo cảm giác nhanh với dữ liệu nhỏ nhưng không phản ánh đúng toàn bộ dữ liệu khi chuyển sang tải từng trang.

### Việc cần thực hiện

- [ ] Xác định một contract tải dữ liệu thống nhất gồm:
  - `page` hoặc `skip`;
  - `pageSize` hoặc `take`;
  - từ khóa tìm kiếm;
  - danh sách filter;
  - danh sách sort;
  - `totalCount`;
  - dữ liệu của trang hiện tại.
- [ ] Sửa luồng `ViewPage`/`DataView` để lưới phát sinh yêu cầu server khi đổi trang, sort, filter hoặc tìm kiếm.
- [ ] Dùng debounce cho tìm kiếm nhanh và hủy request cũ bằng cancellation token khi người dùng nhập liên tục.
- [ ] Không tải lại dữ liệu nếu trạng thái truy vấn thực tế không thay đổi.
- [ ] Backend dùng truy vấn phân trang thật sự (`OFFSET/FETCH` hoặc chiến lược tương đương) và một truy vấn đếm phù hợp.
- [ ] Áp dụng tenant scope, quyền xem và điều kiện metadata trước khi đếm/tải dữ liệu.
- [ ] Chuẩn hóa biểu diễn filter/sort; chỉ cho phép cột nằm trong metadata hợp lệ để tránh ghép SQL tùy ý.
- [ ] Giữ selection theo khóa bản ghi, không theo vị trí dòng.
- [ ] Quy định rõ xóa hàng loạt áp dụng cho:
  - các dòng được chọn ở trang hiện tại; hoặc
  - tập khóa được người dùng chọn qua nhiều trang.
- [ ] Phân biệt data source của lưới nghiệp vụ với lookup nhỏ; lookup nhỏ có thể cache client nhưng danh sách lớn phải tải có giới hạn.
- [ ] Thêm trạng thái loading, empty, lỗi và nút thử lại mà không làm mất filter hiện tại.
- [ ] Đo thời gian API/DB và kích thước response để có số liệu trước–sau.

### Khu vực dự kiến liên quan

- `src/frontend/ICare247_UI/Pages/View/ViewPage.razor`
- `src/frontend/ICare247_UI/Components/View/DataView.razor`
- `src/frontend/ICare247_UI/Services/ViewApiService.cs`
- DTO/query handler/repository/controller View ở backend

### Tiêu chí nghiệm thu

- Không còn request tải toàn bộ bằng `int.MaxValue`.
- Khi mở màn, chỉ dữ liệu của trang cấu hình được tải.
- Tổng số bản ghi, số trang, sort, filter và tìm kiếm đúng trên toàn bộ tập dữ liệu.
- Request cũ bị hủy hoặc kết quả cũ không ghi đè kết quả mới.
- Chuyển trang liên tục không sinh bản ghi trùng hoặc thiếu.
- Màn có hành vi chấp nhận được với bộ dữ liệu thử 10.000 và 100.000 bản ghi.
- Lỗi API không làm mất điều kiện tìm kiếm/filter người dùng đã nhập.

### Rủi ro cần lưu ý

- Đây là thay đổi xuyên frontend và backend; không nên chỉ sửa `DxGrid` ở client.
- Layout grid lưu theo user có thể chứa page/sort/filter cũ; cần xác định cách migrate hoặc bỏ qua thuộc tính không còn hợp lệ.
- Bulk action và export phải được thiết kế lại nếu trước đây dựa vào toàn bộ dữ liệu đã có trong bộ nhớ.

---

## WEB-UX-02 — Mở rộng workspace và hoàn thiện responsive

**Trạng thái:** Đã code (2026-07-19) — **chưa build/test trực quan** (cần chụp baseline nhiều viewport theo cổng kiểm soát).
Quyết định cùng user: **lật default** `.page-container` sang rộng · cap **1680px** · filter **stack-above** ≤991px.

**Đã làm:**
- `.page-container` default: max-width **1100px → 1680px** (workspace danh sách rộng, cap 1680 để 4K không
  kéo bảng quá dài). Thêm `.page-container.page-narrow { max-width: 1100px }` cho trang form/đọc.
- Gắn `.page-narrow`: `MasterDataTabPage` (form edit), `Home` (/dev/forms). FormRunner giữ nguyên
  `.form-runner-page` (880px, selector riêng tự thắng). 2 màn list (`ViewPage`, `MasterDataListPage`) → rộng.
- **Bỏ cuộn ngang cấp trang**: `MainLayout.razor.css .app-content` → `overflow-x: hidden; overflow-y: auto`.
  Cuộn ngang chỉ trong vùng lưới (DevExpress `ColumnsContainer`). Modal/toast/dropdown đều `position:fixed`/
  teleport ra body nên KHÔNG bị cắt (không ancestor transform).
- **Filter responsive (stack-above ≤991px)**: `.dv-with-filter` → `flex-direction: column`; `.fp-panel`
  full-width (bỏ cột cố định 280px). Body: tablet 2 cột (span 3-4 = 2 cột), mobile (≤767px) 1 cột.
  Desktop >991px giữ panel 280px cạnh lưới, thu gọn thủ công như cũ.
- Chiều cao lưới `calc(100vh - 220px)` (đã có từ trước) → lưới ~70-80% chiều cao. Không đụng.
- **Thanh tiêu đề màn View GỌN** (theo yêu cầu user, ViewPage): bỏ dải search riêng của DxGrid
  (`ShowSearchBox=false`), **gộp ô tìm lên thanh** (`.ml-search-inline` bind `DxGrid.SearchText`, lọc client);
  nút tiện ích **icon-only** (`↻` Xóa cache, `⬆` Import — `.btn-icon-text` + tooltip/aria-label), giữ
  "+ Thêm mới" là CTA chữ; thu `.md-list-header` margin còn 8px. Search gate: chỉ Grid (không TreeList),
  reset khi đổi View. Bỏ được ~2 dải chrome trước lưới. Áp cho MỌI màn View (DataView/ViewPage dùng chung).

**Không làm (đúng phạm vi):** min-width cột mã/tên/trạng thái là **metadata per-Ui_View** (`Width`/`MinWidth`
đã có ở DataView), không nhét cứng CSS. Modal không đổi (độc lập page width, `max-width:100%` đã cap viewport).

**Breakpoint:** dùng lại 991px/767px sẵn có trong file (không thêm magic number). Khoảng 992-1280px (tablet
ngang) vẫn giữ filter cạnh lưới — chấp nhận vì đủ rộng cho cả 2.

**File đụng:** `wwwroot/css/app.css`, `Layout/MainLayout.razor.css`, `Components/View/FilterPanel.razor.css`,
`Pages/MasterData/MasterDataTabPage.razor`, `Pages/Home.razor`.

**Cần khi build:** chụp so sánh 1280/1440/1920 (list rộng ra) + 991/767 (filter xếp trên) + kiểm modal
Thêm/Sửa không tràn sau khi list full-width.

**Mục tiêu:** Cho lưới đủ không gian làm việc trên desktop, đồng thời giữ filter và thao tác sử dụng được trên tablet/mobile.

### Hiện trạng cần xử lý

- `.page-container` đang giới hạn chiều rộng khoảng `1100px`.
- Filter panel cố định khoảng `280px`, khiến vùng grid còn hẹp dù màn hình lớn.
- Việc ép tất cả loại trang dùng chung một max-width làm form và màn danh sách có nhu cầu trái ngược nhau.

### Việc cần thực hiện

- [ ] Tách layout thành ít nhất hai biến thể:
  - layout đọc/form có giới hạn chiều rộng để dễ đọc;
  - layout danh sách/workspace dùng toàn bộ chiều rộng khả dụng.
- [ ] Cho vùng grid chiếm khoảng 70–80% không gian làm việc trên desktop.
- [ ] Desktop lớn: filter panel hiển thị bên cạnh grid, rộng khoảng 260–280px và có thể thu gọn.
- [ ] Tablet: filter chuyển thành panel đóng/mở từ toolbar hoặc drawer, không chiếm cố định chiều ngang.
- [ ] Mobile: filter hiển thị off-canvas hoặc phía trên grid; CTA và tìm kiếm vẫn truy cập được bằng một tay.
- [ ] Cấu hình horizontal scroll ở container cột của grid; không ép cột co nhỏ đến mức nội dung khó đọc.
- [ ] Giữ header/toolbar cần thiết ở vị trí dễ tiếp cận khi cuộn.
- [ ] Loại bỏ horizontal scroll ở cấp toàn trang; chỉ vùng grid được phép cuộn ngang.
- [ ] Xác định min-width hợp lý cho cột mã, tên, trạng thái và thao tác.
- [ ] Không card hóa filter, grid và toolbar thành nhiều lớp container có shadow/radius.
- [ ] Kiểm tra modal thêm/sửa không vượt viewport sau khi workspace chuyển full-width.

### Khu vực dự kiến liên quan

- `src/frontend/ICare247_UI/wwwroot/css/app.css`
- `src/frontend/ICare247_UI/Pages/View/ViewPage.razor`
- `src/frontend/ICare247_UI/Components/View/FilterPanel.razor`
- CSS của `FilterPanel`, `DataView` và layout trang

### Tiêu chí nghiệm thu

- Ở độ rộng 1280, 1440 và 1920px, grid tận dụng rõ rệt không gian trống.
- Form vẫn có chiều rộng dễ đọc, không bị kéo giãn theo layout danh sách.
- Không có thanh cuộn ngang cho toàn trang.
- Filter dùng được ở desktop, tablet và mobile mà không che mất CTA chính.
- Cột quan trọng không bị co đến mức phải đoán nội dung.
- Kiểm tra tối thiểu tại các mốc: `<768px`, `768–1280px`, `>=1280px`.

### Rủi ro cần lưu ý

- CSS layout dùng chung có thể ảnh hưởng nhiều màn; cần tạo modifier/class rõ ràng thay vì đổi toàn cục thiếu kiểm soát.
- Grid DevExpress có cơ chế tính chiều rộng riêng; cần kiểm tra DOM thực tế trước khi đặt `overflow`.

---

## WEB-UX-03 — Chống mất dữ liệu form và làm rõ trạng thái lưu

**Trạng thái:** Đã code cho modal nhập liệu thật (2026-07-19) — **chưa build/test trực quan**.
Quyết định cùng user: Hướng A (BeforeClose trên DraggableModal) · có Escape qua dirty-guard ·
**KHÔNG áp cho FormRunner** (phát hiện: FormRunner chỉ validate, không lưu DB → nav-block vô nghĩa).

**Đã làm:**
- **Dirty-tracking** trong `MasterDataForm`: chụp mốc JSON chuẩn hóa (FieldCode→Value) SAU prefill/default;
  `public bool IsDirty` so mốc (không so tham chiếu → tránh báo sai với lookup/list/ngày); cập nhật mốc
  sau khi lưu OK để đóng không hỏi nhầm.
- **Guard đóng (Hướng A)**: `DraggableModal` thêm `[Parameter] Func<Task<bool>>? BeforeClose` (null = đóng
  luôn, không phá popup khác). ✕ và **Escape** (`@onkeydown` ở backdrop, bắt qua bubbling) đều qua guard.
  Nút **Hủy** trong form cũng đi qua cùng guard (parent `CancelFormAsync`). Click nền vẫn không đóng (cũ).
- **ConfirmDialog** dùng chung (`ShowAsync → Task<bool>`, TaskCompletionSource): "Bỏ thay đổi?" 2 nút —
  "Tiếp tục sửa" (primary, an toàn) / "Bỏ & đóng" (danger outline); Escape = giữ dữ liệu.
- **ToastService** đa kiểu (success/error/warning/info, phong cách riêng — viền trái semantic, tự tắt,
  xếp chồng) + `ToastHost` ở MainLayout. "Dùng cả 2": toast cho kết quả thoáng qua; banner/field inline
  cho lỗi tại chỗ.
- **Lỗi LƯU hiện đồng thời 2 nơi** (user chốt): lỗi save (spc_Grid_ + validate) vừa inline (field đỏ +
  banner) vừa toast (`MasterDataForm.ShowSaveErrorToasts` — ≤3 lỗi toast từng dòng, >3 toast tóm tắt;
  ngoại lệ/API cũng Toast.Error). Đường spc_Grid_ giữ nguyên, chỉ thêm nơi hiển thị thứ 2.
- **Tách trạng thái lưu**: `OnFormSaved` (ViewPage + MasterDataListPage) bọc reload trong try/catch → lưu
  OK + reload OK = `Toast.Success("Đã lưu")`; lưu OK nhưng reload lỗi = `Toast.Warning(...)` (KHÔNG báo
  "lưu thất bại"). Chống double-submit + lỗi validation/API giữ dữ liệu: đã có sẵn từ trước.

**Chưa làm / cố ý bỏ:** FormRunner (không persist). Escape chỉ chắc chắn hoạt động sau khi user đã focus
1 field trong form (bubbling) — nhưng đó đúng là lúc có dữ liệu cần bảo vệ.

**File đụng:** `Components/DraggableModal.razor`, `Components/ConfirmDialog.razor` (mới),
`Components/ToastHost.razor` (mới), `Services/ToastService.cs` (mới), `Components/MasterData/MasterDataForm.razor`,
`Pages/View/ViewPage.razor`, `Pages/MasterData/MasterDataListPage.razor`, `Layout/MainLayout.razor`,
`Program.cs`, `wwwroot/css/app.css`.

**Mục tiêu:** Người dùng không mất phần đã nhập do đóng nhầm modal, đồng thời luôn biết thao tác lưu đang chạy, thành công hay thất bại.

### Hiện trạng cần xử lý

- Modal thêm/sửa có thể đóng trong khi form đã thay đổi nhưng chưa lưu.
- Chưa có một dirty-state dùng chung cho các cách đóng: nút X, Hủy, Escape hoặc hành vi đóng khác.
- Phản hồi sau lưu chưa phân biệt đầy đủ giữa lưu thất bại, lưu thành công nhưng reload thất bại và lưu hoàn tất.

### Việc cần thực hiện

- [ ] Chụp snapshot dữ liệu ban đầu sau khi form đã nạp xong.
- [ ] Đánh dấu dirty khi người dùng thay đổi giá trị so với snapshot.
- [ ] Không đánh dấu dirty cho thay đổi do khởi tạo, binding ban đầu hoặc điền mặc định có chủ đích.
- [ ] Chặn tất cả đường đóng modal khi dirty:
  - nút X;
  - nút Hủy;
  - Escape;
  - backdrop nếu component cho phép;
  - chuyển màn hoặc thay record nếu có.
- [ ] Hiển thị confirm với hai lựa chọn rõ ràng:
  - tiếp tục chỉnh sửa;
  - bỏ thay đổi và đóng.
- [ ] Sau khi lưu thành công, cập nhật snapshot/clear dirty trước khi đóng để không hiện confirm sai.
- [ ] Disable nút Lưu trong lúc gửi request và chống double submit.
- [ ] Hiển thị lỗi validation tại field; lỗi API hiển thị ở vùng thông báo của form và giữ nguyên dữ liệu đã nhập.
- [ ] Hiển thị toast/thông báo thành công đã được i18n.
- [ ] Reload grid sau lưu nhưng giữ filter, trang, sort và selection khi còn hợp lệ.
- [ ] Nếu lưu DB thành công nhưng reload grid thất bại, thông báo đúng hai trạng thái thay vì báo chung là “lưu thất bại”.
- [ ] Khôi phục focus hợp lý khi đóng confirm hoặc modal.

### Khu vực dự kiến liên quan

- `src/frontend/ICare247_UI/Components/DynamicForms/MasterDataForm.razor`
- `src/frontend/ICare247_UI/Components/Common/DraggableModal.razor`
- `src/frontend/ICare247_UI/Pages/View/ViewPage.razor`
- Dịch vụ toast/notification và resource i18n dùng chung

### Tiêu chí nghiệm thu

- Form chưa thay đổi đóng ngay, không hỏi thừa.
- Form đã thay đổi luôn cảnh báo trước khi bỏ dữ liệu.
- Chọn “tiếp tục chỉnh sửa” giữ nguyên toàn bộ giá trị và focus hợp lý.
- Lưu thành công không xuất hiện cảnh báo dirty.
- Lỗi validation/API không xóa dữ liệu người dùng đã nhập.
- Double-click nút Lưu chỉ tạo một request.
- Tất cả đường đóng modal bằng chuột và bàn phím có hành vi nhất quán.

### Rủi ro cần lưu ý

- So sánh object trực tiếp có thể báo dirty sai với lookup, danh sách hoặc kiểu ngày; cần snapshot chuẩn hóa.
- Field phụ thuộc có thể tự thay đổi sau khi chọn field cha; phải xác định đó là thay đổi nghiệp vụ của người dùng và đưa vào dirty-state.

---

## WEB-UX-04 — Chuẩn hóa design system cho giao diện quản trị

**Trạng thái:** Phần AN TOÀN đã làm (2026-07-19): **audit + đồng bộ tài liệu**. Phần **migrate token
(sửa CSS component) CHƯA làm** — rủi ro cao (đã rollback toàn bộ 2026-07-18), để sau theo Cổng kiểm soát.

**Đã làm (không đổi giao diện):**
- **Audit** → `docs/design-system/DESIGN_AUDIT.md` (bản đồ migrate). Phát hiện chính:
  P0 màu tím legacy `var(--color-violet-600, #6B44E0)` ở `TreeLookupBoxRenderer.razor.css:158,163` (token
  không tồn tại → tím rò ra, trái brand xanh) · P1 font `Inter` khai trước nhưng KHÔNG bundle (`tokens.css:62`)
  · P2 ~148 hex hardcode (DynamicForms 83 · ICare247_UI razor.css 53 · app.css ~12) · P3 mâu thuẫn chính sách
  override `--dxbl-*` (skill "cấm" vs tokens.css "cho phép trên selector component").
- **Đồng bộ doc:** thay `README.md` (đang mô tả brand CŨ Coral/Violet/Teal + Plus Jakarta + token `--text-*`
  không tồn tại) bằng bản ngắn TRỎ VỀ `tokens.css` làm nguồn chuẩn (không duplicate token → hết drift).
  Sửa comment tự-mâu-thuẫn trong `tokens.css` (berry tím → Fluent). `hrm-layout-principles.md` kiểm lại: còn chuẩn.

**Đã xử lý thêm phần rủi ro-thấp (2026-07-19):**
- **P0 tím legacy → ĐÃ SỬA:** node cây/item dropdown được chọn ở LookupBox/TreeLookupBox đổi tím → xanh
  brand (`--color-primary`/`--color-primary-soft`); dọn hết fallback tím chết. Không còn hex tím trong code.
- **P1 font → ĐÃ SỬA:** `--font-sans` đảo Segoe UI lên đầu (bỏ phụ thuộc Inter chưa bundle).
- 2 cái này là thay đổi GIAO DIỆN (surgical, rõ ràng đúng) → cần verify trực quan khi build.

**Canary 1 — cụm control lookup ĐÃ tokenize (2026-07-19):** `TreeLookupBoxRenderer` + `LookupBoxRenderer`
dọn sạch dead-token/hex về token chuẩn (liên quan WEB-UX-05). Thay đổi thị giác nhỏ (xám nhích, đỏ lỗi về
brand) + P0 tím→xanh. **Cần verify control lookup khi build.**

**Chưa làm (canary tiếp — cần duyệt ảnh):** ~117 hex còn lại (Attachment/LookupAddDialog + ImportWizard/
I18nTools/FilterPanel/UserManagement razor.css + app.css). Dựng audit/lint CSS. KHÔNG gộp nhiều component/1 bước.

**Mục tiêu:** Loại bỏ cảm giác mỗi component thuộc một sản phẩm khác nhau; tạo nền tảng thị giác ổn định cho các task sau.

### Hiện trạng cần xử lý

- Web đang tồn tại nhiều “phương ngữ” CSS: Fluent/ERP hiện tại, fallback tím cũ trong DynamicForms và style riêng của Auth.
- Typography khai báo `Inter` trước `Segoe UI` nhưng font không được bundle nhất quán, dẫn đến khác biệt giữa máy.
- Chiều cao control đang có nhiều mức gần nhau như 34, 38 và 42px.
- Tài liệu `docs/design-system/README.md` cũ còn mô tả font, radius, card và phong cách không còn khớp chuẩn admin UI Fluent Light đã khóa.

### Việc cần thực hiện

- [ ] Chốt một bộ token Web chuẩn, ưu tiên token semantic thay vì hard-code theo component.
- [ ] Dùng `Segoe UI` làm font chính của giao diện quản trị; không phụ thuộc font chưa được bundle.
- [ ] Giới hạn typography vào tối đa bốn cấp thực dụng cho title, section title, body/label và caption.
- [ ] Chuẩn hóa spacing theo thang `4/8/12/16/24/32`.
- [ ] Chuẩn hóa radius:
  - control, button, badge: `4px`;
  - popup/modal: `8px`;
  - table phẳng có thể dùng `0`;
  - group container đang là baseline tốt của form không được xóa nếu chưa có phương án trực quan tốt hơn.
- [ ] Chuẩn hóa shadow: chỉ overlay tạm thời như popup/modal dùng `0 2px 8px rgba(0,0,0,.08)`.
- [ ] Chuẩn hóa chiều cao control và button; loại bỏ các biến thể 34/42px không có lý do nghiệp vụ.
- [ ] Loại bỏ fallback tím/legacy alias không còn dùng trong DynamicForms.
- [ ] Không tạo thêm alias để che token sai; migrate component sang token canonical rồi xóa alias cũ có kiểm soát.
- [ ] Giới hạn mỗi màn tối đa ba màu chức năng; màu semantic chỉ dùng cho success, warning, error và info.
- [ ] Bỏ shadow mặc định ở button; chỉ một CTA chính trên mỗi màn.
- [ ] Giữ `auth.css` là vùng style có chủ đích, nhưng đồng bộ font, brand semantic, focus và accessibility với hệ thống chung.
- [ ] Cập nhật tài liệu design-system sau khi token code đã được chốt để tài liệu và runtime không mâu thuẫn.
- [ ] Thêm audit/lint cho:
  - màu hard-code ngoài danh sách cho phép;
  - CSS variable không tồn tại;
  - spacing/radius ngoài thang;
  - font literal không được phép;
  - ghi đè biến `--dxbl-*`.
- [ ] Chuẩn bị trang/component showcase để kiểm tra input, button, grid, popup, lookup và trạng thái lỗi.
- [ ] Chọn một màn canary và tạo ảnh baseline trước khi thay token hoặc CSS dùng chung.
- [ ] Kiểm tra trực tiếp DOM/computed style của mọi DevExpress editor xuất hiện trên màn canary.
- [ ] Duyệt ảnh trước–sau của màn canary trước khi migrate component thứ hai.

### Khu vực dự kiến liên quan

- `src/frontend/ICare247_UI/wwwroot/css/tokens.css`
- `src/frontend/ICare247_UI/wwwroot/css/app.css`
- CSS của DynamicForms, FilterPanel và ImportWizard
- CSS scoped của các shared component
- `docs/design-system/README.md`
- `docs/design-system/hrm-layout-principles.md`

### Tiêu chí nghiệm thu

- Không còn màu tím legacy rò vào màn quản trị nếu không phải màu semantic/brand đã chốt.
- Cùng một loại control có font, chiều cao, radius và focus state nhất quán.
- Grid/table không bị card hóa hoặc phủ shadow không cần thiết; panel nhóm nghiệp vụ có chủ đích vẫn giữ hierarchy tốt.
- Popup/modal có cùng radius và shadow.
- Form canary không kém baseline về hierarchy, khoảng cách, độ thẳng hàng, khả năng cuộn và khả năng nhìn thấy footer.
- Auth vẫn giữ nhận diện riêng nhưng không phá chuẩn font/focus/accessibility.
- Audit CSS không báo biến không tồn tại hoặc giá trị ngoài chuẩn ở các component đã migrate.
- Tài liệu design-system mô tả đúng code đang chạy.

### Rủi ro cần lưu ý

- Thay token toàn cục có blast radius lớn; cần migrate theo nhóm component và visual regression từng bước.
- Không chỉnh trực tiếp biến nội bộ DevExpress; chỉ style qua API/class/biến do dự án sở hữu.
- Không suy luận DOM DevExpress từ tên component Razor; phải kiểm tra markup và cascade thật của đúng phiên bản theme.
- Không dùng audit token làm bằng chứng giao diện đẹp. Audit chỉ phát hiện sai quy ước; nghiệm thu cuối cùng vẫn cần ảnh và thao tác trên màn thật.

---

## WEB-UX-05 — Xây dựng LookupBox đa cột thực sự

**Trạng thái:** Chưa triển khai  
**Mục tiêu:** Lookup hiển thị và tìm kiếm dữ liệu có cấu trúc rõ ràng, hoạt động tốt với cả danh mục nhỏ và dữ liệu lớn.

### Hiện trạng cần xử lý

- Renderer hiện ghép các giá trị bằng ký tự `|`, chưa phải bảng/popup grid đa cột.
- Metadata `caption`, `width`, `DropDownWidth`, `SearchEnabled`, `FilterMinLength` và `EditBoxMode` chưa được áp dụng đầy đủ.
- Chưa có header và cell alignment đúng theo từng cột.
- `LookupQueryService` có thể biến lỗi thành danh sách rỗng, khiến người dùng không phân biệt “không có dữ liệu” với “tải thất bại”.
- Schema column giữa tài liệu/cấu hình/runtime có dấu hiệu lệch tên thuộc tính.

### Việc cần thực hiện

- [ ] Chuẩn hóa schema column runtime, tối thiểu gồm:
  - `fieldName`;
  - `captionKey` hoặc `caption`;
  - `width`;
  - kiểu căn lề/format nếu cần.
- [ ] Render popup bằng CSS grid/table thực sự, có header cố định và cell thẳng cột.
- [ ] Áp dụng width từng cột và `DropDownWidth` nhưng vẫn giới hạn theo viewport.
- [ ] Chuẩn hóa row height khoảng 36px cho danh sách lookup mật độ cao.
- [ ] Hỗ trợ hai chế độ edit box rõ ràng:
  - chỉ tên;
  - mã và tên.
- [ ] Nếu `SearchEnabled=false`, không hiển thị ô tìm kiếm hoặc hành vi tìm kiếm ngầm.
- [ ] Chỉ kích hoạt tìm kiếm khi đạt `FilterMinLength`; hiển thị hướng dẫn trước ngưỡng.
- [ ] Tìm trên CodeField, DisplayColumn và các cột được cấu hình cho phép tìm.
- [ ] Danh mục nhỏ: cho phép tải/cache client có giới hạn rõ ràng.
- [ ] Danh mục lớn: tải khi mở popup, debounce tìm kiếm và phân trang/lazy load khoảng 30–50 dòng mỗi lần.
- [ ] Contract server lookup cần có `keyword`, `skip`, `take`, `totalCount` và điều kiện phụ thuộc field cha nếu có.
- [ ] Phân biệt các trạng thái:
  - đang tải;
  - chưa đủ ký tự tìm;
  - không có kết quả;
  - lỗi tải dữ liệu;
  - nút thử lại.
- [ ] Đảm bảo popup không bị cắt bởi modal/overflow container; kiểm tra cơ chế portal/teleport hoặc stacking context.
- [ ] Chốt phạm vi `EditBoxMode=Custom`: chỉ triển khai khi có template contract rõ ràng; nếu chưa có thì không quảng bá là tính năng đã hỗ trợ.
- [ ] Thêm test mapping metadata → column và test query/search.

### Khu vực dự kiến liên quan

- Renderer/CSS LookupBox trong `src/frontend/ICare247_UI/Components/DynamicForms/`
- DTO và `LookupQueryService`
- API/query handler/repository lookup ở backend
- Tài liệu schema metadata của LookupBox

### Tiêu chí nghiệm thu

- Popup có header và các cột căn thẳng hàng, không còn chuỗi ghép bằng `|`.
- Caption và width từ metadata được áp dụng.
- Tìm được theo mã, tên và cột được cấu hình.
- Dữ liệu lớn không bị tải toàn bộ khi mở form.
- Lỗi API không hiển thị như danh sách rỗng.
- Popup dùng được trong modal, không bị cắt và không vượt viewport.
- Chế độ hiển thị tên hoặc mã + tên đúng theo cấu hình.
- Lookup hoạt động ở desktop, tablet và mobile.

### Rủi ro cần lưu ý

- Task này phụ thuộc Task 1 để tái sử dụng cách phân trang/tìm kiếm server và Task 4 để dùng token chuẩn.
- Lookup phụ thuộc nhiều metadata; cần giữ backward compatibility hoặc có bước migrate cấu hình rõ ràng.

---

## WEB-UX-06 — Chuẩn hóa action, icon và selection của grid

**Trạng thái:** Chưa triển khai  
**Mục tiêu:** Người dùng nhận ra ngay hành động chính, hành động theo ngữ cảnh và hành động nguy hiểm; grid không hiển thị selection khi không có nhu cầu.

### Hiện trạng cần xử lý

- `ViewPage` có nhóm action hard-code trong khi `DataView` còn render action từ metadata, tạo nguy cơ trùng “Thêm” hoặc trùng chức năng.
- Action metadata đang dùng style gần như nhau, chưa phản ánh mức ưu tiên.
- Flat grid luôn có selection column; giá trị selection không phải `single` hiện có thể bị suy thành `multiple`.
- Row action vẫn hard-code sửa/xóa trong khi metadata có scope `Row/Both`.
- `RequireSelection` và `Confirm` chưa được thực thi đầy đủ.
- Server action có nguy cơ dùng bản ghi đầu tiên khi người dùng chọn nhiều, gây mơ hồ nghiệp vụ.

### Việc cần thực hiện

- [ ] Xây dựng một action model dùng chung gồm:
  - id;
  - scope: page, toolbar, selection, row;
  - visual priority;
  - permission;
  - yêu cầu selection;
  - số lượng selection hợp lệ;
  - confirm;
  - semantic icon;
  - resource key.
- [ ] Phân vùng action:
  - page header: một CTA chính, thường là Thêm;
  - utility toolbar: làm mới, export, cột, saved view;
  - contextual selection bar: action chỉ xuất hiện khi chọn dòng;
  - row action: sửa trực tiếp và overflow menu cho action phụ/nguy hiểm.
- [ ] Loại bỏ action trùng giữa hard-code và metadata; xác định một nguồn sự thật duy nhất.
- [ ] Chỉ hiển thị checkbox column khi:
  - selection mode là `multiple`; và
  - có ít nhất một bulk action mà người dùng có quyền thực hiện.
- [ ] Selection mode `none`: không có checkbox.
- [ ] Selection mode `single`: dùng focused/selected row, không giả thành multiple.
- [ ] Selection mode `multiple`: checkbox column có chiều rộng thống nhất, khoảng 44px.
- [ ] Thực thi đúng scope `Row` và `Both`.
- [ ] Action yêu cầu selection phải disable/ẩn hợp lý và có tooltip giải thích.
- [ ] Action nguy hiểm phải có confirm với nội dung nêu rõ số lượng/đối tượng.
- [ ] Không tự chọn “bản ghi đầu tiên” khi cardinality không hợp lệ; phải chặn và báo rõ.
- [ ] Dùng một semantic icon registry; không trộn emoji, ký tự và nhiều bộ icon tùy ý.
- [ ] Icon-only button phải có accessible name, tooltip và focus state.
- [ ] Giữ thứ tự nhất quán: hành động chính trước, hành động phụ sau, nguy hiểm cuối.

### Khu vực dự kiến liên quan

- `src/frontend/ICare247_UI/Components/View/DataView.razor`
- `src/frontend/ICare247_UI/Pages/View/ViewPage.razor`
- `MasterDataGrid` và component action dùng chung nếu được tách
- DTO action trong `ViewApiService`
- Metadata/API action ở backend nếu contract hiện tại thiếu cardinality

### Tiêu chí nghiệm thu

- Mỗi màn chỉ có một CTA chính và không còn nút Thêm trùng.
- Mode `none`, `single`, `multiple` hiển thị đúng selection UI.
- Checkbox chỉ xuất hiện khi thật sự có bulk action khả dụng.
- Action `Page`, `Row`, `Both` xuất hiện đúng vị trí.
- `RequireSelection`, permission và confirm được thực thi.
- Không có action âm thầm chạy trên bản ghi đầu tiên khi selection không hợp lệ.
- Icon, tooltip, focus và thứ tự action nhất quán giữa các grid.

### Rủi ro cần lưu ý

- Layout grid lưu theo user có thể chứa selection/action column cũ; cần xử lý layout version.
- Nếu metadata hiện không biểu diễn đủ cardinality hoặc danger level, backend contract cần mở rộng có kiểm soát.

---

## WEB-UX-07 — Hoàn thiện keyboard navigation và ARIA

**Trạng thái:** Chưa triển khai  
**Mục tiêu:** Các picker/lookup dùng được hoàn toàn bằng bàn phím và được screen reader thông báo đúng trạng thái, giá trị và quan hệ.

### Hiện trạng cần xử lý

- Một số component có `role` ở container nhưng item con chưa có role/state tương ứng.
- Lookup có xử lý một phần Arrow/Enter/Escape nhưng thiếu mô hình combobox hoàn chỉnh.
- Label, input và validation message chưa phải lúc nào cũng liên kết qua id/ARIA.
- Popup chưa bảo đảm focus đầu vào, focus trap hợp lý và trả focus về trigger khi đóng.
- Trạng thái loading, số kết quả và lỗi chưa được thông báo qua live region.

### Việc cần thực hiện

- [ ] Tạo stable id cho label, input, popup, helper text và validation message.
- [ ] Liên kết label bằng `for`/`id`; dùng `aria-describedby` cho helper/error và `aria-invalid` khi lỗi.
- [ ] LookupBox theo pattern combobox:
  - `role="combobox"`;
  - `aria-expanded`;
  - `aria-controls`;
  - `aria-autocomplete`;
  - `aria-activedescendant`;
  - option có `role="option"` và `aria-selected`.
- [ ] Hỗ trợ Arrow Up/Down, Home/End, Enter chọn, Escape đóng và Tab theo thứ tự tự nhiên.
- [ ] Khi active option thay đổi, tự cuộn option vào vùng nhìn thấy.
- [ ] TreeLookup/CompanyPicker theo pattern tree:
  - item có `role="treeitem"`;
  - `aria-level`;
  - `aria-expanded`;
  - `aria-selected` hoặc `aria-checked`;
  - Left/Right để thu/mở và di chuyển cha/con.
- [ ] Với cây multi-check, giữ native checkbox nếu có thể; bổ sung thông báo trạng thái chọn một phần/cascade.
- [ ] Address picker:
  - không đặt ô search sai cấu trúc bên trong listbox;
  - option có role đúng;
  - tự focus ô search khi mở;
  - thông báo loading, số kết quả và lỗi.
- [ ] Company picker:
  - accessible name phải chứa giá trị công ty hiện tại;
  - trạng thái disabled/selected được thông báo;
  - Escape đóng và trả focus về trigger.
- [ ] Tất cả button trong form/popup khai báo `type="button"` nếu không phải submit.
- [ ] Xây dựng focus lifecycle dùng chung cho popup:
  - lưu trigger;
  - focus điểm đầu phù hợp khi mở;
  - không để focus rơi ra nền khi modal yêu cầu trap;
  - trả focus khi đóng.
- [ ] Chuẩn hóa `:focus-visible`; không xóa outline nếu chưa có focus indicator thay thế.
- [ ] Dùng live region có mức `polite`/`assertive` phù hợp cho loading, kết quả và lỗi.
- [ ] Với `TreeSelectBox` DevExpress, kiểm tra DOM/accessibility có sẵn trước khi thêm role để tránh role lồng sai.
- [ ] Localize mọi chuỗi hướng dẫn, trạng thái và accessible name.
- [ ] Thực hiện kiểm thử:
  - keyboard-only;
  - NVDA với Chrome/Edge;
  - zoom 200%;
  - axe hoặc công cụ tương đương;
  - kiểm tra duplicate id.

### Khu vực dự kiến liên quan

- `IcCompanyPicker`
- `IcAddressBlock`
- `LookupBoxRenderer`
- `TreeLookupBoxRenderer`
- `TreeSelectBox`
- `FieldRenderer` và component validation dùng chung
- CSS focus state của shared components

### Tiêu chí nghiệm thu

- Có thể mở, tìm, duyệt, chọn và đóng mọi picker mà không dùng chuột.
- Screen reader đọc đúng label, giá trị hiện tại, trạng thái mở/đóng, option active/selected và lỗi.
- Escape đóng popup và focus quay lại đúng trigger.
- Label/error/helper được liên kết đúng, không có duplicate id.
- Focus indicator nhìn rõ ở mọi control tương tác.
- Zoom 200% không làm mất chức năng hoặc che nội dung bắt buộc.
- Không có lỗi accessibility nghiêm trọng từ công cụ tự động trong phạm vi component đã sửa.

### Rủi ro cần lưu ý

- Không nên tự gắn ARIA lên DevExpress component khi chưa kiểm tra markup được render; ARIA sai có thể tệ hơn thiếu ARIA.
- Task này cần triển khai đồng thời với cấu trúc mới của LookupBox/Action để tránh sửa accessibility hai lần.

---

## Gợi ý chia đợt triển khai

- **Đợt A — Hiệu năng và an toàn dữ liệu:** Task 1, 2, 3.
- **Đợt B — Nền tảng giao diện:** Task 4.
- **Đợt C — Component nghiệp vụ:** Task 5, 6.
- **Đợt D — Accessibility và regression:** Task 7, sau đó kiểm thử xuyên suốt cả bảy task.

Tài liệu này chỉ xác lập backlog và tiêu chí nghiệm thu. Việc impact analysis, phân công ownership, chỉnh sửa code, build/test và cập nhật handoff sẽ thực hiện ở từng task sau khi được duyệt.

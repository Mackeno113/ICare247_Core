# QUYẾT ĐỊNH THIẾT KẾ — ĐỢT 2 · BIỂU QUYẾT VÀ CHI QUỸ BẢO TRÌ

| | |
|---|---|
| **Tài liệu** | Architecture Decision Record (ADR) |
| **Dự án** | Phần mềm Quản lý Nhà chung cư |
| **Phạm vi** | Engine biểu quyết (mẫu số, kiểm phiếu, bầu cử) và ràng buộc chi quỹ bảo trì |
| **Ngày lập** | 23/08/2026 |
| **Trạng thái** | Đề xuất — chờ chốt với team kỹ thuật và pháp chế |
| **Liên quan** | Đặc tả §5 (M1-02, M1-05, M1-11), §9 (vấn đề pháp chế), §10.3 (DB4–DB6); ERD-2, ERD-3; bản rà soát kỹ thuật (KT·1, KT·2, KT·3) |

---

## MỤC LỤC

- [Bối cảnh chung](#bối-cảnh-chung)
- [ADR-003 · Mẫu số biểu quyết và cấu trúc VoterRoll](#adr-003--mẫu-số-biểu-quyết-và-cấu-trúc-voterroll)
- [ADR-004 · Phương pháp đếm phiếu bầu Ban quản trị](#adr-004--phương-pháp-đếm-phiếu-bầu-ban-quản-trị)
- [ADR-005 · Chi quỹ bảo trì ngoài kế hoạch](#adr-005--chi-quỹ-bảo-trì-ngoài-kế-hoạch)
- [Tóm tắt hành động](#tóm-tắt-hành-động)

---

## Bối cảnh chung

Bản rà soát kỹ thuật chỉ ra ba mâu thuẫn **chặn việc viết engine và viết test**, đều nằm ở phần lõi pháp lý của sản phẩm:

| Mã | Mâu thuẫn | ADR xử lý |
|---|---|---|
| KT·1 | "Snapshot cử tri / mẫu số" mang **hai con số khác nhau** (toàn tòa vs hiện diện); DB6 lệch với màn kiểm phiếu MH-07 | ADR-003 |
| KT·2 | **Phương pháp đếm phiếu bầu BQT (N trong M) chưa định nghĩa**; mô hình `Vote/Resolution` chỉ hợp biểu quyết thông qua, quan hệ `AgendaItem 1-1 Ballot` chặn bầu lại vòng 2 | ADR-004 |
| KT·3 | DB5 tuyệt đối ("phiếu chi bắt buộc gắn hạng mục kế hoạch") **mâu thuẫn** luồng "chi ngoài kế hoạch" (M1-11 + cờ `is_off_plan`) | ADR-005 |

Nguyên tắc xuyên suốt ba ADR này giống đợt 1: **mọi ngưỡng, mẫu số, phương pháp phải cấu hình được, không hard-code** — vì căn cứ pháp lý (Thông tư 05, Quy chế bầu cử, Nội quy tòa) có thể khác nhau theo tòa và có thể sửa đổi. Kiến trúc phải tách sẵn cấu trúc để chốt pháp chế muộn cũng không phá schema.

> **Điều kiện tiên quyết pháp chế** (đặc tả §9): ADR-003 phụ thuộc §9.3 (mẫu số áp cho từng loại nội dung); ADR-004 phụ thuộc **Quy chế bầu cử** do hội nghị thông qua. Hai điểm này quyết định *giá trị mặc định*, không quyết định *cấu trúc dữ liệu* — nên vẫn code được phần khung trước.

---

# ADR-003 · Mẫu số biểu quyết và cấu trúc VoterRoll

## 1. Bối cảnh

Với cùng một kỳ (Conference #14) trong bộ wireframe:

- Audit log (F-02) ghi `CREATE_VOTERROLL → 492 mục · 41.280,50 m²` — **toàn bộ tòa**.
- Màn kiểm phiếu (MH-07) ghi *"mẫu số: tổng m² của snapshot cử tri = 19.649,50 m²"* — **m² hiện diện** (khớp MH-06 "47,6% × 41.280").

ERD-2 gọi `VOTER_ROLL.total_weight_sqm` là "MẪU SỐ chốt tại khai mạc" nhưng không nói rõ nó là con số nào. DB6 nói *"tổng weight 4 trạng thái = tổng weight VoterRoll"*, trong khi MH-07 lại kiểm *"tổng 4 trạng thái = 100%"* trên con số hiện diện.

→ Một từ ("snapshot / mẫu số") đang gánh **ba khái niệm khác nhau**.

## 2. Vì sao đây là quyết định kiến trúc

Chọn "mẫu số" nào là chọn **kết quả nghị quyết**. Một nội dung cần ">50% tán thành" cho kết quả khác hẳn khi mẫu số là 41.280 (toàn tòa) so với 19.649 (hiện diện). Nếu code chốt cứng một con số, mọi nghị quyết tính sai theo — và đây chính là loại lỗi chỉ lộ ra khi có khiếu nại.

## 3. Ba đại lượng phải tách bạch

| Đại lượng | Định nghĩa | Dùng cho | Ví dụ #14 |
|---|---|---|---|
| **Mẫu số điều kiện tiến hành (A)** | % **đại diện chủ sở hữu căn hộ đã bàn giao** đang tham dự — đếm theo **đầu đại diện**, KHÔNG theo m² | R2/R3 · quyết định có được khai mạc | 51,3% (160/312) |
| **Tổng trọng số đủ điều kiện (B-total)** | Tổng m² của **mọi** đơn vị có quyền phiếu, kể cả vắng mặt — đóng băng tại khai mạc | Cân đối toàn vẹn (DB6); mẫu số các nội dung tính theo toàn tòa | 41.280,50 m² |
| **Trọng số hiện diện (B-present)** | Tổng m² của các đơn vị **không vắng mặt** tại khai mạc — đóng băng | Mẫu số các nội dung tính theo hiện diện | 19.649,50 m² |

## 4. Quyết định

> **VoterRoll chụp TOÀN BỘ cử tri đủ điều kiện (kể cả vắng), lưu tách bạch hai tổng trọng số. Mẫu số áp cho ngưỡng thông qua của từng nội dung là tham số cấu hình, không phải hằng số toàn hệ thống.**

### 4.1 Cấu trúc VoterRoll (bổ sung ERD-2)

```
VOTER_ROLL
 ├─ total_eligible_weight_sqm   -- B-total: tổng m² MỌI cử tri đủ điều kiện (đóng băng)
 ├─ present_weight_sqm          -- B-present: tổng m² KHÔNG vắng (đóng băng tại khai mạc)
 ├─ eligible_apartment_reps     -- mẫu số A: số đại diện căn hộ đã bàn giao (đóng băng)
 ├─ present_apartment_reps      -- số đại diện có mặt (đóng băng)
 ├─ snapshot_hash               -- giữ nguyên
 └─ created_at (= opened_at, giao dịch nguyên tử)

VOTER_ROLL_ENTRY
 ├─ voting_weight_sqm           -- copy, không join động (DB4, giữ nguyên)
 ├─ attendance_type [IN_PERSON | BY_PROXY | HOME_BALLOT | ABSENT]
 └─ eligibility_flag
   → present = SUM(voting_weight_sqm WHERE attendance_type <> ABSENT)
```

### 4.2 Mẫu số theo từng nội dung

```
AGENDA_ITEM.denominator_basis  ∈ { PRESENT_SQM | TOTAL_ELIGIBLE_SQM }
```

- Giá trị mặc định theo **loại nội dung** do Quy chế / pháp chế quyết (§9.3) — seed sau khi chốt, không hard-code.
- Mỗi `RESOLUTION` ghi cả `denominator_basis` **và** `denominator_used` (giá trị tuyệt đối đã dùng) để in nguyên văn lên biên bản.

### 4.3 Sửa DB6 và màn kiểm phiếu

- **DB6 (sửa):** với một nội dung, tổng weight của 4 trạng thái `choice` = **`total_eligible_weight_sqm`** (người vắng → `NOT_VOTED` vẫn có weight). Phép cân "= 100%" là phép cân **toàn vẹn**, luôn tính trên B-total.
- **Ngưỡng thông qua** của nội dung thì tính trên mẫu số do `denominator_basis` chọn (có thể là B-present).
- **MH-07 (sửa nhãn):** ghi rõ hai con số riêng — "mẫu số ngưỡng thông qua" (theo `denominator_basis`) và "tổng cử tri (cân đối toàn vẹn)". Không gọi chung là "snapshot".

## 5. Ràng buộc kỹ thuật bắt buộc

| # | Ràng buộc |
|---|---|
| K1 | VoterRoll gồm **mọi** đơn vị `eligibility_flag = true`, kể cả `ABSENT` |
| K2 | `present_weight_sqm`, `eligible_apartment_reps` đóng băng tại khai mạc, không tính lại (nhất quán DB3) |
| K3 | Cân đối toàn vẹn (DB6) kiểm trên `total_eligible_weight_sqm`, KHÔNG trên hiện diện |
| K4 | Mỗi `RESOLUTION` lưu `denominator_basis` + `denominator_used` |
| K5 | **Test bắt buộc:** một nội dung ngưỡng theo `TOTAL_ELIGIBLE_SQM` và một nội dung theo `PRESENT_SQM`, cùng bộ phiếu → hai kết quả khác nhau rõ rệt, đối chiếu đúng thủ công đến 2 chữ số thập phân |

## 6. Hệ quả

**Tích cực:** hết nhập nhằng mẫu số; chốt pháp chế §9.3 chỉ là đổi *giá trị mặc định*, không phá schema; biên bản nêu rõ mẫu số cho mỗi nội dung.

**Tiêu cực cần chấp nhận:** VoterRoll nặng hơn (chứa cả người vắng); UI phải hiển thị nhiều con số hơn, cần chú thích rõ để không rối.

---

# ADR-004 · Phương pháp đếm phiếu bầu Ban quản trị

## 1. Bối cảnh

M1-05 AC1 yêu cầu "bộ test 20 tình huống trọng số", nhưng **không nơi nào định nghĩa cách đếm** khi bầu "N trong M". Cùng lúc, mô hình hiện tại có ba vấn đề:

- `VOTE.choice = FOR/AGAINST/ABSTAIN/NOT_VOTED` và `RESOLUTION.for/against/abstain_sqm` là hình dạng của **biểu quyết thông qua**, không hợp bầu cử (bầu cử tally **theo từng ứng viên**).
- `AGENDA_ITEM ||--|| BALLOT` (1-1) chặn **bầu lại vòng 2**, dù `RESOLUTION.result` có `PENDING_ROUND_2`.
- `VOTER_ROLL_ENTRY.voter_ref` gắn `unit_id` → nhãn "MÃ ẨN DANH" là ảo; nhưng bầu BQT theo thực tế thường **bỏ phiếu kín**.

## 2. Vì sao đây là quyết định kiến trúc

Không có định nghĩa đếm phiếu thì **không viết được engine lẫn 20 test case** (AC1). Cách đếm cũng có hệ quả pháp lý: máy chọn thay người khi bằng phiếu là rủi ro.

## 3. Quyết định

> **Mặc định là "bầu chọn có trọng số kiểu tán thành" (weighted approval). Phương pháp là tham số cấu hình theo Quy chế bầu cử. Hệ thống không bao giờ tự xử khi bằng phiếu.**

### 3.1 Cách đếm mặc định (WEIGHTED_APPROVAL)

- Mỗi cử tri được chọn **tối đa N** ứng viên trong M.
- Mỗi ứng viên được chọn nhận **đủ trọng số m² của cử tri** (không chia nhỏ).
- Xếp hạng ứng viên theo tổng m² tán thành; lấy **N người cao nhất**.
- Khớp với MH-07 (ứng viên dẫn đầu ≈ 82% m² hiện diện).

```
AGENDA_ITEM.election_method ∈ { WEIGHTED_APPROVAL | SINGLE_CHOICE | CUMULATIVE }
   -- mặc định WEIGHTED_APPROVAL; biến thể khác chỉ bật khi Quy chế bầu cử yêu cầu
AGENDA_ITEM.elect_n           -- số người cần bầu (N)
AGENDA_ITEM.is_secret         -- bỏ phiếu kín? (xem 3.4)
```

### 3.2 Xử lý bằng phiếu (tie)

- `BALLOT.has_tie = true` khi có bằng phiếu ở vị trí cắt (thứ N).
- Hệ thống **cảnh báo, không tự chọn** (chốt lại nguyên tắc MH-07); nêu phương án theo Quy chế: bầu lại vòng 2 giữa các ứng viên bằng phiếu.

### 3.3 Tách hình dạng tally + hỗ trợ vòng 2

- Sửa quan hệ: **`AGENDA_ITEM ||--o{ BALLOT`** kèm `BALLOT.round_no` (1, 2, …).
- **Nội dung ELECTION** dùng bảng kết quả riêng, không dùng `RESOLUTION.for/against`:

```
ELECTION_RESULT
 ├─ agenda_item_id, ballot_id (round_no)
 ├─ option_id (ứng viên)
 ├─ total_weight_sqm    -- tổng m² tán thành
 ├─ rank
 └─ is_elected
```

- **Nội dung APPROVAL / DISMISSAL** giữ nguyên `VOTE` 4 trạng thái + `RESOLUTION.for/against/abstain/not_voted_sqm`.

### 3.4 Ứng viên rút tên & bỏ phiếu kín

- `VOTE_OPTION.is_withdrawn`: phiếu **bất biến** (không xóa); kết quả **tính loại** option đó; ghi log. Rút trước khi mở phiếu → loại khỏi danh sách.
- **Bỏ phiếu kín** (`is_secret = true`): với ELECTION, phiếu **không** lưu `voter_roll_entry_id` trực tiếp — chỉ ghi trọng số vào tổng hợp theo option (mất liên kết phiếu ↔ đơn vị). Biểu quyết thông qua theo m² (approval) mặc định **công khai**. → gỡ nhãn "MÃ ẨN DANH" khỏi VoterRollEntry (ADR-003), ẩn danh chỉ áp cho ELECTION khi quy chế yêu cầu.

## 4. Ràng buộc kỹ thuật bắt buộc

| # | Ràng buộc |
|---|---|
| K1 | Trọng số lấy từ `VOTER_ROLL_ENTRY` (DB4), copy, không join động |
| K2 | Số ứng viên một cử tri chọn ≤ `elect_n` |
| K3 | Khi `is_secret = true`, cấm mọi truy vấn ánh xạ phiếu ELECTION → đơn vị/cử tri |
| K4 | **Chốt `election_method` là điều kiện tiên quyết** của bộ 20 test (M1-05 AC1) — không viết test khi chưa chốt |
| K5 | Bằng phiếu ở vị trí cắt → bắt buộc `has_tie`, chặn tự công bố nội dung đó cho tới khi xử lý theo quy chế |

## 5. Hệ quả

**Tích cực:** viết được engine + test; hỗ trợ vòng 2 và bỏ phiếu kín; tally bầu cử không còn ép vào cấu trúc biểu quyết thông qua.

**Tiêu cực cần chấp nhận:** thêm bảng `ELECTION_RESULT` và hai đường tally song song (approval vs election) — engine phức tạp hơn; `is_secret` đòi kỷ luật truy vấn nghiêm ngặt.

---

# ADR-005 · Chi quỹ bảo trì ngoài kế hoạch

## 1. Bối cảnh

Hai chỗ mâu thuẫn trực tiếp:

- **DB5 + M1-11 AC1:** *"Phiếu chi quỹ bảo trì bắt buộc có `maintenance_plan_item_id` hợp lệ, không có thì bị chặn."*
- **M1-11 (mô tả) + ERD-3:** *"Chi ngoài kế hoạch → luồng phê duyệt riêng"* và có cờ `EXPENSE_VOUCHER.is_off_plan`.

Chi ngoài kế hoạch **không** gắn hạng mục → vi phạm DB5 tuyệt đối. Nhưng cấm hoàn toàn thì sự cố khẩn cấp (vỡ ống, hỏng PCCC, kẹt thang máy) không có đường chi hợp lệ — trái thực tế vận hành.

## 2. Vì sao đây là quyết định kiến trúc

DB5 là ràng buộc **chặn cứng ở tầng dữ liệu**. Viết sai một lần thì hoặc là khóa chết luồng khẩn cấp, hoặc là mở toang cửa lạm dụng quỹ. Phải định nghĩa chính xác điều kiện hợp lệ.

## 3. Quyết định

> **Phiếu chi quỹ bảo trì hợp lệ theo MỘT trong hai đường: gắn hạng mục kế hoạch đã thông qua (on-plan), HOẶC đánh dấu ngoài kế hoạch kèm căn cứ và cơ chế hậu kiểm (off-plan). Vẫn giữ tách vai trò lập (BQL) / duyệt (BQT).**

### 3.1 Hai đường hợp lệ

| Đường | Điều kiện | Căn cứ pháp lý |
|---|---|---|
| **On-plan** | `maintenance_plan_item_id` trỏ hạng mục thuộc kế hoạch **đã có nghị quyết** | R9 trực tiếp |
| **Off-plan · khẩn cấp** | `is_off_plan = true`, `off_plan_reason = EMERGENCY`, có file căn cứ, trong **hạn mức cấu hình**, BQT duyệt; **bắt buộc hậu kiểm** tại hội nghị gần nhất | R9 (hợp thức hóa sau) |
| **Off-plan · chờ hội nghị bất thường** | Không được chi cho tới khi có nghị quyết → sau khi thông qua trở thành on-plan | R9 trực tiếp |

### 3.2 Cơ chế hậu kiểm (ratification) cho khẩn cấp

- Chi trước theo hạn mức + phê duyệt BQT → `ratification_status = PENDING`.
- Đưa vào chương trình nghị sự hội nghị gần nhất để **hợp thức hóa**:
  - Thông qua → `RATIFIED` (gắn `ratified_by_resolution_id`).
  - Không thông qua → `REJECTED`: hệ thống **chỉ cảnh báo + lập hồ sơ** xử lý theo quy chế (thu hồi/hoàn ứng), **không** tự động trừ/khóa (nhất quán với nguyên tắc "phần mềm không tự cắt dịch vụ" ở C-03).

### 3.3 Sửa DB5 và AC

- **DB5 (sửa):** `fund_type = MAINTENANCE` ⇒ `maintenance_plan_item_id IS NOT NULL` **XOR** (`is_off_plan = true` AND `off_plan_reason IS NOT NULL`).
- **M1-11 AC1 (sửa):** *"Tạo phiếu chi không gắn hạng mục **và không phải off-plan hợp lệ** → bị chặn."*
- Báo cáo minh bạch (C-05) **tách dòng** on-plan / off-plan / chờ hậu kiểm.

### 3.4 Bổ sung trường (ERD-3)

```
EXPENSE_VOUCHER
 ├─ is_off_plan
 ├─ off_plan_reason        ∈ { EMERGENCY | EXTRAORDINARY_CONFERENCE }
 ├─ off_plan_evidence      -- file căn cứ (bắt buộc khi is_off_plan)
 ├─ ratification_status    ∈ { NOT_REQUIRED | PENDING | RATIFIED | REJECTED }
 └─ ratified_by_resolution_id

BUILDING_CONFIG.emergency_expense_cap   -- hạn mức chi khẩn cấp trước-hậu-kiểm, theo tòa
```

## 4. Ràng buộc kỹ thuật bắt buộc

| # | Ràng buộc |
|---|---|
| K1 | CHECK tầng DB theo DB5 sửa đổi (on-plan XOR off-plan hợp lệ) |
| K2 | `is_off_plan = true` ⇒ `off_plan_evidence` và `off_plan_reason` NOT NULL |
| K3 | `off_plan_reason = EMERGENCY` ⇒ `ratification_status` khởi tạo `PENDING`; số tiền ≤ `emergency_expense_cap` (nếu vượt → chặn, buộc chờ hội nghị) |
| K4 | Chi ngoài kế hoạch vẫn **truy vết được**: căn cứ → phê duyệt BQT → hậu kiểm hội nghị |
| K5 | Tách vai trò lập/duyệt giữ nguyên; hai quỹ không bù trừ (DB — ADR đợt sau nếu cần) |

## 5. Hệ quả

**Tích cực:** luồng khẩn cấp có đường hợp lệ mà không mở cửa lạm dụng; hậu kiểm buộc mọi khoản off-plan quay lại hội nghị; DB5 hết mâu thuẫn với M1-11.

**Tiêu cực cần chấp nhận:** thêm trạng thái hậu kiểm cần theo dõi qua nhiều kỳ; cần cấu hình hạn mức cho từng tòa; báo cáo phức tạp hơn (ba nhóm khoản chi).

---

# Tóm tắt hành động

| # | Việc | Người chịu trách nhiệm | Thời hạn |
|---|---|---|---|
| 1 | Chốt §9.3 — mẫu số (`PRESENT` vs `TOTAL_ELIGIBLE`) áp cho từng loại nội dung | Pháp chế + Product owner | Trước khi seed giá trị mặc định (ADR-003) |
| 2 | Chuẩn hóa VoterRoll 2 tổng trọng số + `denominator_basis`; sửa DB6 | Kiến trúc sư | Trước khi code engine kiểm phiếu |
| 3 | Chốt `election_method` mặc định với Quy chế bầu cử pilot | Product owner + BQT pilot | Điều kiện tiên quyết bộ 20 test (ADR-004) |
| 4 | Sửa quan hệ `AgendaItem–Ballot` 1-n + thêm `ELECTION_RESULT` | Kiến trúc sư | Cùng sprint engine bầu cử |
| 5 | Sửa DB5 (XOR on/off-plan) + trường hậu kiểm + `emergency_expense_cap` | Kiến trúc sư + BA | Trước khi code phiếu chi quỹ bảo trì |
| 6 | Cập nhật đặc tả §10.3 (DB5, DB6) và AC (M1-05 AC1, M1-11 AC1) theo 3 ADR này | BA | Cùng đợt cập nhật đặc tả |

---

*Tài liệu ghi nhận quyết định thiết kế phục vụ thảo luận nội bộ, nối tiếp ADR đợt 1. Mọi quyết định có thể xem xét lại khi có căn cứ mới, nhưng phải cập nhật kèm lý do thay đổi. Các giá trị mặc định về mẫu số và phương pháp bầu cử phải đối chiếu Thông tư 05/2024/TT-BXD và Quy chế bầu cử được hội nghị thông qua trước khi đưa vào phát triển.*

# UX Backlog P0/P1/P2 - ICare247 Config Studio

## Muc tieu

Chuyen 12 de xuat nang cao trai nghiem nguoi dung thanh backlog uu tien theo P0/P1/P2, map truc tiep vao cac man hinh 01-11 da dinh nghia trong bo prompt.

## Mapping man hinh 01-11

- `01`: Shell + Navigation
- `02`: Form Manager
- `03`: Form Editor
- `04`: Field Config
- `05`: Validation Rule Editor
- `06`: Event Editor
- `07`: Expression Builder Dialog
- `08`: Dependency Viewer
- `09`: Grammar Library
- `10`: i18n Manager
- `11`: Publish Checklist

## P0 (bat buoc lam truoc)

### UX-01 - Auto-save + Draft Recovery

- Uu tien: `P0`
- Map man hinh: `01`, `03`, `04`, `05`, `06`, `07`, `10`, `11`
- Gia tri: Giam mat du lieu khi cau hinh dai va nhieu tab.
- Tieu chi hoan thanh:
  - Tu dong luu draft moi 20-30 giay hoac khi user doi tab.
  - Khi mo lai form/rule/event dang do, hien dialog "Khoi phuc draft?".
  - Co chi bao trang thai draft o status bar (`01`).
  - Publish (`11`) chi dung du lieu da save chinh thuc, khong dung draft.
- Phu thuoc:
  - Can draft storage key theo `Tenant + Form + Screen + User`.

### UX-02 - Undo/Redo toan cuc

- Uu tien: `P0`
- Map man hinh: `01`, `03`, `04`, `05`, `06`, `07`
- Gia tri: Hoan tac nhanh thao tac sai, tang toc do chinh sua.
- Tieu chi hoan thanh:
  - Co stack `Undo/Redo` cho moi context form.
  - Ho tro shortcut `Ctrl+Z`, `Ctrl+Y`.
  - Hien toast mo ta thao tac vua hoan tac/lap lai.
  - Khong undo qua buoc da publish.
- Phu thuoc:
  - Can command history abstraction dung chung cho cac module.

### UX-03 - Live Linting (rule/event/expression)

- Uu tien: `P0`
- Map man hinh: `04`, `05`, `06`, `07`, `10`, `11`
- Gia tri: Bat loi som truoc khi bam save/publish.
- Tieu chi hoan thanh:
  - Validate real-time: AST, type mismatch, whitelist function/operator, i18n key.
  - Hien inline issue voi muc do `Error/Warning`.
  - Nut Save bi disable neu co `Error`.
  - `11` tong hop lai tat ca issue dang mo.
- Phu thuoc:
  - Tai su dung `AstValidator` tu `07`.

### UX-04 - Impact Preview truoc Save/Publish

- Uu tien: `P0`
- Map man hinh: `03`, `04`, `05`, `06`, `08`, `11`
- Gia tri: User nhin thay pham vi anh huong truoc khi xac nhan thay doi.
- Tieu chi hoan thanh:
  - Truoc Save/Publish hien danh sach field/rule/event bi anh huong.
  - Highlight nguy co vo dependency (cross-field, missing target).
  - Tu `11` co nut "View Impact Detail".
  - Tu `08` click node/edge mo impact list lien quan.
- Phu thuoc:
  - Can service phan tich dependency va expression references.

## P1 (nen lam tiep theo)

### UX-05 - Rule/Event Trace Debugger

- Uu tien: `P1`
- Map man hinh: `05`, `06`, `07`, `11`
- Gia tri: Debug quy tac nhanh, giam thoi gian tim loi logic.
- Tieu chi hoan thanh:
  - Chay simulate tung buoc: Trigger -> Condition -> Action.
  - Hien bang log "input -> evaluate -> output".
  - Cho phep export log JSON.
  - Tu `11` co nut "Run Trace".
- Phu thuoc:
  - Can evaluator mock hoac sandbox evaluator dung chung.

### UX-06 - Template/Wizard nhanh

- Uu tien: `P1`
- Map man hinh: `03`, `04`, `05`, `06`
- Gia tri: Rut ngan thao tac lap lai khi tao form/rule/event pho bien.
- Tieu chi hoan thanh:
  - Co wizard tao nhanh rule numeric/required/compare.
  - Co wizard tao event show-hide/calculate/co ban call api.
  - Template co preview truoc khi apply.
  - Co bo mau theo domain (PO, Invoice, Customer).
- Phu thuoc:
  - Can thu vien template metadata JSON.

### UX-07 - Bulk Edit (chinh sua hang loat)

- Uu tien: `P1`
- Map man hinh: `03`, `04`, `10`
- Gia tri: Tang nang suat cho form lon nhieu field.
- Tieu chi hoan thanh:
  - Cho multi-select field trong `03` va `04`.
  - Cap nhat hang loat `IsVisible`, `IsReadOnly`, `LabelKey`, `OrderNo`.
  - Co preview diff truoc khi apply.
  - Co rollback 1 buoc qua Undo stack.
- Phu thuoc:
  - Can model patch operation cho nhieu record.

### UX-08 - Context Panel "Dang duoc dung o dau"

- Uu tien: `P1`
- Map man hinh: `03`, `04`, `05`, `06`, `08`
- Gia tri: Tranh sua field/rule ma khong biet anh huong den dau.
- Tieu chi hoan thanh:
  - Khi chon field/rule/event, panel hien "Used by".
  - Liet ke dependency node, rule, event, action lien quan.
  - Co deep-link mo den editor dich.
  - Co canh bao khi xoa doi tuong dang duoc tham chieu.
- Phu thuoc:
  - Can index dependency trong local cache.

## P2 (hoan thien nang cao)

### UX-09 - Version History + Diff

- Uu tien: `P2`
- Map man hinh: `02`, `03`, `04`, `05`, `06`, `11`
- Gia tri: Doi chieu thay doi va rollback co kiem soat.
- Tieu chi hoan thanh:
  - Xem lich su version theo form.
  - Compare 2 version theo JSON diff va semantic diff.
  - Cho restore ve version cu (co confirm).
  - Publish ghi nhan changelog.
- Phu thuoc:
  - Can schema luu version snapshot.

### UX-10 - Soft Delete + Archive

- Uu tien: `P2`
- Map man hinh: `02`, `03`, `04`, `05`, `06`
- Gia tri: An toan du lieu khi xoa nham.
- Tieu chi hoan thanh:
  - Xoa doi tuong chuyen sang `Archived`, khong xoa cung.
  - Co man hinh/bo loc khoi phuc.
  - Neu doi tuong co dependency thi canh bao muc do.
  - Lich su khoi phuc duoc ghi audit.
- Phu thuoc:
  - Can cot trang thai archive va luong restore.

### UX-11 - Keyboard-first UX + Command Palette

- Uu tien: `P2`
- Map man hinh: `01` (global), ap dung cho `02`-`11`
- Gia tri: Tang toc thao tac cho power-user.
- Tieu chi hoan thanh:
  - `Ctrl+K` mo command palette.
  - Shortcut cho Save, Publish check, Add rule/event, mo Expression Builder.
  - Hien tooltip shortcut tren button.
  - Co bang shortcut help.
- Phu thuoc:
  - Can command registry toan app.

### UX-12 - Permission-aware UI

- Uu tien: `P2`
- Map man hinh: `01`, `02`, `03`, `04`, `05`, `06`, `09`, `10`, `11`
- Gia tri: Giam nham lan, dung quyen dung vai tro.
- Tieu chi hoan thanh:
  - Role `Editor/Reviewer/Publisher/Admin` co quyen rieng.
  - Nut khong du quyen thi disable + tooltip ly do.
  - `11` chi role co quyen moi publish duoc.
  - Co man hinh xem permission matrix co ban.
- Phu thuoc:
  - Can service phan giai role/permission phia client.

## Goi y chia sprint

- Sprint 1 (P0): `UX-01`, `UX-02`, `UX-03`, `UX-04`
- Sprint 2 (P1): `UX-05`, `UX-06`, `UX-07`, `UX-08`
- Sprint 3 (P2): `UX-09`, `UX-10`, `UX-11`, `UX-12`

## Dinh nghia Done chung

- Pass build solution va khong co compile warning nghiem trong.
- Co test checklist manual cho moi man hinh bi anh huong.
- Co demo script 5-10 phut cho user nghiep vu.
- Co cap nhat tai lieu su dung trong `HOW_TO_USE.html` hoac file huong dan moi.

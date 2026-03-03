# P0 Technical Tasks By WPF Module - ICare247 Config Studio

## Pham vi

File nay chi tach 4 hang muc `P0` tu backlog:

- `UX-01`: Auto-save + Draft Recovery
- `UX-02`: Undo/Redo toan cuc
- `UX-03`: Live Linting
- `UX-04`: Impact Preview truoc Save/Publish

Nguon: `docs/ICare247 Config Studio/UX_BACKLOG_P0_P1_P2.md`

## Module map

- `App Shell`: `ICare247.ConfigStudio`
- `Core`: `ICare247.ConfigStudio.Core`
- `Forms`: `ICare247.ConfigStudio.Modules.Forms`
- `Rules`: `ICare247.ConfigStudio.Modules.Rules`
- `Events`: `ICare247.ConfigStudio.Modules.Events`
- `Grammar`: `ICare247.ConfigStudio.Modules.Grammar`
- `I18n`: `ICare247.ConfigStudio.Modules.I18n`

## P0-01 Auto-save + Draft Recovery

### Core

- `P0-01-CORE-01`: Tao `IDraftStoreService` + model `DraftContextKey(TenantId, User, FormId, ScreenCode, EntityId)`
- `P0-01-CORE-02`: Tao `DraftSnapshot` (payload json, timestamp, schema version, hash)
- `P0-01-CORE-03`: Tao `IDraftSerializer` de serialize/deserialize ViewModel state
- `P0-01-CORE-04`: Tao policy `DraftSavePolicy` (interval 20-30s, debounce change)

### App Shell

- `P0-01-SHELL-01`: Them draft indicator vao status bar (`Saved`, `Saving`, `Unsaved Draft`)
- `P0-01-SHELL-02`: Global event bus channel `DraftStatusChanged`
- `P0-01-SHELL-03`: Dialog template `RestoreDraftDialog`

### Forms

- `P0-01-FORMS-01`: Hook auto-save vao `FormEditorViewModel`
- `P0-01-FORMS-02`: Hook auto-save vao `FieldConfigViewModel`
- `P0-01-FORMS-03`: OnNavigatedTo -> check draft ton tai -> show restore dialog
- `P0-01-FORMS-04`: Save thanh cong -> xoa draft hoac mark clean

### Rules

- `P0-01-RULES-01`: Hook auto-save vao `ValidationRuleEditorViewModel`
- `P0-01-RULES-02`: Luu ca `ExpressionJson`, `ConditionExpr`, quick-config state
- `P0-01-RULES-03`: Khoi phuc state va re-validate sau restore

### Events

- `P0-01-EVENTS-01`: Hook auto-save vao `EventEditorViewModel`
- `P0-01-EVENTS-02`: Luu chain actions + order + condition
- `P0-01-EVENTS-03`: Restore action config typed theo action code

### Grammar

- `P0-01-GRAMMAR-01`: Dialog ExpressionBuilder support draft theo caller context
- `P0-01-GRAMMAR-02`: Restore AST tree + selected node + expected return type

### I18n

- `P0-01-I18N-01`: Draft cho inline edit grid batch chua save
- `P0-01-I18N-02`: Restore pending translation edits theo language tab

### Done cho P0-01

- Mat ket noi hoac app crash, mo lai van co the khoi phuc du lieu dang sua.
- Khong co cross-screen draft overwrite sai context.

## P0-02 Undo/Redo toan cuc

### Core

- `P0-02-CORE-01`: Tao `IUndoRedoService` voi stack `Undo`/`Redo` theo `FormContext`
- `P0-02-CORE-02`: Tao `IReversibleCommand` (`Do`, `Undo`, `Description`)
- `P0-02-CORE-03`: Tao wrapper `TrackedSetProperty` de ghi command history cho property change
- `P0-02-CORE-04`: Gioi han stack size (vd 100-200 actions/context)

### App Shell

- `P0-02-SHELL-01`: Register hotkey global `Ctrl+Z`, `Ctrl+Y`
- `P0-02-SHELL-02`: Hien toast "Undo: <action>", "Redo: <action>"
- `P0-02-SHELL-03`: Disable undo/redo khi context khong ho tro

### Forms

- `P0-02-FORMS-01`: Track add/edit/delete/reorder section
- `P0-02-FORMS-02`: Track add/edit/delete/reorder field
- `P0-02-FORMS-03`: Gom action nhieu buoc thanh transaction command (bulk reorder)

### Rules

- `P0-02-RULES-01`: Track doi `RuleType`, quick config params, error key, translation text
- `P0-02-RULES-02`: Track thay doi `ExpressionJson`/`ConditionExpr`
- `P0-02-RULES-03`: Undo action duplicate/delete rule local state

### Events

- `P0-02-EVENTS-01`: Track add/remove/move action chain
- `P0-02-EVENTS-02`: Track edit param cua tung action type
- `P0-02-EVENTS-03`: Track doi trigger/scope/condition

### Grammar

- `P0-02-GRAMMAR-01`: Undo node insert/delete/replace/wrap trong AST tree
- `P0-02-GRAMMAR-02`: Undo literal edit va operator change

### I18n

- `P0-02-I18N-01`: Undo inline edit cell
- `P0-02-I18N-02`: Undo import batch (rollback transaction local truoc save)

### Done cho P0-02

- Undo/Redo hoat dong on dinh tren 5 luong chinh: FormEditor, FieldConfig, RuleEditor, EventEditor, ExpressionBuilder.
- Khong phat sinh state leak giua 2 form dang mo khac nhau.

## P0-03 Live Linting

### Core

- `P0-03-CORE-01`: Tao `IValidationIssueService` tra ve `Error/Warning/Info`
- `P0-03-CORE-02`: Tao `ValidationIssue` model (code, message, screen, entity, severity)
- `P0-03-CORE-03`: Tao aggregator `IWorkspaceIssueStore` de tong hop issue theo form
- `P0-03-CORE-04`: Event `IssuesChanged` de cap nhat UI realtime

### App Shell

- `P0-03-SHELL-01`: Badge issue tren nav item va status bar
- `P0-03-SHELL-02`: Quick panel "Current Issues" co deep-link den man hinh loi

### Forms

- `P0-03-FORMS-01`: Lint field config co ban (missing label key, invalid order, invalid editor map)
- `P0-03-FORMS-02`: Tab Rules/Events trong `FieldConfig` hien warning count

### Rules

- `P0-03-RULES-01`: Validate realtime expression return type boolean (tru Required)
- `P0-03-RULES-02`: Validate referenced fields ton tai va net type compatible
- `P0-03-RULES-03`: Validate error key format va translation ton tai (voi I18n module)

### Events

- `P0-03-EVENTS-01`: Validate trigger/scope hop le
- `P0-03-EVENTS-02`: Validate action param json theo action schema
- `P0-03-EVENTS-03`: Validate `CallAPI` placeholder field ton tai
- `P0-03-EVENTS-04`: Validate `Calculate` expected return type khop field target

### Grammar

- `P0-03-GRAMMAR-01`: Mo rong `AstValidator` tra ve issue chi tiet theo node path
- `P0-03-GRAMMAR-02`: Validate depth <= 20, function/operator whitelist
- `P0-03-GRAMMAR-03`: Expose validator cho Rules/Events dung chung

### I18n

- `P0-03-I18N-01`: Validate missing translation theo language required
- `P0-03-I18N-02`: Push issue sang `WorkspaceIssueStore` cho `PublishChecklist`

### Done cho P0-03

- Issue hien real-time < 300ms sau khi user thay doi.
- Save button disable khi co `Error` critical theo screen policy.

## P0-04 Impact Preview truoc Save/Publish

### Core

- `P0-04-CORE-01`: Tao `IImpactAnalysisService`
- `P0-04-CORE-02`: Model `ImpactItem` (source, target, relation, riskLevel, message)
- `P0-04-CORE-03`: Tao analyzer extract refs tu AST (`Identifier`, `Function`, `CustomHandler`)
- `P0-04-CORE-04`: Tao policy phan cap rui ro (`Low/Medium/High`)

### Forms

- `P0-04-FORMS-01`: Truoc save field/section, hien impact dialog
- `P0-04-FORMS-02`: Khi doi `ColumnCode` hoac xoa field, hien danh sach rule/event bi anh huong

### Rules

- `P0-04-RULES-01`: Truoc save rule, hien cross-field dependencies se tao/cap nhat
- `P0-04-RULES-02`: Canh bao neu doi expression lam thay doi referenced fields

### Events

- `P0-04-EVENTS-01`: Truoc save event, hien field targets bi tac dong theo action chain
- `P0-04-EVENTS-02`: Canh bao action invalid target (field da xoa/doi type)

### Grammar

- `P0-04-GRAMMAR-01`: Tu ExpressionBuilder tra `ReferencedFields` cho impact service
- `P0-04-GRAMMAR-02`: Ho tro highlight node gay impact lon

### I18n

- `P0-04-I18N-01`: Khi doi/xoa resource key, hien cac rule/event/field dang tham chieu key do

### Publish Checklist

- `P0-04-PUBLISH-01`: Them section "Impact Summary" trong `PublishChecklistViewModel`
- `P0-04-PUBLISH-02`: Nut "View Impact Detail" deep-link sang `DependencyViewer` (`08`)

### Done cho P0-04

- Moi hanh dong save/publish co impact preview khi co phat hien phu thuoc.
- User co the deep-link tu impact item den editor de fix.

## Thu tu implement de giam rui ro

1. `Core` contracts + shared services (`Draft`, `UndoRedo`, `Issues`, `Impact`)
2. `Grammar` (validator/analyzer) vi duoc dung chung boi Rules/Events
3. `Rules` + `Events` (nhieu logic AST nhat)
4. `Forms` + `I18n`
5. `App Shell` polish (status, badge, hotkey, dialog)
6. `PublishChecklist` integration cuoi

## Uoc luong tuong doi (goi y)

- `P0-01`: 8-12 ngay cong
- `P0-02`: 8-10 ngay cong
- `P0-03`: 10-14 ngay cong
- `P0-04`: 7-10 ngay cong

Tong P0: 33-46 ngay cong (doi 3-4 dev trong 2 sprint la hop ly).

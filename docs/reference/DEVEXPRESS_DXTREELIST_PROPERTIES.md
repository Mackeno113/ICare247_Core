# DevExpress Blazor — TreeList: Toàn bộ thuộc tính

> Trích xuất tự động từ `DevExpress.Blazor.v25.2` **v25.2.3.0** (đúng version project đang dùng).
> Công cụ: `tools/DxReflect` (MetadataLoadContext). Cột **P** = `[Parameter]` (dùng trực tiếp trong `.razor`).

## DxTreeList

Tổng: **103** thuộc tính (đều là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowColumnReorder` | `Boolean` |
| ✅ | `AllowDragRows` | `Boolean` |
| ✅ | `AllowedDropTarget` | `TreeListAllowedDropTarget` |
| ✅ | `AllowSelectRowByClick` | `Boolean` |
| ✅ | `AllowSort` | `Boolean` |
| ✅ | `Attributes` | `IReadOnlyDictionary<String, Object>` |
| ✅ | `AutoExpandAllNodes` | `Boolean` |
| ✅ | `ChildrenFieldName` | `String` |
| ✅ | `ChildrenLoading` | `Action<TreeListChildrenLoadingEventArgs>` |
| ✅ | `ChildrenLoadingOnDemand` | `Func<TreeListChildrenLoadingOnDemandEventArgs, Task>` |
| ✅ | `ColumnCaptionAlignment` | `TreeListTextAlignment?` |
| ✅ | `ColumnFooterTemplate` | `RenderFragment<TreeListColumnFooterTemplateContext>` |
| ✅ | `ColumnHeaderCaptionTemplate` | `RenderFragment<TreeListColumnHeaderCaptionTemplateContext>` |
| ✅ | `ColumnResizeMode` | `TreeListColumnResizeMode` |
| ✅ | `Columns` | `RenderFragment` |
| ✅ | `ContextMenus` | `TreeListContextMenus` |
| ✅ | `CssClass` | `String` |
| ✅ | `CustomizeCellDisplayText` | `Action<TreeListCustomizeCellDisplayTextEventArgs>` |
| ✅ | `CustomizeContextMenu` | `Action<TreeListCustomizeContextMenuEventArgs>` |
| ✅ | `CustomizeDataRowEditor` | `Action<TreeListCustomizeDataRowEditorEventArgs>` |
| ✅ | `CustomizeEditModel` | `EventCallback<TreeListCustomizeEditModelEventArgs>` |
| ✅ | `CustomizeElement` | `Action<TreeListCustomizeElementEventArgs>` |
| ✅ | `CustomizeFilterMenu` | `Action<TreeListCustomizeFilterMenuEventArgs>` |
| ✅ | `CustomizeFilterRowEditor` | `Action<TreeListCustomizeFilterRowEditorEventArgs>` |
| ✅ | `CustomizeSummaryDisplayText` | `Action<TreeListCustomizeSummaryDisplayTextEventArgs>` |
| ✅ | `CustomSort` | `Action<TreeListCustomSortEventArgs>` |
| ✅ | `CustomSummary` | `Action<TreeListCustomSummaryEventArgs>` |
| ✅ | `CustomValidators` | `RenderFragment` |
| ✅ | `Data` | `Object` |
| ✅ | `DataColumnCellDisplayTemplate` | `RenderFragment<TreeListDataColumnCellDisplayTemplateContext>` |
| ✅ | `DataColumnCellEditTemplate` | `RenderFragment<TreeListDataColumnCellEditTemplateContext>` |
| ✅ | `DataColumnFilterMenuTemplate` | `RenderFragment<TreeListDataColumnFilterMenuTemplateContext>` |
| ✅ | `DataColumnFilterRowCellTemplate` | `RenderFragment<TreeListDataColumnFilterRowCellTemplateContext>` |
| ✅ | `DataItemDeleting` | `EventCallback<TreeListDataItemDeletingEventArgs>` |
| ✅ | `DragHintTextTemplate` | `RenderFragment<TreeListDragHintTextTemplateContext>` |
| ✅ | `DropTargetMode` | `TreeListDropTargetMode` |
| ✅ | `EditCanceling` | `EventCallback<TreeListEditCancelingEventArgs>` |
| ✅ | `EditFormButtonsVisible` | `Boolean` |
| ✅ | `EditFormTemplate` | `RenderFragment<TreeListEditFormTemplateContext>` |
| ✅ | `EditMode` | `TreeListEditMode` |
| ✅ | `EditModelSaving` | `EventCallback<TreeListEditModelSavingEventArgs>` |
| ✅ | `EditNewRootRowPosition` | `TreeListEditNewRootRowPosition` |
| ✅ | `EditOnKeyPress` | `Boolean` |
| ✅ | `EditStart` | `EventCallback<TreeListEditStartEventArgs>` |
| ✅ | `EmptyDataAreaTemplate` | `RenderFragment<TreeListEmptyDataAreaTemplateContext>` |
| ✅ | `EnterKeyDirection` | `TreeListEnterKeyDirection` |
| ✅ | `FilterBuilderTemplate` | `RenderFragment<TreeListFilterBuilderTemplateContext>` |
| ✅ | `FilterCriteriaChanged` | `EventCallback<TreeListFilterCriteriaChangedEventArgs>` |
| ✅ | `FilterMenuButtonDisplayMode` | `TreeListFilterMenuButtonDisplayMode` |
| ✅ | `FilterPanelDisplayMode` | `TreeListFilterPanelDisplayMode` |
| ✅ | `FilterTreeMode` | `TreeListFilterTreeMode` |
| ✅ | `FocusedRowChanged` | `EventCallback<TreeListFocusedRowChangedEventArgs>` |
| ✅ | `FocusedRowEnabled` | `Boolean` |
| ✅ | `FooterDisplayMode` | `TreeListFooterDisplayMode` |
| ✅ | `HasChildrenFieldName` | `String` |
| ✅ | `HighlightRowOnHover` | `Boolean` |
| ✅ | `ItemsDropped` | `EventCallback<TreeListItemsDroppedEventArgs>` |
| ✅ | `KeyFieldName` | `String` |
| ✅ | `LayoutAutoLoading` | `Func<TreeListPersistentLayoutEventArgs, Task>` |
| ✅ | `LayoutAutoSaving` | `Func<TreeListPersistentLayoutEventArgs, Task>` |
| ✅ | `PageIndex` | `Int32` |
| ✅ | `PageIndexChanged` | `EventCallback<Int32>` |
| ✅ | `PagerAutoHideNavButtons` | `Boolean` |
| ✅ | `PagerNavigationMode` | `PagerNavigationMode` |
| ✅ | `PagerPosition` | `TreeListPagerPosition` |
| ✅ | `PagerSwitchToInputBoxButtonCount` | `Int32` |
| ✅ | `PagerVisible` | `Boolean` |
| ✅ | `PagerVisibleNumericButtonCount` | `Int32` |
| ✅ | `PageSize` | `Int32` |
| ✅ | `PageSizeChanged` | `EventCallback<Int32>` |
| ✅ | `PageSizeSelectorAllRowsItemVisible` | `Boolean` |
| ✅ | `PageSizeSelectorItems` | `IReadOnlyList<Int32>` |
| ✅ | `PageSizeSelectorVisible` | `Boolean` |
| ✅ | `ParentKeyFieldName` | `String` |
| ✅ | `PopupEditFormCssClass` | `String` |
| ✅ | `PopupEditFormHeaderText` | `String` |
| ✅ | `RootValue` | `Object` |
| ✅ | `RowClick` | `EventCallback<TreeListRowClickEventArgs>` |
| ✅ | `RowDoubleClick` | `EventCallback<TreeListRowClickEventArgs>` |
| ✅ | `SearchBoxInputDelay` | `Int32` |
| ✅ | `SearchBoxNullText` | `String` |
| ✅ | `SearchBoxTemplate` | `RenderFragment<TreeListSearchBoxTemplateContext>` |
| ✅ | `SearchText` | `String` |
| ✅ | `SearchTextChanged` | `EventCallback<String>` |
| ✅ | `SearchTextParseMode` | `TreeListSearchTextParseMode` |
| ✅ | `SelectAllCheckboxMode` | `TreeListSelectAllCheckboxMode` |
| ✅ | `SelectedDataItem` | `Object` |
| ✅ | `SelectedDataItemChanged` | `EventCallback<Object>` |
| ✅ | `SelectedDataItems` | `IReadOnlyList<Object>` |
| ✅ | `SelectedDataItemsChanged` | `EventCallback<IReadOnlyList<Object>>` |
| ✅ | `SelectionMode` | `TreeListSelectionMode` |
| ✅ | `ShowAllRows` | `Boolean` |
| ✅ | `ShowAllRowsChanged` | `EventCallback<Boolean>` |
| ✅ | `ShowFilterRow` | `Boolean` |
| ✅ | `ShowSearchBox` | `Boolean` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `SkeletonRowsEnabled` | `Boolean?` |
| ✅ | `TextWrapEnabled` | `Boolean` |
| ✅ | `ToolbarTemplate` | `RenderFragment<TreeListToolbarTemplateContext>` |
| ✅ | `TotalSummary` | `RenderFragment` |
| ✅ | `ValidationEnabled` | `Boolean` |
| ✅ | `VirtualScrollingEnabled` | `Boolean` |
| ✅ | `VirtualScrollingMode` | `TreeListVirtualScrollingMode` |

## DxTreeListBandColumn

Tổng: **17** thuộc tính (đều là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowReorder` | `Boolean?` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionAlignment` | `TreeListTextAlignment?` |
| ✅ | `Columns` | `RenderFragment` |
| ✅ | `FixedPosition` | `TreeListColumnFixedPosition` |
| ✅ | `FooterTemplate` | `RenderFragment<TreeListColumnFooterTemplateContext>` |
| ✅ | `HeaderCaptionTemplate` | `RenderFragment<TreeListColumnHeaderCaptionTemplateContext>` |
| ✅ | `MinWidth` | `Int32` |
| ✅ | `Name` | `String` |
| ✅ | `ShowInColumnChooser` | `Boolean` |
| ✅ | `TextAlignment` | `TreeListTextAlignment` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `VisibleIndex` | `Int32` |
| ✅ | `VisibleIndexChanged` | `EventCallback<Int32>` |
| ✅ | `Width` | `String` |
| ✅ | `WidthChanged` | `EventCallback<String>` |

## DxTreeListColumn

Tổng: **16** thuộc tính (đều là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowReorder` | `Boolean?` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionAlignment` | `TreeListTextAlignment?` |
| ✅ | `FixedPosition` | `TreeListColumnFixedPosition` |
| ✅ | `FooterTemplate` | `RenderFragment<TreeListColumnFooterTemplateContext>` |
| ✅ | `HeaderCaptionTemplate` | `RenderFragment<TreeListColumnHeaderCaptionTemplateContext>` |
| ✅ | `MinWidth` | `Int32` |
| ✅ | `Name` | `String` |
| ✅ | `ShowInColumnChooser` | `Boolean` |
| ✅ | `TextAlignment` | `TreeListTextAlignment` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `VisibleIndex` | `Int32` |
| ✅ | `VisibleIndexChanged` | `EventCallback<Int32>` |
| ✅ | `Width` | `String` |
| ✅ | `WidthChanged` | `EventCallback<String>` |

## DxTreeListCommandColumn

Tổng: **27** thuộc tính (đều là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowReorder` | `Boolean?` |
| ✅ | `CancelButtonVisible` | `Boolean` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionAlignment` | `TreeListTextAlignment?` |
| ✅ | `CellDisplayTemplate` | `RenderFragment<TreeListCommandColumnCellDisplayTemplateContext>` |
| ✅ | `CellEditTemplate` | `RenderFragment<TreeListCommandColumnCellEditTemplateContext>` |
| ✅ | `ClearFilterButtonVisible` | `Boolean` |
| ✅ | `DeleteButtonVisible` | `Boolean` |
| ✅ | `DisplayMode` | `TreeListCommandColumnDisplayMode` |
| ✅ | `EditButtonVisible` | `Boolean` |
| ✅ | `FilterRowCellTemplate` | `RenderFragment<TreeListCommandColumnFilterRowCellTemplateContext>` |
| ✅ | `FixedPosition` | `TreeListColumnFixedPosition` |
| ✅ | `FooterTemplate` | `RenderFragment<TreeListColumnFooterTemplateContext>` |
| ✅ | `HeaderCaptionTemplate` | `RenderFragment<TreeListColumnHeaderCaptionTemplateContext>` |
| ✅ | `HeaderTemplate` | `RenderFragment<TreeListCommandColumnHeaderTemplateContext>` |
| ✅ | `MinWidth` | `Int32` |
| ✅ | `Name` | `String` |
| ✅ | `NewButtonVisible` | `Boolean` |
| ✅ | `SaveButtonVisible` | `Boolean` |
| ✅ | `ShowInColumnChooser` | `Boolean` |
| ✅ | `TextAlignment` | `TreeListTextAlignment` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `VisibleIndex` | `Int32` |
| ✅ | `VisibleIndexChanged` | `EventCallback<Int32>` |
| ✅ | `Width` | `String` |
| ✅ | `WidthChanged` | `EventCallback<String>` |

## DxTreeListDataColumn

Tổng: **42** thuộc tính (đều là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowReorder` | `Boolean?` |
| ✅ | `AllowSort` | `Boolean?` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionAlignment` | `TreeListTextAlignment?` |
| ✅ | `CellDisplayTemplate` | `RenderFragment<TreeListDataColumnCellDisplayTemplateContext>` |
| ✅ | `CellEditTemplate` | `RenderFragment<TreeListDataColumnCellEditTemplateContext>` |
| ✅ | `DataRowEditorVisible` | `Boolean` |
| ✅ | `DisplayFormat` | `String` |
| ✅ | `EditSettings` | `RenderFragment` |
| ✅ | `ExportEnabled` | `Boolean` |
| ✅ | `ExportWidth` | `Int32` |
| ✅ | `FieldName` | `String` |
| ✅ | `FilterBuilderFieldDisplayMode` | `TreeListColumnFilterBuilderFieldDisplayMode` |
| ✅ | `FilterMenuButtonDisplayMode` | `TreeListFilterMenuButtonDisplayMode` |
| ✅ | `FilterMenuTemplate` | `RenderFragment<TreeListDataColumnFilterMenuTemplateContext>` |
| ✅ | `FilterMode` | `TreeListColumnFilterMode` |
| ✅ | `FilterRowCellTemplate` | `RenderFragment<TreeListDataColumnFilterRowCellTemplateContext>` |
| ✅ | `FilterRowEditorVisible` | `Boolean` |
| ✅ | `FilterRowOperatorType` | `TreeListFilterRowOperatorType` |
| ✅ | `FilterRowOperatorTypeChanged` | `EventCallback<TreeListFilterRowOperatorType>` |
| ✅ | `FilterRowValue` | `Object` |
| ✅ | `FilterRowValueChanged` | `EventCallback<Object>` |
| ✅ | `FixedPosition` | `TreeListColumnFixedPosition` |
| ✅ | `FooterTemplate` | `RenderFragment<TreeListColumnFooterTemplateContext>` |
| ✅ | `HeaderCaptionTemplate` | `RenderFragment<TreeListColumnHeaderCaptionTemplateContext>` |
| ✅ | `MinWidth` | `Int32` |
| ✅ | `Name` | `String` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `SearchEnabled` | `Boolean` |
| ✅ | `ShowInColumnChooser` | `Boolean` |
| ✅ | `SortIndex` | `Int32` |
| ✅ | `SortIndexChanged` | `EventCallback<Int32>` |
| ✅ | `SortMode` | `TreeListColumnSortMode` |
| ✅ | `SortOrder` | `TreeListColumnSortOrder` |
| ✅ | `SortOrderChanged` | `EventCallback<TreeListColumnSortOrder>` |
| ✅ | `TextAlignment` | `TreeListTextAlignment` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `VisibleIndex` | `Int32` |
| ✅ | `VisibleIndexChanged` | `EventCallback<Int32>` |
| ✅ | `Width` | `String` |
| ✅ | `WidthChanged` | `EventCallback<String>` |

## DxTreeListSelectionColumn

Tổng: **20** thuộc tính (đều là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowReorder` | `Boolean?` |
| ✅ | `AllowSelectAll` | `Boolean` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionAlignment` | `TreeListTextAlignment?` |
| ✅ | `CellDisplayTemplate` | `RenderFragment<TreeListSelectionColumnCellDisplayTemplateContext>` |
| ✅ | `FilterRowCellTemplate` | `RenderFragment<TreeListSelectionColumnFilterRowCellTemplateContext>` |
| ✅ | `FixedPosition` | `TreeListColumnFixedPosition` |
| ✅ | `FooterTemplate` | `RenderFragment<TreeListColumnFooterTemplateContext>` |
| ✅ | `HeaderCaptionTemplate` | `RenderFragment<TreeListColumnHeaderCaptionTemplateContext>` |
| ✅ | `HeaderTemplate` | `RenderFragment<TreeListSelectionColumnHeaderTemplateContext>` |
| ✅ | `MinWidth` | `Int32` |
| ✅ | `Name` | `String` |
| ✅ | `ShowInColumnChooser` | `Boolean` |
| ✅ | `TextAlignment` | `TreeListTextAlignment` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `VisibleIndex` | `Int32` |
| ✅ | `VisibleIndexChanged` | `EventCallback<Int32>` |
| ✅ | `Width` | `String` |
| ✅ | `WidthChanged` | `EventCallback<String>` |

## DxTreeListSummaryItem

Tổng: **7** thuộc tính (đều là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `DisplayText` | `String` |
| ✅ | `FieldName` | `String` |
| ✅ | `FooterColumnName` | `String` |
| ✅ | `Name` | `String` |
| ✅ | `SummaryType` | `TreeListSummaryItemType` |
| ✅ | `ValueDisplayFormat` | `String` |
| ✅ | `Visible` | `Boolean` |

---

## Ghi chú thực dụng (đúc kết khi dùng)

- **Cây cha–con:** bind `Data` + `KeyFieldName` + `ParentKeyFieldName`; node gốc có parent = `null`
  (hoặc đặt `RootValue`). Có thể dùng `ChildrenFieldName` nếu dữ liệu lồng sẵn.
- **Hiện hết, KHÔNG phân trang:** dùng **`ShowAllRows="true"`** (đúng API). ⚠️ KHÔNG dùng `PageSize="0"`
  — với TreeList nó làm cây chỉ còn 1 dòng (đã dính lỗi này). Bung sẵn mọi cấp: `AutoExpandAllNodes="true"`.
- **Ẩn pager:** `PagerVisible="false"` (khi vẫn để paging mà muốn giấu thanh trang).
- **Ô tùy biến (checkbox…):** `DxTreeListDataColumn` + `<CellDisplayTemplate>`; trong template lấy dòng qua
  `context.DataItem` (cast về model). Cột cây = `DxTreeListDataColumn` đầu tiên (FieldName text).
- **Cuộn dọc giữ header:** `VirtualScrollingEnabled="true"` (cần đặt chiều cao cho TreeList qua CssClass).
- **Cố định cột:** `DxTreeListDataColumn.FixedPosition`; **căn lề:** `TextAlignment`; **độ rộng:** `Width`/`MinWidth`.

> File này sinh tự động bằng `tools/DxReflect` — chạy lại: `dotnet run --project tools/DxReflect -- TreeList`
> (đổi `TreeList` thành tên control khác để xuất control tương ứng).

# DevExpress Blazor — Thuộc tính các control hay dùng

> Trích xuất tự động từ `DevExpress.Blazor.v25.2` **v25.2.3.0** (đúng version project đang dùng).
> Cột **P** = `[Parameter]` (dùng trực tiếp trong markup `.razor`). Sinh lại bằng `tools/DxReflect`.

## Mục lục

- Grid & TreeList
- Editor — Text & Number
- Editor — Lựa chọn
- Editor — Ngày giờ
- Layout
- Nút & Toolbar
- Overlay

# Grid & TreeList

## DxGrid

Tổng: **113** thuộc tính (113 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowColumnReorder` | `Boolean` |
| ✅ | `AllowDragRows` | `Boolean` |
| ✅ | `AllowedDropTarget` | `GridAllowedDropTarget` |
| ✅ | `AllowGroup` | `Boolean` |
| ✅ | `AllowSelectRowByClick` | `Boolean` |
| ✅ | `AllowSort` | `Boolean` |
| ✅ | `Attributes` | `IReadOnlyDictionary<String, Object>` |
| ✅ | `AutoCollapseDetailRow` | `Boolean` |
| ✅ | `AutoExpandAllGroupRows` | `Boolean` |
| ✅ | `ColumnCaptionAlignment` | `GridTextAlignment?` |
| ✅ | `ColumnFooterTemplate` | `RenderFragment<GridColumnFooterTemplateContext>` |
| ✅ | `ColumnGroupFooterTemplate` | `RenderFragment<GridColumnGroupFooterTemplateContext>` |
| ✅ | `ColumnHeaderCaptionTemplate` | `RenderFragment<GridColumnHeaderCaptionTemplateContext>` |
| ✅ | `ColumnResizeMode` | `GridColumnResizeMode` |
| ✅ | `Columns` | `RenderFragment` |
| ✅ | `ContextMenus` | `GridContextMenus` |
| ✅ | `CssClass` | `String` |
| ✅ | `CustomGroup` | `Action<GridCustomGroupEventArgs>` |
| ✅ | `CustomizeCellDisplayText` | `Action<GridCustomizeCellDisplayTextEventArgs>` |
| ✅ | `CustomizeContextMenu` | `Action<GridCustomizeContextMenuEventArgs>` |
| ✅ | `CustomizeDataRowEditor` | `Action<GridCustomizeDataRowEditorEventArgs>` |
| ✅ | `CustomizeEditModel` | `EventCallback<GridCustomizeEditModelEventArgs>` |
| ✅ | `CustomizeElement` | `Action<GridCustomizeElementEventArgs>` |
| ✅ | `CustomizeFilterMenu` | `Action<GridCustomizeFilterMenuEventArgs>` |
| ✅ | `CustomizeFilterRowEditor` | `Action<GridCustomizeFilterRowEditorEventArgs>` |
| ✅ | `CustomizeGroupValueDisplayText` | `Action<GridCustomizeGroupValueDisplayTextEventArgs>` |
| ✅ | `CustomizeSummaryDisplayText` | `Action<GridCustomizeSummaryDisplayTextEventArgs>` |
| ✅ | `CustomSort` | `Action<GridCustomSortEventArgs>` |
| ✅ | `CustomSummary` | `Action<GridCustomSummaryEventArgs>` |
| ✅ | `CustomValidators` | `RenderFragment` |
| ✅ | `Data` | `Object` |
| ✅ | `DataColumnCellDisplayTemplate` | `RenderFragment<GridDataColumnCellDisplayTemplateContext>` |
| ✅ | `DataColumnCellEditTemplate` | `RenderFragment<GridDataColumnCellEditTemplateContext>` |
| ✅ | `DataColumnFilterMenuTemplate` | `RenderFragment<GridDataColumnFilterMenuTemplateContext>` |
| ✅ | `DataColumnFilterRowCellTemplate` | `RenderFragment<GridDataColumnFilterRowCellTemplateContext>` |
| ✅ | `DataColumnGroupRowTemplate` | `RenderFragment<GridDataColumnGroupRowTemplateContext>` |
| ✅ | `DataItemDeleting` | `EventCallback<GridDataItemDeletingEventArgs>` |
| ✅ | `DetailExpandButtonDisplayMode` | `GridDetailExpandButtonDisplayMode` |
| ✅ | `DetailRowDisplayMode` | `GridDetailRowDisplayMode` |
| ✅ | `DetailRowTemplate` | `RenderFragment<GridDetailRowTemplateContext>` |
| ✅ | `DragHintTextTemplate` | `RenderFragment<GridDragHintTextTemplateContext>` |
| ✅ | `DropTargetMode` | `GridDropTargetMode` |
| ✅ | `EditCanceling` | `EventCallback<GridEditCancelingEventArgs>` |
| ✅ | `EditFormButtonsVisible` | `Boolean` |
| ✅ | `EditFormTemplate` | `RenderFragment<GridEditFormTemplateContext>` |
| ✅ | `EditMode` | `GridEditMode` |
| ✅ | `EditModelSaving` | `EventCallback<GridEditModelSavingEventArgs>` |
| ✅ | `EditNewRowPosition` | `GridEditNewRowPosition` |
| ✅ | `EditOnKeyPress` | `Boolean` |
| ✅ | `EditorRenderMode` | `GridEditorRenderMode` |
| ✅ | `EditStart` | `EventCallback<GridEditStartEventArgs>` |
| ✅ | `EmptyDataAreaTemplate` | `RenderFragment<GridEmptyDataAreaTemplateContext>` |
| ✅ | `EnterKeyDirection` | `GridEnterKeyDirection` |
| ✅ | `FilterBuilderTemplate` | `RenderFragment<GridFilterBuilderTemplateContext>` |
| ✅ | `FilterCriteriaChanged` | `EventCallback<GridFilterCriteriaChangedEventArgs>` |
| ✅ | `FilterMenuButtonDisplayMode` | `GridFilterMenuButtonDisplayMode` |
| ✅ | `FilterPanelDisplayMode` | `GridFilterPanelDisplayMode` |
| ✅ | `FocusedRowChanged` | `EventCallback<GridFocusedRowChangedEventArgs>` |
| ✅ | `FocusedRowEnabled` | `Boolean` |
| ✅ | `FooterDisplayMode` | `GridFooterDisplayMode` |
| ✅ | `GroupFooterDisplayMode` | `GridGroupFooterDisplayMode` |
| ✅ | `GroupSummary` | `RenderFragment` |
| ✅ | `HighlightRowOnHover` | `Boolean` |
| ✅ | `ItemsDropped` | `EventCallback<GridItemsDroppedEventArgs>` |
| ✅ | `KeyboardNavigationEnabled` | `Boolean` |
| ✅ | `KeyFieldName` | `String` |
| ✅ | `KeyFieldNames` | `IReadOnlyList<String>` |
| ✅ | `LayoutAutoLoading` | `Func<GridPersistentLayoutEventArgs, Task>` |
| ✅ | `LayoutAutoSaving` | `Func<GridPersistentLayoutEventArgs, Task>` |
| ✅ | `PageIndex` | `Int32` |
| ✅ | `PageIndexChanged` | `EventCallback<Int32>` |
| ✅ | `PagerAutoHideNavButtons` | `Boolean` |
| ✅ | `PagerNavigationMode` | `PagerNavigationMode` |
| ✅ | `PagerPosition` | `GridPagerPosition` |
| ✅ | `PagerSwitchToInputBoxButtonCount` | `Int32` |
| ✅ | `PagerVisible` | `Boolean` |
| ✅ | `PagerVisibleNumericButtonCount` | `Int32` |
| ✅ | `PageSize` | `Int32` |
| ✅ | `PageSizeChanged` | `EventCallback<Int32>` |
| ✅ | `PageSizeSelectorAllRowsItemVisible` | `Boolean` |
| ✅ | `PageSizeSelectorItems` | `IReadOnlyList<Int32>` |
| ✅ | `PageSizeSelectorVisible` | `Boolean` |
| ✅ | `PopupEditFormCssClass` | `String` |
| ✅ | `PopupEditFormHeaderText` | `String` |
| ✅ | `RowClick` | `EventCallback<GridRowClickEventArgs>` |
| ✅ | `RowDoubleClick` | `EventCallback<GridRowClickEventArgs>` |
| ✅ | `SearchBoxInputDelay` | `Int32` |
| ✅ | `SearchBoxNullText` | `String` |
| ✅ | `SearchBoxTemplate` | `RenderFragment<GridSearchBoxTemplateContext>` |
| ✅ | `SearchText` | `String` |
| ✅ | `SearchTextChanged` | `EventCallback<String>` |
| ✅ | `SearchTextParseMode` | `GridSearchTextParseMode` |
| ✅ | `SelectAllCheckboxMode` | `GridSelectAllCheckboxMode` |
| ✅ | `SelectedDataItem` | `Object` |
| ✅ | `SelectedDataItemChanged` | `EventCallback<Object>` |
| ✅ | `SelectedDataItems` | `IReadOnlyList<Object>` |
| ✅ | `SelectedDataItemsChanged` | `EventCallback<IReadOnlyList<Object>>` |
| ✅ | `SelectionMode` | `GridSelectionMode` |
| ✅ | `ShowAllRows` | `Boolean` |
| ✅ | `ShowAllRowsChanged` | `EventCallback<Boolean>` |
| ✅ | `ShowFilterRow` | `Boolean` |
| ✅ | `ShowGroupedColumns` | `Boolean` |
| ✅ | `ShowGroupPanel` | `Boolean` |
| ✅ | `ShowSearchBox` | `Boolean` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `SkeletonRowsEnabled` | `Boolean?` |
| ✅ | `TextWrapEnabled` | `Boolean` |
| ✅ | `ToolbarTemplate` | `RenderFragment<GridToolbarTemplateContext>` |
| ✅ | `TotalSummary` | `RenderFragment` |
| ✅ | `UnboundColumnData` | `Action<GridUnboundColumnDataEventArgs>` |
| ✅ | `ValidationEnabled` | `Boolean` |
| ✅ | `VirtualScrollingEnabled` | `Boolean` |
| ✅ | `VirtualScrollingMode` | `GridVirtualScrollingMode` |

## DxGridDataColumn

Tổng: **50** thuộc tính (50 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowGroup` | `Boolean?` |
| ✅ | `AllowReorder` | `Boolean?` |
| ✅ | `AllowSort` | `Boolean?` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionAlignment` | `GridTextAlignment?` |
| ✅ | `CellDisplayTemplate` | `RenderFragment<GridDataColumnCellDisplayTemplateContext>` |
| ✅ | `CellEditTemplate` | `RenderFragment<GridDataColumnCellEditTemplateContext>` |
| ✅ | `DataRowEditorVisible` | `Boolean` |
| ✅ | `DisplayFormat` | `String` |
| ✅ | `EditSettings` | `RenderFragment` |
| ✅ | `ExportEnabled` | `Boolean` |
| ✅ | `ExportWidth` | `Int32` |
| ✅ | `FieldName` | `String` |
| ✅ | `FilterBuilderFieldDisplayMode` | `GridColumnFilterBuilderFieldDisplayMode` |
| ✅ | `FilterMenuButtonDisplayMode` | `GridFilterMenuButtonDisplayMode` |
| ✅ | `FilterMenuTemplate` | `RenderFragment<GridDataColumnFilterMenuTemplateContext>` |
| ✅ | `FilterMode` | `GridColumnFilterMode` |
| ✅ | `FilterRowCellTemplate` | `RenderFragment<GridDataColumnFilterRowCellTemplateContext>` |
| ✅ | `FilterRowEditorVisible` | `Boolean` |
| ✅ | `FilterRowOperatorType` | `GridFilterRowOperatorType` |
| ✅ | `FilterRowOperatorTypeChanged` | `EventCallback<GridFilterRowOperatorType>` |
| ✅ | `FilterRowValue` | `Object` |
| ✅ | `FilterRowValueChanged` | `EventCallback<Object>` |
| ✅ | `FixedPosition` | `GridColumnFixedPosition` |
| ✅ | `FooterTemplate` | `RenderFragment<GridColumnFooterTemplateContext>` |
| ✅ | `GroupFooterTemplate` | `RenderFragment<GridColumnGroupFooterTemplateContext>` |
| ✅ | `GroupIndex` | `Int32` |
| ✅ | `GroupIndexChanged` | `EventCallback<Int32>` |
| ✅ | `GroupInterval` | `GridColumnGroupInterval` |
| ✅ | `GroupRowTemplate` | `RenderFragment<GridDataColumnGroupRowTemplateContext>` |
| ✅ | `HeaderCaptionTemplate` | `RenderFragment<GridColumnHeaderCaptionTemplateContext>` |
| ✅ | `MinWidth` | `Int32` |
| ✅ | `Name` | `String` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `SearchEnabled` | `Boolean` |
| ✅ | `ShowInColumnChooser` | `Boolean` |
| ✅ | `SortIndex` | `Int32` |
| ✅ | `SortIndexChanged` | `EventCallback<Int32>` |
| ✅ | `SortMode` | `GridColumnSortMode` |
| ✅ | `SortOrder` | `GridColumnSortOrder` |
| ✅ | `SortOrderChanged` | `EventCallback<GridColumnSortOrder>` |
| ✅ | `TextAlignment` | `GridTextAlignment` |
| ✅ | `UnboundExpression` | `String` |
| ✅ | `UnboundType` | `GridUnboundColumnType` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `VisibleIndex` | `Int32` |
| ✅ | `VisibleIndexChanged` | `EventCallback<Int32>` |
| ✅ | `Width` | `String` |
| ✅ | `WidthChanged` | `EventCallback<String>` |

## DxGridSelectionColumn

Tổng: **21** thuộc tính (21 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowReorder` | `Boolean?` |
| ✅ | `AllowSelectAll` | `Boolean` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionAlignment` | `GridTextAlignment?` |
| ✅ | `CellDisplayTemplate` | `RenderFragment<GridSelectionColumnCellDisplayTemplateContext>` |
| ✅ | `FilterRowCellTemplate` | `RenderFragment<GridSelectionColumnFilterRowCellTemplateContext>` |
| ✅ | `FixedPosition` | `GridColumnFixedPosition` |
| ✅ | `FooterTemplate` | `RenderFragment<GridColumnFooterTemplateContext>` |
| ✅ | `GroupFooterTemplate` | `RenderFragment<GridColumnGroupFooterTemplateContext>` |
| ✅ | `HeaderCaptionTemplate` | `RenderFragment<GridColumnHeaderCaptionTemplateContext>` |
| ✅ | `HeaderTemplate` | `RenderFragment<GridSelectionColumnHeaderTemplateContext>` |
| ✅ | `MinWidth` | `Int32` |
| ✅ | `Name` | `String` |
| ✅ | `ShowInColumnChooser` | `Boolean` |
| ✅ | `TextAlignment` | `GridTextAlignment` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `VisibleIndex` | `Int32` |
| ✅ | `VisibleIndexChanged` | `EventCallback<Int32>` |
| ✅ | `Width` | `String` |
| ✅ | `WidthChanged` | `EventCallback<String>` |

## DxGridCommandColumn

Tổng: **28** thuộc tính (28 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowReorder` | `Boolean?` |
| ✅ | `CancelButtonVisible` | `Boolean` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionAlignment` | `GridTextAlignment?` |
| ✅ | `CellDisplayTemplate` | `RenderFragment<GridCommandColumnCellDisplayTemplateContext>` |
| ✅ | `CellEditTemplate` | `RenderFragment<GridCommandColumnCellEditTemplateContext>` |
| ✅ | `ClearFilterButtonVisible` | `Boolean` |
| ✅ | `DeleteButtonVisible` | `Boolean` |
| ✅ | `DisplayMode` | `GridCommandColumnDisplayMode` |
| ✅ | `EditButtonVisible` | `Boolean` |
| ✅ | `FilterRowCellTemplate` | `RenderFragment<GridCommandColumnFilterRowCellTemplateContext>` |
| ✅ | `FixedPosition` | `GridColumnFixedPosition` |
| ✅ | `FooterTemplate` | `RenderFragment<GridColumnFooterTemplateContext>` |
| ✅ | `GroupFooterTemplate` | `RenderFragment<GridColumnGroupFooterTemplateContext>` |
| ✅ | `HeaderCaptionTemplate` | `RenderFragment<GridColumnHeaderCaptionTemplateContext>` |
| ✅ | `HeaderTemplate` | `RenderFragment<GridCommandColumnHeaderTemplateContext>` |
| ✅ | `MinWidth` | `Int32` |
| ✅ | `Name` | `String` |
| ✅ | `NewButtonVisible` | `Boolean` |
| ✅ | `SaveButtonVisible` | `Boolean` |
| ✅ | `ShowInColumnChooser` | `Boolean` |
| ✅ | `TextAlignment` | `GridTextAlignment` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `VisibleIndex` | `Int32` |
| ✅ | `VisibleIndexChanged` | `EventCallback<Int32>` |
| ✅ | `Width` | `String` |
| ✅ | `WidthChanged` | `EventCallback<String>` |

## DxGridSummaryItem

Tổng: **7** thuộc tính (7 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `DisplayText` | `String` |
| ✅ | `FieldName` | `String` |
| ✅ | `FooterColumnName` | `String` |
| ✅ | `Name` | `String` |
| ✅ | `SummaryType` | `GridSummaryItemType` |
| ✅ | `ValueDisplayFormat` | `String` |
| ✅ | `Visible` | `Boolean` |

## DxTreeList

Tổng: **103** thuộc tính (103 là `[Parameter]`).

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

## DxTreeListDataColumn

Tổng: **42** thuộc tính (42 là `[Parameter]`).

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

# Editor — Text & Number

## DxTextBox

Tổng: **23** thuộc tính (23 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `BindValueMode` | `BindValueMode` |
| ✅ | `Buttons` | `RenderFragment` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `ClearButtonDisplayMode` | `DataEditorClearButtonDisplayMode` |
| ✅ | `CssClass` | `String` |
| ✅ | `DisplayFormat` | `String` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `Id` | `String` |
| ✅ | `InputCssClass` | `String` |
| ✅ | `InputDelay` | `Int32?` |
| ✅ | `InputId` | `String` |
| ✅ | `NullText` | `String` |
| ✅ | `Password` | `Boolean` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `ShowValidationIcon` | `Boolean?` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `Text` | `String` |
| ✅ | `TextChanged` | `EventCallback<String>` |
| ✅ | `TextChanging` | `Action<ParameterValueChangingEventArgs<String>>` |
| ✅ | `TextExpression` | `Expression<Func<String>>` |
| ✅ | `ValidationEnabled` | `Boolean?` |

## DxMemo

Tổng: **25** thuộc tính (24 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `BindValueMode` | `BindValueMode` |
| ✅ | `ClearButtonDisplayMode` | `DataEditorClearButtonDisplayMode` |
| ✅ | `Columns` | `Int32` |
| ✅ | `CssClass` | `String` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `Extensions` | `RenderFragment` |
| ✅ | `Id` | `String` |
| ✅ | `InputDelay` | `Int32?` |
| ✅ | `InputId` | `String` |
| ✅ | `MaxRows` | `Int32?` |
| ✅ | `NullText` | `String` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `ResizeMode` | `MemoResizeMode` |
| ✅ | `Rows` | `Int32` |
| ✅ | `ShowValidationIcon` | `Boolean?` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `Text` | `String` |
| ✅ | `TextAreaCssClass` | `String` |
| ✅ | `TextChanged` | `EventCallback<String>` |
| ✅ | `TextChanging` | `Action<ParameterValueChangingEventArgs<String>>` |
| ✅ | `TextExpression` | `Expression<Func<String>>` |
| ✅ | `ValidationEnabled` | `Boolean?` |
|  | `InputCssClass` | `String` |

## DxMaskedInput<T>

Tổng: **25** thuộc tính (25 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `BindValueMode` | `BindValueMode` |
| ✅ | `Buttons` | `RenderFragment` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `ClearButtonDisplayMode` | `DataEditorClearButtonDisplayMode` |
| ✅ | `CssClass` | `String` |
| ✅ | `DisplayFormat` | `String` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `Id` | `String` |
| ✅ | `InputCssClass` | `String` |
| ✅ | `InputDelay` | `Int32?` |
| ✅ | `InputId` | `String` |
| ✅ | `Mask` | `String` |
| ✅ | `MaskMode` | `MaskMode` |
| ✅ | `NullText` | `String` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `ShowValidationIcon` | `Boolean?` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `TextChanged` | `EventCallback<String>` |
| ✅ | `ValidationEnabled` | `Boolean?` |
| ✅ | `Value` | `T` |
| ✅ | `ValueChanged` | `EventCallback<T>` |
| ✅ | `ValueChanging` | `Action<ParameterValueChangingEventArgs<T>>` |
| ✅ | `ValueExpression` | `Expression<Func<T>>` |

## DxSpinEdit<T>

Tổng: **28** thuộc tính (28 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowMouseWheel` | `Boolean` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `BindValueMode` | `BindValueMode` |
| ✅ | `Buttons` | `RenderFragment` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `ClearButtonDisplayMode` | `DataEditorClearButtonDisplayMode` |
| ✅ | `CssClass` | `String` |
| ✅ | `DisplayFormat` | `String` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `Id` | `String` |
| ✅ | `Increment` | `T` |
| ✅ | `InputCssClass` | `String` |
| ✅ | `InputDelay` | `Int32?` |
| ✅ | `InputId` | `String` |
| ✅ | `Mask` | `String` |
| ✅ | `MaxValue` | `T` |
| ✅ | `MinValue` | `T` |
| ✅ | `NullText` | `String` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `ShowSpinButtons` | `Boolean` |
| ✅ | `ShowValidationIcon` | `Boolean?` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `ValidationEnabled` | `Boolean?` |
| ✅ | `Value` | `T` |
| ✅ | `ValueChanged` | `EventCallback<T>` |
| ✅ | `ValueChanging` | `Action<ParameterValueChangingEventArgs<T>>` |
| ✅ | `ValueExpression` | `Expression<Func<T>>` |

# Editor — Lựa chọn

## DxComboBox<TData, TValue>

Tổng: **61** thuộc tính (61 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowUserInput` | `Boolean` |
| ✅ | `Attributes` | `IReadOnlyDictionary<String, Object>` |
| ✅ | `Buttons` | `RenderFragment` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `ClearButtonDisplayMode` | `DataEditorClearButtonDisplayMode` |
| ✅ | `ColumnCellDisplayTemplate` | `RenderFragment<ComboBoxColumnCellDisplayTemplateContext<TData>>` |
| ✅ | `Columns` | `RenderFragment` |
| ✅ | `CssClass` | `String` |
| ✅ | `CustomData` | `Func<DataSourceLoadOptionsBase, CancellationToken, Task<LoadResult>>` |
| ✅ | `Data` | `IEnumerable<TData>` |
| ✅ | `DataAsync` | `Func<CancellationToken, Task<IEnumerable<TData>>>` |
| ✅ | `DataLoadMode` | `ListDataLoadMode` |
| ✅ | `DisabledFieldName` | `String` |
| ✅ | `DisplayFormat` | `String` |
| ✅ | `DropDownBodyCssClass` | `String` |
| ✅ | `DropDownCssClass` | `String` |
| ✅ | `DropDownDirection` | `DropDownDirection` |
| ✅ | `DropDownTriggerMode` | `DropDownTriggerMode` |
| ✅ | `DropDownVisible` | `Boolean` |
| ✅ | `DropDownVisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `DropDownWidthMode` | `DropDownWidthMode` |
| ✅ | `EditBoxDisplayTemplate` | `RenderFragment<ComboBoxEditBoxDisplayTemplateContext<TData, TValue>>` |
| ✅ | `EditBoxTemplate` | `RenderFragment<TData>` |
| ✅ | `EditFormat` | `String` |
| ✅ | `EmptyDataAreaTemplate` | `RenderFragment<ComboBoxEmptyDataAreaTemplateContext>` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `FilteringMode` | `DataGridFilteringMode` |
| ✅ | `GroupFieldName` | `String` |
| ✅ | `GroupHeaderDisplayTemplate` | `RenderFragment<ComboBoxGroupHeaderDisplayTemplateContext>` |
| ✅ | `Id` | `String` |
| ✅ | `InputCssClass` | `String` |
| ✅ | `InputId` | `String` |
| ✅ | `ItemDisplayTemplate` | `RenderFragment<ComboBoxItemDisplayTemplateContext<TData>>` |
| ✅ | `ItemTemplate` | `RenderFragment<TData>` |
| ✅ | `KeyFieldName` | `String` |
| ✅ | `KeyFieldNames` | `IReadOnlyList<String>` |
| ✅ | `ListRenderMode` | `ListRenderMode` |
| ✅ | `NullText` | `String` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `SearchDelay` | `Int32` |
| ✅ | `SearchFilterCondition` | `ListSearchFilterCondition` |
| ✅ | `SearchMode` | `ListSearchMode` |
| ✅ | `SearchTextParseMode` | `ListSearchTextParseMode` |
| ✅ | `SelectedDataItemChanged` | `EventCallback<SelectedDataItemChangedEventArgs<TData>>` |
| ✅ | `SelectedItemChanged` | `EventCallback<TData>` |
| ✅ | `ShowDropDownButton` | `Boolean` |
| ✅ | `ShowValidationIcon` | `Boolean?` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `Text` | `String` |
| ✅ | `TextChanged` | `EventCallback<String>` |
| ✅ | `TextChanging` | `Action<ParameterValueChangingEventArgs<String>>` |
| ✅ | `TextExpression` | `Expression<Func<String>>` |
| ✅ | `TextFieldName` | `String` |
| ✅ | `ValidateBy` | `ComboBoxValidateBy` |
| ✅ | `ValidationEnabled` | `Boolean?` |
| ✅ | `Value` | `TValue` |
| ✅ | `ValueChanged` | `EventCallback<TValue>` |
| ✅ | `ValueChanging` | `Action<ParameterValueChangingEventArgs<TValue>>` |
| ✅ | `ValueExpression` | `Expression<Func<TValue>>` |
| ✅ | `ValueFieldName` | `String` |

## DxListBox<TData, TValue>

Tổng: **45** thuộc tính (45 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Attributes` | `IReadOnlyDictionary<String, Object>` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `ColumnCellDisplayTemplate` | `RenderFragment<ListBoxColumnCellDisplayTemplateContext<TData>>` |
| ✅ | `Columns` | `RenderFragment` |
| ✅ | `CssClass` | `String` |
| ✅ | `CustomData` | `Func<DataSourceLoadOptionsBase, CancellationToken, Task<LoadResult>>` |
| ✅ | `Data` | `IEnumerable<TData>` |
| ✅ | `DataAsync` | `Func<CancellationToken, Task<IEnumerable<TData>>>` |
| ✅ | `DisabledFieldName` | `String` |
| ✅ | `EmptyDataAreaTemplate` | `RenderFragment<ListBoxEmptyDataAreaTemplateContext>` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `FilterCriteriaChanged` | `EventCallback<ListBoxFilterCriteriaChangedEventArgs<TData, TValue>>` |
| ✅ | `GroupFieldName` | `String` |
| ✅ | `GroupHeaderDisplayTemplate` | `RenderFragment<ListBoxGroupHeaderDisplayTemplateContext>` |
| ✅ | `Id` | `String` |
| ✅ | `ItemClick` | `EventCallback<ListBoxItemClickEventArgs<TData, TValue>>` |
| ✅ | `ItemDisplayTemplate` | `RenderFragment<ListBoxItemDisplayTemplateContext<TData>>` |
| ✅ | `ItemTemplate` | `RenderFragment<TData>` |
| ✅ | `KeyFieldName` | `String` |
| ✅ | `KeyFieldNames` | `IReadOnlyList<String>` |
| ✅ | `ListRenderMode` | `ListRenderMode` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `SearchBoxInputDelay` | `Int32` |
| ✅ | `SearchBoxNullText` | `String` |
| ✅ | `SearchText` | `String` |
| ✅ | `SearchTextChanged` | `EventCallback<String>` |
| ✅ | `SearchTextParseMode` | `ListSearchTextParseMode` |
| ✅ | `SelectedDataItemsChanged` | `EventCallback<SelectedDataItemsChangedEventArgs<TData>>` |
| ✅ | `SelectedItemsChanged` | `EventCallback<IEnumerable<TData>>` |
| ✅ | `SelectionMode` | `ListBoxSelectionMode` |
| ✅ | `ShowCheckboxes` | `Boolean` |
| ✅ | `ShowSearchBox` | `Boolean` |
| ✅ | `ShowSelectAllCheckbox` | `Boolean` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `TextFieldName` | `String` |
| ✅ | `ValidationEnabled` | `Boolean?` |
| ✅ | `Value` | `TValue` |
| ✅ | `ValueChanged` | `EventCallback<TValue>` |
| ✅ | `ValueExpression` | `Expression<Func<TValue>>` |
| ✅ | `ValueFieldName` | `String` |
| ✅ | `Values` | `IEnumerable<TValue>` |
| ✅ | `ValuesChanged` | `EventCallback<IEnumerable<TValue>>` |
| ✅ | `ValuesChanging` | `Action<ParameterValueChangingEventArgs<IEnumerable<TValue>>>` |
| ✅ | `ValuesExpression` | `Expression<Func<IEnumerable<TValue>>>` |

## DxTagBox<TData, TValue>

Tổng: **58** thuộc tính (58 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowCustomTags` | `Boolean` |
| ✅ | `Attributes` | `IReadOnlyDictionary<String, Object>` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `ClearButtonDisplayMode` | `DataEditorClearButtonDisplayMode` |
| ✅ | `ColumnCellDisplayTemplate` | `RenderFragment<TagBoxColumnCellDisplayTemplateContext<TData>>` |
| ✅ | `Columns` | `RenderFragment` |
| ✅ | `CssClass` | `String` |
| ✅ | `CustomData` | `Func<DataSourceLoadOptionsBase, CancellationToken, Task<LoadResult>>` |
| ✅ | `Data` | `IEnumerable<TData>` |
| ✅ | `DataAsync` | `Func<CancellationToken, Task<IEnumerable<TData>>>` |
| ✅ | `DisabledFieldName` | `String` |
| ✅ | `DropDownBodyCssClass` | `String` |
| ✅ | `DropDownCssClass` | `String` |
| ✅ | `DropDownDirection` | `DropDownDirection` |
| ✅ | `DropDownVisible` | `Boolean` |
| ✅ | `DropDownVisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `DropDownWidthMode` | `DropDownWidthMode` |
| ✅ | `EditBoxDisplayTemplate` | `RenderFragment<TagBoxEditBoxDisplayTemplateContext<TData, TValue>>` |
| ✅ | `EditFormat` | `String` |
| ✅ | `EmptyDataAreaTemplate` | `RenderFragment<TagBoxEmptyDataAreaTemplateContext>` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `FilteringMode` | `DataGridFilteringMode` |
| ✅ | `GroupFieldName` | `String` |
| ✅ | `GroupHeaderDisplayTemplate` | `RenderFragment<TagBoxGroupHeaderDisplayTemplateContext>` |
| ✅ | `HideSelectedItems` | `Boolean` |
| ✅ | `Id` | `String` |
| ✅ | `InputCssClass` | `String` |
| ✅ | `InputId` | `String` |
| ✅ | `ItemDisplayTemplate` | `RenderFragment<TagBoxItemDisplayTemplateContext<TData>>` |
| ✅ | `ItemTemplate` | `RenderFragment<TData>` |
| ✅ | `KeyFieldName` | `String` |
| ✅ | `KeyFieldNames` | `IReadOnlyList<String>` |
| ✅ | `ListRenderMode` | `ListRenderMode` |
| ✅ | `NullText` | `String` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `SearchDelay` | `Int32` |
| ✅ | `SearchFilterCondition` | `ListSearchFilterCondition` |
| ✅ | `SearchMode` | `ListSearchMode` |
| ✅ | `SearchTextParseMode` | `ListSearchTextParseMode` |
| ✅ | `SelectedDataItemsChanged` | `EventCallback<SelectedDataItemsChangedEventArgs<TData>>` |
| ✅ | `SelectedItemsChanged` | `EventCallback<IEnumerable<TData>>` |
| ✅ | `ShowValidationIcon` | `Boolean?` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `TagDisplayTemplate` | `RenderFragment<TagBoxTagDisplayTemplateContext<TData>>` |
| ✅ | `Tags` | `IEnumerable<String>` |
| ✅ | `TagsChanged` | `EventCallback<IEnumerable<String>>` |
| ✅ | `TagsChanging` | `Action<ParameterValueChangingEventArgs<IEnumerable<String>>>` |
| ✅ | `TagsExpression` | `Expression<Func<IEnumerable<String>>>` |
| ✅ | `TagTemplate` | `RenderFragment<TData>` |
| ✅ | `TextFieldName` | `String` |
| ✅ | `ValidateBy` | `TagBoxValidateBy` |
| ✅ | `ValidationEnabled` | `Boolean?` |
| ✅ | `ValueFieldName` | `String` |
| ✅ | `Values` | `IEnumerable<TValue>` |
| ✅ | `ValuesChanged` | `EventCallback<IEnumerable<TValue>>` |
| ✅ | `ValuesChanging` | `Action<ParameterValueChangingEventArgs<IEnumerable<TValue>>>` |
| ✅ | `ValuesExpression` | `Expression<Func<IEnumerable<TValue>>>` |

## DxRadioGroup<TData, TValue>

Tổng: **23** thuộc tính (23 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `CssClass` | `String` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `EnabledFieldName` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `ItemAlignment` | `CheckBoxContentAlignment` |
| ✅ | `ItemCssClass` | `String` |
| ✅ | `ItemIconCssClass` | `String` |
| ✅ | `ItemLabelPosition` | `LabelPosition` |
| ✅ | `ItemLabelWrapMode` | `LabelWrapMode` |
| ✅ | `Items` | `IEnumerable<TData>` |
| ✅ | `ItemTemplate` | `RenderFragment<TData>` |
| ✅ | `Layout` | `RadioGroupLayout` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `TextFieldName` | `String` |
| ✅ | `ValidationEnabled` | `Boolean?` |
| ✅ | `Value` | `TValue` |
| ✅ | `ValueChanged` | `EventCallback<TValue>` |
| ✅ | `ValueExpression` | `Expression<Func<TValue>>` |
| ✅ | `ValueFieldName` | `String` |

## DxCheckBox<T>

Tổng: **23** thuộc tính (23 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Alignment` | `CheckBoxContentAlignment` |
| ✅ | `AllowIndeterminateState` | `Boolean` |
| ✅ | `AllowIndeterminateStateByClick` | `Boolean` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `Checked` | `T` |
| ✅ | `CheckedChanged` | `EventCallback<T>` |
| ✅ | `CheckedExpression` | `Expression<Func<T>>` |
| ✅ | `CheckType` | `CheckType` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `CssClass` | `String` |
| ✅ | `DisableDefaultRender` | `Boolean` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `Id` | `String` |
| ✅ | `InputId` | `String` |
| ✅ | `LabelPosition` | `LabelPosition` |
| ✅ | `LabelWrapMode` | `LabelWrapMode` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `ValidationEnabled` | `Boolean?` |
| ✅ | `ValueChecked` | `T` |
| ✅ | `ValueIndeterminate` | `T` |
| ✅ | `ValueUnchecked` | `T` |

# Editor — Ngày giờ

## DxDateEdit<T>

Tổng: **47** thuộc tính (47 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `ApplyValueOnOutsideClick` | `Boolean` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `Buttons` | `RenderFragment` |
| ✅ | `CalendarViewMode` | `CalendarViewMode` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `ClearButtonDisplayMode` | `DataEditorClearButtonDisplayMode` |
| ✅ | `CssClass` | `String` |
| ✅ | `CustomDisabledDate` | `Action<CalendarCustomDisabledDateEventArgs>` |
| ✅ | `Date` | `T` |
| ✅ | `DateChanged` | `EventCallback<T>` |
| ✅ | `DateChanging` | `Action<ParameterValueChangingEventArgs<T>>` |
| ✅ | `DateExpression` | `Expression<Func<T>>` |
| ✅ | `DayCellTemplate` | `RenderFragment<DateTime>` |
| ✅ | `DisabledDateNotificationText` | `String` |
| ✅ | `DisplayFormat` | `String` |
| ✅ | `DropDownBodyCssClass` | `String` |
| ✅ | `DropDownCssClass` | `String` |
| ✅ | `DropDownDirection` | `DropDownDirection` |
| ✅ | `DropDownVisible` | `Boolean` |
| ✅ | `DropDownVisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `FirstDayOfWeek` | `DayOfWeek` |
| ✅ | `Format` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `InputCssClass` | `String` |
| ✅ | `InputId` | `String` |
| ✅ | `Mask` | `String` |
| ✅ | `MaskProperties` | `RenderFragment` |
| ✅ | `MaxDate` | `DateTime` |
| ✅ | `MinDate` | `DateTime` |
| ✅ | `NullText` | `String` |
| ✅ | `NullValue` | `DateTime` |
| ✅ | `OutOfRangeNotificationText` | `String` |
| ✅ | `PickerDisplayMode` | `DatePickerDisplayMode` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `ScrollPickerFormat` | `String` |
| ✅ | `ShowDropDownButton` | `Boolean` |
| ✅ | `ShowValidationIcon` | `Boolean?` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `TimeSectionHourIncrement` | `Int32` |
| ✅ | `TimeSectionMinuteIncrement` | `Int32` |
| ✅ | `TimeSectionScrollPickerFormat` | `String` |
| ✅ | `TimeSectionSecondIncrement` | `Int32` |
| ✅ | `TimeSectionVisible` | `Boolean` |
| ✅ | `ValidationEnabled` | `Boolean?` |
| ✅ | `WeekNumberRule` | `WeekNumberRule` |

## DxCalendar<T>

Tổng: **26** thuộc tính (26 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `CssClass` | `String` |
| ✅ | `CustomDisabledDate` | `Action<CalendarCustomDisabledDateEventArgs>` |
| ✅ | `DayCellTemplate` | `RenderFragment<DateTime>` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `EnableMultiSelect` | `Boolean` |
| ✅ | `FirstDayOfWeek` | `DayOfWeek` |
| ✅ | `HeaderCssClass` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `MaxDate` | `DateTime` |
| ✅ | `MinDate` | `DateTime` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `SelectedDate` | `T` |
| ✅ | `SelectedDateChanged` | `EventCallback<T>` |
| ✅ | `SelectedDateExpression` | `Expression<Func<T>>` |
| ✅ | `SelectedDates` | `IEnumerable<T>` |
| ✅ | `SelectedDatesChanged` | `EventCallback<IEnumerable<T>>` |
| ✅ | `SelectedDatesExpression` | `Expression<Func<IEnumerable<T>>>` |
| ✅ | `ShowClearButton` | `Boolean` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `ValidationEnabled` | `Boolean?` |
| ✅ | `ViewMode` | `CalendarViewMode` |
| ✅ | `VisibleDate` | `DateTime` |
| ✅ | `VisibleDateChanged` | `EventCallback<DateTime>` |
| ✅ | `WeekNumberRule` | `WeekNumberRule` |

## DxTimeEdit<T>

Tổng: **35** thuộc tính (35 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `ApplyValueOnOutsideClick` | `Boolean` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `Buttons` | `RenderFragment` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `ClearButtonDisplayMode` | `DataEditorClearButtonDisplayMode` |
| ✅ | `CssClass` | `String` |
| ✅ | `DisplayFormat` | `String` |
| ✅ | `DropDownBodyCssClass` | `String` |
| ✅ | `DropDownCssClass` | `String` |
| ✅ | `DropDownDirection` | `DropDownDirection` |
| ✅ | `DropDownVisible` | `Boolean` |
| ✅ | `DropDownVisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `Format` | `String` |
| ✅ | `HourIncrement` | `Int32` |
| ✅ | `Id` | `String` |
| ✅ | `InputCssClass` | `String` |
| ✅ | `InputId` | `String` |
| ✅ | `Mask` | `String` |
| ✅ | `MaxTime` | `TimeSpan` |
| ✅ | `MinTime` | `TimeSpan` |
| ✅ | `MinuteIncrement` | `Int32` |
| ✅ | `NullText` | `String` |
| ✅ | `OutOfRangeNotificationText` | `String` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `ScrollPickerFormat` | `String` |
| ✅ | `SecondIncrement` | `Int32` |
| ✅ | `ShowDropDownButton` | `Boolean` |
| ✅ | `ShowValidationIcon` | `Boolean?` |
| ✅ | `ShowValidationSuccessState` | `Boolean?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `Time` | `T` |
| ✅ | `TimeChanged` | `EventCallback<T>` |
| ✅ | `TimeExpression` | `Expression<Func<T>>` |
| ✅ | `ValidationEnabled` | `Boolean?` |

# Layout

## DxFormLayout

Tổng: **12** thuộc tính (12 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `CaptionPosition` | `CaptionPosition` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `CssClass` | `String` |
| ✅ | `Data` | `Object` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `Id` | `String` |
| ✅ | `ItemCaptionAlignment` | `ItemCaptionAlignment` |
| ✅ | `ItemSizeMode` | `SizeMode?` |
| ✅ | `ItemUpdating` | `EventCallback<KeyValuePair<String, Object>>` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `SizeMode` | `SizeMode?` |

## DxFormLayoutItem

Tổng: **20** thuộc tính (20 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `BeginRow` | `Boolean` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionCssClass` | `String` |
| ✅ | `CaptionFor` | `String` |
| ✅ | `CaptionPosition` | `CaptionPosition` |
| ✅ | `CaptionTemplate` | `RenderFragment` |
| ✅ | `ChildContent` | `RenderFragment<Object>` |
| ✅ | `ColSpanLg` | `Int32` |
| ✅ | `ColSpanMd` | `Int32` |
| ✅ | `ColSpanSm` | `Int32` |
| ✅ | `ColSpanXl` | `Int32` |
| ✅ | `ColSpanXs` | `Int32` |
| ✅ | `ColSpanXxl` | `Int32` |
| ✅ | `CssClass` | `String` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `Field` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `Template` | `RenderFragment<Object>` |
| ✅ | `Visible` | `Boolean` |

## DxFormLayoutGroup

Tổng: **31** thuộc tính (31 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AnimationType` | `LayoutAnimationType` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `BeginRow` | `Boolean` |
| ✅ | `Caption` | `String` |
| ✅ | `CaptionCssClass` | `String` |
| ✅ | `CaptionPosition` | `CaptionPosition` |
| ✅ | `CaptionTemplate` | `RenderFragment<IFormLayoutGroupInfo>` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `CollapseButtonIconCssClass` | `String` |
| ✅ | `ColSpanLg` | `Int32` |
| ✅ | `ColSpanMd` | `Int32` |
| ✅ | `ColSpanSm` | `Int32` |
| ✅ | `ColSpanXl` | `Int32` |
| ✅ | `ColSpanXs` | `Int32` |
| ✅ | `ColSpanXxl` | `Int32` |
| ✅ | `CssClass` | `String` |
| ✅ | `Decoration` | `FormLayoutGroupDecoration` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `ExpandButtonDisplayMode` | `GroupExpandButtonDisplayMode` |
| ✅ | `ExpandButtonIconCssClass` | `String` |
| ✅ | `Expanded` | `Boolean` |
| ✅ | `ExpandedChanged` | `EventCallback<Boolean>` |
| ✅ | `HeaderContentTemplate` | `RenderFragment<IFormLayoutGroupInfo>` |
| ✅ | `HeaderCssClass` | `String` |
| ✅ | `HeaderIconCssClass` | `String` |
| ✅ | `HeaderIconUrl` | `String` |
| ✅ | `HeaderTemplate` | `RenderFragment<IFormLayoutGroupInfo>` |
| ✅ | `Id` | `String` |
| ✅ | `Items` | `RenderFragment` |
| ✅ | `ReadOnly` | `Boolean` |
| ✅ | `Visible` | `Boolean` |

## DxGridLayout

Tổng: **9** thuộc tính (9 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `Columns` | `RenderFragment` |
| ✅ | `ColumnSpacing` | `String` |
| ✅ | `CssClass` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `ItemContainerCssClass` | `String` |
| ✅ | `Items` | `RenderFragment` |
| ✅ | `Rows` | `RenderFragment` |
| ✅ | `RowSpacing` | `String` |

## DxTabs

Tổng: **14** thuộc tính (14 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `ActiveTabIndex` | `Int32` |
| ✅ | `ActiveTabIndexChanged` | `EventCallback<Int32>` |
| ✅ | `AllowTabReorder` | `Boolean` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `CssClass` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `RenderMode` | `TabsRenderMode` |
| ✅ | `ScrollMode` | `TabsScrollMode` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `TabClick` | `EventCallback<TabClickEventArgs>` |
| ✅ | `TabClosing` | `EventCallback<TabCloseEventArgs>` |
| ✅ | `TabReordering` | `EventCallback<TabReorderEventArgs>` |
| ✅ | `TabsPosition` | `TabsPosition` |

## DxTabPage

Tổng: **19** thuộc tính (19 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowClose` | `Boolean` |
| ✅ | `Attributes` | `IEnumerable<KeyValuePair<String, Object>>` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `Click` | `EventCallback<TabClickEventArgs>` |
| ✅ | `ContentAttributes` | `IEnumerable<KeyValuePair<String, Object>>` |
| ✅ | `CssClass` | `String` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `Id` | `String` |
| ✅ | `SyncRoot` | `Object` |
| ✅ | `TabIconCssClass` | `String` |
| ✅ | `TabIconUrl` | `String` |
| ✅ | `TabTemplate` | `RenderFragment` |
| ✅ | `Text` | `String` |
| ✅ | `TextTemplate` | `RenderFragment` |
| ✅ | `Tooltip` | `String` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `VisibleIndex` | `Int32` |
| ✅ | `VisibleIndexChanged` | `EventCallback<Int32>` |

# Nút & Toolbar

## DxButton

Tổng: **15** thuộc tính (15 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `ChildContent` | `RenderFragment<RenderFragment>` |
| ✅ | `Click` | `EventCallback<MouseEventArgs>` |
| ✅ | `CssClass` | `String` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `IconCssClass` | `String` |
| ✅ | `IconPosition` | `ButtonIconPosition` |
| ✅ | `Id` | `String` |
| ✅ | `NavigateUrl` | `String` |
| ✅ | `RenderStyle` | `ButtonRenderStyle` |
| ✅ | `RenderStyleMode` | `ButtonRenderStyleMode` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `SubmitFormOnClick` | `Boolean` |
| ✅ | `Text` | `String` |
| ✅ | `Visible` | `Boolean` |

## DxToolbar

Tổng: **20** thuộc tính (20 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AdaptivityAutoCollapseItemsToIcons` | `Boolean` |
| ✅ | `AdaptivityAutoHideRootItems` | `Boolean` |
| ✅ | `AdaptivityMinRootItemCount` | `Int32` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `CssClass` | `String` |
| ✅ | `Data` | `IEnumerable` |
| ✅ | `DataMappings` | `RenderFragment` |
| ✅ | `DropDownCssClass` | `String` |
| ✅ | `DropDownDisplayMode` | `DropDownDisplayMode` |
| ✅ | `DropDownMaxHeight` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `ItemClick` | `EventCallback<ToolbarItemClickEventArgs>` |
| ✅ | `ItemRenderStyleMode` | `ToolbarRenderStyleMode` |
| ✅ | `Items` | `RenderFragment` |
| ✅ | `ItemSizeMode` | `SizeMode?` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `Target` | `String` |
| ✅ | `Title` | `String` |
| ✅ | `TitleTemplate` | `RenderFragment<Object>` |

## DxToolbarItem

Tổng: **33** thuộc tính (33 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AdaptivePriority` | `Int32` |
| ✅ | `AdaptiveText` | `String` |
| ✅ | `Alignment` | `ToolbarItemAlignment` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `BeginGroup` | `Boolean` |
| ✅ | `Checked` | `Boolean` |
| ✅ | `CheckedChanged` | `EventCallback<Boolean>` |
| ✅ | `ChildContent` | `RenderFragment<IToolbarItemInfo>` |
| ✅ | `Click` | `EventCallback<ToolbarItemClickEventArgs>` |
| ✅ | `CloseMenuOnClick` | `Boolean?` |
| ✅ | `CssClass` | `String` |
| ✅ | `DropDownCaption` | `String` |
| ✅ | `DropDownCssClass` | `String` |
| ✅ | `DropDownDisplayMode` | `DropDownDisplayMode` |
| ✅ | `DropDownVisible` | `Boolean` |
| ✅ | `DropDownVisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `Enabled` | `Boolean` |
| ✅ | `GroupName` | `String` |
| ✅ | `IconCssClass` | `String` |
| ✅ | `IconUrl` | `String` |
| ✅ | `Items` | `RenderFragment` |
| ✅ | `Name` | `String` |
| ✅ | `NavigateUrl` | `String` |
| ✅ | `RenderStyle` | `ButtonRenderStyle` |
| ✅ | `RenderStyleMode` | `ToolbarItemRenderStyleMode` |
| ✅ | `SplitDropDownButton` | `Boolean` |
| ✅ | `SubmitFormOnClick` | `Boolean` |
| ✅ | `SyncRoot` | `Object` |
| ✅ | `Target` | `String` |
| ✅ | `Template` | `RenderFragment<IToolbarItemInfo>` |
| ✅ | `Text` | `String` |
| ✅ | `Tooltip` | `String` |
| ✅ | `Visible` | `Boolean` |

# Overlay

## DxPopup

Tổng: **55** thuộc tính (54 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AllowDrag` | `Boolean` |
| ✅ | `AllowDragByHeaderOnly` | `Boolean` |
| ✅ | `AllowResize` | `Boolean` |
| ✅ | `ApplyBackgroundShading` | `Boolean` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `BodyContentTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `BodyCssClass` | `String` |
| ✅ | `BodyTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `BodyText` | `String` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `CloseButtonClick` | `EventCallback` |
| ✅ | `Closed` | `EventCallback<PopupClosedEventArgs>` |
| ✅ | `CloseOnEscape` | `Boolean` |
| ✅ | `CloseOnOutsideClick` | `Boolean` |
| ✅ | `Closing` | `EventCallback<PopupClosingEventArgs>` |
| ✅ | `Content` | `RenderFragment` |
| ✅ | `ContentLoadMode` | `PopupContentLoadMode` |
| ✅ | `Created` | `EventCallback<PopupCreatedEventArgs>` |
| ✅ | `CssClass` | `String` |
| ✅ | `Disposed` | `EventCallback<PopupDisposedEventArgs>` |
| ✅ | `DragCompleted` | `EventCallback<PopupDragCompletedEventArgs>` |
| ✅ | `DragStarted` | `EventCallback<PopupDragStartedEventArgs>` |
| ✅ | `EnableAdaptivity` | `Boolean` |
| ✅ | `FooterContentTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `FooterCssClass` | `String` |
| ✅ | `FooterTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `FooterText` | `String` |
| ✅ | `HeaderContentTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `HeaderCssClass` | `String` |
| ✅ | `HeaderTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `HeaderText` | `String` |
| ✅ | `Height` | `String` |
| ✅ | `HorizontalAlignment` | `HorizontalAlignment?` |
| ✅ | `Id` | `String` |
| ✅ | `MaxHeight` | `String` |
| ✅ | `MaxWidth` | `String` |
| ✅ | `MinHeight` | `String` |
| ✅ | `MinWidth` | `String` |
| ✅ | `PositionX` | `Int32?` |
| ✅ | `PositionY` | `Int32?` |
| ✅ | `ResizeCompleted` | `EventCallback<PopupResizeCompletedEventArgs>` |
| ✅ | `ResizeStarted` | `EventCallback<PopupResizeStartedEventArgs>` |
| ✅ | `Scrollable` | `Boolean` |
| ✅ | `ShowCloseButton` | `Boolean` |
| ✅ | `ShowFooter` | `Boolean` |
| ✅ | `ShowHeader` | `Boolean` |
| ✅ | `Showing` | `EventCallback<PopupShowingEventArgs>` |
| ✅ | `Shown` | `EventCallback<PopupShownEventArgs>` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `VerticalAlignment` | `VerticalAlignment?` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `Width` | `String` |
| ✅ | `ZIndex` | `Int32?` |
|  | `IsInitialized` | `Boolean` |

## DxFlyout

Tổng: **54** thuộc tính (52 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `AnimationType` | `FlyoutAnimationType` |
| ✅ | `Attributes` | `Dictionary<String, Object>` |
| ✅ | `BodyContentTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `BodyCssClass` | `String` |
| ✅ | `BodyTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `BodyText` | `String` |
| ✅ | `BodyTextTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `Closed` | `EventCallback<FlyoutClosedEventArgs>` |
| ✅ | `CloseMode` | `FlyoutCloseMode` |
| ✅ | `CloseOnOutsideClick` | `Boolean` |
| ✅ | `Closing` | `EventCallback<FlyoutClosingEventArgs>` |
| ✅ | `ContentLoadMode` | `PopupContentLoadMode` |
| ✅ | `Created` | `EventCallback<FlyoutCreatedEventArgs>` |
| ✅ | `CssClass` | `String` |
| ✅ | `Disposed` | `EventCallback<FlyoutDisposedEventArgs>` |
| ✅ | `Distance` | `Int32?` |
| ✅ | `FitToRestriction` | `Boolean` |
| ✅ | `FooterContentTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `FooterCssClass` | `String` |
| ✅ | `FooterTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `FooterText` | `String` |
| ✅ | `FooterTextTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `FooterVisible` | `Boolean` |
| ✅ | `HeaderContentTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `HeaderCssClass` | `String` |
| ✅ | `HeaderTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `HeaderText` | `String` |
| ✅ | `HeaderTextTemplate` | `RenderFragment<IPopupElementInfo>` |
| ✅ | `HeaderVisible` | `Boolean` |
| ✅ | `Height` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `IsOpen` | `Boolean` |
| ✅ | `IsOpenChanged` | `EventCallback<Boolean>` |
| ✅ | `MaxHeight` | `String` |
| ✅ | `MaxWidth` | `String` |
| ✅ | `MinHeight` | `String` |
| ✅ | `MinWidth` | `String` |
| ✅ | `Offset` | `Int32?` |
| ✅ | `Position` | `FlyoutPosition` |
| ✅ | `PositionRectangle` | `Rectangle` |
| ✅ | `PositionTarget` | `String` |
| ✅ | `PreventCloseOnPositionTargetClick` | `Boolean` |
| ✅ | `RestrictionMode` | `FlyoutRestrictionMode` |
| ✅ | `RestrictionRectangle` | `Rectangle` |
| ✅ | `RestrictionTarget` | `String` |
| ✅ | `Scrollable` | `Boolean` |
| ✅ | `Showing` | `EventCallback<FlyoutShowingEventArgs>` |
| ✅ | `Shown` | `EventCallback<FlyoutShownEventArgs>` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `StopOutsideClickPropagation` | `Boolean` |
| ✅ | `Width` | `String` |
|  | `InitializedTask` | `Task` |
|  | `IsInitialized` | `Boolean` |

## DxToast

Tổng: **17** thuộc tính (17 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `Click` | `EventCallback<MouseEventArgs>` |
| ✅ | `CssClass` | `String` |
| ✅ | `DisplayTime` | `TimeSpan?` |
| ✅ | `FreezeOnClick` | `Boolean?` |
| ✅ | `IconCssClass` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `MaxHeight` | `String` |
| ✅ | `ProviderName` | `String` |
| ✅ | `RenderStyle` | `ToastRenderStyle?` |
| ✅ | `ShowCloseButton` | `Boolean` |
| ✅ | `ShowIcon` | `Boolean` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `Template` | `RenderFragment` |
| ✅ | `Text` | `String` |
| ✅ | `ThemeMode` | `ToastThemeMode?` |
| ✅ | `Title` | `String` |

## DxLoadingPanel

Tổng: **19** thuộc tính (19 là `[Parameter]`).

| P | Thuộc tính | Kiểu |
|---|---|---|
| ✅ | `ApplyBackgroundShading` | `Boolean` |
| ✅ | `ChildContent` | `RenderFragment` |
| ✅ | `CloseOnClick` | `Boolean` |
| ✅ | `CssClass` | `String` |
| ✅ | `Id` | `String` |
| ✅ | `IndicatorAnimationType` | `WaitIndicatorAnimationType` |
| ✅ | `IndicatorAreaVisible` | `Boolean` |
| ✅ | `IndicatorCssClass` | `String` |
| ✅ | `IndicatorTemplate` | `RenderFragment` |
| ✅ | `IndicatorVisible` | `Boolean` |
| ✅ | `IsContentBlocked` | `Boolean` |
| ✅ | `IsContentVisible` | `Boolean` |
| ✅ | `PositionTarget` | `String` |
| ✅ | `SizeMode` | `SizeMode?` |
| ✅ | `Text` | `String` |
| ✅ | `TextAlignment` | `LoadingPanelTextAlignment` |
| ✅ | `Visible` | `Boolean` |
| ✅ | `VisibleChanged` | `EventCallback<Boolean>` |
| ✅ | `ZIndex` | `Int32?` |


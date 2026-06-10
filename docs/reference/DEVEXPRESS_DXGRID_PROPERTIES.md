# DevExpress Blazor — DxGrid: Toàn bộ thuộc tính

> Trích xuất tự động từ `DevExpress.Blazor.v25.2` **v25.2.3.0** (đúng version project đang dùng).
> Cột **P** = `[Parameter]` (dùng được trực tiếp trong markup `.razor`).

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


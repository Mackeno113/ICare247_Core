-- =============================================================================
-- File    : 090_drop_ma_rule_legacy_columns.sql
-- Database: ICare247_Config  (Config DB — master canonical + mỗi tenant 1 Config DB)
-- Purpose : Dọn lệch schema — DB sống đang có 3 cột từ bản thiết kế "chuỗi mẫu +
--           bộ đếm" (Pattern token '{SEQ}', Reset_Scope NGAY/THANG/NAM/KHONG,
--           Scope_Column) mà spec 32 đã CHỐT BỎ trước khi 089_create_sys_ma_rule.sql
--           được viết (xem §2.1/§3 spec 32: "KHÔNG có cột Reset_Scope/Scope_Column —
--           mọi phạm vi tự suy ra từ các đoạn đứng trước SEQ"; "⛔ KHÔNG CÓ BẢNG BỘ
--           ĐẾM"). Vì 089 dùng guard IF OBJECT_ID(...) IS NULL nên không tạo lại bảng
--           đã tồn tại từ bản nháp cũ ⇒ 3 cột rác còn sót lại trên DB thật, Pattern
--           lại NOT NULL không default nên MỌI INSERT từ WPF (chỉ ghi theo schema 089)
--           đều FAIL. Xác nhận qua DDL sống (2026-07-25): bảng gốc có
--           CONSTRAINT CHK_Sys_Ma_Rule_Pattern / CHK_Sys_Ma_Rule_ResetScope — phải
--           drop constraint trước khi drop cột (SQL Server không tự dọn CHECK khi
--           drop column, khác DEFAULT constraint cũng phải drop tay).
--
--           Đã kiểm chứng: không proc/view/C# nào trong repo đọc/ghi Pattern,
--           Reset_Scope, Scope_Column của Sys_Ma_Rule — an toàn để xóa.
--
-- Note    : Idempotent — chỉ ALTER khi cột/constraint còn tồn tại. Dò CHECK
--           constraint theo nội dung định nghĩa (không chỉ theo tên) để chạy được
--           cả trên môi trường đặt tên constraint khác đi.
-- Spec    : docs/spec/32_SINH_MA_TU_DONG_SPEC.md §2.1 · §3 · ADR-036.
-- =============================================================================

USE [ICare247_Config];
GO

SET XACT_ABORT ON;
GO

DECLARE @sql NVARCHAR(MAX);

-- 1) Drop mọi CHECK constraint tham chiếu Pattern / Reset_Scope (dò theo text định nghĩa,
--    không phụ thuộc tên — phòng trường hợp môi trường khác đặt tên constraint khác).
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT 'ALTER TABLE dbo.Sys_Ma_Rule DROP CONSTRAINT [' + cc.name + '];'
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID('dbo.Sys_Ma_Rule')
      AND (cc.definition LIKE '%Pattern%' OR cc.definition LIKE '%Reset_Scope%');
OPEN cur;
FETCH NEXT FROM cur INTO @sql;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC(@sql);
    FETCH NEXT FROM cur INTO @sql;
END;
CLOSE cur;
DEALLOCATE cur;
GO

-- 2) Drop DEFAULT constraint của Reset_Scope (nếu có — tên hệ thống sinh tự động).
IF COL_LENGTH('dbo.Sys_Ma_Rule', 'Reset_Scope') IS NOT NULL
BEGIN
    DECLARE @dfReset NVARCHAR(200);
    SELECT @dfReset = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Sys_Ma_Rule') AND c.name = 'Reset_Scope';
    IF @dfReset IS NOT NULL
        EXEC('ALTER TABLE dbo.Sys_Ma_Rule DROP CONSTRAINT [' + @dfReset + ']');

    ALTER TABLE dbo.Sys_Ma_Rule DROP COLUMN Reset_Scope;
END;
GO

-- 3) Drop Scope_Column (không CHECK/DEFAULT ràng buộc theo DDL đã xác nhận).
IF COL_LENGTH('dbo.Sys_Ma_Rule', 'Scope_Column') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Sys_Ma_Rule DROP COLUMN Scope_Column;
END;
GO

-- 4) Drop Pattern (CHECK đã gỡ ở bước 1).
IF COL_LENGTH('dbo.Sys_Ma_Rule', 'Pattern') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Sys_Ma_Rule DROP COLUMN Pattern;
END;
GO

PRINT N'Migration 090 completed — Sys_Ma_Rule khớp lại đúng schema 089 (bỏ Pattern/Reset_Scope/Scope_Column).';
GO

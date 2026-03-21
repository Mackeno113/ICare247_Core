-- ============================================================
-- Migration : 003_remove_val_rule_field.sql
-- Mục tiêu  : Bỏ bảng junction Val_Rule_Field — gộp Field_Id,
--             Severity, Order_No trực tiếp vào Val_Rule.
-- Lý do     : Error_Key theo pattern {table}.val.{column}.{type}
--             đã unique per field → quan hệ luôn là 1-N (field→rules),
--             không cần bảng N-N.
-- Thực hiện : Chạy trong transaction. Rollback nếu lỗi.
-- ============================================================

BEGIN TRANSACTION;

BEGIN TRY

    -- ──────────────────────────────────────────────────────────
    -- Bước 1: Thêm cột mới vào Val_Rule (tạm thời NULL để migrate)
    -- ──────────────────────────────────────────────────────────
    ALTER TABLE dbo.Val_Rule
        ADD Field_Id  int           NULL,
            Severity  nvarchar(20)  NOT NULL CONSTRAINT DF_Val_Rule_Severity DEFAULT 'Error',
            Order_No  int           NOT NULL CONSTRAINT DF_Val_Rule_OrderNo  DEFAULT 0;

    -- ──────────────────────────────────────────────────────────
    -- Bước 2: Migrate Field_Id + Order_No từ Val_Rule_Field
    -- ──────────────────────────────────────────────────────────
    UPDATE vr
    SET    vr.Field_Id = vrf.Field_Id,
           vr.Order_No = vrf.Order_No
    FROM   dbo.Val_Rule vr
    JOIN   dbo.Val_Rule_Field vrf ON vrf.Rule_Id = vr.Rule_Id;

    -- ──────────────────────────────────────────────────────────
    -- Bước 3: Xóa rule orphan (không liên kết field nào)
    -- ──────────────────────────────────────────────────────────
    DELETE FROM dbo.Val_Rule WHERE Field_Id IS NULL;

    -- ──────────────────────────────────────────────────────────
    -- Bước 4: Set NOT NULL sau khi đã migrate
    -- ──────────────────────────────────────────────────────────
    ALTER TABLE dbo.Val_Rule
        ALTER COLUMN Field_Id int NOT NULL;

    -- ──────────────────────────────────────────────────────────
    -- Bước 5: FK + Index + Unique constraint
    -- ──────────────────────────────────────────────────────────
    ALTER TABLE dbo.Val_Rule
        ADD CONSTRAINT FK_Val_Rule_Ui_Field
            FOREIGN KEY (Field_Id) REFERENCES dbo.Ui_Field(Field_Id);

    -- Error_Key unique toàn bảng (pattern đảm bảo không trùng)
    CREATE UNIQUE INDEX UX_Val_Rule_ErrorKey
        ON dbo.Val_Rule (Error_Key);

    -- Index lookup theo Field_Id
    CREATE INDEX IX_Val_Rule_Field_Id
        ON dbo.Val_Rule (Field_Id, Order_No);

    -- ──────────────────────────────────────────────────────────
    -- Bước 6: Drop bảng junction Val_Rule_Field
    -- ──────────────────────────────────────────────────────────
    DROP TABLE dbo.Val_Rule_Field;

    COMMIT TRANSACTION;
    PRINT 'Migration 003 hoàn thành.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Migration 003 thất bại — đã rollback.';
    THROW;
END CATCH;

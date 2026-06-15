-- =============================================================================
-- File    : 036_create_catalog.sql
-- Database: ICare247_Master  (Catalog DB — chạy 1 lần, dùng chung mọi tenant)
-- Purpose : Tạo bảng Tenant trong CATALOG DB master (vd ICare247_Master) — danh bạ
--           ánh xạ tenant → cặp connection string (Config DB + Data DB), nhận diện
--           qua Subdomain. Connection string LƯU DẠNG MÃ HÓA (giải mã bằng key ở
--           appsettings.local.json của API). Xem ADR-018.
-- Note    : Chạy trên DB master (KHÔNG phải DB tenant). Idempotent.
--           Tạo DB master trước: CREATE DATABASE ICare247_Master; (ops làm thủ công).
-- =============================================================================

-- USE [ICare247_Master];   -- bỏ comment khi chạy trên DB master thật
GO

IF OBJECT_ID('dbo.Tenant', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenant
    (
        Tenant_Id              INT             IDENTITY(1,1) NOT NULL,
        Tenant_Code            NVARCHAR(100)   NOT NULL,
        Subdomain              NVARCHAR(100)   NOT NULL,   -- congtyA → congtyA.icare247.vn
        Display_Name           NVARCHAR(255)   NULL,
        Config_Conn_Encrypted  NVARCHAR(MAX)   NOT NULL,   -- AES-GCM base64 (nonce+tag+cipher)
        Data_Conn_Encrypted    NVARCHAR(MAX)   NOT NULL,
        Plan_Tier              NVARCHAR(50)     NULL,
        Region                 NVARCHAR(50)     NULL,
        Is_Active              BIT             NOT NULL DEFAULT 1,
        Created_At             DATETIME        NOT NULL DEFAULT GETDATE(),
        Updated_At             DATETIME        NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_Tenant PRIMARY KEY (Tenant_Id)
    );

    CREATE UNIQUE INDEX UQ_Tenant_Code      ON dbo.Tenant (Tenant_Code);
    CREATE UNIQUE INDEX UQ_Tenant_Subdomain ON dbo.Tenant (Subdomain);
    CREATE INDEX        IX_Tenant_Active    ON dbo.Tenant (Is_Active);
END;
GO

PRINT N'Migration 036 completed — Catalog dbo.Tenant created.';
GO

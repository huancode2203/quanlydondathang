-- NEW DATABASE ONLY. Run from the repository root with sqlcmd -b -i Database/Initialize.sql.
-- Existing databases: run migration 007 preflight, back up, then apply separately.
:On Error exit
:r Database/QuanLyDonDatHangDB.sql
:r Database/002_AddProductStock.sql
:r Database/003_EnsureAdminFullPermissions.sql
:r Database/004_AddAccountPermissions.sql
:r Database/005_AddOrderStockWorkflow.sql
:r Database/006_AddMultipleAccountRoles.sql
:setvar ApplyChanges 1
:r Database/007_NormalizeDomains.sql
:r Database/008_OrderDiscountsAndTaxes.sql

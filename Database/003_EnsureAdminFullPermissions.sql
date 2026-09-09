USE QuanLyDonDatHangDB;
GO

-- Nhóm ADMIN luôn được cấp toàn bộ quyền đang có trong hệ thống.
-- Script có thể chạy lại nhiều lần mà không tạo dữ liệu trùng.
INSERT INTO tbl_CapQuyen (NhomQuyenID, QuyenID)
SELECT nq.NhomQuyenID, q.QuyenID
FROM tbl_NhomQuyen nq
CROSS JOIN tbl_Quyen q
WHERE nq.MaNhomQuyen = 'ADMIN'
  AND NOT EXISTS (
      SELECT 1
      FROM tbl_CapQuyen cq
      WHERE cq.NhomQuyenID = nq.NhomQuyenID
        AND cq.QuyenID = q.QuyenID
  );
GO

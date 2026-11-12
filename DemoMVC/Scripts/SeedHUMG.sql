PRAGMA foreign_keys = ON;
BEGIN IMMEDIATE;

INSERT INTO "Khoa" ("Name") VALUES
    ('Khoa Công nghệ thông tin'),
    ('Khoa Cơ - Điện'),
    ('Khoa Dầu khí và Năng lượng'),
    ('Khoa Giáo dục Quốc phòng'),
    ('Khoa Khoa học cơ bản'),
    ('Khoa Khoa học và Kỹ thuật Địa chất'),
    ('Khoa Kinh tế và Quản trị kinh doanh'),
    ('Khoa Lý luận chính trị'),
    ('Khoa Mỏ'),
    ('Khoa Môi trường'),
    ('Khoa Trắc địa - Bản đồ và Quản lý đất đai'),
    ('Khoa Xây dựng')
ON CONFLICT ("Name") DO NOTHING;

INSERT INTO "TrainingSystems" ("Name", "StudentCode") VALUES
    ('Đại học chính quy', NULL),
    ('Kỹ sư tài năng', NULL),
    ('Chương trình tiên tiến', NULL),
    ('Sau đại học', NULL),
    ('Văn bằng 2', NULL),
    ('Liên thông, vừa làm vừa học', NULL)
ON CONFLICT ("Name") DO NOTHING;

INSERT INTO "Majors" ("Name", "AdmissionCode", "StudentCode") VALUES
    ('Công nghệ thông tin', '7480201', NULL),
    ('Khoa học dữ liệu', '7460108', NULL),
    ('Quản trị kinh doanh', '7340101', NULL),
    ('Kế toán', '7340301', NULL),
    ('Kỹ thuật cơ khí', '7520103', NULL),
    ('Kỹ thuật mỏ', '7520601', NULL)
ON CONFLICT DO NOTHING;

COMMIT;

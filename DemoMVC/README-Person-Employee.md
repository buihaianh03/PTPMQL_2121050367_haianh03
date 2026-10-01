# Bài tập Person và Employee

Dự án cần chạy: `DemoMVC/DemoMVC.csproj` (không phải project ở thư mục gốc).
Yêu cầu .NET 10 SDK.

## Thay đổi

- Person bổ sung `Email` (không bắt buộc để tương thích dữ liệu cũ; kiểm tra định dạng khi nhập).
- Employee kế thừa Person, thêm `EmployeeId` (string, bắt buộc, tối đa 50 ký tự) và `Age` (int, từ 1 đến 120).
- `Id` kế thừa từ Person vẫn là khóa chính; `EmployeeId` là mã nhân viên.
- EF Core dùng table-per-hierarchy: Person và Employee cùng lưu trong bảng `Persons`, phân biệt bằng cột `Discriminator`.
- Hai Controller cung cấp Index, Details, Create, Edit, Delete; form có kiểm tra dữ liệu và chống CSRF.
- Trang Person chỉ quản lý bản ghi Person; trang Employee quản lý Employee, bao gồm các thuộc tính kế thừa.

## Cập nhật database và chạy

Mở terminal tại thư mục `DemoMVC` có `App.db`:

```sh
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet restore
dotnet ef database update
dotnet run
```

Nếu đã cài `dotnet-ef`, bỏ qua lệnh install. Migration đã được tạo sẵn trong mã nguồn, không cần tạo lại.
Trong Visual Studio, chọn DemoMVC làm Default project trong Package Manager Console và chạy `Update-Database`.

Mở địa chỉ chương trình in trong terminal và chọn Person hoặc Employee trên menu.
Database SQLite sử dụng đường dẫn tương đối `App.db`, vì vậy cần chạy từ đúng thư mục.

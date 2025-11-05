using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public class Program
{
    // Địa chỉ IP và cổng của server
    const string SERVER_IP = "127.0.0.1";
    const int SERVER_PORT = 2000;

    public static void Main()
    {
        Console.WriteLine("Client started. Enter an employee ID (integer) or press Enter to exit.");

        while (true)
        {
            // Nhập employee ID
            Console.Write("Enter employee ID: ");
            string input = Console.ReadLine();

            // Nếu người dùng ấn Enter (rỗng) → thoát
            if (string.IsNullOrEmpty(input))
                break;

            // Kiểm tra dữ liệu nhập có phải là số nguyên hay không
            if (!int.TryParse(input, out int empId))
            {
                Console.WriteLine("Invalid input! Please enter a valid integer.");
                continue; // nhập lại
            }

            try
            {
                // ✅ 1. Kết nối TCP tới server
                using var client = new TcpClient(SERVER_IP, SERVER_PORT);

                // ✅ 2. Lấy luồng dữ liệu từ kết nối
                var stream = client.GetStream();

                // ✅ 3. Gửi dữ liệu: 
                // - chuyển empId thành chuỗi → byte[] để gửi qua mạng
                byte[] data = Encoding.UTF8.GetBytes(empId.ToString());
                stream.Write(data, 0, data.Length); // gửi đi

                // ✅ 4. Nhận dữ liệu phản hồi (JSON)
                using var ms = new MemoryStream();
                byte[] buffer = new byte[1024];
                int bytesRead;

                // Đọc dữ liệu đến khi server đóng stream hoặc hết dữ liệu
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Chỉ ghi phần có dữ liệu thực sự
                    ms.Write(buffer, 0, bytesRead);

                    // Nếu không còn dữ liệu trong stream thì dừng
                    if (!stream.DataAvailable) break;
                }

                // ✅ 5. Chuyển byte nhận được → chuỗi JSON
                string json = Encoding.UTF8.GetString(ms.ToArray());

                // ✅ 6. Deserialize JSON thành danh sách EmployeeProject
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // không phân biệt hoa thường
                };

                List<EmployeeProject>? projects =
                    JsonSerializer.Deserialize<List<EmployeeProject>>(json, options);

                // ✅ 7. Hiển thị kết quả
                if (projects == null || projects.Count == 0)
                {
                    Console.WriteLine($"No project found for employee ID {empId}");
                }
                else
                {
                    Console.WriteLine($"Project for employee ID {empId}");
                    Console.WriteLine();
                    foreach (var p in projects)
                    {

                        Console.WriteLine($"ID: {p.Id}");
                        Console.WriteLine($"Title: {p.Title}");
                        Console.WriteLine($"Description: {p.Description}");
                        Console.WriteLine($"Position: {p.Position}");
                        Console.WriteLine("---");
                    }
                }
            }
            catch (Exception)
            {
                // Nếu server chưa bật hoặc mất kết nối
                Console.WriteLine("server is not running. Please try again later");
            }
        }
    }
}


public class Employee
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class Project
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }

}

public class EmployeeProject
{
    public int EmployeeId { get; set; }

    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Position { get; set; }
}
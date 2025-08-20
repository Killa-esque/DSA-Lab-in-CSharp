using System.Diagnostics;
using System.Text;

public static class BracketChecker
{
    // Trả về danh sách lỗi; rỗng nghĩa là không thiếu ngoặc
    public record InsertEdit(int Position, char Ch);

    public static List<InsertEdit> SuggestClosers(string code, out List<string> warnings)
    {
        var edits = new List<InsertEdit>();
        warnings = new List<string>();

        var stack = new Stack<(char ch, int index)>();
        ReadOnlySpan<char> OPEN = "([{";

        // Helpler
        static char MatchingOpen(char close) => close
            switch
            {
                ')' => '(',
                ']' => '[',
                '}' => '{',
                _ => '\0'
            };

        static char MatchingClose(char close) => close switch
        {
            '(' => ')',
            '[' => ']',
            '{' => '}',
            _ => '\0'
        };

        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];

            if (OPEN.IndexOf(c) >= 0) // Nếu là ngoặc mở
            {
                stack.Push((c, i));
                continue;
            }

            char neededOpen = MatchingOpen(c);

            if (neededOpen == '\0') continue; // Không phải ngoặc đóng -> bỏ qua

            while (stack.Count > 0 && stack.Peek().ch != neededOpen)
            {
                // Chèn ngoặc đóng phù hợp với đỉnh stack ngay trước i
                var (topOpen, _) = stack.Pop();
                edits.Add(new InsertEdit(i, MatchingClose(topOpen)));
            }

            if (stack.Count == 0)
            {
                warnings.Add($"Ngoặc đóng dư '{c}' tại vị trí {i} (không thể tự sửa chỉ bằng thêm ngoặc đóng).");
            }
            else
            {
                stack.Pop();
            }
        }

        while (stack.Count > 0)
        {
            var (topOpen, _) = stack.Pop();
            edits.Add(new InsertEdit(code.Length, MatchingClose(topOpen)));
        }
        
        return edits;
    }

    public static List<string> Check(string code)
    {
        var errors = new List<string>();
        var stack = new Stack<(char ch, int idx)>(); // chỉ 1 stack
        ReadOnlySpan<char> OPEN = "([{"; // các ngoặc mở

        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];

            // Nếu là ngoặc mở -> push
            if (OPEN.IndexOf(c) >= 0)
            {
                stack.Push((c, i));
                continue;
            }

            // Nếu là ngoặc đóng -> xác định ngoặc mở tương ứng
            char expectedOpen = c switch
            {
                ')' => '(',
                ']' => '[',
                '}' => '{',
                _ => '\0'
            };
            if (expectedOpen == '\0') continue; // không phải ngoặc -> bỏ qua

            // Kiểm tra khớp
            if (stack.Count == 0 || stack.Peek().ch != expectedOpen)
            {
                errors.Add($"Thiếu ngoặc mở cho '{c}' tại vị trí {i}");
            }
            else
            {
                stack.Pop();
            }
        }

        // Còn dư ngoặc mở -> thiếu ngoặc đóng
        while (stack.Count > 0)
        {
            var (ch, idx) = stack.Pop();
            errors.Add($"Thiếu ngoặc đóng cho '{ch}' tại vị trí {idx}");
        }

        return errors;
    }

    public static void Main(String[] args)
    {
        Console.OutputEncoding = Encoding.UTF8; // Cho phép in Unicode UTF-8
        Console.InputEncoding = Encoding.UTF8; // Cho phép nhập Unicode UTF-8

        var samples = new[]
        {
            "{ ( ( ) ) }",          // đủ ngoặc
            "{ ( [ x ) y }",        // cần chèn ']' trước ')'
            "if (a > 0 { b();",     // thiếu '}' ở cuối
            "{ [ )",                // cần chèn ']' trước ')' và thiếu '}' ở cuối
            "a ) b"                 // ')' dư (không thể sửa chỉ bằng thêm ngoặc đóng)
        };

        foreach (var s in samples)
        {
            var edits = SuggestClosers(s, out var warns);
            Console.WriteLine($"\nCODE: {s}");
            if (edits.Count == 0 && warns.Count == 0)
            {
                Console.WriteLine("→ Không cần chèn ngoặc đóng (đủ ngoặc).");
                continue;
            }

            if (edits.Count > 0)
            {
                Console.WriteLine("→ Cần chèn ngoặc đóng tại:");
                foreach (var e in edits)
                    Console.WriteLine($"  - Trước vị trí {e.Position}: chèn '{e.Ch}'");
            }

            foreach (var w in warns)
                Console.WriteLine($"(Cảnh báo) {w}");
        }
    }
}
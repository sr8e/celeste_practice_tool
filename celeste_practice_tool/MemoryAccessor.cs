using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace celeste_practice_tool
{
    public class MemoryAccessor
    {
        public static List<MemoryPage> getPages(IntPtr handle)
        {
            List<MemoryPage> pages = new();
            IntPtr pagePtr = IntPtr.Zero;
            while (true)
            {
                int success = VirtualQueryEx(handle, pagePtr, out MemoryPage page, Marshal.SizeOf<MemoryPage>());
                if (success == 0) // failed
                {
                    break;
                }
                if (page.IsTarget)
                {
                    pages.Add(page);
                }
                pagePtr = page.BaseAddress + page.RegionSize;
            }
            return pages;
        }

        public static IntPtr findSignature(IntPtr handle, Signature s)
        {
            s.ToBytes(out byte[] pattern, out bool[] mask, out int[] offsets);
            List<MemoryPage> pages = getPages(handle);
            Debug.WriteLine($"{pages.Count} pages");


            for (int i = 0; i < pages.Count; i++)
            {
                MemoryPage page = pages[i];
                IntPtr index = IntPtr.Zero;

                while (index < page.RegionSize)
                {
                    int amountToRead = (int)(page.RegionSize - index);
                    byte[] buf = new byte[amountToRead];
                    bool success = ReadProcessMemory(handle, page.BaseAddress + index, buf, amountToRead, out int bytesRead);
                    if (!success)
                    {
                        Debug.WriteLine($"unsuccessful memory read at page{i} -> {page}");
                        break;
                    }
                    int current = 0;
                    int len = pattern.Length;
                    while (current <= bytesRead - len)
                    {
                        for (int j = len - 1; buf[current + j] == pattern[j] || mask[j]; j--)
                        {
                            if (j == 0)
                            {
                                return page.BaseAddress + index + current;
                            }
                        }
                        int offset = offsets[buf[current + len - 1]];
                        current += offset;
                    }

                    // not found
                    index += bytesRead;
                }

            }
            return IntPtr.Zero;
        }
        public static byte[]? ReadBytes(IntPtr handle, IntPtr addr, int len)
        {
            byte[] buf = new byte[len];
            bool success = ReadProcessMemory(handle, addr, buf, len, out int bytesRead);
            if (!success || bytesRead != len)
            {
                return null;
            }
            return buf;
        }

        public static IntPtr ResolvePtr(IntPtr handle, IntPtr addr, params int[] offsets)
        {
            if (addr == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            for (int i = 0; i < offsets.Length; i++)
            {
                byte[]? buf = ReadBytes(handle, addr, 4);
                if (buf == null)
                {
                    return IntPtr.Zero;
                }
                addr = (IntPtr)BitConverter.ToUInt32(buf, 0);
                if (addr == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }
                addr += offsets[i];
            }
            return addr;
        }

        public static int ReadInt(IntPtr handle, IntPtr addr, params int[] offsets)
        {
            addr = ResolvePtr(handle, addr, offsets);
            if (addr == IntPtr.Zero)
            {
                return int.MinValue;
            }
            byte[]? body = ReadBytes(handle, addr, 4);
            if (body == null)
            {
                return int.MinValue;
            }
            return BitConverter.ToInt32(body, 0);

        }
        public static long ReadLong(IntPtr handle, IntPtr addr, params int[] offsets)
        {
            addr = ResolvePtr(handle, addr, offsets);
            if (addr == IntPtr.Zero)
            {
                return long.MinValue;
            }
            byte[]? body = ReadBytes(handle, addr, 8);
            if (body == null)
            {
                return long.MinValue;
            }
            return BitConverter.ToInt64(body, 0);

        }
        public static string ReadUTF16String(IntPtr handle, IntPtr addr, params int[] offsets)
        {
            addr = ResolvePtr(handle, addr, offsets);
            if (addr == IntPtr.Zero)
            {
                return string.Empty;
            }
            int header = ReadInt(handle, addr);
            if (header != 0x7209abdc)
            {
                return string.Empty;
            }
            int strLen = ReadInt(handle, addr + 4);
            if (strLen < 0)
            {
                return string.Empty;
            }
            byte[]? body = ReadBytes(handle, addr + 8, strLen * 2);
            if (body == null)
            {
                return string.Empty;
            }
            return Encoding.Unicode.GetString(body);
        }
        public static bool ReadBool(IntPtr handle, IntPtr addr, params int[] offsets)
        {
            addr = ResolvePtr(handle, addr, offsets);
            if (addr == IntPtr.Zero)
            {
                return false;
            }
            byte[]? body = ReadBytes(handle, addr, 1);
            if (body == null)
            {
                return false;
            }
            return body[0] != 0;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MemoryPage lpBuffer, int dwLength);
    }

    public struct MemoryPage
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public IntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;

        public bool IsTarget => (State & 0x1000) != 0 && (Protect & 0x100) == 0; // commited, not protected

        public override string ToString()
        {
            return $"base addr->0x{BaseAddress:x8}, page size->0x{RegionSize:x8}, state -> 0x{State:x}, protect -> 0x{Protect:x}";
        }
    }
}
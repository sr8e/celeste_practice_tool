using System.Diagnostics;
using System.Reactive.Linq;

namespace celeste_practice_tool
{
    public class CelesteProcess
    {
        private bool hooked;
        private Process? proc;
        private IntPtr basePtr;
        private int refoffset = -1;

        private IDisposable hookObserver;
        private IDisposable updateObserver;
        public CelesteDataContext Context { get; private set; }

        private static Signature XNA_SIGNATURE = new("83c604f30f7e06660fd6078bcbff15????????8d15", 21);


        public CelesteProcess()
        {
            Context = new();

            tryHookProcess(0);
            hookObserver = Observable.Interval(TimeSpan.FromSeconds(5)).Subscribe(tryHookProcess);
            updateObserver = Observable.Interval(TimeSpan.FromMilliseconds(16)).Subscribe(updateDataContext);
        }

        public void tryHookProcess(long _)
        {
            hooked = proc != null && !proc.HasExited;
            if (hooked)
            {
                return;
            }
            Process[] procs = Process.GetProcessesByName("Celeste");
            if (procs.Length == 0)
            {
                Debug.WriteLine("not hooked");
                Context.StatusText = "Cannot found Celeste process. Retrying...";
                refoffset = -1;
                return;
            }
            proc = procs[0];
            hooked = true;
            Debug.WriteLine("hooked");
            IntPtr signaturePtr = MemoryAccessor.findSignature(proc.Handle, XNA_SIGNATURE);
            if (signaturePtr == IntPtr.Zero)
            {
                Debug.WriteLine("signature not found...");
                Context.StatusText = "Cannot find memory region.";
            }
            else
            {
                Debug.WriteLine($"signature found -> 0x{signaturePtr:x8}");
                basePtr = MemoryAccessor.ResolvePtr(proc.Handle, signaturePtr + XNA_SIGNATURE.offsetToPtr, 0);
                Debug.WriteLine($"base ptr -> 0x{basePtr:x8}");
            }
            fieldOffset();
        }

        public void fieldOffset()
        {
            if (!hooked || refoffset != -1)
            {
                return;
            }

            int size = MemoryAccessor.ReadInt(proc.Handle, basePtr, 0, 4);
            Debug.WriteLine($"size -> {size}");

            for (int offs = size - 4; offs >= 0; offs -= 4)
            {
                if (MemoryAccessor.ReadUTF16String(proc.Handle, basePtr, offs, 0) == "Celeste")
                {
                    Debug.WriteLine(string.Format("Baseptr: {0}, size: {1}, ofs:{2}", basePtr, size, offs));
                    Context.StatusText = "Successfully found memory region.";
                    refoffset = offs;
                    return;
                }
            }
            Debug.WriteLine("title string not found");
            Context.StatusText = "Cannot find memory region.";
        }

        public void updateDataContext(long _)
        {
            if (!hooked || refoffset == -1)
            {
                return;
            }
            Context.update(getChapterId(), getChapterSide(), getRoomIdentifier(), getChapterDeaths(), getRoomDeaths(), getChapterTimeMillis(), getChapterCompleted());
        }

        public int getChapterDeaths()
        {
            return MemoryAccessor.ReadInt(proc.Handle, basePtr, refoffset + 8, 0x2c, 0x40);
        }
        public int getRoomDeaths()
        {
            return MemoryAccessor.ReadInt(proc.Handle, basePtr, refoffset + 8, 0x2c, 0x4c);
        }
        public RoomIdentifier getRoomIdentifier()
        {
            string room1 = MemoryAccessor.ReadUTF16String(proc.Handle, basePtr, refoffset + 8, 0x2c, 0x30, 0);
            string room2 = MemoryAccessor.ReadUTF16String(proc.Handle, basePtr, refoffset + 8, 0x2c, 0x34, 0);
            return new(room1, room2);
        }
        public int getChapterId()
        {
            return MemoryAccessor.ReadInt(proc.Handle, basePtr, refoffset + 0x1c, 0x18);
        }
        public int getChapterSide()
        {
            return MemoryAccessor.ReadInt(proc.Handle, basePtr, refoffset + 0x1c, 0x1c);
        }
        public long getChapterTimeMillis()
        {
            return MemoryAccessor.ReadLong(proc.Handle, basePtr, refoffset + 0x1c, 4) / 10000;
        }
        public bool getChapterCompleted()
        {
            return MemoryAccessor.ReadBool(proc.Handle, basePtr, refoffset + 0x1c, 0x32);
        }
    }


}

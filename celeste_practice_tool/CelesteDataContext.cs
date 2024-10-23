using System.ComponentModel;
using System.Diagnostics;

namespace celeste_practice_tool
{
    public class CelesteDataContext : INotifyPropertyChanged
    {
        private int chapterId = -2;
        private RoomIdentifier? roomId;
        private int cdeath = -1;
        private int rdeath = -1;
        private long chapterTime;
        private bool compFlag;
        private Dictionary<RoomIdentifier, DeathStat> deathStatDict = new();

        // properties
        public string ChapterName
        {
            get
            {
                string? val = Enum.GetName(typeof(Chapters), chapterId);
                if (val == null)
                {
                    return "";
                }
                return val;
            }
        }
        public string ChapterTime
        {
            get
            {
                TimeSpan t = TimeSpan.FromMilliseconds(chapterTime);
                if (t.Hours > 0)
                {
                    return $"{t.Hours}:{t.Minutes:d2}:{t.Seconds:d2}.{t.Milliseconds:d3}";
                }
                else
                {
                    return $"{t.Minutes}:{t.Seconds:d2}.{t.Milliseconds:d3}";
                }
            }
        }
        public string RoomName
        {
            get
            {
                if (roomId == null)
                {
                    return string.Empty;
                }
                return roomId.ToString();
            }
        }
        public int ChapterDeathCount => cdeath;
        public int RoomDeathCount => rdeath;
        public DeathStat[] DeathStats => deathStatDict.Values.ToArray();

        // implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void invokePropertyChange(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void commitCurrentStat(bool passed)
        {
            if (roomId == null)
            {
                return;
            }
            DeathStat ds = deathStatDict[roomId];
            ds.Passed = passed;
            ds.CurrentDeath += rdeath;

            deathStatDict.Values.ToList().ForEach(v => v.commit());
            invokePropertyChange("DeathStats");
        }
        public void update(int cid, RoomIdentifier room, int cd, int rd, long ctime, bool comp)
        {
            if (comp && !compFlag)
            {
                commitCurrentStat(true);
                compFlag = true;
                return;
            }
            if (cid != chapterId)
            {
                chapterId = cid;
                invokePropertyChange("ChapterName");
            }
            if (ctime < chapterTime) // reset
            {
                // chapter death will be reset, but room death won't be reset
                cdeath = -1;

                if (cid == (int)Chapters.Menu) // chapter restart
                {
                    Debug.WriteLine($"reset condition t={ctime} cd={cd} rd={rd}");
                    commitCurrentStat(false);
                    roomId = null;
                }
            }
            chapterTime = ctime;
            invokePropertyChange("ChapterTime");

            if (!room.Equals(roomId) && !room.isEmpty() && chapterTime > 0) // room transition
            {
                Debug.WriteLine($"room update {roomId} -> {room} t={ctime} cd={cd} rd={rd}");
                if (!deathStatDict.ContainsKey(room))
                {
                    deathStatDict[room] = new() { RoomId = room };
                    invokePropertyChange("DeathStats");
                }
                if (roomId == null) // init
                {
                    compFlag = false;
                    rdeath = -1;
                    cdeath = -1;
                }
                else
                {
                    // save
                    DeathStat ds = deathStatDict[roomId];
                    ds.Passed = true;
                    ds.CurrentDeath += rdeath;
                    rdeath = -1;
                    invokePropertyChange("DeathStats");
                }
                roomId = room;
                invokePropertyChange("RoomName");
            }
            if (cd - cdeath == 1 || cdeath == -1)
            {
                cdeath = cd;
                invokePropertyChange("ChapterDeathCount");
            }

            if (rd - rdeath == 1 || rdeath == -1)
            {
                rdeath = rd;
                invokePropertyChange("RoomDeathCount");
            }
        }
    }
    public class DeathStat
    {
        public required RoomIdentifier RoomId { get; set; }
        public bool Passed { get; set; }
        public int CurrentDeath { get; set; }
        public string CurrentDeathStr => Passed ? $"{CurrentDeath}" : "-";
        public int PrevDeath { get; set; }
        public int TotalDeath => CurrentDeath + PrevDeath;
        public int PrevSuccess { get; set; }
        public int TotalSuccess => PrevSuccess + (Passed ? 1 : 0);

        public void commit()
        {
            PrevDeath += CurrentDeath;
            CurrentDeath = 0;
            if (Passed)
            {
                PrevSuccess++;
            }
            Passed = false;
        }
    }
    public class RoomIdentifier : Tuple<string, string>
    {
        public RoomIdentifier(string item1, string item2) : base(item1, item2)
        {
        }

        public override string ToString()
        {
            if (isEmpty())
            {
                return string.Empty;
            }
            if (Item1 == Item2)
            {
                return Item2;
            }
            return $"{Item2} (via {Item1})";
        }

        public bool isEmpty()
        {
            return string.IsNullOrEmpty(Item1) || string.IsNullOrEmpty(Item2);
        }
    }
    public enum Chapters
    {
        // todo add description
        Menu = -1,
        Prologue = 0,
        ForsakenCity = 1,
        OldSite = 2,
        CelestialResort = 3,
        GoldenRidge = 4,
        MirrorTemple = 5,
        Reflection = 6,
        TheSummit = 7,
        Epilogue = 8,
        Core = 9,
        Farewell = 10
    }
}


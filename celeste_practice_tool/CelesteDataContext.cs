using System.ComponentModel;
using System.Diagnostics;

namespace celeste_practice_tool
{
    public class CelesteDataContext : INotifyPropertyChanged
    {
        private int chapterId = -2;
        private int chapterSide = -1;
        private RoomIdentifier? roomId;
        private int cdeath = -1;
        private int rdeath = -1;
        private long chapterTime;
        private bool compFlag;
        private Dictionary<RoomIdentifier, DeathStat> deathStatDict = new();
        private Dictionary<Tuple<Chapters, Sides>, Dictionary<RoomIdentifier, DeathStat>> statStore = new();

        // properties
        public Chapters ChapterName
        {
            get { return (Chapters)chapterId; }
            set
            {
                chapterId = (int)value;
                invokePropertyChange("ChapterName");
            }
        }
        public Sides ChapterSide
        {
            get { return (Sides)chapterSide; }
            set
            {
                chapterSide = (int)value;
                invokePropertyChange("ChapterSide");
            }
        }
        public long ChapterTime
        {
            get { return chapterTime; }
            set
            {
                chapterTime = value;
                invokePropertyChange("ChapterTime");
            }
        }
        public RoomIdentifier? RoomName
        {
            get { return roomId; }
            set
            {
                roomId = value;
                invokePropertyChange("RoomName");
                invokePropertyChange("IndexOfStat");
            }
        }
        public int ChapterDeathCount
        {
            get { return int.Max(0, cdeath); }
            set
            {
                cdeath = value;
                invokePropertyChange("ChapterDeathCount");
            }
        }
        public int RoomDeathCount
        {
            get { return int.Max(0, rdeath); }
            set
            {
                rdeath = value;
                invokePropertyChange("RoomDeathCount");
            }
        }
        public DeathStat[] DeathStats => deathStatDict.Values.ToArray();
        public int IndexOfStat => deathStatDict.Keys.Select((key, index) => (key, index))
            .Where(p => p.key.Equals(roomId)).Select(p => p.index).FirstOrDefault(-1);

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
        public void update(int cid, int side, RoomIdentifier room, int cd, int rd, long ctime, bool comp)
        {
            if (comp && !compFlag)
            {
                commitCurrentStat(true);
                compFlag = true;
                return;
            }
            if (cid != chapterId)
            {
                Chapters c = (Chapters)cid;
                if (Enum.IsDefined(c) && c != Chapters.Menu)
                {
                    Tuple<Chapters, Sides> key = Tuple.Create(c, (Sides)side);
                    if (!statStore.ContainsKey(key))
                    {
                        statStore.Add(key, new());
                    }
                    deathStatDict = statStore[key];
                    invokePropertyChange("DeathStats");
                }
                ChapterName = c;
            }
            if (side != chapterSide)
            {
                ChapterSide = (Sides)side;
            }
            if (ctime < chapterTime) // reset
            {
                // chapter death will be reset, but room death won't be reset
                cdeath = -1;

                if (cid == (int)Chapters.Menu) // chapter restart
                {
                    Debug.WriteLine($"reset condition t={ctime} cd={cd} rd={rd}");
                    commitCurrentStat(false);
                    RoomName = null;
                }
            }
            ChapterTime = ctime;

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
                RoomName = room;
            }
            if (cd - cdeath == 1 || cdeath == -1)
            {
                ChapterDeathCount = cd;
            }

            if (rd - rdeath == 1 || rdeath == -1)
            {
                RoomDeathCount = rd;
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
        public float SuccessRate => (TotalDeath + TotalSuccess) > 0 ? (float)TotalSuccess / (TotalDeath + TotalSuccess) : 0f;

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
        Menu = -1,
        Prologue = 0,
        [Description("Forsaken City")]
        ForsakenCity = 1,
        [Description("Old Site")]
        OldSite = 2,
        [Description("Celestial Resort")]
        CelestialResort = 3,
        [Description("Golden Ridge")]
        GoldenRidge = 4,
        [Description("Mirror Temple")]
        MirrorTemple = 5,
        Reflection = 6,
        [Description("The Summit")]
        TheSummit = 7,
        Epilogue = 8,
        Core = 9,
        Farewell = 10
    }

    public enum Sides
    {
        [Description("-")]
        None = -1,
        [Description("A Side")]
        ASide = 0,
        [Description("B Side")]
        BSide = 1,
        [Description("C Side")]
        CSide = 2
    }

}


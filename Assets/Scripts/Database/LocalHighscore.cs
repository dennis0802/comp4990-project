namespace Database{
    public class LocalHighscore : DatabaseEntity
    {
        public string LeaderName {get; set;}
        public int Difficulty {get; set;}
        public int Distance {get; set;}
        public int FriendsAlive {get; set;}
        public int FinalScore {get; set;}
    }
}
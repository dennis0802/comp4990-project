namespace Database{
    public class ActiveCharacter : DatabaseEntity
    {
        public int FileId {get; set;}
        public string CharacterName {get; set;}
        public int IsLeader {get; set;}
        public int Perk {get; set;}
        public int Trait {get; set;}
        public int Acessory {get; set;}
        public int Outfit {get; set;}
        public int Color {get; set;}
        public int Hat {get; set;}
        public int Health {get; set;}
        public int Morale {get; set;}
        public int CustomCharacterId {get; set;}
    }
}
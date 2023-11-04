using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using SQLite.Attribute;

namespace Database{
        [UnityEngine.Scripting.Preserve]
        public class CustomCharacter
        {
                [PrimaryKey] public int Id {get; set;}
                public string CharacterName {get; set;}
                public int Perk {get; set;}
                public int Trait {get; set;}
                public int Acessory {get; set;}
                public int Outfit {get; set;}
                public int Color {get; set;}
                public int Hat {get; set;}
        }
}


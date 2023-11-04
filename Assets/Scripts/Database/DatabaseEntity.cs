using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using SQLite.Attribute;

namespace Database{
    [UnityEngine.Scripting.Preserve]
    public class DatabaseEntity
    {
        [PrimaryKey][AutoIncrement] public int Id {get; set;}
    }
}
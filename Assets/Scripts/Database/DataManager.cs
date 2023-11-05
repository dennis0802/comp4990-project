using Database;
using System;
using SqlCipher4Unity3D;
using SQLite.Attribute;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace Database{
    public class DataManager
    {
        private readonly SQLiteConnection _connection;
        // Start is called before the first frame update
        public DataManager()
        {
            string dbUri = "AppData.sqlite";
            _connection = new SQLiteConnection(dbUri, "b'$2b$12$YWNdD4iKGkC5IqvDAUOwJet7ebD5PwZXSQn1zftXWWfiw4/raC.He'");
        }

        /// <summary>
        /// Create all tables
        /// </summary>
        public void StartUp(){
            _connection.CreateTable<ActiveCharacter>();
            _connection.CreateTable<Car>();
            _connection.CreateTable<CustomCharacter>();
            _connection.CreateTable<LocalHighscore>();
            _connection.CreateTable<PerishedCustomCharacter>();
            _connection.CreateTable<Save>();
            _connection.CreateTable<TownEntity>();
        }

        /// <summary>
        /// Get saves in the database
        /// </summary>
        public IEnumerable<Save> GetSaves(){
            return _connection.Table<Save>();
        }

        /// <summary>
        /// Get active characters in the database
        /// </summary>
        public IEnumerable<ActiveCharacter> GetActiveCharacters(){
            return _connection.Table<ActiveCharacter>();
        }

        /// <summary>
        /// Get custom characters in the database
        /// </summary>
        public IEnumerable<CustomCharacter> GetCustomCharacters(){
            return _connection.Table<CustomCharacter>();
        }

        /// <summary>
        /// Get scores in the database
        /// </summary>
        public IEnumerable<LocalHighscore> GetScores(){
            return _connection.Table<LocalHighscore>();
        }

        /// <summary>
        /// Get the null saves in the database (no leader)
        /// </summary>
        public IEnumerable<ActiveCharacter> GetNullSaves(){
            IEnumerable<ActiveCharacter> characters = _connection.Table<ActiveCharacter>(); 
            characters = characters.Where<ActiveCharacter>(c=>c.IsLeader == 1 && c.CharacterName == null);
            return characters;
        }

        /// <summary>
        /// Get perished custom characters
        /// </summary>
        public IEnumerable<PerishedCustomCharacter> GetPerishedCustomCharacters(){
            return _connection.Table<PerishedCustomCharacter>();
        }

        /// <summary>
        /// Delete save file and related data
        /// </summary>
        /// <param name="id">The id of the file to delete</param>
        public void DeleteSave(int id){
            IEnumerable<ActiveCharacter> characters = _connection.Table<ActiveCharacter>();
            IEnumerable<PerishedCustomCharacter> perished = _connection.Table<PerishedCustomCharacter>();
            characters = characters.Where<ActiveCharacter>(c=>c.FileId == id);
            perished = perished.Where<PerishedCustomCharacter>(p=>p.FileId == id);

            foreach(ActiveCharacter ac in characters){
                _connection.Delete<ActiveCharacter>(ac.Id);
            }
            foreach(PerishedCustomCharacter pc in perished){
                _connection.Delete<PerishedCustomCharacter>(pc.Id);
            }
            _connection.Delete<Save>(id);
            _connection.Delete<Car>(id);
            _connection.Delete<TownEntity>(id);
        }

        /// <summary>
        /// Delete active character
        /// </summary>
        /// <param name="id">The id of the character</param>
        public void DeleteActiveCharacter(int id){
            _connection.Delete<ActiveCharacter>(id);
        }

        /// <summary>
        /// Delete custom character
        /// </summary>
        /// <param name="id">The id of the character</param>
        public void DeleteCharacter(int id){
            _connection.Delete<CustomCharacter>(id);
        }

        /// <summary>
        /// Clear the scoreboard
        /// </summary>
        public void DeleteScores(){
            _connection.DeleteAll<LocalHighscore>();
        }

        /// <summary>
        /// Get a save by its id
        /// </summary>
        /// <param name="id">The file id of the save</param>
        public Save GetSaveById(int id){
            return _connection.Find<Save>(id);
        }

        /// <summary>
        /// Get a car by its id
        /// </summary>
        /// <param name="id">The file id of the car</param>
        public Car GetCarById(int id){
            return _connection.Find<Car>(id);
        }

        /// <summary>
        /// Get a town by its id
        /// </summary>
        /// <param name="id">The file id of the town</param>
        public TownEntity GetTownById(int id){
            return _connection.Find<TownEntity>(id);
        }

        /// <summary>
        /// Get the leader
        /// </summary>
        /// <param name="id">The file id of the leader to find</param>
        public ActiveCharacter GetLeader(int id){
            return _connection.Find<ActiveCharacter>(a=>a.FileId == id && a.IsLeader == 1);
        }

        /// <summary>
        /// Get a character
        /// </summary>
        /// <param name="fileId">The file id of the character to find</param>
        /// <param name="charId">The character id of the character to find</param>
        public ActiveCharacter GetCharacter(int fileId, int charId){
            return _connection.Find<ActiveCharacter>(a=>a.FileId == fileId && a.Id == charId);
        }

        /// <summary>
        /// Get a custom character by id
        /// </summary>
        /// <param name="id">The file id of the leader to find</param>
        public CustomCharacter GetCustomCharacterById(int id){
            return _connection.Find<CustomCharacter>(c=>c.Id == id);
        }

        /// <summary>
        /// Insert the new character
        /// </summary>
        /// <param name="character">The character to insert</param>
        public void InsertCharacter(ActiveCharacter character){
            _connection.Insert(character);
        }

        /// <summary>
        /// Insert the new perished custom character
        /// </summary>
        /// <param name="character">The character to insert</param>
        public void InsertPerishedCustomCharacter(PerishedCustomCharacter character){
            _connection.Insert(character);
        }

        /// <summary>
        /// Insert the new character
        /// </summary>
        /// <param name="character">The character to insert</param>
        public void InsertOrReplaceCharacter(CustomCharacter character){
            _connection.InsertOrReplace(character);
        }

        /// <summary>
        /// Insert the new save
        /// </summary>
        /// <param name="save">The save to insert</param>
        public void InsertSave(Save save){
            _connection.Insert(save);
        }

        /// <summary>
        /// Insert the new car
        /// </summary>
        /// <param name="car">The car to insert</param>
        public void InsertCar(Car car){
            _connection.Insert(car);
        }

        /// <summary>
        /// Insert the new town
        /// </summary>
        /// <param name="town">The town to insert</param>
        public void InsertTown(TownEntity town){
            _connection.Insert(town);
        }

        /// <summary>
        /// Insert the new score
        /// </summary>
        /// <param name="score">The score to insert</param>
        public void InsertScore(LocalHighscore score){
            _connection.Insert(score);
        }

        /// <summary>
        /// Update the save
        /// </summary>
        /// <param name="save">The save to update</param>
        public void UpdateSave(Save save){
            _connection.Update(save);
        }

        /// <summary>
        /// Update a town entity
        /// </summary>
        /// <param name="townEntity">The town to update</param>
        public void UpdateTown(TownEntity townEntity){
            _connection.Update(townEntity);
        }

        /// <summary>
        /// Update a car
        /// </summary>
        /// <param name="car">The car to update</param>
        public void UpdateCar(Car car){
            _connection.Update(car);
        }

        /// <summary>
        /// Update the active characters
        /// </summary>
        /// <param name="characters">The characters to update</param>
        public void UpdateCharacters(IEnumerable<ActiveCharacter> characters){
            _connection.UpdateAll(characters);
        }

        /// <summary>
        /// Update an active character
        /// </summary>
        /// <param name="character">The character to update</param>
        public void UpdateCharacter(ActiveCharacter character){
            _connection.Update(character);
        }

        public void UpdateTravel(string query, object[] parameters){
            _connection.Execute(query, parameters);
        }
    }
}
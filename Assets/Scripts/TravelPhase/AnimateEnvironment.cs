using System.Collections;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;
using TravelPhase;
using Database;
using UI;

namespace TravelPhase{
    public class AnimateEnvironment : MonoBehaviour
    {
        public GameObject markerUsed, genericMarker, cityMarker;
        public GameObject[] carTires, carAxles;
        private Vector3 movement, normalMovement;
        private Vector3 originalPos, normalPos;
        private bool statusRead, carIsBroken;
        private static bool nearingTown;
        private float rotationSpeed;

        // Start is called before the first frame update
        void Start()
        {
            SetMovementSpeed();
            originalPos = markerUsed.transform.position;
            normalPos = originalPos;
        }

        void OnEnable(){
            markerUsed = genericMarker;
        }

        // Update is called once per frame
        void Update()
        {
            // Check car status
            if(!statusRead){
                carIsBroken = TravelLoop.IsCarBroken();
                statusRead = true;
            }

            // "Animate" the car
            AnimateCar();

            // "Animate" the background
            AnimateBackground();
        }

        void SetMovementSpeed(){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT speed FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int speed = dataReader.GetInt32(0);
            speed = speed == 0 ? 40 : speed == 1 ? 50 : 60;

            dbConnection.Close();

            movement = new Vector3((float)(speed) * 0.75f, 0f, 0f);
            rotationSpeed = speed * -2f;
        }

        /// <summary>
        /// Animate elements on the road
        /// </summary>
        void AnimateBackground(){
            // Animate if car is actively driving (ie. interval from 3-8s)
            if(!carIsBroken && TravelLoop.Timer >= 3.0f && TravelLoop.Timer < 8.0f){
                markerUsed.transform.Translate(movement * Time.deltaTime);
            }
            else{
                statusRead = false;
            }

            // Reset marker when offscreen to the right
            if(markerUsed.transform.position.x > 150f){
                markerUsed.transform.position = originalPos;
            }
        }

        /// <summary>
        /// Animate elements of the car
        /// </summary>
        void AnimateCar(){
            // Animate if car is actively driving (ie. interval from 3-8s)
            if(!carIsBroken && TravelLoop.Timer >= 3.0f && TravelLoop.Timer < 8.0f){
                for(int i = 0; i < carTires.Length; i++){
                    carTires[i].transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }
}
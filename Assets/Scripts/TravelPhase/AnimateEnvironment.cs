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
        public GameObject genericMarker, cityMarker;
        public GameObject[] carTires;
        private Vector3 movement, originalPos;
        private bool statusRead, carIsBroken;
        public static bool NearingTown;
        private float rotationSpeed;

        // Start is called before the first frame update
        void Start()
        {
            originalPos = genericMarker.transform.position;
        }

        void OnEnable(){
            SetMovementSpeed();
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

        /// <summary>
        /// Reset the position of markers when reaching a destination
        /// </summary>
        public void OnLeaveTravel(){
            genericMarker.transform.position = originalPos;
            cityMarker.transform.position = originalPos;
        }

        /// <summary>
        /// Set the movement speed of items in the travel phase
        /// </summary>
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
            // Animate generic object if car is actively driving (ie. interval from 3-8s)
            if(!carIsBroken && TravelLoop.Timer >= 3.0f && TravelLoop.Timer < 8.0f){
                genericMarker.transform.Translate(movement * Time.deltaTime);
                if(NearingTown){
                    cityMarker.transform.Translate(movement * Time.deltaTime);
                }
            }
            else{
                statusRead = false;
            }

            // Reset marker when offscreen to the right
            if(genericMarker.transform.position.x > 150f){
                genericMarker.transform.position = originalPos;
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
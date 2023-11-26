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
        public GameObject genericMarker, cityMarker, wireMarker;
        public GameObject[] carTires;
        public Vector3 originalGenPos, originalCityPos, originalWirePos;
        private Vector3 movement;
        private bool statusRead, carIsBroken;
        public static bool NearingTown;
        private float rotationSpeed;

        void OnEnable(){
            genericMarker.GetComponent<RectTransform>().localPosition = originalGenPos;
            wireMarker.GetComponent<RectTransform>().localPosition = originalWirePos;
            cityMarker.GetComponent<RectTransform>().localPosition = originalCityPos;
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
        /// Set the movement speed of items in the travel phase
        /// </summary>
        void SetMovementSpeed(){
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            int speed = save.PaceMode;
            speed = speed == 0 ? 40 : speed == 1 ? 50 : 60;
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
            if(genericMarker.GetComponent<RectTransform>().localPosition.x > 710f){
                genericMarker.GetComponent<RectTransform>().localPosition = originalGenPos;
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
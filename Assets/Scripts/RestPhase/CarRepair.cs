using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Mono.Data.Sqlite;
using TMPro;
using UI;
using Database;
using System.Linq;

namespace RestPhase{
    [DisallowMultipleComponent]
    public class CarRepair : MonoBehaviour{
        [Header("Rest Menu")]
        [Tooltip("Rest menu script.")]
        [SerializeField]
        private RestMenu restMenu;

        [Header("Repair Screen Components")]
        [Tooltip("Text displaying repair screen prompts.")]
        [SerializeField]
        private TextMeshProUGUI repairScreenCountText;

        [Tooltip("Text displaying repair screen total recovered.")]
        [SerializeField]
        private TextMeshProUGUI repairScreenRecoverText;

        [Tooltip("Player input object")]
        [SerializeField]
        private PlayerInput playerInput;

        [Tooltip("Button click audio")]
        [SerializeField]
        private AudioSource buttonClick;

        [Tooltip("Button to return to main repair menu")]
        [SerializeField]
        private GameObject returnButton;

        [Tooltip("List of targets to line up with the prompts")]
        [SerializeField]
        private List<GameObject> targets;

        [Tooltip("List of status of targets hit")]
        [SerializeField]
        private List<TextMeshProUGUI> statuses;

        [Tooltip("List of prompts to line up with the targets")]
        [SerializeField]
        private List<GameObject> prompts;

        // To track left click triggers
        private InputAction leftClickAction;
        // To track minigame status
        private bool gameStarted = false;

        // To track active coroutine
        private Coroutine coroutine;
        // To track scrap used
        private int scrapUsed = 0, tries = 0, speed = 0;

        void Start(){
            leftClickAction = playerInput.actions["LeftClick"];
        }

        /// <summary>
        /// Initialize car repairs using scrap
        /// </summary>
        /// <param name="id">The amount of scrap to use, indicated by the button</param>
        public void InitializeRepair(int id){
            scrapUsed = (int)(Mathf.Pow(2, id));
            tries = scrapUsed;
            returnButton.SetActive(false);
            coroutine = StartCoroutine(Process());
        }

        /// <summary>
        /// Toggle obstacles to start moving as the minigame starts
        /// </summary>
        private void StartMoving(){
            gameStarted = !gameStarted;
            speed = Random.Range(50,90);
        }

        private void Update(){
            if(gameStarted){
                if(tries == 0){
                    StopCoroutine(coroutine);
                    gameStarted = !gameStarted;
                    StartCoroutine(ConcludeGame());
                    return;
                }
                // Move the prompts
                prompts[tries-1].transform.Translate(Vector3.right * speed * Time.deltaTime);

                if(leftClickAction.triggered || prompts[tries-1].transform.position.x >= 17f){
                    tries--;
                    speed = Random.Range(50,90);
                    buttonClick.Play();
                }
            }
        }

        /// <summary>
        /// Conclude the game by calculating the score
        /// </summary>
        private IEnumerator ConcludeGame(){
            int amountRecovered = 0;

            // Calculate the score and text to display the status of each target hit and total car hp restored.
            // Miss = 0, Near Miss = 1, Good = 2, Great = 3
            for(int i = 0; i < statuses.Count; i++){
                // Skip higher numbers if less scrap used.
                if(i >= scrapUsed){
                    break;
                }

                // Rate based on falling in between specified x-coordinates
                if(Mathf.Abs(prompts[i].transform.localPosition.x) > 12f && Mathf.Abs(prompts[i].transform.localPosition.x) <= 17f){
                    statuses[i].text = "Near Miss";
                    amountRecovered += 6;
                }
                else if(Mathf.Abs(prompts[i].transform.localPosition.x) >= 9f && Mathf.Abs(prompts[i].transform.localPosition.x) <= 12f){
                    statuses[i].text = "Good";
                    amountRecovered += 7;
                }
                else if(prompts[i].transform.localPosition.x > -9f && prompts[i].transform.localPosition.x < 9f){
                    statuses[i].text = "Great";
                    amountRecovered += 8;
                }
                else{
                    statuses[i].text = "Miss";
                }

                yield return new WaitForSeconds(0.5f);
            }

            // Update as necessary in the database
            // Check if a mechanic was used.
            bool mechanicPresent = false;
            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters().Where<ActiveCharacter>(c=>c.FileId == GameLoop.FileId);
            foreach(ActiveCharacter character in characters){
                if(character.Perk == 0){
                    amountRecovered += 5;
                    mechanicPresent = true;
                    break;
                }
            }

            Car car = DataUser.dataManager.GetCarById(GameLoop.FileId);
            car.CarHP += amountRecovered;
            car.CarHP = car.CarHP > 100 ? 100 : car.CarHP;

            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            save.Scrap -= scrapUsed;

            DataUser.dataManager.UpdateSave(save);
            DataUser.dataManager.UpdateCar(car);

            repairScreenRecoverText.text = "You repaired for " + amountRecovered + " HP.";
            repairScreenRecoverText.text += mechanicPresent ? "\nA +5 bonus was applied because of a mechanic's presence." : "";
            
            restMenu.PerformWaitingAction(3);
            returnButton.SetActive(true);
        }

        /// <summary>
        /// Process all necessary delays for the minigame
        /// </summary>
        private IEnumerator Process(){
            repairScreenRecoverText.text = "";
            repairScreenCountText.text = "Get ready!";
            for(int i = 0; i < targets.Count; i++){
                targets[i].SetActive(i < scrapUsed);
                statuses[i].text = "";
                prompts[i].SetActive(i < scrapUsed);
                prompts[i].transform.localPosition = new Vector3(-400f, prompts[i].transform.localPosition.y, prompts[i].transform.localPosition.z);
            }

            yield return new WaitForSeconds(3.0f);
            repairScreenCountText.text = "3";
            yield return new WaitForSeconds(1.0f);
            repairScreenCountText.text = "2";
            yield return new WaitForSeconds(1.0f);
            repairScreenCountText.text = "1";
            yield return new WaitForSeconds(1.0f);
            repairScreenCountText.text = "Go!";
            yield return new WaitForSeconds(1.0f);
            repairScreenCountText.text = "";
            StartMoving();
            yield return new WaitForSeconds(10.0f);
            StartCoroutine(ConcludeGame());
        }
    }
}
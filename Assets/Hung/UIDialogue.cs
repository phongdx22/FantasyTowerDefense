using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using TDTK;

public class UIDialogue : MonoBehaviour
{
    public RectTransform dialogueBox;
    public TMP_Text dialogueText;

    private Vector3 offscreenAnchorPos;
    public RectTransform onscreenAnchorRef;

    public Slider imageSlider;

    // Start is called before the first frame update
    void Start()
    {
        offscreenAnchorPos = dialogueBox.position;
        dialogueBox.position = offscreenAnchorPos;
        if (SceneManager.GetActiveScene().name == "Level2Map")
        {
            switch (SpawnManager.GetCurrentWaveIndex())
            {
                //preparing
                case -1:
                    DisplayDialogue("Endless mode. Survive as long as you can. Enemies will get stronger and stronger. Godspeed.");
                    Invoke("HideDialogue", 5f);
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (SceneManager.GetActiveScene().name == "TutorialMap")
        {

            switch (SpawnManager.GetCurrentWaveIndex())
            {
                //preparing
                case -1:
                    DisplayDialogue("Right click to move character. Left click on tower icon, then click on slot to build tower. Towers spawn soldiers to help you. Soldiers can be rallied by pressing R");
                    break;
                //wave 1
                case 0:
                    if (SpawnManager.GetTimeToNextWave() == -1)
                        DisplayDialogue("Enemies go through path, you lose life when they reach the end, kill them all.");
                    else
                        DisplayDialogue("Good job. More enemies coming, build more towers to kill them. Building tower needs money, kill more enemies to get money.");
                    break;
                //wave 2
                case 1:
                    if (SpawnManager.GetTimeToNextWave() == -1)
                        HideDialogue();
                    else
                        DisplayDialogue("Nice. Enemies have different types, use different towers to counter them.");
                    break;
                //wave 3
                case 2:
                    if (SpawnManager.GetTimeToNextWave() == -1)
                        HideDialogue();
                    else
                        DisplayDialogue("Cool.");
                    break;
                //wave 4
                case 3:
                    if (SpawnManager.GetTimeToNextWave() == -1)
                        HideDialogue();
                    else
                        DisplayDialogue("Very nice. Last wave, good luck.");
                    break;
                //wave 5
                case 4:
                    if (SpawnManager.GetTimeToNextWave() == -1)
                        HideDialogue();
                    else
                        DisplayDialogue("Congrats, you've completed the tutorial. Now get to the first level.");
                    break;
                default:
                    break;
            }

        }
    }

    private void DisplayDialogue(string textToDisplay)
    {
        dialogueBox.position = onscreenAnchorRef.position;

        dialogueText.text = textToDisplay;
    }
    private void HideDialogue()
    {
        dialogueBox.position = offscreenAnchorPos;
    }
    private void UnhideTowers(float howMuchToUnhide)
    {
        if (imageSlider == null)
            return;

        imageSlider.maxValue = 8;
        imageSlider.value = 8 - howMuchToUnhide;
    }

}

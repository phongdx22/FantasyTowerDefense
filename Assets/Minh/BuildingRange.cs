using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingRange : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject uiElement; 

    private void Start()
    {
        if (uiElement != null)
        {
            uiElement.SetActive(false); 
        }
        else
        {
            Debug.LogWarning("UI Element is not assigned in the inspector.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            if (uiElement != null)
            {
                uiElement.SetActive(true); 
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            if (uiElement != null)
            {
                uiElement.SetActive(false);
            }
        }
    }
}

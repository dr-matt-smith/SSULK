

using UnityEngine;
using System.Collections;

public class CreateLogMessages : MonoBehaviour
{
    	
    // Update is called once per frame
    void Update () { 		
        
        if (Input.GetKeyDown(KeyCode.Space)) {       
            Debug.Log("space was pressed");
        } 	
        
        if (Input.GetKeyDown(KeyCode.LeftShift)) {       
            Debug.LogWarning("LEFT shift was pressed");
        } 	
        
        if (Input.GetKeyDown(KeyCode.RightShift)) {       
            Debug.LogError("RIGHT shift was pressed");
        } 	
    }  

}
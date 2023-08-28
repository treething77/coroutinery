using System.Collections;
using System.Collections.Generic;
using aeric.coroutinery;
//using MEC;
using UnityEngine;

public class MECTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    //    Timing.RunCoroutine(_CheckForWin());
    }
    /*
    IEnumerator<float> _CheckForWin()
    {
        
        while (counter < 3)
        {
            Debug.Log(counter);
     //       yield return Timing.WaitUntilDone(_CheckForDone());
        }
        
        Debug.Log("You win!");
    }
    
    IEnumerator<float> _CheckForDone()
    {
        
        while (!done)
        {
            Debug.Log("Not done yet");
       //     yield return Timing.WaitForSeconds(0.2f);
        }
        
        Debug.Log("Ok I'fffSdddfsfdfdfm done");
    }

    private int counter;
    private bool done;
    
    // Update is called once per frame
    void Update()
    {
        counter++;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            done = true;
        }
    }
    */
}

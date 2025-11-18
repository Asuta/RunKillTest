using UnityEngine;
using Drakkar.Examples;
using System.Security.Cryptography.X509Certificates;

public class TrailTest : MonoBehaviour
{

    public AnimationEvents animationEvents;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animationEvents.StartTrail();
    }

    // Update is called once per frame
    void Update()
    {
        // animationEvents.UpdateTrail();
        
    }
}

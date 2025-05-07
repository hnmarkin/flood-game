using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartUIController : MonoBehaviour
{
    [SerializeField] GameObject buttonS;
    [SerializeField] GameObject buttonQ;
    [SerializeField] GameObject Title;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        LeanTween.moveLocalX(buttonS, 0, 0.75f).setEaseOutBack();
        StartCoroutine(hold() );
    }

    // Update is called once per frame
    float tickTimer = 0.5f;
    float tickTimerMax = 0.5f;
    bool sw;
    void Update()
    {
        
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickTimerMax)
        {
            tickTimer -= tickTimerMax;
            if(sw) 
            { 
                LeanTween.scale(Title, new Vector3(1.1f, 1.1f, 1), 0.5f);
                LeanTween.scale(buttonS, new Vector3(1.05f, 1.05f, 1), 0.5f);
                LeanTween.scale(buttonQ, new Vector3(1.05f, 1.05f, 1), 0.5f);
                //LeanTween.moveLocalY(buttonS, buttonS.transform.position.y + 1, 0.75f).setEaseOutBack();
                sw = false;
                //LeanTween.moveLocalY(buttonS, buttonS.transform.position.y - 1, 0.75f).setEaseOutBack();
                return;
            }
            if (!sw)
            {
                LeanTween.scale(Title, new Vector3(1f, 1f, 1), 0.5f);
                LeanTween.scale(buttonS, new Vector3(1f, 1f, 1), 0.5f);
                LeanTween.scale(buttonQ, new Vector3(1f, 1f, 1), 0.5f);
                // LeanTween.moveLocalY(buttonQ, buttonQ.transform.position.y + 20, 0.75f).setEaseOutBack();
                sw = true;
                //  LeanTween.moveLocalY(buttonS, buttonS.transform.position.y - 1, 0.75f).setEaseOutBack();
                return;
            }
        }
        
    }
    IEnumerator hold()
    {
        yield return new WaitForSeconds(0.1f);
        LeanTween.moveLocalX(buttonQ, 0, 0.75f).setEaseOutBack();

    }
    public void StartUp()
    {
        SceneManager.LoadScene("WaterTest");
        SceneManager.UnloadScene("StartScene");
    }
    public void QUIT()
    {
        Application.Quit();
    }
}

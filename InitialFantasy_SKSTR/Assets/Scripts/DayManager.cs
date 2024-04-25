using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using IFSKSTR.SaveSystem;
using TMPro; 

public class DayManager : MonoBehaviour
{
    public TMP_Text dialogueText; 
    public string fullText;
    public float typingSpeed = 0.05f;
    public GameObject playerPrefab;

    private void Start()
    {
        SaveSystem.Load();
        StartCoroutine(TypeSentence(fullText));
    }

    IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = ""; 
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter; 
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(1f); //
        SceneManager.LoadScene("House"); // 
    }
}

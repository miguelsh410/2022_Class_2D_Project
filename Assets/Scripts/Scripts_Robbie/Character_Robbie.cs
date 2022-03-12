using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Character_Robbie : MonoBehaviour
{
    // Code adapted from MyCharacterController (Dr.Craven)

    public int score = 0;

    Rigidbody2D body;

    BoxCollider2D bc2d; // Handles boxCasting, which tells if the player interacts with things
    public LayerMask interactable;
    public float boxCastDistance;

    bool canMove;
    bool hasDied; // Keeps track of whether or not the player has any deaths

    float horizontal;
    float vertical;
    float moveLimiter = 0.7f;
    public float runSpeed = 5.0f;
    
    public GameObject scoreGO;
    public Text scoreText;

    public Text dialogueText;
    public string openingDialoguePath;
    public string allergyDialoguePath;

    string dialoguePath;
    AudioSource dialogueSound;

    public AudioSource pickupSound;

    float characterOriginX;
    float characterOriginY;

    public float boxCastSize;

    void Start()
    {
        // Get the rigid body component for the player character.
        // (required to have one)
        body = GetComponent<Rigidbody2D>();
        bc2d = GetComponent<BoxCollider2D>();

        characterOriginX = transform.position.x;
        characterOriginY = transform.position.y;

        hasDied = false;

        scoreText.fontSize = 20;

        StartCoroutine(ReadDialogue(new StreamReader(openingDialoguePath)));
    }

    void Update()
    {
        if(canMove)
        {
            horizontal = Input.GetAxisRaw("Horizontal"); 
            vertical = Input.GetAxisRaw("Vertical"); 
        }

        if(Input.GetKeyDown("space"))
        {
            RaycastHit2D hit = Physics2D.BoxCast(bc2d.bounds.center, bc2d.bounds.size, 0f, Vector2.down, boxCastDistance, interactable);

            if (!hit)
            {
                hit = Physics2D.BoxCast(bc2d.bounds.center, bc2d.bounds.size, 0f, Vector2.up, boxCastDistance, interactable);
            }
            if (!hit)
            {
                hit = Physics2D.BoxCast(bc2d.bounds.center, bc2d.bounds.size, 0f, Vector2.right, boxCastDistance, interactable);
            }
            if (!hit)
            {
                hit = Physics2D.BoxCast(bc2d.bounds.center, bc2d.bounds.size, 0f, Vector2.left, boxCastDistance, interactable);
            }
            if (hit)
            {
                DialogueHolder dialogueInfo = hit.transform.gameObject.GetComponent<DialogueHolder>();
                Color color = dialogueInfo.color;
                string dialoguePath = dialogueInfo.dialoguePath;
                dialogueSound = dialogueInfo.dialogueSound;
                float lowPitch = dialogueInfo.lowPitch;
                float highPitch = dialogueInfo.highPitch;
                StartCoroutine(ReadDialogue(new StreamReader(dialoguePath), dialogueSound, color, lowPitch, highPitch));
            }
        }
    }

    void FixedUpdate()
    {

        // If player is running diagonally, we don't want them to move extra-fast.
        if (horizontal != 0 && vertical != 0) // Check for diagonal movement
        {
            // limit movement speed diagonally, so you move at 70% speed
            horizontal *= moveLimiter;
            vertical *= moveLimiter;
        }

        if(canMove){
            body.velocity = new Vector2(horizontal * runSpeed, vertical * runSpeed);
        }
    }

    void OnTriggerEnter2D(Collider2D colliderEvent)
    {
        // Did we run into an object that will affect our score?
        ScoreScript scoreObject = colliderEvent.gameObject.GetComponent(typeof(ScoreScript))
                                  as ScoreScript;

        if (scoreObject != null)
        {
            // Yes, change the score
            score += scoreObject.points;
            // Destroy the object
            pickupSound.Play();
            Destroy(colliderEvent.gameObject);
            scoreText.text = "Pumpkin Points: " + score;
        }
        else if(colliderEvent.gameObject.CompareTag("Enemy"))
        {
            // Respawn the player, they keep their points
            body.velocity = new Vector2(0, 0);
            transform.position = new Vector2(characterOriginX, characterOriginY);

            if(!hasDied)
            {
                hasDied = true;
                StartCoroutine(ReadDialogue(new StreamReader(allergyDialoguePath)));
            }
        }

        // Did we run into an object that will cause a scene change?
        SceneChangeScript sceneChangeObject = colliderEvent.gameObject.GetComponent(typeof(SceneChangeScript))
                                              as SceneChangeScript;
        if (sceneChangeObject != null) StartCoroutine(DelaySceneChange(sceneChangeObject));
    }

    IEnumerator ReadDialogue(StreamReader dialogueReader)
    {
        canMove = false;
        string line;
        scoreGO.SetActive(false);
        dialogueText.gameObject.SetActive(true);
        yield return new WaitForSeconds(.5f);

        while((line = dialogueReader.ReadLine()) != " ")
        {
            dialogueText.text = line;
            yield return new WaitForSeconds(.5f);
            while((line = dialogueReader.ReadLine()) != "")
            {
                dialogueText.text = line;
                yield return new WaitForSeconds(.5f);
            }
        }

        yield return new WaitForSeconds(1f);

        //reverts all changes made, letting the player continue playing
        scoreGO.SetActive(true);
        dialogueText.text = "";
        dialogueText.gameObject.SetActive(false);
        canMove = true;
        dialogueReader.Close();
        StopCoroutine(ReadDialogue(dialogueReader));
    }

    IEnumerator ReadDialogue(StreamReader dialogueReader, AudioSource dialogueSound, Color color, float lowPitch, float highPitch)
    {
        canMove = false;
        string line;
        scoreGO.SetActive(false);
        dialogueText.gameObject.SetActive(true);
        dialogueText.color = color;
        while((line = dialogueReader.ReadLine()) != " ")
        {
            dialogueText.text = line;
            yield return new WaitForSeconds(.5f);
            while((line = dialogueReader.ReadLine()) != "")
            {
                dialogueText.text = line;
                dialogueSound.pitch = Random.Range(lowPitch, highPitch);
                dialogueSound.Play();
                yield return new WaitForSeconds(.5f);
            }
        }

        yield return new WaitForSeconds(1f);

        //reverts all changes made, letting the player continue playing
        scoreGO.SetActive(true);
        dialogueText.text = "";
        dialogueText.gameObject.SetActive(false);
        canMove = true;
        dialogueReader.Close();
        dialogueText.color = Color.white;
        StopCoroutine(ReadDialogue(dialogueReader, dialogueSound, color, lowPitch, highPitch));
    }

    IEnumerator DelaySceneChange(SceneChangeScript sceneChangeObject)
    {
        dialogueText.gameObject.SetActive(true);
        sceneChangeObject.gameObject.SetActive(false);
        dialogueText.text = "CONGRATS! YOU FOUND THE BOOT!";
        yield return new WaitForSeconds(1f);
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        // Load up the scene accourding to the sceneChange value
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex + sceneChangeObject.sceneChange);
    }
}

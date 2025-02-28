using System;
using MainLevel.Platforms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainLevel
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D), typeof(BoxCollider2D))]
    public class PlayerControls : MonoBehaviour
    {
        [SerializeField] private Transform startPos, bonusStartPos;
        [SerializeField] private bool startState;
        [SerializeField] private GameObject losePanel;
        private Vector3 scale;
        
        [Header("Duality")]
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private Sprite circleSprite, squareSprite;
        private bool state;

        public bool State => state;

        [Header("Movement")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private LayerMask groundLayer;
        private bool onMovingPlat, onHotPlat;
        public bool onIcePlat;
        public bool grounded;
        public bool poweredUp;

        [Header("Circle Behaviour")]
        [SerializeField] private CircleCollider2D circleCol;
        [SerializeField] private float speed = 1;
        [SerializeField] private float poweredSpeed;
        [SerializeField] private Sprite coldCircle;
        [SerializeField] private float platformMeltSpeed;
        [SerializeField] private float poweredPlatformMeltSpeed;
        private Vector2 rollDir = Vector2.right;
    
        [Header("Square Behaviour")]
        [SerializeField] private BoxCollider2D squareCol;
        [SerializeField] private Sprite hotSquare;
        [SerializeField] private bool slide;
        [SerializeField] private float iceSlideSpeed;
        [SerializeField] private float poweredUpSpeed;
        [SerializeField] private float meltSpeed;
        [SerializeField] private float poweredUpMeltSpeed;
    

        private void Start()
        {
            if (!sr) sr = GetComponent<SpriteRenderer>();
            if (!rb) rb = GetComponent<Rigidbody2D>();
            if (!circleCol) circleCol = GetComponent<CircleCollider2D>();
            if (!squareCol) squareCol = GetComponent<BoxCollider2D>();

            Time.timeScale = 1;
            losePanel.SetActive(false);

            // If the player has come from the bonus level, move them to that position
            if (PlayerPrefs.GetInt("BonusLevel") == 1)
            {
                transform.position = bonusStartPos.position;
                PlayerPrefs.SetInt("BonusLevel", 0);
            }
            else transform.position = startPos.position;
            
            ChangeState(startState);
            gameObject.SetActive(true);
            scale = transform.localScale;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                ChangeState();
        }

        // Toggle state
        private void ChangeState()
        {
            state = !state;
            sr.sprite = state ? squareSprite : circleSprite;

            // Circle
            if (!state)
            {
                rb.constraints = RigidbodyConstraints2D.None;
                if (poweredUp) rb.AddForce(rollDir * poweredSpeed); else rb.AddForce(rollDir * speed);
                circleCol.enabled = true;
                squareCol.enabled = false;
                rb.velocity = rollDir;
            }
            // Square
            else
            {
                squareCol.enabled = true;
                circleCol.enabled = false;
            }
        }

        // Set specific state
        private void ChangeState(bool newState)
        {
            state = newState;
            sr.sprite = state ? squareSprite : circleSprite;

            // Circle
            if (!state)
            {
                rb.constraints = RigidbodyConstraints2D.None;
                circleCol.enabled = true;
                squareCol.enabled = false;
                rb.velocity = rollDir;
            }
            // Square
            else
            {
                squareCol.enabled = true;
                circleCol.enabled = false;
            }
        }

        private void Die()
        {
            poweredUp = false;
            Time.timeScale = 0;
            losePanel.SetActive(true);
        }

        private void FixedUpdate()
        {
            //Debug.DrawRay(transform.position, Vector3.down, Color.green);
            
            switch (state)
            {
                case false:
                    grounded = Physics2D.Raycast(transform.position, Vector2.down, circleCol.bounds.extents.y + 0.1f, groundLayer);
                    CircleBehaviour();
                    break;
            
                case true:
                    grounded = Physics2D.Raycast(transform.position, Vector2.down, squareCol.bounds.extents.y + 0.1f, groundLayer);
                    SquareBehaviour();
                    break;
            }
        }

        private void CircleBehaviour()
        {
            rollDir = rb.velocity.x > 0 ? Vector2.right : Vector2.left;
        }

        private void SquareBehaviour()
        {
            var slideSpeed = poweredUp ? poweredUpSpeed : meltSpeed;
            var currentMeltSpeed = poweredUp ? poweredUpMeltSpeed : meltSpeed;
            
            if (onMovingPlat) return;

            if (onIcePlat) rb.velocity = rollDir * slideSpeed;


            if (onHotPlat)
            {
                var material = sr.material;
                Color colour = material.color;
                float fade = colour.a - (currentMeltSpeed * Time.deltaTime);
            
                colour = new Color(colour.r, colour.g, colour.b, fade);
                material.color = colour;
            
                if (colour.a <= 0)
                    Die();
            }
        
            transform.eulerAngles = new Vector3(0, 0, (rollDir == Vector2.right ? -10 : 10));

            if (grounded)
                rb.constraints = slide ? RigidbodyConstraints2D.None : RigidbodyConstraints2D.FreezePositionX;
            else if (!grounded)
                rb.constraints = RigidbodyConstraints2D.FreezePositionX;
        }

    
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.transform.rotation.z < 0) rollDir = Vector2.right;
            else if (other.transform.rotation.z > 0) rollDir = Vector2.left;

            var platform = other.gameObject.GetComponent<Platform>();
            if (!platform) return;

            switch (platform.ThisType)
            {
                case Platform.Type.Ice:
                    onIcePlat = true;
                    sr.sprite = !state ? coldCircle : squareSprite;
                    break;
                
                case Platform.Type.Hot:
                    onHotPlat = true;
                    sr.sprite = !state ? circleSprite : hotSquare;
                    break;
                
                case Platform.Type.Moving:
                    onMovingPlat = true;
                    break;
                
                case Platform.Type.Spike:
                    Die();
                    break;
                
                case Platform.Type.Bonus:
                    other.gameObject.GetComponent<BonusPlatform>().LoadBonusLevel();
                    break;
                
                case Platform.Type.Win:
                    other.gameObject.GetComponent<WinPlatform>().EndLevel();
                    break;
                
                case Platform.Type.Death:
                    Die();
                    break;
                
                default:
                    break;
            }
        }

        private void OnCollisionStay2D(Collision2D other)
        {
            var platform = other.gameObject.GetComponent<Platform>();
            if (!platform) return;

            switch (platform.ThisType)
            {
                case Platform.Type.Ice:
                    IcePlatform icePlatform = other.gameObject.GetComponent<IcePlatform>();
                    if (!state)
                    {
                        icePlatform.Melt(meltSpeed = poweredUp ? poweredUpMeltSpeed : meltSpeed);
                    }

                    break;
                
                case Platform.Type.Moving:
                    scale = scale;
                    break;
            }
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            var platform = other.gameObject.GetComponent<Platform>();
            if (!platform) return;

            switch (platform.ThisType)
            {
                case Platform.Type.Ice:
                    onIcePlat = false;
                    sr.sprite = !state ? circleSprite : squareSprite;
                    break;
                
                case Platform.Type.Hot:
                    onHotPlat = false;
                    sr.sprite = !state ? circleSprite : squareSprite;
                    break;
                
                case Platform.Type.Moving:
                    onMovingPlat = false;
                    break;
                
                default:
                    break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("PowerUp"))
            {
                //do nothing if not power up
            }
            else
            {
                poweredUp = true;
                other.gameObject.SetActive(false);
            }
        }
    }
}

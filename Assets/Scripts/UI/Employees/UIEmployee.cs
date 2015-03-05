using UnityEngine;
using System.Collections;

public class UIEmployee : MonoBehaviour {
    public Transform HUDtarget;
    public Color unhappyColor;
    public Color happyColor;

    [HideInInspector]
    public GameObject HUDgroup;
    [HideInInspector]
    public HUDText hudtext;
    [HideInInspector]
    public UILabel happinessLabel;

    [HideInInspector]
    public Worker worker;

    [HideInInspector]
    public GameObject laborObj;
    public GameObject laborPrefab;

    [SerializeField, HideInInspector]
    private Office.Desk desk;
    [SerializeField, HideInInspector]
    private State state = State.Wandering;

    private NavMeshAgent agent;
    private LineRenderer line;
    private Company company;

    [HideInInspector]
    public Vector3 target;

    void Start() {
        company = GameManager.Instance.playerCompany;
        StartCoroutine(Working());

        agent = GetComponent<NavMeshAgent>();
        line = GetComponent<LineRenderer>();
        target = RandomTarget();
        desk = UIOfficeManager.Instance.RandomDesk();
        desk.occupied = true;

        line.material.color = UIOfficeManager.Instance.RandomColor();
    }

    [System.Serializable]
    private enum State {
        Wandering,
        Idling,
        GoingToDesk,
        AtDesk
    }

    void Update() {
        // Move to target if not at desk and not idling.
        if (state == State.GoingToDesk || state == State.Wandering) {
            line.SetPosition(0, transform.position);
            agent.SetDestination(target);
            DrawPath(agent.path);

            // May randomly go to desk.
            if (state == State.Wandering && company.developing && Random.value < 0.05f * worker.productivity.value) {
                GoToDesk();
            }

            // Check if we've reached the destination
            // For this to work, the stoppingDistance has to be about 1.
            if (Vector3.Distance(agent.nextPosition, agent.destination) <= agent.stoppingDistance) {

                // If going to a desk...
                if (state == State.GoingToDesk) {
                    state = State.AtDesk;

                } else {
                    // Else, continue wandering around.
                    StartCoroutine(Pause());
                    target = RandomTarget();
                }
            }
        }
    }

    IEnumerator Pause() {
        state = State.Idling;
        yield return new WaitForSeconds(1f + Random.value * 3f);
        state = State.Wandering;
    }

    void OnEnable() {
        // On enable, reset the target and desk.
        target = RandomTarget();
        desk = UIOfficeManager.Instance.RandomDesk();
        desk.occupied = true;
    }

    void OnDisable() {
        // Relinquish the deks.
        desk.occupied = false;
        desk = null;
    }

    void OnDestroy() {
        Destroy(HUDgroup);
    }

    // Temporary, to get employees moving about the office.
    Vector3 RandomLocation() {
        return transform.parent.TransformDirection(UIOfficeManager.Instance.RandomLocation());
    }

    Vector3 RandomTarget() {
        return transform.parent.TransformDirection(UIOfficeManager.Instance.RandomTarget());
    }

    public void GoToDesk() {
        Debug.Log("GOING TO DESK!");
        state = State.GoingToDesk;
        target = transform.parent.TransformDirection(desk.transform.position);
    }
    public void LeaveDesk() {
        Debug.Log("LEAVING DESK!");
        state = State.Wandering;
        target = RandomTarget();
    }

    IEnumerator Working() {
        while(true) {
            float happy = worker.happiness.value;
            if (happy >= 20) {
                happinessLabel.text = ":D";
                happinessLabel.color = happyColor;
            } else if (happy >= 14) {
                happinessLabel.text = ":)";
                happinessLabel.color = happyColor;
            } else if (happy >= 8) {
                happinessLabel.text = ":\\";
                happinessLabel.color = happyColor;
            } else if (happy >= 4) {
                happinessLabel.text = ":(";
                happinessLabel.color = unhappyColor;
            } else {
                happinessLabel.text = ">:(";
                happinessLabel.color = unhappyColor;
            }

            if (company.developing && state == State.AtDesk && laborObj == null) {
                // Decide whether or not to work
                // or leave the desk.
                // Robots don't leave their desk.
                // TO DO may need to tweak this value.
                if (!worker.robot && Random.value < 0.8f/worker.productivity.value) {
                    LeaveDesk();
                } else {
                    laborObj = NGUITools.AddChild(HUDgroup, laborPrefab);

                    Stat stat;
                    if (Random.value <= 0.02f * worker.happiness.value) {
                        stat = new Stat("Breakthrough", Randomize(
                            (worker.creativity.value + worker.cleverness.value + worker.charisma.value)/3f
                        ));
                    } else {
                        float roll = Random.value;
                        if (roll <= 0.33) {
                            stat = new Stat("Design", Randomize(worker.creativity.value ));
                        } else if (roll <= 0.66) {
                            stat = new Stat("Engineering", Randomize(worker.cleverness.value ));
                        } else {
                            stat = new Stat("Marketing", Randomize(worker.charisma.value ));
                        }
                    }

                    laborObj.GetComponent<UILabor>().stat = stat;

                    UIFollowTarget uift = laborObj.GetComponent<UIFollowTarget>();
                    UIOfficeManager.Instance.SetupFollowTarget(this, uift);
                }
            }

            yield return new WaitForSeconds(2 * Random.value);
        }
    }

    private float Randomize(float value) {
        return Mathf.Max(1, (0.5f + Random.value) * value);
    }

    // Double click to force back to desk,
    // depending on happiness.
    void OnDoubleClick() {
        if (state != State.AtDesk) {
            StartCoroutine(Pulse(0.4f, 0.5f));
            if (Random.value <= 0.5f * worker.happiness.value) {
                AudioManager.Instance.PlayEmployeeTouchedFX();
                GoToDesk();
            }
        }
    }

    private IEnumerator Pulse(float from, float to) {
        Vector3 fromScale = new Vector3(from,from,from);
        Vector3 toScale = new Vector3(to,to,to);
        float step = 0.15f;

        for (float f = 0f; f <= 1f + step; f += step) {
            transform.localScale = Vector3.Lerp(fromScale, toScale, Mathf.SmoothStep(0f, 1f, f));
            yield return null;
        }

        for (float f = 0f; f <= 1f + step; f += step) {
            transform.localScale = Vector3.Lerp(toScale, fromScale, Mathf.SmoothStep(0f, 1f, f));
            yield return null;
        }
    }

    // Draw path to employee's target.
    private void DrawPath(NavMeshPath path) {
        // If the path has 1 or no corners, there is no need.
        if (path.corners.Length < 2)
            return;

        // Set the array of positions to the amount of corners.
        line.SetVertexCount(path.corners.Length);

        // Go through each corner and set that to the line renderer's position.
        for(int i=0; i<path.corners.Length; i++){
            line.SetPosition(i, path.corners[i]);
        }
    }
}

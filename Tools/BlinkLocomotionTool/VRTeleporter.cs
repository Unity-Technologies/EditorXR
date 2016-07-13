using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

public class VRTeleporter : MonoBehaviour
{
    public bool useColliders = true;

    [Tooltip( "The layer of the collider that the blink tool will cast against" )]
    public LayerMask layerMask;

    public bool useBlinkAvatar;
    public Transform avatar;
    [Tooltip( "The transform of the object used to indicate where theplayArea's head will be after the blink. Should be a child of the avatar" )]
    public Transform avatarHead;

    [Tooltip( "The max distance that the user can move in one blink" )]
    public float maxDistance = 30f;

    public float moveTime = 0.2f;
    public float moveDelay = 0.5f;

    public float hoverSphereRadius = 0.05f;
    public LayerMask hoverLayerMask = -1;
    public float hoverUpdateInterval = 0.1f;

    // refs
    protected Transform hmdTransform;
    protected Vector3 lastTargetPosition;
    //

    protected bool isBlinking = false;
    protected bool isMoving = false; // are we lerping to our blink target?
    protected Vector3 startingPosition = Vector3.zero;
    protected Transform avatarCachedParent;

    protected GameObject[] blinkColliders;
    Transform playArea;
    Transform toolPoint;

    public void Awake()
    {
        avatar.gameObject.SetActive(false);
        playArea = FindObjectOfType<SteamVR_PlayArea>().transform;
    }

    public void Start()
    {
        blinkColliders = GameObject.FindGameObjectsWithTag("Blink Colliders");
    }

    /// <summary>
    /// Start the blink
    /// </summary>
    public void InitializeTeleport(Transform origin)
    {
        if (isBlinking || isMoving || IsOriginInsideBlocker(transform) || !gameObject.activeInHierarchy) return;

        // cache the parent for returning to after blink
        avatarCachedParent = avatar.parent;

        // unparent the avatar for free movement
        avatar.parent = null;

        // save the starting position to get the blink vector when the blink is done
        startingPosition = new Vector3(hmdTransform.position.x, 0f, hmdTransform.position.z);

        StartCoroutine(ProcessLocomotionAiming());
        // avatar only set to be visible when a ray hits a valid location
    }

    /// <summary>
    /// Called when the appropriate button is lifted up
    /// </summary>
    public void EndTeleport(GameObject hand)
    {
        // make sure it's with the same hand as we started with
        if (toolPoint != hand.transform)
        {
            Debug.Log( "Blink button raised on a different controller!" );
            return;
        }

        //todo add a fadein/out
        avatar.gameObject.SetActive( false );
        if (!isBlinking || isMoving) return;

        isBlinking = false;

        if (!IsOriginInsideBlocker(hand.transform))
        {
            StartCoroutine(MoveToTargetLocation());
        }

        // place the avatar back under it's old parent
        avatar.parent = avatarCachedParent;
    }

    public IEnumerator MoveToTargetLocation()
    {
        isMoving = true;
        // find out how far we moved
        var blinkVector = avatar.position - startingPosition;
        var targetPosition = playArea.transform.position + blinkVector;

        yield return new WaitForSecondsRealtime(moveDelay);

        var t = 0f;
        var perc = 0f;
        while (perc < 1f)
        {
            playArea.transform.position = Vector3.Lerp(playArea.transform.position, targetPosition, perc);
            t += Time.unscaledDeltaTime;
            perc = t / moveTime;
            yield return null;
        }
        // move the root transform
        playArea.transform.position = targetPosition;

        isMoving = false;
//        avatar.parent = avatarCachedParent;
    }

    // Update is called once per frame
    IEnumerator ProcessLocomotionAiming()
    {
        isBlinking = true;

        while (isBlinking)
        {
            if (IsOriginInsideBlocker(toolPoint))
            {
                avatar.gameObject.SetActive(false);
                yield return null;
                continue;
            }

            avatar.gameObject.SetActive(true);
        
            // Move the target visualizer off by the beam

            var transformPos = playArea.transform.position; // ???

            var targetPosition = transformPos;

            Ray ray = new Ray(toolPoint.position, toolPoint.forward);

            // if there are no colliders just use a ray
            // If we are, project a ray from the controller to the ground
            if (!useColliders)
            {
                // Start at the max distance
                var timeToFloor = maxDistance;

                // make sure ray isn't parallel to the ground
                if (Mathf.Abs(ray.direction.y) > 0.01f)
                {
                    var rayToFloor = (transformPos.y - toolPoint.position.y)/ray.direction.y;
                    timeToFloor = Mathf.Min(Mathf.Abs(rayToFloor), timeToFloor);

                    avatar.gameObject.SetActive(true);
                }

                //	    // Move the target visualizer off by the beam
                //		var targetPosition = objectToMove.position;
                targetPosition.x += (ray.direction.x*timeToFloor);
                targetPosition.z += (ray.direction.z*timeToFloor);
            }
            else // there are blink colliders
            {
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, maxDistance, layerMask) )
//                    && (LayerMask.LayerToName(hit.transform.gameObject.layer) == "Blink Collider"))
                {
                    // if we hit a blink blocker...
                    if (LayerMask.LayerToName(hit.transform.gameObject.layer) == "Blink Blocker")
                    {
                        if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.9f) // horizontal-ish -- most likely hit a wall collider...
                        {
                            var newRayOrigin = hit.point + hit.normal * 0.1f;
                            var newRay = new Ray( newRayOrigin, Vector3.down );

                            // cast down a little ways from the wall looking for blink collider
                            if ( Physics.Raycast( newRay, out hit, maxDistance, layerMask ) )
                            {
                                if ( LayerMask.LayerToName( hit.transform.gameObject.layer ) == "Blink Blocker" )
                                {
                                    var shortestDistance = Mathf.Infinity;

                                    // we hit another blocker, need to do a search for the closest collider
                                    foreach (var bc in blinkColliders)
                                    {
                                        var closestPoint = bc.GetComponent<Collider>().ClosestPointOnBounds(hit.point);
                                        var distance = Vector3.Distance(toolPoint.position, closestPoint);
                                        if (distance < shortestDistance)
                                        {
                                            shortestDistance = distance;
                                            avatar.gameObject.SetActive(true);
                                            targetPosition = closestPoint;
                                        }
                                    }
                                }
                                else if ( LayerMask.LayerToName( hit.transform.gameObject.layer ) == "Blink Collider" )
                                {
                                    avatar.gameObject.SetActive( true );
                                    targetPosition = hit.point;
                                }
                            }
                            else
                            {
                                Debug.LogError( "No collider hit -- odd" );
                                break;
                            }
                        }
                        else // over a hole or prop
                        {
                            // find a spot where you can be
                            var spotFound = false;
                            var newRayOrigin = hit.point + Vector3.up * 1f;
                            while (!spotFound)
                            {
                                var newRay = new Ray(newRayOrigin, Vector3.down);

                                if (Physics.Raycast(newRay, out hit, maxDistance, layerMask))
                                {
                                    if (LayerMask.LayerToName(hit.transform.gameObject.layer) != "Blink Collider")
                                    {
                                        newRayOrigin = newRayOrigin + 0.1f*-ray.direction;

                                        if (Vector3.Distance(newRayOrigin, toolPoint.position) < 0.2f)
                                        {
                                            spotFound = true;
                                        }
                                    }
                                    else if ( LayerMask.LayerToName( hit.transform.gameObject.layer ) == "Blink Collider" )
                                    {
                                        spotFound = true;
                                        avatar.gameObject.SetActive( true );
                                        targetPosition = hit.point;
                                    }
                                }
                                else
                                {
                                    Debug.LogError("No collider hit -- odd");
                                    break;
                                }

                            }
                        }

                    } // else we are gravy
                    else if (LayerMask.LayerToName(hit.transform.gameObject.layer) == "Blink Collider")
                    {
                        avatar.gameObject.SetActive(true);
                        targetPosition = hit.point;

                    }
                }
                else // we didn't hit any collider
                {
                    targetPosition = lastTargetPosition;
                }
            }
            
            // if no new valid blink location was found, hide the avatar
            if (targetPosition == transformPos)
            {
                avatar.gameObject.SetActive(false);
            }

            var smoothingFactor = Mathf.Min((lastTargetPosition - targetPosition).magnitude, 1.0f);
            targetPosition = Vector3.Lerp(lastTargetPosition, targetPosition, smoothingFactor);
            lastTargetPosition = targetPosition;

            var standInPosition = targetPosition;
            standInPosition.y = 0f;
            avatar.transform.position = standInPosition;

            // show hmd height on avatar
            var hmdPos = hmdTransform.position;
            avatarHead.position = new Vector3(avatarHead.position.x, hmdPos.y, avatarHead.position.z);

            // show hmd head rotation on avatar
            avatar.rotation = Quaternion.Euler(avatar.eulerAngles.x, hmdTransform.eulerAngles.y, avatar.eulerAngles.z);

            yield return null;
        }
    }

    protected bool IsOriginInsideBlocker(Transform hand)
    {
        var overlappingColliders = Physics.OverlapSphere( hand.position, hoverSphereRadius, hoverLayerMask.value );
        foreach ( Collider collider in overlappingColliders )
        {
            if (LayerMask.LayerToName(collider.gameObject.layer) == "Blink Blocker")
            {
                return true;
            }
        }

        return false;
    }
}

using UnityEngine;

using System.Collections;

public class SceneScroller : MonoBehaviour
{
    public float stepDistance = 5f;
    public float scrollSpeed = 2f;

    public IEnumerator ScrollOneStep()
    {
        Vector3 start = transform.position;
        Vector3 end = start - new Vector3(stepDistance, 0, 0); 

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * scrollSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
    }
}

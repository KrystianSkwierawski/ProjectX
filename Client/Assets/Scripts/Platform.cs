using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public float minX = 40.68893f;
    public float maxX = 43.0405f;
    public float speed = 0.5f; // Możesz dostosować prędkość

    private bool movingRight = true;

    void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 pos = transform.position;

        if (movingRight)
        {
            pos.x += step;
            if (pos.x >= maxX)
            {
                pos.x = maxX;
                movingRight = false;
            }
        }
        else
        {
            pos.x -= step;
            if (pos.x <= minX)
            {
                pos.x = minX;
                movingRight = true;
            }
        }

        transform.position = pos;
    }
}

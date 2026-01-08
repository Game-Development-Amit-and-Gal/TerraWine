using UnityEngine;

public class SquareFloat : MonoBehaviour
{
    [SerializeField] private RectTransform squareTransform;
    [SerializeField] private float floatAmplitude = 30;
    [SerializeField] private float speed = 1f;


    private void Start()
    {
        if(squareTransform == null)
        {
            squareTransform = GetComponent<RectTransform>();
        }

    }

    private void Update()
    {
        if (squareTransform)
        {
            float move = Mathf.Sin(floatAmplitude * Time.time) * speed;
            squareTransform.anchoredPosition += new Vector2(0, move);
        }
    }
}

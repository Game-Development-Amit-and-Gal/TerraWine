using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FollowParentSortOrder : MonoBehaviour
{
    [SerializeField] private int offsetFromParent = -1; // -1 = תמיד מאחור
    [SerializeField] private bool matchSortingLayer = true;

    private SpriteRenderer _sr;
    private SpriteRenderer _parentSr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _parentSr = transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;
    }

    // חשוב: LateUpdate כדי לרוץ אחרי ה-YSort של ההורה
    private void LateUpdate()
    {
        if (_sr == null || _parentSr == null) return;

        if (matchSortingLayer)
            _sr.sortingLayerID = _parentSr.sortingLayerID;

        _sr.sortingOrder = _parentSr.sortingOrder + offsetFromParent;
    }
}

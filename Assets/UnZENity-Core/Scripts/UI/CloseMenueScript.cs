using UnityEngine;

public class CloseMenueScript : MonoBehaviour
{
    private Vector3 _menuePosition;
    private Quaternion _menueRotation;
    private GameObject _menueParent;

    private void Start()
    {
        _menuePosition = transform.localPosition;
        _menueRotation = transform.localRotation;
        _menueParent = transform.parent.gameObject;
        gameObject.SetActive(false);
    }

    public void CloseFunction()
    {
        transform.parent = _menueParent.transform;
        transform.localRotation = _menueRotation;
        transform.localPosition = _menuePosition;
        gameObject.SetActive(false);
    }
}

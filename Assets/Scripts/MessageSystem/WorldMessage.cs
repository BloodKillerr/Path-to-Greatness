using TMPro;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class WorldMessage : MonoBehaviour
{
    public TMP_Text textField;
    public float lifetime = 2f;
    public float floatSpeed = 0.2f;

    private Transform followTarget;
    private Vector3 localOffset = new Vector3(0f, 1.8f, 0.5f);
    private float timer;

    public void Init(Vector3 worldPos, string text, float duration)
    {
        followTarget = null;
        transform.position = worldPos;
        ApplyContent(text, duration);
    }

    public void InitFollow(Transform target, Vector3 localOffset, string text, float duration)
    {
        followTarget = target;
        this.localOffset = localOffset;
        ApplyContent(text, duration);
        transform.position = target.TransformPoint(localOffset);
    }

    private void ApplyContent(string text, float duration)
    {
        if (textField != null)
        {
            textField.text = text ?? "";
        }

        lifetime = duration;
        timer = 0f;
    }

    private void Update()
    {
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

        if (followTarget != null)
        {
            transform.position = followTarget.TransformPoint(localOffset);
            transform.position += Vector3.up * (floatSpeed * timer);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}

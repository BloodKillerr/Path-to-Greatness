using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class AutoScrollDynamic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scrollSpeed = 10f;
    private bool mouseOver = false;

    private List<Selectable> m_Selectables = new List<Selectable>();
    private ScrollRect m_ScrollRect;

    private Vector2 m_NextScrollPosition = Vector2.up;

    void Awake()
    {
        m_ScrollRect = GetComponent<ScrollRect>();
    }

    void Start()
    {
        RefreshSelectables();
        ScrollToSelected(true);
    }

    void Update()
    {
        if (!mouseOver)
        {
            InputScroll();
            m_ScrollRect.normalizedPosition = Vector2.Lerp(
                m_ScrollRect.normalizedPosition,
                m_NextScrollPosition,
                scrollSpeed * Time.unscaledDeltaTime
            );
        }
        else
        {
            m_NextScrollPosition = m_ScrollRect.normalizedPosition;
        }
    }

    void InputScroll()
    {
        RefreshSelectables();
        ScrollToSelected(false);
    }

    void RefreshSelectables()
    {
        m_Selectables.Clear();
        m_ScrollRect.content.GetComponentsInChildren(true, m_Selectables);
    }

    void ScrollToSelected(bool quickScroll)
    {
        GameObject selectedObject = null;
        if (EventSystem.current != null)
            selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null) return;

        Selectable selectedElement = selectedObject.GetComponent<Selectable>();
        if (selectedElement == null) return;

        // make sure we have the required rects
        RectTransform content = m_ScrollRect.content;
        RectTransform viewport = m_ScrollRect.viewport ?? m_ScrollRect.GetComponent<RectTransform>();
        RectTransform selRT = selectedElement.GetComponent<RectTransform>();
        if (content == null || viewport == null || selRT == null) return;

        // get world corners
        Vector3[] contentCorners = new Vector3[4];
        Vector3[] selCorners = new Vector3[4];
        Vector3[] viewportCorners = new Vector3[4];
        content.GetWorldCorners(contentCorners);       // 0: bl, 1: tl, 2: tr, 3: br
        selRT.GetWorldCorners(selCorners);
        viewport.GetWorldCorners(viewportCorners);

        float contentWorldHeight = contentCorners[1].y - contentCorners[0].y;
        float viewportWorldHeight = viewportCorners[1].y - viewportCorners[0].y;

        // If content fits inside viewport, just show top (1)
        if (contentWorldHeight <= viewportWorldHeight)
        {
            if (quickScroll)
                m_ScrollRect.verticalNormalizedPosition = 1f;
            m_NextScrollPosition = new Vector2(m_ScrollRect.normalizedPosition.x, 1f);
            return;
        }

        // distance from content top to selected top (world units, positive downward)
        float contentTopWorld = contentCorners[1].y;
        float selTopWorld = selCorners[1].y;
        float distanceFromContentTop = contentTopWorld - selTopWorld;

        // total scrollable distance (world units)
        float totalScrollable = contentWorldHeight - viewportWorldHeight;
        // compute normalized: 1 => top, 0 => bottom
        float normalized = 1f - Mathf.Clamp01(distanceFromContentTop / totalScrollable);

        if (quickScroll)
            m_ScrollRect.verticalNormalizedPosition = normalized;

        // keep X as it was (usually 0)
        m_NextScrollPosition = new Vector2(m_ScrollRect.normalizedPosition.x, normalized);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOver = false;
        ScrollToSelected(false);
    }
}

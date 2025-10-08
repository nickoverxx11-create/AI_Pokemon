using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform; // Current object's RectTransform
    private Vector2 originalPosition; // Position recorded at the start of dragging
    private Image targetImage; // Target image
    private GameObject targetDraggableItem; // Target draggable item
    private Transform originalParent; // Original parent

    void Awake()
    {
        // Get the current object's RectTransform component
        rectTransform = GetComponent<RectTransform>();
    }

    // Called when drag begins
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (tag != "LeftState")
        {
            Debug.Log("Cannot be dragged");
            // Cancel drag state
            eventData.pointerDrag = null;
            return; // Exit and don't allow dragging
        }

        // Record original parent and position
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
        targetImage = null;
        targetDraggableItem = null;

        // Set the current object's parent to the top layer to avoid other UI nodes
        //transform.SetParent(GameObject.Find("Canvas").transform);
        transform.SetParent(GameObject.Find("CardCanvas").transform);
        transform.SetAsLastSibling(); // Set to front
    }

    // Called during dragging
    public void OnDrag(PointerEventData eventData)
    {
        // Convert screen position to local coordinates in current canvas
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (rectTransform.parent as RectTransform),
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);
        // Update current object position
        rectTransform.anchoredPosition = localPoint;
    }

    // Called when drag ends
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Finding target image...");
        // Check if dragged to a target image
        CheckForTargetImage();

        //Debug.Log(GetComponent<Image>().sprite.name + targetDraggableItem.name);

        // Restore to original parent
        transform.SetParent(originalParent, false);

        if (targetDraggableItem!=null && targetDraggableItem.tag == "ShuiJingQiu")
        {

            Debug.Log($"Image source: {tag}, Target: {targetImage.tag}");

            //Add the card to the controller
            JiaQuanController.Instance.AddCard(rectTransform);

            // Restore original position
            rectTransform.anchoredPosition = originalPosition;

            // Clear target image and target draggable item
            targetImage = null;
            targetDraggableItem = null;

            rectTransform.GetComponent<DraggableItem>().enabled = false;
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
        }

    }

    // Check if dragged to a target image
    private void CheckForTargetImage()
    {
        // Find all draggable objects with ShuiJingQiu tag
        GameObject shuiJingQiuObjects = GameObject.FindGameObjectWithTag("ShuiJingQiu");
        if (shuiJingQiuObjects == null)
        {
            Debug.Log(1);
            return;
        }

        Debug.Log(2);
        RectTransform targetRect = shuiJingQiuObjects.GetComponent<RectTransform>();
        // Check if position is inside the target RectTransform
        if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, Input.mousePosition, Camera.main))
        {
            targetImage = shuiJingQiuObjects.GetComponent<Image>();
            targetDraggableItem = shuiJingQiuObjects; // Record target draggable item
            Debug.Log($"Identified target image: {targetImage.sprite.name}");
        }
    }
}
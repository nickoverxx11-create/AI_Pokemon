using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItemMode3 : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform; // Current object's RectTransform
    private Vector2 originalPosition; // Position recorded at the start of dragging
    private Image targetImage; // Target image
    private GameObject targetDraggableItem; // Target draggable item
    private Transform originalParent; // Original parent

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Called when drag begins
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (tag != "Mode1Drag")
        {
            Debug.Log("Cannot be dragged");
            eventData.pointerDrag = null;
            return; // Exit and don't allow dragging
        }

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
        targetImage = null;
        targetDraggableItem = null;

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

        if (targetDraggableItem != null && targetDraggableItem.tag == "Mode1Place")
        {

            Debug.Log($"Image source: {tag}, Target: {targetImage.tag}");

            //Add the card to the controller
            //if (name == "¼ÓºÅ")
            {
                Mode3Control.Instance.AddCard(targetDraggableItem.transform, gameObject);
            }
            


            // Restore original position
            rectTransform.anchoredPosition = originalPosition;

            // Clear target image and target draggable item
            targetImage = null;
            targetDraggableItem = null;

            //rectTransform.GetComponent<DraggableItem>().enabled = false;
        }
        else if (targetDraggableItem != null && targetDraggableItem.CompareTag("Del") && name != "jian" && transform.parent.name != "dragRoot")
        {
            //Debug.Log(targetDraggableItem.name);
            //Debug.Log(gameObject.name);
            targetImage = null;
            targetDraggableItem = null;
            Destroy(gameObject);
            Mode3Control.Instance.CheckSpr();
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
        GameObject[] geziObjects = GameObject.FindGameObjectsWithTag("Mode1Place");
        GameObject delObjects = GameObject.FindGameObjectWithTag("Del");
        if (geziObjects == null && delObjects == null)
        {
            Debug.Log(1);
            return;
        }
        foreach (var item in geziObjects)
        {
            RectTransform targetRect = item.GetComponent<RectTransform>();
            // Check if position is inside the target RectTransform
            if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, Input.mousePosition, Camera.main))
            {
                targetImage = item.GetComponent<Image>();
                targetDraggableItem = item; // Record target draggable item
                Debug.Log($"Identified target image: {targetImage.name}");
            }
            if (RectTransformUtility.RectangleContainsScreenPoint(delObjects.GetComponent<RectTransform>(), Input.mousePosition, Camera.main))
            {
                targetImage = delObjects.GetComponent<Image>();
                targetDraggableItem = delObjects;
                Debug.Log($"Identified target image: {targetImage.name}");
            }
        }

    }
}
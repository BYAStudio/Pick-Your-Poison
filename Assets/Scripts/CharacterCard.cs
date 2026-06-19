using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Karakter seçme ekranındaki her bir karakter kartını kontrol eder.
/// Fare ile üzerine gelindiğinde büyüme ve kenar çizgisi parlaması efektlerini yönetir.
/// </summary>
public class CharacterCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Karakter Ayarları")]
    [SerializeField] private CharacterType characterType;
    [SerializeField] private Image borderImage;

    [Header("Görsel Efekt Ayarları")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color hoverColor = new Color(0f, 0.9f, 1f, 1f); // Parlayan turkuaz/mavi renk
    [SerializeField] private float hoverScale = 1.05f;

    private Vector3 originalScale;
    private CharacterSelectionController selectionController;

    void Awake()
    {
        originalScale = transform.localScale;
        selectionController = FindAnyObjectByType<CharacterSelectionController>();

        if (borderImage != null)
        {
            borderImage.color = normalColor;
        }
    }

    /// <summary>
    /// Fare ile karakter üzerine gelindiğinde tetiklenir.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * hoverScale;
        
        if (borderImage != null)
        {
            borderImage.color = hoverColor;
        }

        // Hover ses efekti
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.CardDraw);
    }

    /// <summary>
    /// Fare karakter üzerinden ayrıldığında tetiklenir.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
        
        if (borderImage != null)
        {
            borderImage.color = normalColor;
        }
    }

    /// <summary>
    /// Karakter tıklandığında tetiklenir.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Tıklama ses efekti
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);

        if (selectionController != null)
        {
            selectionController.OnCharacterSelected(characterType);
        }
        else
        {
            Debug.LogError("[CharacterCard] CharacterSelectionController bulunamadı!");
        }
    }
}

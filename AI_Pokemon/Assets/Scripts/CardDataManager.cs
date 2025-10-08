using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CardCategory
{
    Feature,
    Pokemon,
    Dataset
}

[CreateAssetMenu(menuName = "LabZone/Card Data Manager")]
public class CardDataManager : ScriptableObject
{
    [Header("All Labzone Cards")]
    public List<LabzoneCardInfo> allCards;

    private Dictionary<string, LabzoneCardInfo> _infoMap;
    private Dictionary<string, string> _nameMap;
    private Dictionary<string, Sprite> _spriteMap;
    private static CardDataManager _instance; 
    public static CardDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<CardDataManager>("AllCards");
                _instance.Initialize();
            }
            
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod]
    private static void AutoLoad()
    {
        _instance = Resources.Load<CardDataManager>("FeatureCards");
        if (_instance != null)
        {
            _instance.Initialize();
        }
    }

    private void Initialize()
    {
        if (allCards == null)
        {
            Debug.LogError("CardDataManager Initialize failed: allCards is null");
            return;
        }

        _infoMap   = allCards.ToDictionary(c => c.index);
        _nameMap   = allCards.ToDictionary(c => c.index, c => c.name);
        _spriteMap = allCards.ToDictionary(c => c.index, c => c.sprite);
    }

    public string GetName(string index)
    {
        Initialize();
        return _nameMap.TryGetValue(index, out var name) ? name : null;
    }

    public Sprite GetSprite(string index)
    {
        Initialize();
        return _spriteMap.TryGetValue(index, out var sprite) ? sprite : null;
    }
    
    public bool IsFeatureCard(string index)
    {
        return _infoMap.TryGetValue(index, out var info)
               && info.category == CardCategory.Feature;
    }
    
    public bool IsDatasetCard(string index)
    {
        return _infoMap.TryGetValue(index, out var info)
               && info.category == CardCategory.Dataset;
    }
}
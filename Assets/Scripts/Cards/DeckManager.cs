using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance => _instance;
    private static DeckManager _instance;

    public int CardCountInDeck => _cardsInDeck.Count;

    [SerializeField] CardResources _cardResources;
    [SerializeField] TextMeshPro _remainingCountText;

    private List<Card> _cardsInDeck = new();
    private List<Card> _cardsUsed = new();

    public void DrawCard(Card selectedCard)
    {
        GameObject cardPrefab;
        Sprite cardImage = null;
        if (selectedCard.Type == CardType.Topic)
        {
            cardPrefab = _cardResources.CardObjectTextOnly;
        }
        else
        {
            cardPrefab = _cardResources.CardObjectWithImage;
            cardImage = Resources.Load<Sprite>($"CardImages/{selectedCard.Name}");
        }

        var randomPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
        var cardObject = Instantiate(cardPrefab, randomPosition, Quaternion.identity).GetComponent<CardObject>();
        cardObject.Initialize(selectedCard, _cardResources.cardTypeColors[(int)selectedCard.Type], cardImage);

        HandManager.Instance.AddCardToDeck(cardObject);
        UpdateCardCountText();
    }

    public void DrawCard()
    {
        if (_cardsInDeck.Count == 0) return;

        var selectedIndex = Random.Range(0, _cardsInDeck.Count);
        var selectedCard = _cardsInDeck[selectedIndex];
        _cardsInDeck.RemoveAt(selectedIndex);

        DrawCard(selectedCard);
    }

    public void ReleaseCardToPool(Card card)
    {
        _cardsUsed.Add(card);
    }

    public Card? PutBackIntoDeck(string cardName)
    {
        var card = _cardsUsed.Find(x => x.Name == cardName);
        if (card.Equals(default(Card))) return null;

        _cardsUsed.Remove(card);
        _cardsInDeck.Add(card);
        return card;
    }

    public (int playingCardCount, bool hasDuplicateJoker, bool hasMegaCharmJoker) GetDeckCardInfo()
    {
        return GetCardListInfo(_cardsInDeck);
    }

    public static (int playingCardCount, bool hasDuplicateJoker, bool hasMegaCharmJoker)  GetCardListInfo(IEnumerable<Card> cardList)
    {
        int playingCards = 0;
        bool duplicate = false;
        bool megaCharm = false;

        foreach (var card in cardList)
        {
            if (card.Type == CardType.Joker)
            {
                if (card.Name == "Duplicate")
                {
                    duplicate = true;
                }
                else if (card.Name == "Mega Charm")
                {
                    megaCharm = true;
                }
            }
            else
            {
                playingCards++;
            }
        }

        return (playingCards, duplicate, megaCharm);
    }

    private void Awake()
    {
        _instance = this;
        GenerateDeck();
        UpdateCardCountText();
    }

    void Update()
    {
        
    }

    private void UpdateCardCountText()
    {
        _remainingCountText.SetText(_cardsInDeck.Count.ToString());

    }

    private void GenerateDeck()
    {
        foreach (var topicCard in CardNames.TopicCards.Split(','))
        {
            _cardsInDeck.Add(new Card(CardType.Topic, topicCard));
        }

        foreach (var structCard in CardNames.StructCards.Split(','))
        {
            for (var i = 0; i < 3; i++)
                _cardsInDeck.Add(new Card(CardType.Structure, structCard));
        }

        foreach (var toneCard in CardNames.ToneCards.Split(','))
        {
            for (var i = 0; i < 3; i++)
                _cardsInDeck.Add(new Card(CardType.Tone, toneCard));
        }

        foreach (var jokerCard in CardNames.JokerCards.Split(','))
        {
            for (var i = 0; i < 5; i++)
                _cardsInDeck.Add(new Card(CardType.Joker, jokerCard));
        }
    }
}

public enum CardType
{
    Topic,
    Structure,
    Tone,
    Joker
}

public readonly struct Card
{
    public readonly CardType Type;
    public readonly string Name;

    public Card(CardType type, string name)
    {
        Type = type;
        Name = name;
    }
}

public static class CardNames
{
    public static string TopicCards = "Weather,Trees,Flowers,Fruits,Clouds,Hike,Sky,Birds,Moon,Ocean,Fight,Trauma,Loneliness,Violence,Threat,Abuse,Weapons,Fear,Tears,Regrets,Family,Friends,Dating,Trust,Love,Partner,Breakups,Gossip,Marriage,Conflict,Games,Movies,Sports,Travel,Music,Books,Relax,Sleep,Food,Hobbies,Work,Chores,Commute,Shopping,Routines,Stress,News,Money,Plans,Purpose,Internet,Phones,Robots,Cyber,Gaming,Future,Power,Laws,Global Warming,Disasters";
    public static string StructCards = "Greeting,Casual Opener,Direct Opener,Light Compliment,Bold Compliment,Small Talk,Personal Question,Tease,Emotional Disclosure,Validation,Follow-up,Flirt,Respect,Apology,Ask-Out,Harassment,Contradiction,Mockery,Fun-fact,Mourning";
    public static string ToneCards = "Warm,Playful,Enthusiastic,Sarcastic,Defensive,Aggressive,Serious,Curious,Calm,Reserved,Flirty,Polite,Annoyed,Depressed,Sincere,Confident,Shy,Detached,Supportive,Uncertain";
    public static string JokerCards = "Prevent Rejection,Prevent Rejection,Duplicate,Renew All,Mega Charm";
}

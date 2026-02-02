using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance => _instance;
    private static HandManager _instance;

    [SerializeField] AudioClip[] cardSlideSounds;
    [SerializeField] AudioClip[] cardSelectSounds;
    [SerializeField] AudioClip[] jokerSounds;
    [SerializeField] LlmCharacterChatManager characterChatManager;

    private const int MAX_CARDS = 5;
    private const float PLACEMENT_DURATION = 0.5f;
    private const float DELTA_ANGLE = 12f;
    private const float RADIUS = 10f;
    private List<CardObject> _cardList = new();
    private List<Coroutine> _cardPlacementRoutines = new();


    public void AddCardToDeck(CardObject cardObject)
    {
        _cardList.Add(cardObject);
        AdjustDeckPlacements();
    }

    public void RemoveCardFromDeck(CardObject cardObject)
    {
        DeckManager.Instance.ReleaseCardToPool(cardObject.ThisCard);

        _cardList.Remove(cardObject);
        AdjustDeckPlacements();
    }

    public void DiscardCards()
    {
        var deckManager = DeckManager.Instance;
        List<CardObject> cardsToRemove = new();

        foreach (var card in _cardList)
        {
            if (card.IsSelected)
            {
                cardsToRemove.Add(card);

                card.MoveCardsTowards(card.transform.position + Vector3.up, 1);
                card.Dissolve();
            }
        }

        foreach (var c in cardsToRemove)
        {
            RemoveCardFromDeck(c);
        }

        RefreshHand();

        PlayRandomSoundEffectMultiple(cardSlideSounds, cardsToRemove.Count);

        if (CheckEndCondition())
        {
            GameplayStateManager.Instance.FinishGame(true);
        }
    }

    public void PlayCards()
    {
        if (!characterChatManager.AreModelsReady())
        {
            SlidingText.Instance.ShowSlidingText("Please Wait Until LLM Model is Loaded");
            return;
        }

        if (_cardList.Count == 0) return;

        var deckManager = DeckManager.Instance;
        List<CardObject> cardsToPlay = new();

        int jokerCount = 0;
        int playingCardCount = 0;

        foreach (var card in _cardList)
        {
            if (card.IsSelected)
            {
                cardsToPlay.Add(card);

                if (card.ThisCard.Type == CardType.Joker)
                {
                    jokerCount++;
                }
                else
                {
                    playingCardCount++;
                }
            }
        }

        // dont allow single playing card play
        if (playingCardCount < 2 && (jokerCount == 0 || playingCardCount != 0))
        {
            SlidingText.Instance.ShowSlidingText("You can't play just one playing card!");
            return;
        }

        List<string> structCards = new();
        List<string> topicCards = new();
        List<string> toneCards = new();
        List<string> jokerCards = new();

        foreach (var c in cardsToPlay)
        {
            RemoveCardFromDeck(c);

            c.MoveCardsTowards(Vector3.zero, 0.5f, updateOrientation: true);
            c.Dissolve();

            switch (c.ThisCard.Type)
            {
                case CardType.Structure:
                    structCards.Add(c.ThisCard.Name);
                    break;
                case CardType.Topic:
                    topicCards.Add(c.ThisCard.Name);
                    break;
                case CardType.Tone:
                    toneCards.Add(c.ThisCard.Name);
                    break;
                case CardType.Joker:
                    jokerCards.Add(c.ThisCard.Name);
                    break;
            }
        }

        PlayRandomSoundEffectMultiple(jokerSounds, jokerCards.Count);

        bool containsRenewAll = jokerCards.Contains("Renew All");
        bool containsDuplicate = jokerCards.Contains("Duplicate");

        if (containsRenewAll)
        {
            foreach (var c in _cardList)
            {
                c.Dissolve();
            }

            _cardList.Clear();
        }

        if (containsDuplicate)
        {
            foreach (var c in cardsToPlay)
            {
                var card = deckManager.PutBackIntoDeck(c.ThisCard.Name);
                if (card.HasValue && card.Value.Name != "Duplicate")
                {
                    deckManager.DrawCard(card.Value);
                }
            }
        }

        bool preventRejection = jokerCards.Contains("Prevent Rejection");
        int megaCharm = jokerCards.Count(x => x == "Mega Charm");

        if (playingCardCount > 1)
        {
            var forceLose = Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Escape);
            GameplayStateManager.Instance.PushTurnEndValues(structCards.ToArray(), topicCards.ToArray(), toneCards.ToArray(), preventRejection, megaCharm, forceLose: forceLose);
        }
        else if (megaCharm > 0)
        {
            // Only end the turn if MegaCharm is used out of all jokers
            UseSingleMegaCharm(preventRejection, megaCharm);
        }

        RefreshHand();

        PlayRandomSoundEffectMultiple(cardSlideSounds, cardsToPlay.Count);

        CheckEndCondition();
    }

    public void PlayCardSelectSound()
    {
        var sfxManager = SoundEffectManager.Instance;
        sfxManager.PlaySoundEffect(cardSelectSounds[Random.Range(0, cardSelectSounds.Length)], pitch: Random.Range(0.95f, 1.05f));
    }

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        var deckManager = DeckManager.Instance;
        while (_cardList.Count < MAX_CARDS)
        {
            deckManager.DrawCard();
        }
    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                SelectAllCards();
                DiscardCards();
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                SelectAllCards();
                PlayCards();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                UseSingleMegaCharm(true, 10);
            }
        }
    }

    private void SelectAllCards()
    {
        foreach (var c in _cardList)
        {
            c.OnPointerDown();
        }
    }

    private void RefreshHand()
    {
        var deckManager = DeckManager.Instance;

        while (_cardList.Count < MAX_CARDS && deckManager.CardCountInDeck > 0)
        {
            deckManager.DrawCard();
        }
    }

    private void UseSingleMegaCharm(bool preventRejection, int megaCharmCount)
    {
        var tempStructCards = new string[] { "Bold Compliment" };
        var tempTopicCards = new string[] { "Love" };
        var tempToneCards = new string[] { "Flirty" };

        GameplayStateManager.Instance.PushTurnEndValues(tempStructCards, tempTopicCards, tempToneCards, preventRejection, megaCharmCount);
    }

    private bool CheckEndCondition()
    {
        if (DeckManager.Instance.CardCountInDeck > 0) return false;

        int playingCards = 0;
        bool hasDuplicate = false;
        bool hasMegaCharm = false;

        foreach (var c in _cardList)
        {
            var card = c.ThisCard;

            if (card.Type == CardType.Joker)
            {
                if (card.Name == "Duplicate")
                {
                    hasDuplicate = true;
                }
                else if (card.Name == "Mega Charm")
                {
                    hasMegaCharm = true;
                }
            }
            else
            {
                playingCards++;
            }
        }

        if (playingCards < 2 && !hasMegaCharm && (playingCards == 0 || !hasDuplicate))
        {
            GameplayStateManager.Instance.NoCardsLeft = true;
            return true;
        }

        return false;
    }

    private void PlayRandomSoundEffectMultiple(AudioClip[] clips, int playedCardCount)
    {
        var sfxManager = SoundEffectManager.Instance;
        float soundVolume = 0.5f / playedCardCount + 0.5f;

        for (int i = 0; i < playedCardCount; i++)
        {
            sfxManager.PlaySoundEffect(clips[Random.Range(0, clips.Length)], soundVolume, Random.Range(0.95f, 1.05f));
        }
    }

    private void AdjustDeckPlacements()
    {
        foreach (Coroutine placementRoutine in _cardPlacementRoutines)
        {
            if (placementRoutine == null) continue;
            StopCoroutine(placementRoutine);
        }

        for (var i = 0; i < _cardList.Count; i++)
        {
            var routine = StartCoroutine(CardPlacementRoutine(_cardList[i].transform, i));
            _cardPlacementRoutines.Add(routine);
        }
    }

    private IEnumerator CardPlacementRoutine(Transform cardTransform, int cardOrder)
    {
        var placementInfo = GetPlacementInfo(_cardList.Count, cardOrder);
        var desiredCardPosition = placementInfo.position;
        var desiredCardOrientation = placementInfo.orientation;

        var startTime = Time.time;
        var elapsedTime = 0f;
        while (elapsedTime < PLACEMENT_DURATION)
        { 
            yield return null;
            elapsedTime = Time.time - startTime;
         
            cardTransform.position = Vector3.Lerp(cardTransform.position, desiredCardPosition, elapsedTime/ PLACEMENT_DURATION);
            cardTransform.up = Vector3.Lerp(cardTransform.up, desiredCardOrientation, elapsedTime/ PLACEMENT_DURATION);
        }
    }

    private (Vector3 position, Vector3 orientation) GetPlacementInfo(int count, int order)
    {
        var angle = DELTA_ANGLE * ((count - 1) * 0.5f - order);
        var cardUpDirection = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;

        var desiredCardPosition = transform.position + cardUpDirection * RADIUS + Vector3.back * order * 0.11f;

        return (desiredCardPosition, cardUpDirection);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, RADIUS);

        for (var i = 0; i < 5; i++)
        {
            var pointPosition = GetPlacementInfo(5, i).position;
            Gizmos.DrawSphere(pointPosition, 0.1f);
        }
    }
}

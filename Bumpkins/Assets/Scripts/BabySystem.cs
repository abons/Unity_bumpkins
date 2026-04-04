using System.Collections;
using UnityEngine;

/// <summary>
/// Beheert de makeBaby-sequentie: baby-sprite, kind-spawn en groei-timer.
/// Wordt automatisch aangemaakt als singleton bij eerste gebruik.
/// </summary>
public class BabySystem : MonoBehaviour
{
    public static BabySystem Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("BabySystem");
                _instance = go.AddComponent<BabySystem>();
            }
            return _instance;
        }
    }
    private static BabySystem _instance;

    [Header("Timing")]
    public float babyDisplaySeconds  = 4f;
    public float childGrowUpSeconds  = 40f;

    /// <summary>Geroepen door de female zodra ze aankomt bij het huis.</summary>
    public void OnFemaleArrived(BumpkinController female, BuildingTag house)
    {
        StartCoroutine(BabySequence(female, house));
    }

    private IEnumerator BabySequence(BumpkinController female, BuildingTag house)
    {
        // Verberg female sprite
        var femaleSr = female.GetComponentInChildren<SpriteRenderer>();
        if (femaleSr) femaleSr.enabled = false;

        // Spawn baby.png als child van het huis
        var babyGo = new GameObject("Baby");
        babyGo.transform.SetParent(house.transform);
        babyGo.transform.localPosition = new Vector3(-0.5f, -0.1f, 0f);
        babyGo.transform.localScale    = Vector3.one;
        var sr = babyGo.AddComponent<SpriteRenderer>();
        sr.sprite       = Resources.Load<Sprite>("Sprites/Units/baby");
        sr.sortingOrder = 20;

        yield return new WaitForSeconds(babyDisplaySeconds);

        Destroy(babyGo);

        // Female vrijlaten
        if (femaleSr) femaleSr.enabled = true;
        female.ReleaseFromBaby();

        // Reservering vrijgeven
        house.ReleaseReservation();

        // Kind spawnen
        bool isBoy = Random.value > 0.5f;
        SpawnChild((Vector2)house.transform.position, isBoy);
    }

    private void SpawnChild(Vector2 pos, bool isBoy)
    {
        var go = new GameObject(isBoy ? "KidBoy" : "KidGirl");
        go.transform.position = (Vector3)pos + new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f);
        go.transform.localScale = Vector3.one * 0.55f;   // kleiner dan volwassene

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.1f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        sr.sprite = Resources.Load<Sprite>($"Sprites/Units/{(isBoy ? "kidm" : "kidf")}");

        var bc = go.AddComponent<BumpkinController>();
        bc.bumpkinType = isBoy ? BumpkinController.BumpkinType.Male : BumpkinController.BumpkinType.Female;
        bc.isChild     = true;

        go.AddComponent<BumpkinClick>();
        go.AddComponent<BumpkinAnimator>();

        bc.StartGrowUp(childGrowUpSeconds);

        Debug.Log($"[BabySystem] Kind geboren: {(isBoy ? "jongen 👦" : "meisje 👧")}");
    }
}

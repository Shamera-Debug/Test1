using UnityEngine;

[System.Serializable]
public class ItemForSale
{
    public ItemData item; // ItemData를 스크립터블 오브젝트로 참조
    public int price;
    public string sellerId;
}

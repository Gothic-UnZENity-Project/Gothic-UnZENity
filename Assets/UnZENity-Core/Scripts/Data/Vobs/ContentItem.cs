namespace GUZ.Core.Data.Vobs
{
    public struct ContentItem
    {
        public string Name;
        public int Amount;

        
        public ContentItem(string name, int amount)
        {
            Name = name;
            Amount = amount;
        }

        public ContentItem(ContentItem item, int amountChange)
        {
            Name = item.Name;
            Amount = item.Amount + amountChange;
        }
    }
}
